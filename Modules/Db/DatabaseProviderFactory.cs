using System.IO;
using RetakesAllocator.Modules.Config;

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
        if (!config.IsSqlite())
        {
            return new MySqlProvider(config.BuildConnectionString());
        }

        var path = Path.IsPathRooted(config.SqlitePath)
            ? config.SqlitePath
            : Path.Combine(baseDirectory, config.SqlitePath);

        // Pooling keeps the file handle warm: the store opens a connection per query,
        // and without it every preference read/write re-opens the database file.
        var connectionString = $"Data Source={path};Pooling=True";
        return new SqliteProvider(connectionString);
    }
}
