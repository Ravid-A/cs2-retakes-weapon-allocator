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

        return VoteManagers.FirstOrDefault(x => x.Command == command)!;
    }

    public static void Votes_OnConfigParsed()
    {
        foreach (var vote in WeaponVotes)
        {
            Plugin.AddCommand(vote.Command, vote.Description, OnVoteCommand);
            AsyncVoteManager voteManager = new(vote);

            VoteManagers.Add(voteManager);
        }
    }

    public static void Votes_OnMapStart()
    {
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
        foreach (var vote in WeaponVotes)
        {
            Plugin.RemoveCommand(vote.Command, OnVoteCommand);
        }
    }

    public static void Votes_OnVoteReached(AsyncVoteManager voteManager)
    {
        string description = voteManager.vote.Description;
        description = description.Substring(0, 1).ToUpper() + description.Substring(1);

        if(currentVote != null! && voteManager.IsRunningVote())
        {
            currentVote = null!;

            PrintToChatAll($"{PREFIX} {description} rounds will be canceled next round.");
            return;
        }
        
        currentVote = voteManager;
        PrintToChatAll($"{PREFIX} {description} rounds will start next round!");
    }
}