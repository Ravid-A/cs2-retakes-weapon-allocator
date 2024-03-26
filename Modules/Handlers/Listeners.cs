using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Entities;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Utils;
using static RetakesAllocator.Modules.Weapons.Menu;

namespace RetakesAllocator.Modules.Handlers;

internal static class Listeners
{
    public static void RegisterListeners()
    {
        Plugin.RegisterListener<CounterStrikeSharp.API.Core.Listeners.OnMapStart>(OnMapStart);
        Plugin.RegisterListener<CounterStrikeSharp.API.Core.Listeners.OnClientAuthorized>(OnClientAuthorized);
        Plugin.RegisterListener<CounterStrikeSharp.API.Core.Listeners.OnClientDisconnect>(OnClientDisconnect);

        Plugin.AddCommandListener("say", OnSay);
        Plugin.AddCommandListener("say_team", OnSay);
    }  

    private static void OnMapStart(string map_name)
    {
        RoundsCounter = 0;
        Players.Clear();
        Utilities.GetPlayers().ForEach(AddPlayerToList);
    }

    private static void OnClientAuthorized(int playerSlot, SteamID steamID)
    {
        CCSPlayerController player = Utilities.GetPlayerFromSlot(playerSlot);
        AddPlayerToList(player);
    }

    private static void OnClientDisconnect(int playerSlot)
    {
        var player = Utilities.GetPlayerFromSlot(playerSlot);
        RemovePlayerFromList(player);
    }

    private static HookResult OnSay(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if(command.ArgCount < 2)
                return HookResult.Continue;

        var message = command.GetArg(1);

        if(!Core.Config.triggerWords.Any(word => word.Equals(message)))
            return HookResult.Continue;

        var playerObj = FindPlayer(player); 

        if (playerObj == null!)
        {
            ReplyToCommand(command, $"{PREFIX} This command can only be executed by a valid player.");
            return HookResult.Continue;
        }

        OpenTPrimaryMenu(player);

        return HookResult.Continue;
    }
}
