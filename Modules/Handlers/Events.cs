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
        if (!IsLive())
        {
            return HookResult.Continue;
        }

        SetupPlayers(Players);

        return HookResult.Continue;
    }

    private static HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var playerController = @event.Userid;

        if (playerController == null! || !playerController.IsValid)
        {
            return HookResult.Continue;
        }

        if (!IsLive())
        {
            return HookResult.Continue;
        }

        var player = FindPlayer(playerController);

        if (player == null! || !player.IsValid())
        {
            return HookResult.Continue;
        }

        player.CreateSpawnDelay();

        return HookResult.Continue;
    }
}
