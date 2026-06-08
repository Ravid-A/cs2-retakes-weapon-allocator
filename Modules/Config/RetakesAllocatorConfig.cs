using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using RetakesAllocator.Modules.Weapons;
using RetakesAllocator.Modules.Votes;
using VotesClass = RetakesAllocator.Modules.Votes.Votes;

namespace RetakesAllocator.Modules;

/// <summary>
/// Single consolidated plugin config, loaded and saved by CounterStrikeSharp's
/// IPluginConfig mechanism. Each subsystem gets its own labeled JSON section.
/// </summary>
public class RetakesAllocatorConfig : BasePluginConfig
{
    [JsonPropertyName("DbConnection")]
    public ConnectionConfig DbConnection { get; set; } = new();

    [JsonPropertyName("Prefix")]
    public PrefixConfig Prefix { get; set; } = new();

    [JsonPropertyName("PistolRound")]
    public PistolRoundConfig PistolRound { get; set; } = new();

    [JsonPropertyName("GiveArmor")]
    public bool GiveArmor { get; set; } = true;

    [JsonPropertyName("TriggerWords")]
    public string[] TriggerWords { get; set; } = { "guns", "gun", "weapon", "weapons" };

    [JsonPropertyName("AddSkipOption")]
    public bool AddSkipOption { get; set; } = true;

    [JsonPropertyName("Weapons")]
    public WeaponsSection Weapons { get; set; } = new();

    [JsonPropertyName("Nades")]
    public NadesConfig Nades { get; set; } = new();

    [JsonPropertyName("Votes")]
    public VotesSection Votes { get; set; } = new();

    public bool IsValid() => DbConnection.IsValid();
}

/// <summary>The four selectable weapon lists. Defaults mirror Allocator's canonical lists.</summary>
public class WeaponsSection
{
    [JsonPropertyName("PrimaryT")]
    public List<Weapon> PrimaryT { get; set; } = new(Allocator.PrimaryT);

    [JsonPropertyName("PrimaryCt")]
    public List<Weapon> PrimaryCt { get; set; } = new(Allocator.PrimaryCt);

    [JsonPropertyName("PistolsT")]
    public List<Weapon> PistolsT { get; set; } = new(Allocator.PistolsT);

    [JsonPropertyName("PistolsCt")]
    public List<Weapon> PistolsCt { get; set; } = new(Allocator.PistolsCT);
}

/// <summary>Vote definitions plus the vote tuning values. Defaults mirror Votes' canonical values.</summary>
public class VotesSection
{
    [JsonPropertyName("RequiredPercentage")]
    public int RequiredPercentage { get; set; } = VotesClass.RequiredPrecentage;

    [JsonPropertyName("WeaponSelectionTime")]
    public int WeaponSelectionTime { get; set; } = VotesClass.WeaponSelectionTime;

    [JsonPropertyName("Votes")]
    public List<Vote> Votes { get; set; } = new(VotesClass.WeaponVotes);
}
