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

        Plugin.AddCommand("css_ct_guns", "Opens the CT guns menu", CTGunsCommand);
        Plugin.AddCommand("css_t_guns", "Opens the T guns menu", TGunsCommand);
        Plugin.AddCommand("css_pistols", "Opens the pistols menu", PistolsCommand);
        Plugin.AddCommand("css_awp", "Opens the awps menu", AwpCommand);

        Plugin.AddCommand("css_weapons_reload", "Reloads the weapons allocator's weapons configs", ReloadCommand);
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

    private static void CTGunsCommand(CCSPlayerController? player, CommandInfo commandInfo)
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

        OpenCTPrimaryMenu(player, false);
    }

    private static void TGunsCommand(CCSPlayerController? player, CommandInfo commandInfo)
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

        OpenTPrimaryMenu(player, false);
    }

    private static void PistolsCommand(CCSPlayerController? player, CommandInfo commandInfo)
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

        OpenSecondaryMenu(player, false);
    }

    private static void AwpCommand(CCSPlayerController? player, CommandInfo commandInfo)
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

        OpenGiveAWPMenu(player);
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

        LoadConfigs(false);
        PrintToChat(player, $"{PREFIX} Configs reloaded.");
    }
}
