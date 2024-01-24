using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Entities;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Utils;

namespace RetakesAllocator.Modules.Handlers;

internal static class Listeners
{
    public static void RegisterListeners()
    {
        Plugin.RegisterListener<CounterStrikeSharp.API.Core.Listeners.OnMapStart>(OnMapStart);
        Plugin.RegisterListener<CounterStrikeSharp.API.Core.Listeners.OnClientAuthorized>(OnClientAuthorized);
        Plugin.RegisterListener<CounterStrikeSharp.API.Core.Listeners.OnClientConnected>(OnClientConnected);
        Plugin.RegisterListener<CounterStrikeSharp.API.Core.Listeners.OnClientDisconnect>(OnClientDisconnect);
    }  

    private static void OnMapStart(string map_name)
    {
        Players.Clear();
        Utilities.GetPlayers().ForEach(player =>
        {
            AddPlayerToList(player, player.AuthorizedSteamID!);
        });
    }

    private static void OnClientAuthorized(int playerSlot, [CastFrom(typeof(ulong))] SteamID steamId)
    {
        if (steamId.SteamId2 == string.Empty)
        {
            ServerCommand($"kickid {playerSlot} \"SteamID2 not found.\"");
            return;
        }

        var player = Utilities.GetPlayerFromSlot(playerSlot);
        AddPlayerToList(player, steamId);
    }

    private static void OnClientConnected(int playerSlot)
    {
        var player = Utilities.GetPlayerFromSlot(playerSlot);
        AddPlayerToList(player, player.AuthorizedSteamID!);
    }

    private static void OnClientDisconnect(int playerSlot)
    {
        var player = Utilities.GetPlayerFromSlot(playerSlot);
        RemovePlayerFromList(player);
    }
}
