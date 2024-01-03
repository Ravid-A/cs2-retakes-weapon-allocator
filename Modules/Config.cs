using System.Text.Json;
using MySqlConnector;

using static RetakesAllocator.Modules.Core;

namespace RetakesAllocator.Modules;

public class Config
{ 
    public ConnectionConfig DbConnection { get; init; } = null!;
    public PrefixConfig Prefix { get; init; } = null!;

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

public static class Configs
{
    public static Config LoadConfig()
    {
        var configPath = Path.Combine(Plugin.ModuleDirectory, "config.json");
        if (!File.Exists(configPath))
        {
            return CreateConfig(configPath);
        }

        var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath))!;

        return config;
    }

    private static Config CreateConfig(string configPath)
    {
        var config = new Config
        {
            DbConnection = new ConnectionConfig(),
            Prefix = new PrefixConfig(),
        };

        File.WriteAllText(configPath,
            JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        return config;
    }
}
