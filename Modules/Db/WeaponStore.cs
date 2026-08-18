using Dapper;

namespace RetakesAllocator.Modules;

/// <summary>
/// Engine-agnostic data access for the `weapons` table. All queries are async and
/// parameterized; the concrete engine comes from the injected <see cref="IDatabaseProvider"/>.
/// </summary>
public class WeaponStore
{
    private readonly IDatabaseProvider _provider;

    static WeaponStore()
    {
        // Map snake_case columns (t_primary) to PascalCase properties (TPrimary).
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public WeaponStore(IDatabaseProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Creates the schema if it does not already exist.</summary>
    public async Task InitializeAsync()
    {
        await using var conn = _provider.CreateConnection();
        await conn.OpenAsync();
        await conn.ExecuteAsync(_provider.CreateTableSql);
        await AddColumnIfMissing(conn, "t_pistol_round");
        await AddColumnIfMissing(conn, "ct_pistol_round");
    }

    private static async Task AddColumnIfMissing(System.Data.Common.DbConnection conn, string column)
    {
        try
        {
            await conn.ExecuteAsync($"ALTER TABLE weapons ADD COLUMN {column} INTEGER NOT NULL DEFAULT 0");
        }
        catch
        {
            // Column already exists. CREATE TABLE handles fresh installs; this keeps old DBs working.
        }
    }

    /// <summary>Returns the stored preferences for a SteamID, or null if none exist.</summary>
    public async Task<WeaponPreference?> GetUserAsync(string auth)
    {
        await using var conn = _provider.CreateConnection();
        await conn.OpenAsync();
        return await conn.QuerySingleOrDefaultAsync<WeaponPreference>(
            """
            SELECT auth, name, t_primary, ct_primary, t_secondary, ct_secondary, t_pistol_round, ct_pistol_round, give_awp
            FROM weapons
            WHERE auth = @auth
            """,
            new { auth });
    }

    /// <summary>
    /// Inserts a new user row with default (zeroed) preferences. A row already
    /// existing for that auth is not an error — two joins racing on the same SteamID
    /// used to surface as a logged exception.
    /// </summary>
    public async Task CreateUserAsync(string auth, string name)
    {
        await using var conn = _provider.CreateConnection();
        await conn.OpenAsync();
        await conn.ExecuteAsync(_provider.InsertUserSql, new { auth, name });
    }

    /// <summary>Persists every weapon preference column, inserting the row if missing.</summary>
    public async Task SaveUserAsync(WeaponPreference pref)
    {
        await using var conn = _provider.CreateConnection();
        await conn.OpenAsync();
        await conn.ExecuteAsync(_provider.SaveUserSql, pref);
    }
}
