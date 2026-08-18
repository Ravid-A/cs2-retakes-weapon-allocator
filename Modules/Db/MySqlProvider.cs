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
            `t_pistol_round` INT NOT NULL DEFAULT 0,
            `ct_pistol_round` INT NOT NULL DEFAULT 0,
            `give_awp` INT NOT NULL DEFAULT 0,
            PRIMARY KEY (`id`),
            UNIQUE (`auth`)
        ) ENGINE = InnoDB;
        """;

    public string InsertUserSql =>
        """
        INSERT IGNORE INTO `weapons` (`auth`, `name`)
        VALUES (@auth, @name)
        """;

    public string SaveUserSql =>
        """
        INSERT INTO `weapons` (`auth`, `name`, `t_primary`, `ct_primary`, `t_secondary`, `ct_secondary`, `t_pistol_round`, `ct_pistol_round`, `give_awp`)
        VALUES (@Auth, @Name, @TPrimary, @CtPrimary, @TSecondary, @CtSecondary, @TPistolRound, @CtPistolRound, @GiveAwp)
        ON DUPLICATE KEY UPDATE
            `t_primary` = VALUES(`t_primary`),
            `ct_primary` = VALUES(`ct_primary`),
            `t_secondary` = VALUES(`t_secondary`),
            `ct_secondary` = VALUES(`ct_secondary`),
            `t_pistol_round` = VALUES(`t_pistol_round`),
            `ct_pistol_round` = VALUES(`ct_pistol_round`),
            `give_awp` = VALUES(`give_awp`)
        """;
}
