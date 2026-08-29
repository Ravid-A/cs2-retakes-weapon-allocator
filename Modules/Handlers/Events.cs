using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Utils;
using static RetakesAllocator.Modules.Models.Player;
using static RetakesAllocator.Modules.Weapons.Allocator;

namespace RetakesAllocator.Modules.Handlers;

internal static class Events
{
    static bool _ignoreRoundEnd = false;

    public static void RegisterEvents()
    {
        Plugin.RegisterEventHandler<EventRoundPrestart>(OnRoundPreStart);
        Plugin.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        Plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    }

    private static HookResult OnRoundPreStart(EventRoundPrestart @event, GameEventInfo info)
    {
        var warmup = GetGameRules() is { WarmupPeriod: true };

        // A warmup round still must not count towards the pistol-round tally.
        if (warmup)
        {
            _ignoreRoundEnd = true;
        }

        // These two run in warmup as well. The nade pools are shared by every player and refilled
        // per round, so skipping this while players are spawning drains them across warmup respawns
        // and never refills; SetupPlayers is what decides who carries the AWP.
        SetupPlayers(Players);
        ResetNades();

        // The vote belongs to the match, not to warmup.
        if(!warmup && CurrentVote != null && CurrentVote.Vote.OnlyHeadshots)
        {
            mp_damage_headshot_only.SetValue(true);
        }

        return HookResult.Continue;
    }

    private static HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        if (GetGameRules().WarmupPeriod || _ignoreRoundEnd)
        {
            _ignoreRoundEnd = false;
            return HookResult.Continue;
        }

        RoundsCounter++;
        mp_damage_headshot_only.SetValue(false);

        return HookResult.Continue;
    }

    private static HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        // No warmup guard here any more. Newer retakes builds let players spawn during warmup, and
        // bailing out left them with nothing at all; Timer_GiveWeapons decides what a warmup spawn
        // gets, which is the same loadout a normal round gives.
        var playerController = @event.Userid;

        if (playerController == null! || !playerController.IsValid)
        {
            Plugin.Logger.LogDebug("Skipping spawn allocation: player controller is null or invalid");
            return HookResult.Continue;
        }

        var player = FindPlayer(playerController);

        if (player == null! || !player.IsValid())
        {
            Plugin.Logger.LogDebug("Skipping spawn allocation: player is not tracked or invalid");
            return HookResult.Continue;
        }

        player.CreateSpawnDelay();

        return HookResult.Continue;
    }
}
