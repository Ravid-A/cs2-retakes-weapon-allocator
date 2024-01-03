using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Database;
using Player = RetakesAllocator.Modules.Models.Player;

namespace RetakesAllocator.Modules;

internal static class Utils
{
    public static string PREFIX { get; set; } = Core.Config.Prefix.Prefix;
    public static string PREFIX_CON { get; set; } = Core.Config.Prefix.PrefixCon;

    public static void PrintToChat(CCSPlayerController controller, string msg)
    {
        controller.PrintToChat(msg);
    }

    public static void PrintToServer(string msg, ConsoleColor color = ConsoleColor.Cyan)
    {
        Console.ForegroundColor = color;

        msg = $"{PREFIX_CON} {msg}";
        Console.WriteLine(msg);
        Console.ResetColor();
    }

    public static void ThrowError(string msg)
    {
        throw new Exception(msg);
    }

    public static void ReplyToCommand(CommandInfo commandInfo, string msg)
    {
        commandInfo.ReplyToCommand(msg);
    }

    public static Player FindPlayer(CCSPlayerController player)
    {
        foreach(var playerObj in Players)
        {
            if (playerObj.player == player)
            {
                return playerObj;
            }
        }

        return null!;
    }

    public static void ServerCommand(string command, params object[] args)
    {
        Server.ExecuteCommand(string.Format(command, args));
    }

    public static void AddPlayerToList(CCSPlayerController player, SteamID steamId)
    {
        if (player == null || !player.IsValid || player.IsBot || steamId is null)
        {
            return;
        }

        var playerObj = new Player(player, steamId);

        Players.Add(playerObj);

        var index = Players.IndexOf(playerObj);

        Query(SQL_FetchUser_CB, $"SELECT * FROM `weapons` WHERE `auth` = '{playerObj.GetSteamId2()}'", index);
    }

    public static void RemovePlayerFromList(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || player.IsBot)
        {
            return;
        }

        var playerObj = FindPlayer(player);

        if (playerObj == null!)
        {
            return;
        }

        Query(SQL_CheckForErrors, $"UPDATE `weapons` SET `t_primary` = '{playerObj.WeaponsAllocator.PrimaryWeaponT}', `ct_primary` = '{playerObj.WeaponsAllocator.PrimaryWeaponCt}', `secondary` = '{playerObj.WeaponsAllocator.SecondaryWeapon}', `give_awp` = '{(int)playerObj.WeaponsAllocator.GiveAwp}' WHERE `auth` = '{playerObj.GetSteamId2()}'");

        Players.Remove(playerObj);
    }
}
