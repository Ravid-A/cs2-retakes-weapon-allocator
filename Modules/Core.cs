using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using MySqlConnector;
using static RetakesAllocator.Modules.Database;
using static RetakesAllocator.Modules.Utils;
using static RetakesAllocator.Modules.Configs;
using static RetakesAllocator.Modules.Handlers.Commands;
using static RetakesAllocator.Modules.Handlers.Events;
using static RetakesAllocator.Modules.Handlers.Listeners;
using RetakesAllocator.Modules.Models;
using RetakesAllocator.Modules.Weapons;

namespace RetakesAllocator.Modules;

[MinimumApiVersion(202)]
public class Core : BasePlugin
{
    public static Core Plugin = null!;

    public override string ModuleName => "[Retakes] Weapons Allocator";
    public override string ModuleVersion => "1.1.5";
    public override string ModuleAuthor => "Ravid & B3none";
    public override string ModuleDescription => "Weapons Allocator plugin for retakes";

    public static Config Config = null!;

    public static Database Db = null!;
    public static List<Player> Players = new();
    public static int RoundsCounter = 0;

    public override void Load(bool hotReload)
    {
        Plugin = this;

        LoadConfigs();

        Connect(SQL_ConnectCallback);

        RegisterCommands();
        RegisterEvents();
        RegisterListeners();

        if (hotReload)
        {
            Utilities.GetPlayers().ForEach(AddPlayerToList);
        }
    }

    public override void Unload(bool hotReload)
    {
        UnRegisterCommands();

        Utilities.GetPlayers().ForEach(RemovePlayerFromList);
    }

    public static CCSGameRules GetGameRules()
    {
        var gameRulesEntities = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
        var gameRules = gameRulesEntities.First().GameRules;

        if(gameRules == null!)
        {
            ThrowError("Failed to get game rules");
            return null!;
        }

        return gameRules;
    }

    private static void SQL_ConnectCallback(string connectionString, Exception exception, dynamic data)
    {
        if (connectionString == null!)
        {
            ThrowError($"Failed to connect to database: {exception.Message}");
            return;
        }

        Db = new Database(connectionString);

        PrintToServer("Connected to database");

        CreateTables();
    }

    public static void SQL_FetchUser_CB(MySqlDataReader reader, Exception exception, dynamic data)
    {
        if (exception != null!)
        {
            ThrowError($"Database error, {exception.Message}");
            return;
        }

        if (reader.HasRows)
        {
            while(reader.Read())
            {
                var tPrimary = reader.GetInt32("t_primary");
                var ctPrimary = reader.GetInt32("ct_primary");
                var secondary = reader.GetInt32("secondary");
                var giveAwp = (GiveAwp)reader.GetInt32("give_awp");

                Player player = Players[data];

                player.WeaponsAllocator.PrimaryWeaponT = tPrimary;
                player.WeaponsAllocator.PrimaryWeaponCt = ctPrimary;
                player.WeaponsAllocator.SecondaryWeapon = secondary;
                player.WeaponsAllocator.GiveAwp = giveAwp;
            }
        }
        else
        {
            Player player = Players[data];

            Query(SQL_CheckForErrors, $"INSERT INTO `weapons` (`auth`, `name`, `t_primary`, `ct_primary`, `secondary`, `give_awp`) VALUES ('{player.GetSteamId2()}', '{EscapeString(player.GetName())}' , '0', '0', '0', '0')");
        }
    }

    public static void LoadConfigs(bool fullReload = true)
    {
        CreateConfigsDirectory();

        if(fullReload)
        {
            Config = LoadConfig();

            if (!Config.IsValid())
            {
                ThrowError("Invalid config, please check your config file.");
                return;
            }
        }

        Weapons.Config.LoadConfig();

        PrintToServer("Configs loaded");
    }
}
