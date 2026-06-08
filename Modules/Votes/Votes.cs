using CounterStrikeSharp.API.Core;
using RetakesAllocator.Modules.Models;
using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Handlers.Commands;
using static RetakesAllocator.Modules.Utils;

namespace RetakesAllocator.Modules.Votes;

public enum VoteResultEnum
{
    Added,
    AlreadyAddedBefore
}

public class Votes
{
    public static List<Vote> WeaponVotes = new()
    {
        new Vote("vp", "pistol only", new() { "glock" }, new() { "usp_silencer" }, false, true, true, true, false),
        new Vote("vph", "pistol only with headshots only", new() { "glock" }, new() { "usp_silencer" }, true, true),
        new Vote("vhs", "headshots only", new(), new(), true, false),
        new Vote("vawp", "awp only", new() { "awp" }, new() { "awp" }, false, true),
        new Vote("vrifles", "rifle only", new() { "ak47", "galilar" }, new() { "m4a1", "m4a1_silencer" }, false, true),
    };

    private static readonly List<AsyncVoteManager> VoteManagers = [];
    public static int RequiredPercentage = 60;
    public static int WeaponSelectionTime = 5;

    public static AsyncVoteManager GetVote(string command)
    {
        return VoteManagers.Count == 0 ? null! : VoteManagers.FirstOrDefault(x => command.Replace("css_", "").Replace("force", "") == x.Vote.Command)!;
    }

    public static void Votes_OnConfigParsed(int weaponSelectionTime, int requiredPercentage)
    {
        WeaponSelectionTime = weaponSelectionTime;
        RequiredPercentage = requiredPercentage;

        RegisterVoteCommands();
    }

    /// <summary>
    /// (Re)registers a chat command per vote in <see cref="WeaponVotes"/>. Idempotent:
    /// any previously registered vote commands are removed first, so this is safe to
    /// call again on a config hot reload.
    /// </summary>
    public static void RegisterVoteCommands()
    {
        UnregisterVoteCommands();

        foreach (var vote in WeaponVotes)
        {
            Plugin.AddCommand($"css_{vote.Command}", vote.Description, OnVoteCommand);
            Plugin.AddCommand($"css_force{vote.Command}", $"force {vote.Description}", OnForceVoteCommand);
            VoteManagers.Add(new AsyncVoteManager(vote));
        }
    }

    /// <summary>Removes every currently registered vote command and clears the managers.</summary>
    public static void UnregisterVoteCommands()
    {
        foreach (var command in VoteManagers.Select(voteManager => voteManager.Vote.Command))
        {
            Plugin.RemoveCommand($"css_{command}", OnVoteCommand);
            Plugin.RemoveCommand($"css_force{command}", OnForceVoteCommand);
        }

        VoteManagers.Clear();
    }

    public static void Votes_OnMapStart()
    {
        CurrentVote = null!;

        foreach (var voteManager in VoteManagers)
        {
            voteManager.OnMapStart();
        }
    }

    public static void Votes_OnPluginUnload()
    {
        UnregisterVoteCommands();
    }

    public static void Votes_OnVoteReached(AsyncVoteManager voteManager)
    {
        string description = voteManager.Vote.Description;
        description = description.Substring(0, 1).ToUpper() + description.Substring(1);
        voteManager.ClearVotes();

        if(CurrentVote != null! && voteManager.IsRunningVote())
        {
            CurrentVote = null!;

            PrintToChatAll($"{Prefix} {description} rounds will be canceled next round.");
            return;
        }

        CurrentVote = voteManager;
        PrintToChatAll($"{Prefix} {description} rounds will start next round!");
    }

    public static void Votes_OnPlayerDisconnect(CCSPlayerController player)
    {
        var userId = player.UserId!.Value;

        foreach (AsyncVoteManager voteManager in VoteManagers)
        {
            voteManager.RemoveVote(userId);
        }
    }
}
