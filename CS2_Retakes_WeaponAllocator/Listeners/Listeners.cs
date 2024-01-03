using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Entities;

using static WeaponsAllocator.Core;
using static WeaponsAllocator.Functions;
using static WeaponsAllocator.Database;

namespace WeaponsAllocator;

class ListenersHandlers
{
    public static void RegisterListeners()
    {
        _plugin.RegisterListener<Listeners.OnMapStart>(OnMapStart);
        _plugin.RegisterListener<Listeners.OnClientAuthorized>(OnClientAuthorized);
        _plugin.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
    }

    public static void OnMapStart(string mapName)
    {
        ServerCommand("mp_give_player_c4 0");
    }   

    private static void OnClientAuthorized(int playerSlot, [CastFrom(typeof(ulong))] SteamID steamId)
    {
        if(steamId.SteamId2 == string.Empty)
        {
            ServerCommand("kickid " + playerSlot + " \"SteamID2 not found.\"");
            return;
        }

        CCSPlayerController player = Utilities.GetPlayerFromSlot(playerSlot);
        AddPlayerToList(player, steamId);
    }

    private static void OnClientDisconnect(int playerSlot)
    {
        CCSPlayerController player = Utilities.GetPlayerFromSlot(playerSlot);
        RemovePlayerFromList(player);
    }
}