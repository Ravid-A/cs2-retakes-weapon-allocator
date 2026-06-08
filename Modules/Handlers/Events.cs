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
        if (GetGameRules().WarmupPeriod)
        {
            _ignoreRoundEnd = true;
            return HookResult.Continue;
        }

        SetupPlayers(Players);
        ResetNades();

        if(CurrentVote != null && CurrentVote.Vote.OnlyHeadshots)
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
        if(GetGameRules().WarmupPeriod)
        {
            return HookResult.Continue;
        }

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
