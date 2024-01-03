using CounterStrikeSharp.API.Core;

using static WeaponsAllocator.Core;
using static WeaponsAllocator.Functions;
using static WeaponsAllocator.Player;

namespace WeaponsAllocator;

class EventsHandlers
{
    public static void RegisterEvents()
    {
        _plugin.RegisterEventHandler<EventRoundPrestart>(OnRoundPreStart);
        _plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    }

    private static HookResult OnRoundPreStart(EventRoundPrestart @event, GameEventInfo info)
    {
        if(!isLive())
        {
            return HookResult.Continue;
        }

        SetupPlayers(players);

        return HookResult.Continue;
    }

    private static HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerController player_controller = @event.Userid;

        if(player_controller == null! || !player_controller.IsValid)
        {
            return HookResult.Continue;
        }

        if(!isLive())
        {
            return HookResult.Continue;
        }

        Player player = FindPlayer(player_controller);

        if(player == null! || !player.IsValid())
        {
            return HookResult.Continue;
        }

        player.CreateSpawnDelay();

        return HookResult.Continue;
    }
}