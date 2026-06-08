using System.Data.Common;
using System.Data.SQLite;

namespace RetakesAllocator.Modules;

public class SqliteProvider : IDatabaseProvider
{
    private readonly string _connectionString;

    public SqliteProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbConnection CreateConnection() => new SQLiteConnection(_connectionString);

    public string CreateTableSql =>
        """
        CREATE TABLE IF NOT EXISTS weapons (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            auth VARCHAR(128) NOT NULL UNIQUE,
            name VARCHAR(128) NOT NULL,
            t_primary INTEGER NOT NULL DEFAULT 0,
            ct_primary INTEGER NOT NULL DEFAULT 0,
            t_secondary INTEGER NOT NULL DEFAULT 0,
            ct_secondary INTEGER NOT NULL DEFAULT 0,
            t_pistol_round INTEGER NOT NULL DEFAULT 0,
            ct_pistol_round INTEGER NOT NULL DEFAULT 0,
            give_awp INTEGER NOT NULL DEFAULT 0
        );
        """;
}
