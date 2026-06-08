namespace RetakesAllocator.Modules.Models;

public class Vote
{
    public string Command { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> WeaponsT { get; set; } = new();
    public List<string> WeaponsCt { get; set; } = new();
    public bool OnlyHeadshots { get; set; } = false;
    public bool GiveWeapons { get; set; } = true;
    public bool GiveNades { get; set; } = true;
    public bool GiveKnife { get; set; } = true;
    public bool GiveArmor { get; set; } = true;
    public bool GiveHelmet { get; set; } = true;

    public Vote(string command, string description, List<string> weaponsT, List<string> weaponsCt, bool onlyHeadshots, bool giveWeapons, bool giveKnife = true, bool giveArmor = true, bool giveHelmet = true)
    {
        Command = command;
        Description = description;
        WeaponsT = weaponsT;
        WeaponsCt = weaponsCt;
        OnlyHeadshots = onlyHeadshots;
        GiveWeapons = giveWeapons;
        GiveKnife = giveKnife;
        GiveArmor = giveArmor;
        GiveHelmet = giveHelmet;
    }
}