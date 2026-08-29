using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocator.Modules.Models;
using RetakesAllocator.Modules.Weapons;
using RetakesAllocator.Modules.Votes;
using VotesClass = RetakesAllocator.Modules.Votes.Votes;

namespace RetakesAllocator.Modules.Config;

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

    [JsonPropertyName("TriggerWords")]
    public string[] TriggerWords { get; set; } = { "guns", "gun", "weapon", "weapons" };

    /// <summary>
    /// Picture shown in the loadout card header, before the title. The client renders the string
    /// as it stands, so it has to be a path Panorama can resolve on the machine it is drawn on -
    /// an `s2r://panorama/images/...` file from the game, or a `file://{images}/...` one your HUD
    /// addon ships. Empty hides the logo panel entirely.
    /// </summary>
    [JsonPropertyName("HudLogoUrl")]
    public string HudLogoUrl { get; set; } = "";

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
    public List<Weapon> PrimaryT { get; set; } = [..Allocator.PrimaryT];

    [JsonPropertyName("PrimaryCt")]
    public List<Weapon> PrimaryCt { get; set; } = [..Allocator.PrimaryCt];

    [JsonPropertyName("PistolsT")]
    public List<Weapon> PistolsT { get; set; } = [..Allocator.PistolsT];

    [JsonPropertyName("PistolsCt")]
    public List<Weapon> PistolsCt { get; set; } = [..Allocator.PistolsCT];
}

/// <summary>Vote definitions plus the vote tuning values. Defaults mirror Votes' canonical values.</summary>
public class VotesSection
{
    [JsonPropertyName("RequiredPercentage")]
    public int RequiredPercentage { get; set; } = VotesClass.RequiredPercentage;

    [JsonPropertyName("WeaponSelectionTime")]
    public int WeaponSelectionTime { get; set; } = VotesClass.WeaponSelectionTime;

    [JsonPropertyName("Votes")]
    public List<Vote> Votes { get; set; } = [..VotesClass.WeaponVotes];
}

public class ConnectionConfig
{
    /// <summary>"mysql" (MySQL/MariaDB) or "sqlite" (default).</summary>
    public string Provider { get; init; } = "sqlite";

    public string Host { get; init; } = string.Empty;
    public string Database { get; init; } = string.Empty;
    public string User { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public uint Port { get; init; } = 3306;

    /// <summary>SQLite database file (used only when Provider == "sqlite"). Relative paths resolve against the plugin's module directory.</summary>
    public string SqlitePath { get; init; } = "weapons.db";

    public bool IsSqlite() => string.Equals(Provider, "sqlite", StringComparison.OrdinalIgnoreCase);

    public bool IsValid()
    {
        if (IsSqlite())
        {
            return SqlitePath != string.Empty;
        }

        return Database != string.Empty
               && Host != string.Empty
               && User != string.Empty
               && Password != string.Empty
               && Port is > 0 and <= 65535;
    }

    public string BuildConnectionString()
    {
        var builder = new MySqlConnector.MySqlConnectionStringBuilder
        {
            Database = Database,
            UserID = User,
            Password = Password,
            Server = Host,
            Port = Port,
        };

        return builder.ConnectionString;
    }
}

public class PrefixConfig
{
    public string Prefix { get; set; } = " [\x04Retakes\x01]";
    public string PrefixCon { get; set; } = "[RetakesAllocator]";
}

public class PistolRoundConfig
{
    public int RoundAmount { get; init; } = 2;
    public string WeaponT { get; init; } = "weapon_glock";
    public string WeaponCt { get; init; } = "weapon_usp_silencer";
}

public class NadesConfig
{
    public Nades CtNades { get; set; } = new Nades()
    {
        Flashbangs = 2,
        Smokes = 1,
        Molotovs = 1,
        HeGrenades = 1
    };
    
    public Nades TNades { get; set; } = new Nades()
    {
        Flashbangs = 1,
        Smokes = 1,
        Molotovs = 1,
        HeGrenades = 1
    };
}
