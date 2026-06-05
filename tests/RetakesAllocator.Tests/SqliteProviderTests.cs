using Dapper;
using Microsoft.Data.Sqlite;
using RetakesAllocator.Modules;
using Xunit;

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
