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
        """
        CREATE TABLE IF NOT EXISTS `weapons` (
            `id` INT NOT NULL AUTO_INCREMENT,
            `auth` VARCHAR(128) NOT NULL,
            `name` VARCHAR(128) NOT NULL,
            `t_primary` INT NOT NULL DEFAULT 0,
            `ct_primary` INT NOT NULL DEFAULT 0,
            `t_secondary` INT NOT NULL DEFAULT 0,
            `ct_secondary` INT NOT NULL DEFAULT 0,
            `give_awp` INT NOT NULL DEFAULT 0,
            PRIMARY KEY (`id`),
            UNIQUE (`auth`)
        ) ENGINE = InnoDB;
        """;
}
