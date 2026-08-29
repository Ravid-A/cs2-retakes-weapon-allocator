using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Utils;
using RetakesAllocator.Modules.Weapons;
using static RetakesAllocator.Modules.Votes.Votes;
using RetakesAllocator.Modules.Votes;

namespace RetakesAllocator.Modules.Handlers;

internal static class Commands
{
    private static readonly string[] LoadoutCommands =
        ["css_guns", "css_pistols", "css_awp"];

    public static void RegisterCommands()
    {
        foreach (var name in LoadoutCommands)
        {
            Plugin.AddCommand(name, "Opens the loadout menu", LoadoutCommand);
        }

        Plugin.AddCommand("css_weapons_reload", "Reloads the weapons allocator's weapons configs", ReloadCommand);
        Plugin.AddCommand("css_skip_pistol", "Skips the pistol round", SkipPistolRoundCommand);
    }

    public static void UnRegisterCommands()
    {
        foreach (var name in LoadoutCommands)
        {
            Plugin.RemoveCommand(name, LoadoutCommand);
        }

        Plugin.RemoveCommand("css_weapons_reload", ReloadCommand);
    }

    /// <summary>
    /// css_guns, css_pistols and css_awp all open the same card: primary, secondary and AWP are
    /// three bands of one panel now, not three menus, so there is nothing for them to open
    /// separately. The names stay because the binds do.
    /// </summary>
    private static void LoadoutCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid)
        {
            ReplyToCommand(commandInfo, $"{Prefix} This command can only be executed by a valid player.");
            return;
        }

        if (FindPlayer(player) == null!)
        {
            ReplyToCommand(commandInfo, $"{Prefix} This command can only be executed by a valid player.");
            return;
        }

        LoadoutPanel.Open(player);
    }

    [RequiresPermissions(new string[] { "@css/root" })]
    private static void ReloadCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null)
        {
            ReplyToCommand(commandInfo, $"{Prefix} This command can only be executed by a player.");
            return;
        }

        if (!player.IsValid)
        {
            ReplyToCommand(commandInfo, $"{Prefix} This command can only be executed by a valid player.");
            return;
        }

        ReloadConfig();
        PrintToChat(player, $"{Prefix} Configs reloaded.");
    }

    public static void OnVoteCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null)
        {
            ReplyToCommand(commandInfo, $"{Prefix} This command can only be executed by a player.");
            return;
        }

        if (!player.IsValid)
        {
            ReplyToCommand(commandInfo, $"{Prefix} This command can only be executed by a valid player.");
            return;
        }

        if(RoundsCounter < Core.Config.PistolRound.RoundAmount)
        {
            ReplyToCommand(commandInfo, $"{Prefix} You can't vote during the pistol rounds.");
            return;
        }

        int userId = player.UserId!.Value;

        string command = commandInfo.GetArg(0);
        AsyncVoteManager voteManager = GetVote(command);

        if (voteManager == null!)
        {
            ReplyToCommand(commandInfo, $"{Prefix} Invalid vote command.");
            return;
        }

        switch(voteManager.AddVote(userId))
        {
            case VoteResultEnum.Added:
                PrintToChatAll($"{Prefix} Player \x03{player.PlayerName}\x01 wants to {(voteManager.IsRunningVote() ? "cancel" : "")} {voteManager.Vote.Description} rounds ({voteManager.VoteCount} voted, {voteManager.RequiredVotes} needed).");
                break;
            case VoteResultEnum.AlreadyAddedBefore:
                voteManager.RemoveVote(userId);
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
        if (player == null)
        {
            ReplyToCommand(commandInfo, $"{Prefix} This command can only be executed by a player.");
            return;
        }

        if (!player.IsValid)
        {
            ReplyToCommand(commandInfo, $"{Prefix} This command can only be executed by a valid player.");
            return;
        }

        if(RoundsCounter < Core.Config.PistolRound.RoundAmount)
        {
            ReplyToCommand(commandInfo, $"{Prefix} You can't vote during the pistol rounds.");
            return;
        }

        string command = commandInfo.GetArg(0);
        AsyncVoteManager voteManager = GetVote(command);

        if (voteManager == null!)
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
        if (player == null)
        {
            ReplyToCommand(commandInfo, $"{Prefix} This command can only be executed by a player.");
            return;
        }

        if (!player.IsValid)
        {
            ReplyToCommand(commandInfo, $"{Prefix} This command can only be executed by a valid player.");
            return;
        }

        if(RoundsCounter >= Core.Config.PistolRound.RoundAmount)
        {
            ReplyToCommand(commandInfo, $"{Prefix} You can't skip the pistol rounds when there is no pistol rounds.");
            return;
        }

        PrintToChatAll($"{Prefix} ADMIN: Skipped the pistol rounds.");
        RoundsCounter = Core.Config.PistolRound.RoundAmount + 1;
    }
}
