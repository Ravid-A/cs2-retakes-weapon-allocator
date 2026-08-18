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

    public string InsertUserSql =>
        """
        INSERT INTO weapons (auth, name)
        VALUES (@auth, @name)
        ON CONFLICT(auth) DO NOTHING
        """;

    public string SaveUserSql =>
        """
        INSERT INTO weapons (auth, name, t_primary, ct_primary, t_secondary, ct_secondary, t_pistol_round, ct_pistol_round, give_awp)
        VALUES (@Auth, @Name, @TPrimary, @CtPrimary, @TSecondary, @CtSecondary, @TPistolRound, @CtPistolRound, @GiveAwp)
        ON CONFLICT(auth) DO UPDATE SET
            t_primary = excluded.t_primary,
            ct_primary = excluded.ct_primary,
            t_secondary = excluded.t_secondary,
            ct_secondary = excluded.ct_secondary,
            t_pistol_round = excluded.t_pistol_round,
            ct_pistol_round = excluded.ct_pistol_round,
            give_awp = excluded.give_awp
        """;
}
