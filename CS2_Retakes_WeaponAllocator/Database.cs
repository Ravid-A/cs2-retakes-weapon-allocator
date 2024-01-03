using System.Linq;
using MySqlConnector;

using static WeaponsAllocator.Core;
using static WeaponsAllocator.Functions;

namespace WeaponsAllocator;

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
        if(config == null!)
        {
            ThrowError("Config cannot be null.");
            return;
        }

        if(!config.IsValid())
        {
            ThrowError("ConnectionConfig is invalid.");
            return;
        }

        _connectionString = config.BuildConnectionString();

        try
        {
            MySqlConnection connection = new MySqlConnection(_connectionString);

            connection.Open();

            callback(_connectionString, null!, data);

            connection.Close();
        }
        catch (Exception e)
        {
            callback(null!, e, data);
        }
    }

    public void CreateTables()
    {
        string query = "CREATE TABLE IF NOT EXISTS `weapons` ( `id` INT NOT NULL AUTO_INCREMENT , `auth` VARCHAR(128) NOT NULL , `name` VARCHAR(128) NOT NULL , `t_primary` INT NOT NULL , `ct_primary` INT NOT NULL , `secondary` INT NOT NULL, `give_awp` INT NOT NULL , PRIMARY KEY (`id`), UNIQUE (`auth`)) ENGINE = InnoDB;";

        Query(SQL_CheckForErrors, query);
    }

    public string EscapeString(string buffer)
    {
        return MySqlHelper.EscapeString(buffer);
    }

    public void Query(QueryCallback callback, string query, dynamic data = null!)
    {
        try 
        {
            if (string.IsNullOrEmpty(query))
            {
                ThrowError("Query cannot be null or empty.");
            }

            using(MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                using(MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using(MySqlDataReader reader = command.ExecuteReader())
                    {
                        callback(reader, null!, data);
                    }
                }

                connection.Close();
            }
        }
        catch (Exception e)
        {
            callback(null!, e, data);
        }
    }

    public static void SQL_CheckForErrors(MySqlDataReader reader, Exception exception, dynamic data)
    {
        if(exception != null!)
        {
            ThrowError($"Databse error, {exception.Message}");
            return;
        }
    } 
}