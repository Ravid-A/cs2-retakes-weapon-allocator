using CounterStrikeSharp.API.Core;

namespace RetakesAllocator.Modules.Votes;

public class Vote
{

    public string Command { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TimeToVote { get; set; } = 30;
    public VoteWeapons VoteWeapons {get; set;} = new();
    public string style {get; set; } = string.Empty;
    public Vote(string Command, string Description, int TimeToVote, VoteWeapons VoteWeapons, string style)
    {
        this.Command = Command;
        this.Description = Description;
        this.TimeToVote = TimeToVote;
        this.VoteWeapons = VoteWeapons;
        this.style = style;
    }

}

public class VoteWeapons
{
    public string Primary = string.Empty;
    public string Secondary = string.Empty;
    public bool GiveKnife = true;
    public List<string> Grenades = new();

    public VoteWeapons(string Primary = "", string Secondary = "", bool GiveKnife = true, List<string> Grenades = null!)
    {
        this.Primary = Primary;
        this.Secondary = Secondary;
        this.GiveKnife = GiveKnife;
        this.Grenades = Grenades;
    }
}

public class Logic
{
    public static List<Vote> WeaponVotes = new()
    {
        new("vp", "Vote for pistol only rounds", 30, )
    }

}