using System.Data.Common;

namespace RetakesAllocator.Modules;

/// <summary>
/// Abstracts the database engine: how to open a connection and the
/// dialect-specific SQL needed to create and upsert into the schema. All CRUD is
/// engine-agnostic and lives in <see cref="WeaponStore"/>.
/// </summary>
public interface IDatabaseProvider
{
    /// <summary>Creates a new, unopened connection for the configured engine.</summary>
    DbConnection CreateConnection();

    /// <summary>Idempotent CREATE TABLE statement for the `weapons` table in this engine's dialect.</summary>
    string CreateTableSql { get; }

    /// <summary>Inserts a user row, doing nothing if one already exists for that auth.</summary>
    string InsertUserSql { get; }

    /// <summary>
    /// Writes preferences for an auth, inserting the row if it is missing. An
    /// UPDATE-only statement silently dropped preferences whenever the join-time
    /// INSERT had failed (database briefly unreachable, row deleted, …).
    /// </summary>
    string SaveUserSql { get; }
}
