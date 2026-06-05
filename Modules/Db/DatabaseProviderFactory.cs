using System.IO;
using Microsoft.Data.Sqlite;

namespace RetakesAllocator.Modules;

public static class DatabaseProviderFactory
{
    /// <summary>
    /// Builds the right provider for the configured engine.
    /// </summary>
    /// <param name="config">The DB connection config.</param>
    /// <param name="baseDirectory">Directory that relative SQLite paths resolve against (the plugin module directory at runtime).</param>
    public static IDatabaseProvider Create(ConnectionConfig config, string baseDirectory)
    {
        if (config.IsSqlite)
        {
            var path = Path.IsPathRooted(config.SqlitePath)
                ? config.SqlitePath
                : Path.Combine(baseDirectory, config.SqlitePath);

            var builder = new SqliteConnectionStringBuilder { DataSource = path };
            return new SqliteProvider(builder.ConnectionString);
        }

        return new MySqlProvider(config.BuildConnectionString());
    }
}
