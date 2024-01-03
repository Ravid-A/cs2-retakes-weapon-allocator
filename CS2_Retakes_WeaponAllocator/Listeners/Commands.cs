using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

using static WeaponsAllocator.Core;
using static WeaponsAllocator.Functions;

using static Weapons.WeaponsMenu;

namespace WeaponsAllocator;

class CommandsHandlers
{
    public static void RegisterCommands()
    {
        _plugin.AddCommand("css_guns", "Opens the guns menu", GunsCommand);
    }

    public static void UnRegisterCommands()
    {
        _plugin.RemoveCommand("css_guns", GunsCommand);
    }

    private static void GunsCommand(CCSPlayerController? player, CommandInfo commandinfo)
    {
        if (player == null)
        {
            ReplyToCommand(commandinfo, $"{PREFIX} This command can only be executed by a player.");
            return;
        }

        if(!player.IsValid)
        {
            ReplyToCommand(commandinfo, $"{PREFIX} This command can only be executed by a valid player.");
            return;
        }
        
        Player player_obj = FindPlayer(player);

        if(player_obj == null!)
        {
            return;
        }

        if(player_obj.inGunMenu)
        {
            ReplyToCommand(commandinfo, $"{PREFIX} You are already in the gun menu!");
            return;
        }

        player_obj.inGunMenu = true;

        OpenTPrimaryMenu(player);
    }
}
