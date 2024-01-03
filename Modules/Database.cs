// Not in use but... **DO NOT REMOVE**
using System.Linq;
using MySqlConnector;

using static RetakesAllocator.Modules.Utils;

namespace RetakesAllocator.Modules;

public class Database
{
    private static string _connectionString = string.Empty;

    public delegate void ConnectCallback(string connectionString, Exception exception, dynamic data);
    public delegate void QueryCallback(MySqlDataReader reader, Exception exception, dynamic data);

    public Database(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static void Connect(ConnectCallback callback, dynamic data = null!)
    {
        if (Core.Config == null!)
        {
            ThrowError("Config cannot be null.");
            return;
        }

        if (!Core.Config.IsValid())
        {
            ThrowError("ConnectionConfig is invalid.");
            return;
        }

        _connectionString = Core.Config.BuildConnectionString();

        try
        {
            var connection = new MySqlConnection(_connectionString);

            connection.Open();

            callback(_connectionString, null!, data);

            connection.Close();
        }
        catch (Exception e)
        {
            callback(null!, e, data);
        }
    }

    public static void CreateTables()
    {
        const string query = "CREATE TABLE IF NOT EXISTS `weapons` ( `id` INT NOT NULL AUTO_INCREMENT , `auth` VARCHAR(128) NOT NULL , `name` VARCHAR(128) NOT NULL , `t_primary` INT NOT NULL , `ct_primary` INT NOT NULL , `secondary` INT NOT NULL, `give_awp` INT NOT NULL , PRIMARY KEY (`id`), UNIQUE (`auth`)) ENGINE = InnoDB;";

        Query(SQL_CheckForErrors, query);
    }

    public static string EscapeString(string buffer)
    {
        return MySqlHelper.EscapeString(buffer);
    }

    public static void Query(QueryCallback callback, string query, dynamic data = null!)
    {
        try 
        {
            if (string.IsNullOrEmpty(query))
            {
                ThrowError("Query cannot be null or empty.");
            }

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            using(var command = new MySqlCommand(query, connection))
            {
                using(var reader = command.ExecuteReader())
                {
                    callback(reader, null!, data);
                }
            }

            connection.Close();
        }
        catch (Exception e)
        {
            callback(null!, e, data);
        }
    }

    public static void SQL_CheckForErrors(MySqlDataReader reader, Exception exception, dynamic data)
    {
        if (exception != null!)
        {
            ThrowError($"Database error, {exception.Message}");
        }
    } 
}
