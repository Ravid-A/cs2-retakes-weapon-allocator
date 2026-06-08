using CounterStrikeSharp.API.Core;
using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Handlers.Commands;
using static RetakesAllocator.Modules.Utils;

namespace RetakesAllocator.Modules.Votes;

public enum VoteResultEnum
{
    Added,
    AlreadyAddedBefore,
    VotesAlreadyReached
}


public class Vote
{
    public string Command { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> weapons_t { get; set; } = new();
    public List<string> weapons_ct { get; set; } = new();
    public bool OnlyHeadshots { get; set; } = false;
    public bool GiveWeapons { get; set; } = true;
    public bool GiveNades { get; set; } = true;
    public bool GiveKnife { get; set; } = true;
    public bool GiveArmor { get; set; } = true;
    public bool GiveHelmet { get; set; } = true;

    public Vote(string Command, string Description, List<string> weapons_t, List<string> weapons_ct, bool OnlyHeadshots, bool GiveWeapons, bool GiveKnife = true, bool GiveArmor = true, bool GiveHelmet = true)
    {
        this.Command = Command;
        this.Description = Description;
        this.weapons_t = weapons_t;
        this.weapons_ct = weapons_ct;
        this.OnlyHeadshots = OnlyHeadshots;
        this.GiveWeapons = GiveWeapons;
        this.GiveKnife = GiveKnife;
        this.GiveArmor = GiveArmor;
        this.GiveHelmet = GiveHelmet;
    }
}

public class Votes
{
    public static List<Vote> WeaponVotes = new()
    {
        new("vp", "pistol only", new() { "glock" }, new() { "usp_silencer" }, false, true, true, true, false),
        new("vph", "pistol only with headshots only", new() { "glock" }, new() { "usp_silencer" }, true, true),
        new("vhs", "headshots only", new(), new(), true, false),
        new("vawp", "awp only", new() { "awp" }, new() { "awp" }, false, true),
        new("vrifles", "rifle only", new() { "ak47", "galilar" }, new() { "m4a1", "m4a1_silencer" }, false, true),
    };

    public static List<AsyncVoteManager> VoteManagers = new();
    public static int RequiredPrecentage = 60;
    public static int WeaponSelectionTime = 5;

    public static AsyncVoteManager GetVote(string command)
    {
        if(VoteManagers.Count == 0)
            return null!;

        return VoteManagers.FirstOrDefault(x => command.Replace("css_", "").Replace("force", "") == x.vote.Command)!;
    }

    public static void Votes_OnConfigParsed(int weaponSelectionTime, int requiredPrecentage)
    {
        WeaponSelectionTime = weaponSelectionTime;
        RequiredPrecentage = requiredPrecentage;

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
        foreach (var voteManager in VoteManagers)
        {
            var command = voteManager.vote.Command;
            Plugin.RemoveCommand($"css_{command}", OnVoteCommand);
            Plugin.RemoveCommand($"css_force{command}", OnForceVoteCommand);
        }

        VoteManagers.Clear();
    }

    public static void Votes_OnMapStart()
    {
        currentVote = null!;

        foreach (var vote in WeaponVotes)
        {
            foreach (var voteManager in VoteManagers)
            {
                voteManager.OnMapStart();
            }
        }
    }

    public static void Votes_OnPluginUnload()
    {
        UnregisterVoteCommands();
    }

    public static void Votes_OnVoteReached(AsyncVoteManager voteManager)
    {
        string description = voteManager.vote.Description;
        description = description.Substring(0, 1).ToUpper() + description.Substring(1);
        voteManager.ClearVotes();


        if(currentVote != null! && voteManager.IsRunningVote())
        {
            currentVote = null!;

            PrintToChatAll($"{PREFIX} {description} rounds will be canceled next round.");
            return;
        }
        
        currentVote = voteManager;
        PrintToChatAll($"{PREFIX} {description} rounds will start next round!");
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