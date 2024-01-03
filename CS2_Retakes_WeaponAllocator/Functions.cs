using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using static WeaponsAllocator.Core;
using static WeaponsAllocator.Database;

namespace WeaponsAllocator;

class Functions
{
    public static string PREFIX { get; set; } = config.PREFIX.PREFIX;
    public static string PREFIX_CON { get; set; } = config.PREFIX.PREFIX_CON;

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
        foreach(Player player_obj in players)
        {
            if(player_obj.player == player)
            {
                return player_obj;
            }
        }

        return null!;
    }

    public static void ServerCommand(string command, params object[] args)
    {
        Server.ExecuteCommand(string.Format(command, args));
    }

    public static void AddPlayerToList(CCSPlayerController player, SteamID steamID)
    {
        if(player is null || !player.IsValid || player.IsBot || steamID is null)
                return;

        Player player_obj = new Player(player, steamID);

        players.Add(player_obj);

        int index = players.IndexOf(player_obj);

        db.Query(SQL_FetchUser_CB, $"SELECT * FROM `weapons` WHERE `auth` = '{player_obj.GetSteamID2()}'", index);
    }

    public static void RemovePlayerFromList(CCSPlayerController player)
    {
        if(player is null || !player.IsValid || player.IsBot)
                return;

        Player player_obj = FindPlayer(player);

        if(player_obj == null!)
        {
            return;
        }

        db.Query(SQL_CheckForErrors, $"UPDATE `weapons` SET `t_primary` = '{player_obj.weaponsAllocator.primaryWeapon_t}', `ct_primary` = '{player_obj.weaponsAllocator.primaryWeapon_ct}', `secondary` = '{player_obj.weaponsAllocator.secondaryWeapon}', `give_awp` = '{(int)player_obj.weaponsAllocator.giveAWP}' WHERE `auth` = '{player_obj.GetSteamID2()}'");

        players.Remove(player_obj);
    }
}