using System.Text.Json;
using MySqlConnector;

using static WeaponsAllocator.Core;

namespace WeaponsAllocator;

public class Config
{
     public ConnectionConfig DBConnection { get; set; } = null!;
     public PREFIXConfig PREFIX { get; set; } = null!;

    public bool IsValid()
    {
        return DBConnection.Database != string.Empty && DBConnection.Host != string.Empty && DBConnection.User != string.Empty && DBConnection.Password != string.Empty && 0 < DBConnection.Port && DBConnection.Port < 65535;
    }

    public string BuildConnectionString()
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Database = DBConnection.Database,
            UserID = DBConnection.User,
            Password = DBConnection.Password,
            Server = DBConnection.Host,
            Port = DBConnection.Port,
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

public class PREFIXConfig
{
    public string PREFIX { get; set; } = " \x04[Retakes]\x01";
    public string PREFIX_CON { get; set; } = "[Retakes]";
}

public class Configs
{
    public static Config LoadConfig()
    {
        var configPath = Path.Combine(_plugin.ModuleDirectory, "config.json");
        if (!File.Exists(configPath)) return CreateConfig(configPath);

        var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath))!;

        return config;
    }

    private static Config CreateConfig(string configPath)
    {
        var config = new Config
        {
            DBConnection = new ConnectionConfig(),
            PREFIX = new PREFIXConfig(),
        };

        File.WriteAllText(configPath,
            JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        return config;
    }
}