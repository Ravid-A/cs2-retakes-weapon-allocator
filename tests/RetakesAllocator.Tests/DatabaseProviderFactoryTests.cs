using Xunit;
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
