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
