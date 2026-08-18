using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Utils;
using static RetakesAllocator.Modules.Weapons.Menu;
using static RetakesAllocator.Modules.Votes.Votes;
using RetakesAllocator.Modules.Votes;
using Player = RetakesAllocator.Modules.Models.Player;

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
        // Was missing before: leaving it registered meant a second copy of the command
        // after every plugin reload.
        Plugin.RemoveCommand("css_skip_pistol", SkipPistolRoundCommand);
    }

    /// <summary>
    /// Shared guard for the player-facing commands: the caller must be a valid,
    /// tracked player. Returns null (after replying) when they are not.
    /// </summary>
    private static Player? RequirePlayer(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player is null || !player.IsValid)
        {
            ReplyToCommand(commandInfo, $"{Prefix} This command can only be executed by a valid player.");
            return null;
        }

        var playerObj = FindPlayer(player);

        if (playerObj is null)
        {
            ReplyToCommand(commandInfo, $"{Prefix} This command can only be executed by a valid player.");
            return null;
        }

        return playerObj;
    }

    private static void GunsCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (RequirePlayer(player, commandInfo) is null)
        {
            return;
        }

        OpenPistolRoundTMenu(player!);
    }

    private static void CTGunsCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (RequirePlayer(player, commandInfo) is null)
        {
            return;
        }

        OpenCTPrimaryMenu(player!, false);
    }

    private static void TGunsCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (RequirePlayer(player, commandInfo) is null)
        {
            return;
        }

        OpenTPrimaryMenu(player!, false);
    }

    private static void PistolsTCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (RequirePlayer(player, commandInfo) is null)
        {
            return;
        }

        OpenSecondaryTMenu(player!, false);
    }

    private static void PistolsCTCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (RequirePlayer(player, commandInfo) is null)
        {
            return;
        }

        OpenSecondaryCTMenu(player!, false);
    }

    private static void AwpCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (RequirePlayer(player, commandInfo) is null)
        {
            return;
        }

        OpenGiveAWPMenu(player!);
    }

    [RequiresPermissions(new string[] { "@css/root" })]
    private static void ReloadCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player is not null && !player.IsValid)
        {
            ReplyToCommand(commandInfo, $"{Prefix} This command can only be executed by a valid player.");
            return;
        }

        ReloadConfig();
        ReplyToCommand(commandInfo, $"{Prefix} Configs reloaded.");
    }

    public static void OnVoteCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (RequirePlayer(player, commandInfo) is null)
        {
            return;
        }

        if (RoundsCounter < Core.Config.PistolRound.RoundAmount)
        {
            ReplyToCommand(commandInfo, $"{Prefix} You can't vote during the pistol rounds.");
            return;
        }

        var userId = player!.UserId;

        if (userId is null)
        {
            return;
        }

        var voteManager = GetVote(commandInfo.GetArg(0));

        if (voteManager is null)
        {
            ReplyToCommand(commandInfo, $"{Prefix} Invalid vote command.");
            return;
        }

        switch (voteManager.AddVote(userId.Value))
        {
            case VoteResultEnum.Added:
                PrintToChatAll($"{Prefix} Player \x03{player.PlayerName}\x01 wants to {(voteManager.IsRunningVote() ? "cancel" : "")} {voteManager.Vote.Description} rounds ({voteManager.VoteCount} voted, {voteManager.RequiredVotes} needed).");
                break;
            case VoteResultEnum.AlreadyAddedBefore:
                voteManager.RemoveVote(userId.Value);
                PrintToChatAll($"{Prefix} Player \x03{player.PlayerName}\x01 dont wants {(voteManager.IsRunningVote() ? "to cancel" : "")} {voteManager.Vote.Description} rounds anymore ({voteManager.VoteCount} voted, {voteManager.RequiredVotes} needed).");
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
        if (RequirePlayer(player, commandInfo) is null)
        {
            return;
        }

        if (RoundsCounter < Core.Config.PistolRound.RoundAmount)
        {
            ReplyToCommand(commandInfo, $"{Prefix} You can't vote during the pistol rounds.");
            return;
        }

        var voteManager = GetVote(commandInfo.GetArg(0));

        if (voteManager is null)
        {
            ReplyToCommand(commandInfo, $"{Prefix} Invalid vote command.");
            return;
        }

        PrintToChatAll($"{Prefix} ADMIN: Forced {voteManager.Vote.Description} rounds.");
        Votes_OnVoteReached(voteManager);
    }

    [RequiresPermissions(new string[] { "@css/root" })]
    private static void SkipPistolRoundCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (RoundsCounter >= Core.Config.PistolRound.RoundAmount)
        {
            ReplyToCommand(commandInfo, $"{Prefix} You can't skip the pistol rounds when there is no pistol rounds.");
            return;
        }

        PrintToChatAll($"{Prefix} ADMIN: Skipped the pistol rounds.");
        RoundsCounter = Core.Config.PistolRound.RoundAmount + 1;
    }
}
