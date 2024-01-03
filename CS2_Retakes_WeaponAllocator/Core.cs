using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

using MySqlConnector;

using static WeaponsAllocator.CommandsHandlers;
using static WeaponsAllocator.EventsHandlers;
using static WeaponsAllocator.ListenersHandlers;
using static WeaponsAllocator.Functions;
using static WeaponsAllocator.Configs;
using static WeaponsAllocator.Database;

using Weapons;

namespace WeaponsAllocator;

public enum Site
{
    A,
    B
}

public class Core : BasePlugin
{
    public static Core _plugin = null!;

    public override string ModuleName => "[Retakes] Weapons Allocator";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Ravid";
    public override string ModuleDescription => "Weapons Allocator plugin for retakes";

    private static CCSGameRules? _gameRules = null;
    private static void SetGameRules()
    {
        var gameRulesEntities = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");

        if (gameRulesEntities.Any())
        {
            _gameRules = gameRulesEntities.First().GameRules!;
        }
    }

    private static bool WarmupRunning
    {
        get
        {
            if (_gameRules is null)
                SetGameRules();

            return _gameRules is not null && _gameRules.WarmupPeriod;
        }
    }

    public static Config config = null!;

    public static Database db = null!;
    public static List<Player> players = new List<Player>();

    public override void Load(bool hotReload)
    {
        _plugin = this;

        config = LoadConfig();

        Connect(SQL_ConnectCallback);

        RegisterCommands();
        RegisterEvents();
        RegisterListeners();

        if(hotReload)
        {
            Utilities.GetPlayers().ForEach(AddPlayerToList);
        }
    }

    public override void Unload(bool shutdown)
    {
        UnRegisterCommands();
    }

    public static bool isLive()
    {
        return !WarmupRunning;
    }

    private void SQL_ConnectCallback(string connectionString, Exception exception, dynamic data)
    {
        if(connectionString == null!)
        {
            ThrowError($"Failed to connect to database: {exception.Message}");
            return;
        }

        db = new Database(connectionString);

        PrintToServer($"Connected to database");

        db.CreateTables();
    }

    public static void SQL_FetchUser_CB(MySqlDataReader reader, Exception exception, dynamic data)
    {
        if(exception != null!)
        {
            ThrowError($"Databse error, {exception.Message}");
            return;
        }

        if(reader.HasRows)
        {
            while(reader.Read())
            {
                int t_primary = reader.GetInt32("t_primary");
                int ct_primary = reader.GetInt32("ct_primary");
                int secondary = reader.GetInt32("secondary");
                GiveAWP giveAWP = (GiveAWP)reader.GetInt32("give_awp");

                Player player = players[data];

                player.weaponsAllocator.primaryWeapon_t = t_primary;
                player.weaponsAllocator.primaryWeapon_ct = ct_primary;
                player.weaponsAllocator.secondaryWeapon = secondary;
                player.weaponsAllocator.giveAWP = giveAWP;
            }
        } else{
            Player player = players[data];

            db.Query(SQL_CheckForErrors, $"INSERT INTO `weapons` (`auth`, `name`, `t_primary`, `ct_primary`, `secondary`, `give_awp`) VALUES ('{player.GetSteamID2()}', '{db.EscapeString(player.GetName())}' , '0', '0', '0', '0')");
        }
    }
}
