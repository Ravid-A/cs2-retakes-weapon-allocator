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

    /// <summary>
    /// Command name (with and without the "force" prefix) to vote manager. Built once
    /// at registration so the vote commands are an O(1) lookup with no per-call string
    /// allocations, and so a vote whose own name contains "force" still resolves.
    /// </summary>
    private static readonly Dictionary<string, AsyncVoteManager> VoteCommands =
        new(StringComparer.OrdinalIgnoreCase);

    public static int RequiredPercentage = 60;
    public static int WeaponSelectionTime = 5;

    public static AsyncVoteManager? GetVote(string command)
    {
        return VoteCommands.GetValueOrDefault(command);
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
            var manager = new AsyncVoteManager(vote);

            Plugin.AddCommand($"css_{vote.Command}", vote.Description, OnVoteCommand);
            Plugin.AddCommand($"css_force{vote.Command}", $"force {vote.Description}", OnForceVoteCommand);

            VoteManagers.Add(manager);
            VoteCommands[$"css_{vote.Command}"] = manager;
            VoteCommands[$"css_force{vote.Command}"] = manager;
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

        // The active vote points at a manager that is about to be discarded.
        if (CurrentVote != null!)
        {
            CurrentVote = null!;
        }

        VoteManagers.Clear();
        VoteCommands.Clear();
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
        var description = voteManager.Vote.Description;

        if (description.Length > 0)
        {
            description = char.ToUpperInvariant(description[0]) + description[1..];
        }

        voteManager.ClearVotes();

        if (CurrentVote != null! && voteManager.IsRunningVote())
        {
            CurrentVote = null!;

            PrintToChatAll($"{Prefix} {description} rounds will be canceled next round.");
            return;
        }

        CurrentVote = voteManager;
        PrintToChatAll($"{Prefix} {description} rounds will start next round!");
    }

    public static void Votes_OnPlayerDisconnect(CCSPlayerController? player)
    {
        // The controller is frequently already torn down by the time the disconnect
        // listener runs; dereferencing it here used to throw on every such disconnect.
        var userId = player?.UserId;

        if (userId is null)
        {
            return;
        }

        foreach (var voteManager in VoteManagers)
        {
            voteManager.RemoveVote(userId.Value);
        }
    }
}
