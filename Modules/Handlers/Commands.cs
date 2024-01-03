using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Utils;
using static RetakesAllocator.Modules.Weapons.Menu;

namespace RetakesAllocator.Modules.Handlers;

internal static class Commands
{
    public static void RegisterCommands()
    {
        Plugin.AddCommand("css_guns", "Opens the guns menu", GunsCommand);
    }

    public static void UnRegisterCommands()
    {
        Plugin.RemoveCommand("css_guns", GunsCommand);
    }

    private static void GunsCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null)
        {
            ReplyToCommand(commandInfo, $"{PREFIX} This command can only be executed by a player.");
            return;
        }

        if (!player.IsValid)
        {
            ReplyToCommand(commandInfo, $"{PREFIX} This command can only be executed by a valid player.");
            return;
        }
        
        var playerObj = FindPlayer(player);

        if (playerObj == null!)
        {
            ReplyToCommand(commandInfo, $"{PREFIX} This command can only be executed by a valid player.");
            return;
        }

        if (playerObj.InGunMenu)
        {
            ReplyToCommand(commandInfo, $"{PREFIX} You are already in the gun menu!");
            return;
        }

        playerObj.InGunMenu = true;

        OpenTPrimaryMenu(player);
    }
}
