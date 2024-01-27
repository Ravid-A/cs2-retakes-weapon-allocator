using CounterStrikeSharp.API.Core;
using RetakesAllocator.Modules.Models;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Utils;
using static RetakesAllocator.Modules.Models.Player;

namespace RetakesAllocator.Modules.Handlers;

internal static class Events
{
    public static void RegisterEvents()
    {
        Plugin.RegisterEventHandler<EventRoundPrestart>(OnRoundPreStart);
        Plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    }

    private static HookResult OnRoundPreStart(EventRoundPrestart @event, GameEventInfo info)
    {
        if (GetGameRules().WarmupPeriod)
        {
            return HookResult.Continue;
        }

        SetupPlayers(Players);

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
            PrintToServer("OnPlayerSpawn: playerController is null or invalid");
            return HookResult.Continue;
        }

        var player = FindPlayer(playerController);

        if (player == null! || !player.IsValid())
        {
            PrintToServer("OnPlayerSpawn: player is null or invalid");
            return HookResult.Continue;
        }

        PrintToServer("OnPlayerSpawn");

        player.CreateSpawnDelay();

        return HookResult.Continue;
    }
}
