using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

using static WeaponsAllocator.Core;

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
}