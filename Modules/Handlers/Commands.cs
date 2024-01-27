using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Utils;
using static RetakesAllocator.Modules.Weapons.Menu;



namespace RetakesAllocator.Modules.Handlers;

internal static class Commands
{
    public static void RegisterCommands()
    {
        Plugin.AddCommand("css_guns", "Opens the guns menu", GunsCommand);
        Plugin.AddCommand("css_weapons_reload", "Reloads the weapons allocator's configs", ReloadCommand);
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

        OpenTPrimaryMenu(player);
    }

    [RequiresPermissions(new string[] { "@css/root" })]
    private static void ReloadCommand(CCSPlayerController? player, CommandInfo commandInfo)
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


    }
}
