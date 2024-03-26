using System.Text.Json;
using CounterStrikeSharp.API.Modules.Utils;
using MySqlConnector;

using static RetakesAllocator.Modules.Core;

namespace RetakesAllocator.Modules;

public class Config
{ 
    public ConnectionConfig DbConnection { get; init; } = null!;
    public PrefixConfig Prefix { get; init; } = null!;
    public PistolRoundConfig PistolRound { get; init; } = null!;

    public bool GiveArmor {get; init;} = true;
    public string[] triggerWords {get; init;} = { "guns", "gun", "weapon", "weapons"};
    public bool AddSkipOption {get; init;} = true;

    public Config()
    {
        DbConnection = new ConnectionConfig();
        Prefix = new PrefixConfig();
        PistolRound = new PistolRoundConfig();
        GiveArmor = true;
        triggerWords = new string[] { "guns", "gun", "weapon", "weapons"};
        AddSkipOption = true;
    }

    public bool IsValid()
    {
        return DbConnection.Database != string.Empty && DbConnection.Host != string.Empty && DbConnection.User != string.Empty && DbConnection.Password != string.Empty && 0 < DbConnection.Port && DbConnection.Port < 65535;
    }

    public string BuildConnectionString()
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Database = DbConnection.Database,
            UserID = DbConnection.User,
            Password = DbConnection.Password,
            Server = DbConnection.Host,
            Port = DbConnection.Port,
        };

        return builder.ConnectionString;
    }
}

public class ConnectionConfig
{
    public string Host { get; init; } = string.Empty;
    public string Database { get; init; } = string.Empty;
    public string User { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public uint Port { get; init; } = 3306;
}

public class PrefixConfig
{
    public string Prefix { get; set; } = " [\x04Retakes\x01]";
    public string PrefixCon { get; set; } = "[RetakesAllocator]";
}

public class PistolRoundConfig
{
    public int RoundAmount { get; init; } = 2;
    public string weapon_t { get; init; } = "weapon_glock";
    public string weapon_ct { get; init; } = "weapon_usp_silencer";

    public string GetWeaponByTeam(CsTeam team)
    {
        return (team == CsTeam.Terrorist) ? weapon_t : weapon_ct;
    }
}

public static class Configs
{
    public const string ConfigDirectory = "configs";
    private const string ConfigPath = "retakes_allocator.json";

    public static Config LoadConfig()
    {
        var configPath = Path.Combine(Plugin.ModuleDirectory, ConfigDirectory, ConfigPath);
        if (!File.Exists(configPath))
        {
            return CreateConfig(configPath);
        }

        var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath))!;

        return config;
    }

    private static Config CreateConfig(string configPath)
    {
        var config = new Config();

        File.WriteAllText(configPath,
            JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        return config;
    }

    public static void CreateConfigsDirectory()
    {
        var configDirectory = Path.Combine(Plugin.ModuleDirectory, ConfigDirectory);

        if (!Directory.Exists(configDirectory))
        {
            Directory.CreateDirectory(configDirectory);
        }
    }
}
