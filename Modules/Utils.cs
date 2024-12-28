using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
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

    public static void PrintToChatAll(string msg)
    {
        Server.PrintToChatAll(msg);
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

    public static Player FindPlayer(CCSPlayerController cCSPlayerController)
    {
        return Players.Find(player => player.playerIndex == cCSPlayerController.Index)!;
    }

    public static void ServerCommand(string command, params object[] args)
    {
        Server.ExecuteCommand(string.Format(command, args));
    }

    public static void AddPlayerToList(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || player.IsBot)
        {
            return;
        }

        if(FindPlayer(player) != null!)
        {
            return;
        }

        var playerObj = new Player(player);

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

        Query(SQL_CheckForErrors, $"UPDATE `weapons` SET `t_primary` = '{playerObj.WeaponsAllocator.PrimaryWeaponT}', `ct_primary` = '{playerObj.WeaponsAllocator.PrimaryWeaponCt}', `t_secondary` = '{playerObj.WeaponsAllocator.SecondaryWeaponT}', `ct_secondary` = '{playerObj.WeaponsAllocator.SecondaryWeaponCt}' ,`give_awp` = '{(int)playerObj.WeaponsAllocator.GiveAwp}' WHERE `auth` = '{playerObj.GetSteamId2()}'");

        Players.Remove(playerObj);
    }

    public static int GetRoundsAmount()
    {
        IEnumerable<CTeam> team = Utilities.FindAllEntitiesByDesignerName<CTeam>("cs_team");

        if (team.Count() == 0)
        {
            return 0;
        }

        int rounds = 0;

        foreach (var t in team)
        {
            rounds = t.Score;
        }

        return rounds;
    }

    public static CCSPlayerController[] ValidPlayers(bool considerBots = false)
    {
        //considerBots = true;
        return Utilities.GetPlayers()
        .Where(x => x.ReallyValid(considerBots))
        .Where(x => !x.IsHLTV)
        .Where(x => considerBots || !x.IsBot)
        .ToArray();
    }

    public static bool ReallyValid(this CCSPlayerController? player, bool considerBots = false)
    {
        return player is not null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected &&
            (considerBots || (!player.IsBot && !player.IsHLTV));
    }

    public static int ValidPlayerCount(bool considerBots = false)
    {
        return ValidPlayers(considerBots).Length;
    }

    public static T GetRandomFromList<T>(this List<T> list)
    {
        int index = new Random().Next(list.Count);
        return list[index];
    }   
}
