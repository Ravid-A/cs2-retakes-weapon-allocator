using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Utils;
using static RetakesAllocator.Modules.Weapons.Menu;
using static RetakesAllocator.Modules.Handlers.Events;
using static RetakesAllocator.Modules.Votes.Votes;

namespace RetakesAllocator.Modules.Handlers;

internal static class Listeners
{
    public static void RegisterListeners()
    {
        Plugin.RegisterListener<CounterStrikeSharp.API.Core.Listeners.OnMapStart>(OnMapStart);
        Plugin.RegisterListener<CounterStrikeSharp.API.Core.Listeners.OnMapEnd>(OnMapEnd);
        Plugin.RegisterListener<CounterStrikeSharp.API.Core.Listeners.OnClientAuthorized>(OnClientAuthorized);
        Plugin.RegisterListener<CounterStrikeSharp.API.Core.Listeners.OnClientDisconnect>(OnClientDisconnect);

        Plugin.AddCommandListener("say", OnSay);
        Plugin.AddCommandListener("say_team", OnSay);
    }

    private static void OnMapStart(string mapName)
    {
        // The cached cs_gamerules pointer belongs to the previous map.
        InvalidateGameRules();

        RoundsCounter = 0;
        Players.Clear();
        ResetState();
        ClearHeadshotOnly();

        Utilities.GetPlayers().ForEach(AddPlayerToList);
        Votes_OnMapStart();
    }

    private static void OnMapEnd()
    {
        InvalidateGameRules();
        ResetState();
    }

    private static void OnClientAuthorized(int playerSlot, SteamID steamID)
    {
        AddPlayerToList(Utilities.GetPlayerFromSlot(playerSlot));
    }

    private static void OnClientDisconnect(int playerSlot)
    {
        // The controller may already be torn down here, so everything downstream has
        // to cope with a null. Clean up by slot first, which never needs one.
        CancelPendingAllocation(playerSlot);
        ClearPendingSelection(playerSlot);

        var player = Utilities.GetPlayerFromSlot(playerSlot);

        if (Players.TryGetValue(playerSlot, out var tracked))
        {
            RemoveTrackedPlayer(tracked);
        }

        Votes_OnPlayerDisconnect(player);
    }

    private static HookResult OnSay(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null || !player.IsValid)
        {
            return HookResult.Continue;
        }

        if (command.ArgCount < 2)
        {
            return HookResult.Continue;
        }

        // Runs for every chat message on the server: keep it to a single hash lookup
        // instead of a LINQ scan with a closure allocation per message.
        if (!TriggerWords.Contains(command.GetArg(1)))
        {
            return HookResult.Continue;
        }

        if (FindPlayer(player) is null)
        {
            ReplyToCommand(command, $"{Prefix} This command can only be executed by a valid player.");
            return HookResult.Continue;
        }

        OpenPistolRoundTMenu(player);

        return HookResult.Continue;
    }
}
