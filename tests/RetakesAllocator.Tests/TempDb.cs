using Xunit;
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
