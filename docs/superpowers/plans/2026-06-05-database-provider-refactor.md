# Database Provider Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the hand-rolled, MySQL-only, callback-based `Database` class with a provider-abstracted data-access layer that uses Dapper, supports both MySQL/MariaDB and SQLite (selectable via config), and exposes async/await parameterized methods.

**Architecture:** A small `IDatabaseProvider` abstraction owns engine-specific concerns (creating a `DbConnection`, the dialect-specific `CREATE TABLE` DDL). Two implementations exist: `MySqlProvider` (wrapping the existing `MySqlConnector` DLL) and `SqliteProvider` (wrapping `Microsoft.Data.Sqlite`). A new engine-agnostic `WeaponStore` class runs all CRUD through Dapper using parameterized SQL against whichever provider it's given. A `DatabaseProviderFactory` builds the right provider from config. Because `WeaponStore` and the providers have no dependency on CounterStrikeSharp types, they are unit-testable against a real SQLite file in a separate xUnit project.

**Tech Stack:** C# / .NET 8, CounterStrikeSharp plugin, Dapper (micro-ORM), MySqlConnector (existing local DLL), Microsoft.Data.Sqlite, xUnit for tests.

---

## Why a new class name (`WeaponStore`) instead of rewriting `Database` in place

The existing `Database` class is referenced by `Core.cs` and `Utils.cs` via static calls (`Query`, `Connect`, `CreateTables`, `EscapeString`, `SQL_FetchUser_CB`, `SQL_CheckForErrors`). If we rewrote `Database.cs` directly, the project would stop compiling until every call site was migrated in the same step — a large, un-reviewable change.

Instead we introduce the new data-access type as `WeaponStore` (new files, no name clash). The old `Database` class keeps compiling and working untouched while the new layer and its tests land (Tasks 1–4). Only once `WeaponStore` is proven by tests do we swap the call sites (Task 5) and delete the old `Database` class (Task 6). The project builds green after every task.

## File Structure

**New files:**
- `Modules/Db/IDatabaseProvider.cs` — provider abstraction (connection factory + DDL).
- `Modules/Db/SqliteProvider.cs` — SQLite implementation.
- `Modules/Db/MySqlProvider.cs` — MySQL/MariaDB implementation.
- `Modules/Db/WeaponPreference.cs` — POCO mapped to the `weapons` table by Dapper.
- `Modules/Db/WeaponStore.cs` — async, parameterized CRUD over an `IDatabaseProvider`.
- `Modules/Db/DatabaseProviderFactory.cs` — builds the provider + store from `Config`.
- `tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj` — xUnit test project.
- `tests/RetakesAllocator.Tests/TempDb.cs` — test helper (temp SQLite file + initialized store).
- `tests/RetakesAllocator.Tests/SmokeTest.cs` — harness smoke test (deleted/replaced after Task 1 if desired).
- `tests/RetakesAllocator.Tests/SqliteProviderTests.cs` — provider tests.
- `tests/RetakesAllocator.Tests/WeaponStoreTests.cs` — store CRUD tests.
- `tests/RetakesAllocator.Tests/DatabaseProviderFactoryTests.cs` — factory selection tests.

**Modified files:**
- `RetakesAllocator.csproj` — add Dapper + Microsoft.Data.Sqlite package references.
- `Modules/Config.cs` — add `Provider` + `SqlitePath` fields, branch `IsValid()`, keep `BuildConnectionString()`.
- `Modules/Core.cs` — create `WeaponStore` via factory in `Load`, flush on `Unload`, remove old SQL callbacks.
- `Modules/Utils.cs` — async `AddPlayerToList`/`RemovePlayerFromList` + `ApplyPreferences`.
- `README.md` — document the new provider/SQLite config.

**Deleted files:**
- `Modules/Database.cs` — superseded by `WeaponStore` + providers (Task 6).

---

## Task 1: Add dependencies and stand up the test project

**Files:**
- Modify: `RetakesAllocator.csproj:16-19`
- Create: `tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj`
- Create: `tests/RetakesAllocator.Tests/SmokeTest.cs`

- [ ] **Step 1: Add the runtime NuGet packages to the plugin project**

Edit `RetakesAllocator.csproj`. The existing `<ItemGroup>` with package references is:

```xml
  <ItemGroup>
    <PackageReference Include="CounterStrikeSharp.API" Version="1.0.232" />
    <PackageReference Include="RetakesPluginShared" Version="2.0.0" />
  </ItemGroup>
```

Replace it with (adds Dapper and Microsoft.Data.Sqlite; MySqlConnector stays as the existing local `<Reference>` DLL above it):

```xml
  <ItemGroup>
    <PackageReference Include="CounterStrikeSharp.API" Version="1.0.232" />
    <PackageReference Include="RetakesPluginShared" Version="2.0.0" />
    <PackageReference Include="Dapper" Version="2.1.66" />
    <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.10" />
  </ItemGroup>
```

- [ ] **Step 2: Create the test project**

Create `tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Dapper" Version="2.1.66" />
    <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.10" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\RetakesAllocator.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Write a smoke test to prove the harness runs**

Create `tests/RetakesAllocator.Tests/SmokeTest.cs`:

```csharp
namespace RetakesAllocator.Tests;

public class SmokeTest
{
    [Fact]
    public void Harness_Runs()
    {
        Assert.True(true);
    }
}
```

- [ ] **Step 4: Restore and run the smoke test**

Run: `dotnet test tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj`
Expected: PASS — `Passed!  - Failed: 0, Passed: 1`. (First run restores Dapper, Microsoft.Data.Sqlite, xUnit, and builds the referenced plugin project. If restore fails, confirm network access for NuGet.)

- [ ] **Step 5: Confirm the plugin project still builds with the new packages**

Run: `dotnet build RetakesAllocator.csproj`
Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 6: Commit**

```bash
git add RetakesAllocator.csproj tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj tests/RetakesAllocator.Tests/SmokeTest.cs
git commit -m "build: add Dapper + Sqlite deps and xUnit test project"
```

---

## Task 2: Provider abstraction, SQLite provider, and the row POCO

**Files:**
- Create: `Modules/Db/IDatabaseProvider.cs`
- Create: `Modules/Db/WeaponPreference.cs`
- Create: `Modules/Db/SqliteProvider.cs`
- Create: `tests/RetakesAllocator.Tests/SqliteProviderTests.cs`

- [ ] **Step 1: Write the failing test for the SQLite provider**

Create `tests/RetakesAllocator.Tests/SqliteProviderTests.cs`:

```csharp
using Dapper;
using Microsoft.Data.Sqlite;
using RetakesAllocator.Modules;

namespace RetakesAllocator.Tests;

public class SqliteProviderTests
{
    [Fact]
    public async Task CreateConnection_OpensAndRunsCreateTableSql()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wa_prov_{Guid.NewGuid():N}.db");
        try
        {
            var provider = new SqliteProvider($"Data Source={path}");

            await using var conn = provider.CreateConnection();
            await conn.OpenAsync();
            await conn.ExecuteAsync(provider.CreateTableSql);

            var tableName = await conn.QuerySingleOrDefaultAsync<string>(
                "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'weapons'");

            Assert.Equal("weapons", tableName);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj --filter SqliteProviderTests`
Expected: FAIL to compile — `The type or namespace name 'SqliteProvider' could not be found`.

- [ ] **Step 3: Create the provider interface**

Create `Modules/Db/IDatabaseProvider.cs`:

```csharp
using System.Data.Common;

namespace RetakesAllocator.Modules;

/// <summary>
/// Abstracts the database engine: how to open a connection and the
/// dialect-specific DDL needed to create the schema. All CRUD is engine-agnostic
/// and lives in <see cref="WeaponStore"/>.
/// </summary>
public interface IDatabaseProvider
{
    /// <summary>Creates a new, unopened connection for the configured engine.</summary>
    DbConnection CreateConnection();

    /// <summary>Idempotent CREATE TABLE statement for the `weapons` table in this engine's dialect.</summary>
    string CreateTableSql { get; }
}
```

- [ ] **Step 4: Create the row POCO**

Create `Modules/Db/WeaponPreference.cs`:

```csharp
namespace RetakesAllocator.Modules;

/// <summary>
/// One row of the `weapons` table. Property names map to snake_case columns via
/// Dapper's MatchNamesWithUnderscores (configured in <see cref="WeaponStore"/>).
/// </summary>
public class WeaponPreference
{
    public string Auth { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int TPrimary { get; set; }
    public int CtPrimary { get; set; }
    public int TSecondary { get; set; }
    public int CtSecondary { get; set; }
    public int GiveAwp { get; set; }
}
```

- [ ] **Step 5: Create the SQLite provider**

Create `Modules/Db/SqliteProvider.cs`:

```csharp
using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace RetakesAllocator.Modules;

public class SqliteProvider : IDatabaseProvider
{
    private readonly string _connectionString;

    public SqliteProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbConnection CreateConnection() => new SqliteConnection(_connectionString);

    public string CreateTableSql =>
        "CREATE TABLE IF NOT EXISTS weapons (" +
        "id INTEGER PRIMARY KEY AUTOINCREMENT, " +
        "auth VARCHAR(128) NOT NULL UNIQUE, " +
        "name VARCHAR(128) NOT NULL, " +
        "t_primary INTEGER NOT NULL DEFAULT 0, " +
        "ct_primary INTEGER NOT NULL DEFAULT 0, " +
        "t_secondary INTEGER NOT NULL DEFAULT 0, " +
        "ct_secondary INTEGER NOT NULL DEFAULT 0, " +
        "give_awp INTEGER NOT NULL DEFAULT 0);";
}
```

- [ ] **Step 6: Run the test to verify it passes**

Run: `dotnet test tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj --filter SqliteProviderTests`
Expected: PASS — 1 passed.

- [ ] **Step 7: Commit**

```bash
git add Modules/Db/IDatabaseProvider.cs Modules/Db/WeaponPreference.cs Modules/Db/SqliteProvider.cs tests/RetakesAllocator.Tests/SqliteProviderTests.cs
git commit -m "feat(db): add provider abstraction, SQLite provider, and row POCO"
```

---

## Task 3: `WeaponStore` async CRUD over Dapper

**Files:**
- Create: `Modules/Db/WeaponStore.cs`
- Create: `tests/RetakesAllocator.Tests/TempDb.cs`
- Create: `tests/RetakesAllocator.Tests/WeaponStoreTests.cs`

- [ ] **Step 1: Create the test helper for a temp SQLite-backed store**

Create `tests/RetakesAllocator.Tests/TempDb.cs`:

```csharp
using Microsoft.Data.Sqlite;
using RetakesAllocator.Modules;

namespace RetakesAllocator.Tests;

/// <summary>
/// Creates a unique temp SQLite file, wraps it in a WeaponStore, and runs
/// InitializeAsync. Dispose deletes the file. Uses a real file (not :memory:)
/// because WeaponStore opens a fresh connection per call.
/// </summary>
internal sealed class TempDb : IDisposable
{
    public string Path { get; }
    public WeaponStore Store { get; }

    public TempDb()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"wa_store_{Guid.NewGuid():N}.db");
        var provider = new SqliteProvider($"Data Source={Path}");
        Store = new WeaponStore(provider);
        Store.InitializeAsync().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(Path)) File.Delete(Path);
    }
}
```

- [ ] **Step 2: Write the failing CRUD tests**

Create `tests/RetakesAllocator.Tests/WeaponStoreTests.cs`:

```csharp
using RetakesAllocator.Modules;

namespace RetakesAllocator.Tests;

public class WeaponStoreTests
{
    [Fact]
    public async Task GetUserAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        using var db = new TempDb();

        var result = await db.Store.GetUserAsync("STEAM_1:0:000000");

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateUserAsync_ThenGetUserAsync_ReturnsDefaultsRow()
    {
        using var db = new TempDb();

        await db.Store.CreateUserAsync("STEAM_1:0:111111", "Alice");
        var result = await db.Store.GetUserAsync("STEAM_1:0:111111");

        Assert.NotNull(result);
        Assert.Equal("STEAM_1:0:111111", result!.Auth);
        Assert.Equal("Alice", result.Name);
        Assert.Equal(0, result.TPrimary);
        Assert.Equal(0, result.CtPrimary);
        Assert.Equal(0, result.TSecondary);
        Assert.Equal(0, result.CtSecondary);
        Assert.Equal(0, result.GiveAwp);
    }

    [Fact]
    public async Task SaveUserAsync_PersistsAllPreferenceColumns()
    {
        using var db = new TempDb();
        await db.Store.CreateUserAsync("STEAM_1:0:222222", "Bob");

        var pref = new WeaponPreference
        {
            Auth = "STEAM_1:0:222222",
            TPrimary = 1,
            CtPrimary = 2,
            TSecondary = 1,
            CtSecondary = 2,
            GiveAwp = 2,
        };
        await db.Store.SaveUserAsync(pref);

        var result = await db.Store.GetUserAsync("STEAM_1:0:222222");
        Assert.NotNull(result);
        Assert.Equal(1, result!.TPrimary);
        Assert.Equal(2, result.CtPrimary);
        Assert.Equal(1, result.TSecondary);
        Assert.Equal(2, result.CtSecondary);
        Assert.Equal(2, result.GiveAwp);
    }

    [Fact]
    public async Task CreateUserAsync_EscapesNameWithQuote_NoInjection()
    {
        using var db = new TempDb();

        // A name that would break interpolated SQL; parameterization must handle it.
        await db.Store.CreateUserAsync("STEAM_1:0:333333", "Robert'); DROP TABLE weapons;--");

        var result = await db.Store.GetUserAsync("STEAM_1:0:333333");
        Assert.NotNull(result);
        Assert.Equal("Robert'); DROP TABLE weapons;--", result!.Name);
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj --filter WeaponStoreTests`
Expected: FAIL to compile — `The type or namespace name 'WeaponStore' could not be found`.

- [ ] **Step 4: Implement `WeaponStore`**

Create `Modules/Db/WeaponStore.cs`:

```csharp
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
            "SELECT auth, name, t_primary, ct_primary, t_secondary, ct_secondary, give_awp " +
            "FROM weapons WHERE auth = @auth",
            new { auth });
    }

    /// <summary>Inserts a new user row with default (zeroed) preferences.</summary>
    public async Task CreateUserAsync(string auth, string name)
    {
        await using var conn = _provider.CreateConnection();
        await conn.OpenAsync();
        await conn.ExecuteAsync(
            "INSERT INTO weapons (auth, name) VALUES (@auth, @name)",
            new { auth, name });
    }

    /// <summary>Persists the four weapon preference columns plus give_awp for an existing user.</summary>
    public async Task SaveUserAsync(WeaponPreference pref)
    {
        await using var conn = _provider.CreateConnection();
        await conn.OpenAsync();
        await conn.ExecuteAsync(
            "UPDATE weapons SET " +
            "t_primary = @TPrimary, ct_primary = @CtPrimary, " +
            "t_secondary = @TSecondary, ct_secondary = @CtSecondary, " +
            "give_awp = @GiveAwp WHERE auth = @Auth",
            pref);
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj --filter WeaponStoreTests`
Expected: PASS — 4 passed. (The injection test proves parameterization replaced the old interpolated SQL.)

- [ ] **Step 6: Commit**

```bash
git add Modules/Db/WeaponStore.cs tests/RetakesAllocator.Tests/TempDb.cs tests/RetakesAllocator.Tests/WeaponStoreTests.cs
git commit -m "feat(db): add WeaponStore async parameterized CRUD"
```

---

## Task 4: MySQL provider, config fields, and the provider factory

**Files:**
- Create: `Modules/Db/MySqlProvider.cs`
- Create: `Modules/Db/DatabaseProviderFactory.cs`
- Modify: `Modules/Config.cs:29-56`
- Create: `tests/RetakesAllocator.Tests/DatabaseProviderFactoryTests.cs`

- [ ] **Step 1: Write the failing factory + config tests**

Create `tests/RetakesAllocator.Tests/DatabaseProviderFactoryTests.cs`:

```csharp
using RetakesAllocator.Modules;

namespace RetakesAllocator.Tests;

public class DatabaseProviderFactoryTests
{
    [Fact]
    public void Create_ReturnsSqliteProvider_WhenProviderIsSqlite()
    {
        var cfg = new ConnectionConfig { Provider = "sqlite", SqlitePath = "weapons.db" };

        var provider = DatabaseProviderFactory.Create(cfg, baseDirectory: "/tmp");

        Assert.IsType<SqliteProvider>(provider);
        Assert.Contains("weapons", provider.CreateTableSql);
        Assert.Contains("AUTOINCREMENT", provider.CreateTableSql);
    }

    [Fact]
    public void Create_ReturnsMySqlProvider_WhenProviderIsMysql()
    {
        var cfg = new ConnectionConfig
        {
            Provider = "mysql",
            Host = "localhost",
            Database = "retakes",
            User = "root",
            Password = "secret",
            Port = 3306,
        };

        var provider = DatabaseProviderFactory.Create(cfg, baseDirectory: "/tmp");

        Assert.IsType<MySqlProvider>(provider);
        Assert.Contains("AUTO_INCREMENT", provider.CreateTableSql);
        Assert.Contains("InnoDB", provider.CreateTableSql);
    }

    [Fact]
    public void IsValid_RequiresOnlyPath_ForSqlite()
    {
        var cfg = new ConnectionConfig { Provider = "sqlite", SqlitePath = "weapons.db" };
        Assert.True(cfg.IsValid());
    }

    [Fact]
    public void IsValid_FailsForSqlite_WhenPathEmpty()
    {
        var cfg = new ConnectionConfig { Provider = "sqlite", SqlitePath = "" };
        Assert.False(cfg.IsValid());
    }

    [Fact]
    public void IsValid_RequiresFullCredentials_ForMysql()
    {
        var valid = new ConnectionConfig
        {
            Provider = "mysql",
            Host = "localhost",
            Database = "retakes",
            User = "root",
            Password = "secret",
            Port = 3306,
        };
        var missingHost = new ConnectionConfig
        {
            Provider = "mysql",
            Host = "",
            Database = "retakes",
            User = "root",
            Password = "secret",
            Port = 3306,
        };

        Assert.True(valid.IsValid());
        Assert.False(missingHost.IsValid());
    }
}
```

Note: `IsValid()` currently lives on `Config` and reads `DbConnection`. This task moves the validation onto `ConnectionConfig` itself (`cfg.IsValid()`) so it can be tested without constructing the whole `Config`/plugin. `Config.IsValid()` will delegate to `DbConnection.IsValid()`.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj --filter DatabaseProviderFactoryTests`
Expected: FAIL to compile — `'ConnectionConfig' does not contain a definition for 'Provider'` and `The name 'DatabaseProviderFactory' does not exist`.

- [ ] **Step 3: Extend `ConnectionConfig` with provider selection + validation**

In `Modules/Config.cs`, replace the existing `ConnectionConfig` class (currently lines 49-56):

```csharp
public class ConnectionConfig
{
    public string Host { get; init; } = string.Empty;
    public string Database { get; init; } = string.Empty;
    public string User { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public uint Port { get; init; } = 3306;
}
```

with:

```csharp
public class ConnectionConfig
{
    /// <summary>"mysql" (default, MySQL/MariaDB) or "sqlite".</summary>
    public string Provider { get; init; } = "mysql";

    public string Host { get; init; } = string.Empty;
    public string Database { get; init; } = string.Empty;
    public string User { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public uint Port { get; init; } = 3306;

    /// <summary>SQLite database file (used only when Provider == "sqlite"). Relative paths resolve against the plugin's module directory.</summary>
    public string SqlitePath { get; init; } = "weapons.db";

    public bool IsSqlite => string.Equals(Provider, "sqlite", StringComparison.OrdinalIgnoreCase);

    public bool IsValid()
    {
        if (IsSqlite)
        {
            return SqlitePath != string.Empty;
        }

        return Database != string.Empty
            && Host != string.Empty
            && User != string.Empty
            && Password != string.Empty
            && 0 < Port && Port < 65535;
    }
}
```

- [ ] **Step 4: Delegate `Config.IsValid()` to the connection config**

In `Modules/Config.cs`, replace the existing `Config.IsValid()` method (currently lines 29-32):

```csharp
    public bool IsValid()
    {
        return DbConnection.Database != string.Empty && DbConnection.Host != string.Empty && DbConnection.User != string.Empty && DbConnection.Password != string.Empty && 0 < DbConnection.Port && DbConnection.Port < 65535;
    }
```

with:

```csharp
    public bool IsValid()
    {
        return DbConnection.IsValid();
    }
```

Leave `Config.BuildConnectionString()` (lines 34-46) unchanged — the MySQL provider reuses it.

- [ ] **Step 5: Create the MySQL provider**

Create `Modules/Db/MySqlProvider.cs`:

```csharp
using System.Data.Common;
using MySqlConnector;

namespace RetakesAllocator.Modules;

public class MySqlProvider : IDatabaseProvider
{
    private readonly string _connectionString;

    public MySqlProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbConnection CreateConnection() => new MySqlConnection(_connectionString);

    public string CreateTableSql =>
        "CREATE TABLE IF NOT EXISTS `weapons` ( " +
        "`id` INT NOT NULL AUTO_INCREMENT, " +
        "`auth` VARCHAR(128) NOT NULL, " +
        "`name` VARCHAR(128) NOT NULL, " +
        "`t_primary` INT NOT NULL DEFAULT 0, " +
        "`ct_primary` INT NOT NULL DEFAULT 0, " +
        "`t_secondary` INT NOT NULL DEFAULT 0, " +
        "`ct_secondary` INT NOT NULL DEFAULT 0, " +
        "`give_awp` INT NOT NULL DEFAULT 0, " +
        "PRIMARY KEY (`id`), UNIQUE (`auth`)) ENGINE = InnoDB;";
}
```

- [ ] **Step 6: Create the provider factory**

Create `Modules/Db/DatabaseProviderFactory.cs`:

```csharp
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

        return new MySqlProvider(config.BuildConnectionStringFor(config));
    }
}
```

Note: `MySqlProvider` needs the MySQL connection string. `Config.BuildConnectionString()` is an instance method on `Config`, but the factory only receives a `ConnectionConfig`. Add a static helper on `ConnectionConfig` so the factory can build it from the connection config alone.

- [ ] **Step 7: Add the MySQL connection-string builder to `ConnectionConfig`**

In `Modules/Config.cs`, add this method inside the `ConnectionConfig` class (after `IsValid()`):

```csharp
    public string BuildConnectionStringFor(ConnectionConfig config)
    {
        var builder = new MySqlConnector.MySqlConnectionStringBuilder
        {
            Database = config.Database,
            UserID = config.User,
            Password = config.Password,
            Server = config.Host,
            Port = config.Port,
        };

        return builder.ConnectionString;
    }
```

`Modules/Config.cs` already has `using MySqlConnector;` at the top (line 3), so `MySqlConnectionStringBuilder` resolves; the fully-qualified name above also works regardless.

- [ ] **Step 8: Run the factory tests to verify they pass**

Run: `dotnet test tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj --filter DatabaseProviderFactoryTests`
Expected: PASS — 5 passed.

- [ ] **Step 9: Run the full test suite and build the plugin**

Run: `dotnet test tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj`
Expected: PASS — all tests green (smoke + provider + store + factory).

Run: `dotnet build RetakesAllocator.csproj`
Expected: `Build succeeded.` (old `Database` class still present and compiling; nothing wired to the new layer yet.)

- [ ] **Step 10: Commit**

```bash
git add Modules/Db/MySqlProvider.cs Modules/Db/DatabaseProviderFactory.cs Modules/Config.cs tests/RetakesAllocator.Tests/DatabaseProviderFactoryTests.cs
git commit -m "feat(db): add MySQL provider, sqlite config fields, and provider factory"
```

---

## Task 5: Swap the call sites in `Core` and `Utils` to `WeaponStore`

This task migrates the two call sites together so the project keeps compiling. `Core.cs` and `Utils.cs` currently depend on the old `Database` statics (`Connect`, `Query`, `CreateTables`, `EscapeString`, `SQL_FetchUser_CB`, `SQL_CheckForErrors`); after this task they depend only on `Core.Store` (a `WeaponStore`).

**Files:**
- Modify: `Modules/Core.cs:6,13,37,57,94-142` (usings, field, Load, remove SQL callbacks)
- Modify: `Modules/Utils.cs:1-8,54-92` (usings, both player methods, add ApplyPreferences)

- [ ] **Step 1: Replace the `Db`/`Database` field and connect logic in `Core.cs`**

In `Modules/Core.cs`, remove the MySqlConnector using (line 6):

```csharp
using MySqlConnector;
```

and remove the old Database static import (line 13):

```csharp
using static RetakesAllocator.Modules.Database;
```

Change the `Db` field declaration (line 37) from:

```csharp
    public static Database Db = null!;
```

to:

```csharp
    public static WeaponStore Store = null!;
```

- [ ] **Step 2: Rewrite the connect call inside `Load`**

In `Modules/Core.cs`, replace this line inside `Load` (currently line 57):

```csharp
        Connect(SQL_ConnectCallback);
```

with:

```csharp
        InitializeDatabase();
```

- [ ] **Step 3: Replace the old SQL callbacks with `InitializeDatabase`**

In `Modules/Core.cs`, delete the entire `SQL_ConnectCallback` method (currently lines 94-107) and the entire `SQL_FetchUser_CB` method (currently lines 109-142), and replace them with this single method:

```csharp
    private static void InitializeDatabase()
    {
        try
        {
            var provider = DatabaseProviderFactory.Create(Config.DbConnection, Plugin.ModuleDirectory);
            Store = new WeaponStore(provider);
            Store.InitializeAsync().GetAwaiter().GetResult();
            PrintToServer("Connected to database");
        }
        catch (Exception e)
        {
            ThrowError($"Failed to connect to database: {e.Message}");
        }
    }
```

(Blocking with `GetAwaiter().GetResult()` is intentional and acceptable here: it runs once at plugin load, before any match starts, and the plugin must not continue if the schema can't be created.)

- [ ] **Step 4: Update the `Unload` flush call in `Core.cs`**

In `Modules/Core.cs`, inside `Unload`, replace this line (currently line 75):

```csharp
        Utilities.GetPlayers().ForEach(RemovePlayerFromList);
```

with (flush synchronously so saves complete before the plugin unloads):

```csharp
        Utilities.GetPlayers().ForEach(p => RemovePlayerFromList(p, flush: true));
```

- [ ] **Step 5: Update `Utils.cs` usings**

In `Modules/Utils.cs`, replace the old Database static import (line 5):

```csharp
using static RetakesAllocator.Modules.Database;
```

with the Weapons import needed for `Allocator` and the `GiveAwp` enum:

```csharp
using RetakesAllocator.Modules.Weapons;
```

Also add `using CounterStrikeSharp.API;` is already present (line 1) — `Server.NextFrame` lives there. No change needed for that.

- [ ] **Step 6: Rewrite `AddPlayerToList` in `Utils.cs`**

In `Modules/Utils.cs`, replace the entire `AddPlayerToList` method (currently lines 54-73):

```csharp
    public static void AddPlayerToList(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || player.IsBot)
        {
            return;
        }

        if(FindPlayer(player) != null!)
        {
            return;
        }

        var playerObj = new Player(player);

        Players.Add(playerObj);

        var index = Players.IndexOf(playerObj);

        Query(SQL_FetchUser_CB, $"SELECT * FROM `weapons` WHERE `auth` = '{playerObj.GetSteamId2()}'", index);
    }
```

with:

```csharp
    public static void AddPlayerToList(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || player.IsBot)
        {
            return;
        }

        if (FindPlayer(player) != null!)
        {
            return;
        }

        var playerObj = new Player(player);
        Players.Add(playerObj);

        // Read CounterStrikeSharp-bound values on the game thread before going async.
        var auth = playerObj.GetSteamId2();
        var name = playerObj.GetName();

        Task.Run(async () =>
        {
            try
            {
                var pref = await Store.GetUserAsync(auth);

                if (pref == null)
                {
                    await Store.CreateUserAsync(auth, name);
                }
                else
                {
                    // Apply to the player on the game thread.
                    Server.NextFrame(() => ApplyPreferences(playerObj, pref));
                }
            }
            catch (Exception e)
            {
                Server.NextFrame(() =>
                    PrintToServer($"Database error loading {auth}: {e.Message}", ConsoleColor.Red));
            }
        });
    }

    private static void ApplyPreferences(Player playerObj, WeaponPreference pref)
    {
        var allocator = playerObj.WeaponsAllocator;

        allocator.PrimaryWeaponT = pref.TPrimary > Allocator.PrimaryT.Count ? 0 : pref.TPrimary;
        allocator.PrimaryWeaponCt = pref.CtPrimary > Allocator.PrimaryCt.Count ? 0 : pref.CtPrimary;
        allocator.SecondaryWeaponT = pref.TSecondary > Allocator.PistolsT.Count ? 0 : pref.TSecondary;
        allocator.SecondaryWeaponCt = pref.CtSecondary > Allocator.PistolsCT.Count ? 0 : pref.CtSecondary;
        allocator.GiveAwp = (GiveAwp)pref.GiveAwp;
    }
```

(The `> .Count` clamps preserve the exact bounds logic from the old `SQL_FetchUser_CB`. `Allocator.PrimaryT`/`PrimaryCt`/`PistolsT`/`PistolsCT` and the `GiveAwp` enum come from `RetakesAllocator.Modules.Weapons`, imported in Step 5.)

- [ ] **Step 7: Rewrite `RemovePlayerFromList` in `Utils.cs`**

In `Modules/Utils.cs`, replace the entire `RemovePlayerFromList` method (currently lines 75-92):

```csharp
    public static void RemovePlayerFromList(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || player.IsBot)
        {
            return;
        }

        var playerObj = FindPlayer(player);

        if (playerObj == null!)
        {
            return;
        }

        Query(SQL_CheckForErrors, $"UPDATE `weapons` SET `t_primary` = '{playerObj.WeaponsAllocator.PrimaryWeaponT}', `ct_primary` = '{playerObj.WeaponsAllocator.PrimaryWeaponCt}', `t_secondary` = '{playerObj.WeaponsAllocator.SecondaryWeaponT}', `ct_secondary` = '{playerObj.WeaponsAllocator.SecondaryWeaponCt}' ,`give_awp` = '{(int)playerObj.WeaponsAllocator.GiveAwp}' WHERE `auth` = '{playerObj.GetSteamId2()}'");

        Players.Remove(playerObj);
    }
```

with:

```csharp
    public static void RemovePlayerFromList(CCSPlayerController player, bool flush = false)
    {
        if (player == null || !player.IsValid || player.IsBot)
        {
            return;
        }

        var playerObj = FindPlayer(player);

        if (playerObj == null!)
        {
            return;
        }

        // Snapshot all values on the game thread before going async.
        var pref = new WeaponPreference
        {
            Auth = playerObj.GetSteamId2(),
            TPrimary = playerObj.WeaponsAllocator.PrimaryWeaponT,
            CtPrimary = playerObj.WeaponsAllocator.PrimaryWeaponCt,
            TSecondary = playerObj.WeaponsAllocator.SecondaryWeaponT,
            CtSecondary = playerObj.WeaponsAllocator.SecondaryWeaponCt,
            GiveAwp = (int)playerObj.WeaponsAllocator.GiveAwp,
        };

        Players.Remove(playerObj);

        if (flush)
        {
            // Plugin unload path: block so the save completes before teardown.
            try
            {
                Store.SaveUserAsync(pref).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                PrintToServer($"Database error saving {pref.Auth}: {e.Message}", ConsoleColor.Red);
            }
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                await Store.SaveUserAsync(pref);
            }
            catch (Exception e)
            {
                Server.NextFrame(() =>
                    PrintToServer($"Database error saving {pref.Auth}: {e.Message}", ConsoleColor.Red));
            }
        });
    }
```

- [ ] **Step 8: Build the plugin**

Run: `dotnet build RetakesAllocator.csproj`
Expected: `Build succeeded.` 0 errors. (If you see `Query`/`Connect`/`EscapeString`/`SQL_FetchUser_CB` errors, a reference to the old API was missed — they are all removed in this task; the old `Database` class file itself is still present and unreferenced, which is fine until Task 6.)

- [ ] **Step 9: Run the full test suite (regression check)**

Run: `dotnet test tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj`
Expected: PASS — all tests still green.

- [ ] **Step 10: Commit**

```bash
git add Modules/Core.cs Modules/Utils.cs
git commit -m "refactor(db): wire Core and Utils to async WeaponStore"
```

---

## Task 6: Delete the old `Database` class

**Files:**
- Delete: `Modules/Database.cs`

- [ ] **Step 1: Confirm nothing references the old `Database` class anymore**

Run: `git grep -nE "Database\.|new Database|SQL_FetchUser_CB|SQL_CheckForErrors|EscapeString|CreateTables|\bConnect\(" -- "Modules/*.cs"`
Expected: no matches inside `Modules/` other than (possibly) the `Modules/Database.cs` file itself. If any other file matches, fix that reference before deleting (it should already be clean after Task 5).

Note: `Config.cs` still contains `BuildConnectionString()` and `BuildConnectionStringFor()` — those are connection-string helpers, not references to the `Database` class, and stay.

- [ ] **Step 2: Delete the old file**

```bash
git rm Modules/Database.cs
```

- [ ] **Step 3: Build the plugin**

Run: `dotnet build RetakesAllocator.csproj`
Expected: `Build succeeded.` 0 errors.

- [ ] **Step 4: Run the full test suite**

Run: `dotnet test tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj`
Expected: PASS — all tests green.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(db): remove obsolete Database class"
```

---

## Task 7: Document the provider/SQLite configuration

**Files:**
- Modify: `README.md:15-39`

- [ ] **Step 1: Update the example config block in the README**

In `README.md`, replace the `### Example Config` fenced block (currently lines 17-39) so it documents the new `Provider` and `SqlitePath` fields:

````markdown
### Example Config

The `Provider` field selects the database engine: `"mysql"` (MySQL/MariaDB, the default)
or `"sqlite"` (a local file, no server required).

**MySQL / MariaDB:**

```
{
  "DbConnection": {
    "Provider": "mysql",
    "Host": "<HOST>",
    "Database": "<DB>",
    "User": "<USER>",
    "Password": "<PASSWORD>",
    "Port": 3306,
    "SqlitePath": "weapons.db"
  },
  "Prefix": {
    "Prefix": " [Retakes]",
    "PrefixCon": "[Retakes]"
  },
  "GiveArmor": true,
  "triggerWords": [
    "guns",
    "gun",
    "weapon",
    "weapons"
  ]
}
```

**SQLite** (only `Provider` and `SqlitePath` matter; the path is relative to the
plugin's directory unless absolute):

```
{
  "DbConnection": {
    "Provider": "sqlite",
    "SqlitePath": "weapons.db"
  }
}
```
````

- [ ] **Step 2: Commit**

```bash
git add README.md
git commit -m "docs: document database provider and SQLite config"
```

---

## Task 8: Final full-suite verification

**Files:** none (verification only)

- [ ] **Step 1: Clean build of the plugin**

Run: `dotnet build RetakesAllocator.csproj`
Expected: `Build succeeded.` 0 warnings related to the DB layer, 0 errors.

- [ ] **Step 2: Full test run**

Run: `dotnet test tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj`
Expected: PASS — smoke (1) + SqliteProvider (1) + WeaponStore (4) + DatabaseProviderFactory (5) = 11 passed, 0 failed.

- [ ] **Step 3: (Optional, requires a running CS2 server) Manual in-game smoke test**

1. Build and deploy the plugin to a CS2 server with the retakes plugin.
2. Set `DbConnection.Provider` to `"sqlite"` in `configs/retakes_allocator.json` (fastest to test — no DB server needed).
3. Start the server, connect, change a weapon preference via the `guns` menu, disconnect, reconnect.
   Expected: the preference persists across reconnect; server console prints `Connected to database` on load and no `Database error ...` lines.
4. Repeat with `Provider` set to `"mysql"` against a real MySQL instance to confirm the MySQL path.

---

## Self-Review

**1. Spec coverage** — "rewrite all the db logic to use database provider," with the three clarified decisions:
- *Dapper as the provider* → Task 3 (`WeaponStore` uses `Dapper` `ExecuteAsync`/`QuerySingleOrDefaultAsync`). ✓
- *MySQL + SQLite backends* → `MySqlProvider` (Task 4) + `SqliteProvider` (Task 2), selected by `DatabaseProviderFactory` (Task 4). ✓
- *Async/await + parameterized* → all `WeaponStore` methods are `async Task` with parameterized SQL; injection test in Task 3 proves it; call sites converted to `Task.Run` + `Server.NextFrame` marshaling (Task 5). ✓
- *All* old DB logic removed → old `Database` class deleted (Task 6); both call sites migrated (Task 5); config + docs updated (Tasks 4, 7). ✓

**2. Placeholder scan** — no TBD/TODO/"handle edge cases"; every code step contains full code; commands have explicit expected output. ✓

**3. Type consistency** — verified across tasks:
- `IDatabaseProvider.CreateConnection()` returns `DbConnection`; used with `await conn.OpenAsync()` and Dapper async methods. ✓
- `WeaponPreference` properties (`Auth`, `Name`, `TPrimary`, `CtPrimary`, `TSecondary`, `CtSecondary`, `GiveAwp`) are identical in the POCO (Task 2), the `WeaponStore` SQL parameters (Task 3), and both call sites (Task 5). ✓
- `WeaponStore` method names — `InitializeAsync`, `GetUserAsync`, `CreateUserAsync`, `SaveUserAsync` — match between definition (Task 3), `Core.InitializeDatabase` (Task 5), and `Utils` (Task 5). ✓
- `DatabaseProviderFactory.Create(ConnectionConfig, string)` signature matches its test (Task 4, `baseDirectory:`) and its caller `Core.InitializeDatabase` (`Plugin.ModuleDirectory`). ✓
- `ConnectionConfig.IsValid()` / `IsSqlite` / `SqlitePath` / `Provider` / `BuildConnectionStringFor` defined in Task 4 and used by the factory and `Config.IsValid()`. ✓
- `RemovePlayerFromList(player, flush: true)` overload defined in Task 5 Step 7 matches the `Unload` caller in Task 5 Step 4. ✓
