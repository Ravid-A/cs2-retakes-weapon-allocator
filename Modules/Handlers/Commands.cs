using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Utils;
using static RetakesAllocator.Modules.Weapons.Menu;
using static RetakesAllocator.Modules.Votes.Votes;
using RetakesAllocator.Modules.Votes;

namespace RetakesAllocator.Modules.Handlers;

internal static class Commands
{
    public static void RegisterCommands()
    {
        Plugin.AddCommand("css_guns", "Opens the guns menu", GunsCommand);

        Plugin.AddCommand("css_ct_guns", "Opens the CT guns menu", CTGunsCommand);
        Plugin.AddCommand("css_t_guns", "Opens the T guns menu", TGunsCommand);
        Plugin.AddCommand("css_t_pistols", "Opens the pistols menu", PistolsTCommand);
        Plugin.AddCommand("css_ct_pistols", "Opens the pistols menu", PistolsCTCommand);
        Plugin.AddCommand("css_awp", "Opens the awps menu", AwpCommand);

        Plugin.AddCommand("css_weapons_reload", "Reloads the weapons allocator's weapons configs", ReloadCommand);
        Plugin.AddCommand("css_skip_pistol", "Skips the pistol round", SkipPistolRoundCommand);
    }

    public static void UnRegisterCommands()
    {
        Plugin.RemoveCommand("css_guns", GunsCommand);

        Plugin.RemoveCommand("css_ct_guns", CTGunsCommand);
        Plugin.RemoveCommand("css_t_guns", TGunsCommand);
        Plugin.RemoveCommand("css_t_pistols", PistolsTCommand);
        Plugin.RemoveCommand("css_ct_pistols", PistolsCTCommand);
        Plugin.RemoveCommand("css_awp", AwpCommand);

        Plugin.RemoveCommand("css_weapons_reload", ReloadCommand);
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

    private static void PistolsTCommand(CCSPlayerController? player, CommandInfo commandInfo)
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

        OpenSecondaryTMenu(player, false);
    }

    private static void PistolsCTCommand(CCSPlayerController? player, CommandInfo commandInfo)
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

        OpenSecondaryCTMenu(player, false);
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

    public static void OnVoteCommand(CCSPlayerController? player, CommandInfo commandInfo)
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

        if(RoundsCounter < Core.Config.PistolRound.RoundAmount)
        {
            ReplyToCommand(commandInfo, $"{PREFIX} You can't vote during the pistol rounds.");
            return;
        }

        int userId = player.UserId!.Value;

        string command = commandInfo.GetArg(0);
        AsyncVoteManager voteManager = GetVote(command);

        if (voteManager == null!)
        {
            ReplyToCommand(commandInfo, $"{PREFIX} Invalid vote command.");
            return;
        }

        switch(voteManager.AddVote(userId))
        {
            case VoteResultEnum.Added:
                PrintToChatAll($"{PREFIX} Player \x03{player.PlayerName}\x01 wants to {(voteManager.IsRunningVote() ? "cancel" : "")} {voteManager.vote.Description} rounds ({voteManager.VoteCount} voted, {voteManager.RequiredVotes} needed).");
                break;
            case VoteResultEnum.AlreadyAddedBefore:
                voteManager.RemoveVote(userId);
                PrintToChatAll($"{PREFIX} Player \x03{player.PlayerName}\x01 dont wants {(voteManager.IsRunningVote() ? "to cancel" : "")} {voteManager.vote.Description} rounds anymore ({voteManager.VoteCount} voted, {voteManager.RequiredVotes} needed).");
                break;
            default:
                break;
        }

        if (voteManager.CheckVotes())
        {
            Votes_OnVoteReached(voteManager);
        }
    }

    [RequiresPermissions(new string[] { "@css/root" })]
    public static void OnForceVoteCommand(CCSPlayerController? player, CommandInfo commandInfo)
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

        if(RoundsCounter < Core.Config.PistolRound.RoundAmount)
        {
            ReplyToCommand(commandInfo, $"{PREFIX} You can't vote during the pistol rounds.");
            return;
        }

        string command = commandInfo.GetArg(0);
        AsyncVoteManager voteManager = GetVote(command);

        if (voteManager == null!)
        {
            ReplyToCommand(commandInfo, $"{PREFIX} Invalid vote command.");
            return;
        }

        PrintToChatAll($"{PREFIX} ADMIN: Forced {voteManager.vote.Description} rounds.");
        Votes_OnVoteReached(voteManager);
    }

    [RequiresPermissions(new string[] { "@css/root" })]
    private static void SkipPistolRoundCommand(CCSPlayerController? player, CommandInfo commandInfo)
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

        if(RoundsCounter >= Core.Config.PistolRound.RoundAmount)
        {
            ReplyToCommand(commandInfo, $"{PREFIX} You can't skip the pistol rounds when there is no pistol rounds.");
            return;
        }

        PrintToChatAll($"{PREFIX} ADMIN: Skipped the pistol rounds.");
        RoundsCounter = Core.Config.PistolRound.RoundAmount + 1;
    }
}
