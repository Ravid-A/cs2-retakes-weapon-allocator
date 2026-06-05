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
    }

    /// <summary>Returns the stored preferences for a SteamID, or null if none exist.</summary>
    public async Task<WeaponPreference?> GetUserAsync(string auth)
    {
        await using var conn = _provider.CreateConnection();
        await conn.OpenAsync();
        return await conn.QuerySingleOrDefaultAsync<WeaponPreference>(
            """
            SELECT auth, name, t_primary, ct_primary, t_secondary, ct_secondary, give_awp
            FROM weapons
            WHERE auth = @auth
            """,
            new { auth });
    }

    /// <summary>Inserts a new user row with default (zeroed) preferences.</summary>
    public async Task CreateUserAsync(string auth, string name)
    {
        await using var conn = _provider.CreateConnection();
        await conn.OpenAsync();
        await conn.ExecuteAsync(
            """
            INSERT INTO weapons (auth, name)
            VALUES (@auth, @name)
            """,
            new { auth, name });
    }

    /// <summary>Persists the four weapon preference columns plus give_awp for an existing user.</summary>
    public async Task SaveUserAsync(WeaponPreference pref)
    {
        await using var conn = _provider.CreateConnection();
        await conn.OpenAsync();
        await conn.ExecuteAsync(
            """
            UPDATE weapons
            SET t_primary = @TPrimary,
                ct_primary = @CtPrimary,
                t_secondary = @TSecondary,
                ct_secondary = @CtSecondary,
                give_awp = @GiveAwp
            WHERE auth = @Auth
            """,
            pref);
    }
}
