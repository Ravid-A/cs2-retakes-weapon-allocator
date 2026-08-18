using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Utils;
using static RetakesAllocator.Modules.Models.Player;
using static RetakesAllocator.Modules.Weapons.Allocator;
using static RetakesAllocator.Modules.Weapons.Menu;

namespace RetakesAllocator.Modules.Handlers;

internal static class Events
{
    private const float SpawnAllocationDelay = 0.1f;

    private static bool _ignoreRoundEnd = false;
    private static bool _headshotOnlyActive = false;

    // Everyone spawns in the same frame at round start, so instead of arming a timer
    // per player (20+ native timers per round, all leaking into BasePlugin.Timers for
    // the lifetime of the plugin) we queue the slots and flush them with a single one.
    private static readonly Dictionary<int, float> PendingAllocations = new();
    private static readonly List<int> ReadySlots = new();
    private static Timer? _allocationTimer;

    public static void RegisterEvents()
    {
        Plugin.RegisterEventHandler<EventRoundPrestart>(OnRoundPreStart);
        Plugin.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        Plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    }

    /// <summary>Clears queued spawn allocations and pending vote menus.</summary>
    public static void ResetState()
    {
        PendingAllocations.Clear();
        ReadySlots.Clear();
        _allocationTimer?.Kill();
        _allocationTimer = null;
        ClearPendingSelections();
    }

    public static void CancelPendingAllocation(int slot)
    {
        PendingAllocations.Remove(slot);
    }

    /// <summary>Clears mp_damage_headshot_only, but only when we actually set it.</summary>
    public static void ClearHeadshotOnly()
    {
        if (!_headshotOnlyActive)
        {
            return;
        }

        _headshotOnlyActive = false;
        mp_damage_headshot_only?.SetValue(false);
    }

    private static HookResult OnRoundPreStart(EventRoundPrestart @event, GameEventInfo info)
    {
        if (IsWarmup)
        {
            _ignoreRoundEnd = true;
            return HookResult.Continue;
        }

        // A vote menu left over from the previous round must not hand out weapons now.
        ClearPendingSelections();

        SetupPlayers(Players.Values);
        ResetNades();

        if (CurrentVote != null && CurrentVote.Vote.OnlyHeadshots)
        {
            _headshotOnlyActive = true;
            mp_damage_headshot_only?.SetValue(true);
        }

        return HookResult.Continue;
    }

    private static HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        if (IsWarmup || _ignoreRoundEnd)
        {
            _ignoreRoundEnd = false;
            return HookResult.Continue;
        }

        RoundsCounter++;
        ClearHeadshotOnly();

        return HookResult.Continue;
    }

    private static HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (IsWarmup)
        {
            return HookResult.Continue;
        }

        var playerController = @event.Userid;

        if (playerController is null || !playerController.IsValid)
        {
            Plugin.Logger.LogDebug("Skipping spawn allocation: player controller is null or invalid");
            return HookResult.Continue;
        }

        var player = FindPlayer(playerController);

        if (player is null || !player.IsValid())
        {
            Plugin.Logger.LogDebug("Skipping spawn allocation: player is not tracked or invalid");
            return HookResult.Continue;
        }

        QueueAllocation(player.Slot);

        return HookResult.Continue;
    }

    private static void QueueAllocation(int slot)
    {
        PendingAllocations[slot] = Server.CurrentTime + SpawnAllocationDelay;

        // A timer is already in flight; the flush re-arms itself for any straggler
        // that was queued after it was scheduled.
        _allocationTimer ??= Plugin.AddTimer(
            SpawnAllocationDelay,
            FlushAllocations,
            TimerFlags.STOP_ON_MAPCHANGE);
    }

    private static void FlushAllocations()
    {
        _allocationTimer = null;

        if (PendingAllocations.Count == 0)
        {
            return;
        }

        var now = Server.CurrentTime;
        var nextDue = float.MaxValue;

        ReadySlots.Clear();

        foreach (var (slot, due) in PendingAllocations)
        {
            if (due <= now)
            {
                ReadySlots.Add(slot);
            }
            else if (due < nextDue)
            {
                nextDue = due;
            }
        }

        foreach (var slot in ReadySlots)
        {
            PendingAllocations.Remove(slot);

            if (Players.TryGetValue(slot, out var player))
            {
                player.GiveWeapons();
            }
        }

        if (PendingAllocations.Count > 0)
        {
            _allocationTimer = Plugin.AddTimer(
                Math.Max(nextDue - now, 0.01f),
                FlushAllocations,
                TimerFlags.STOP_ON_MAPCHANGE);
        }
    }
}
