using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Cvars;

using MySqlConnector;

using RetakesAllocator.Modules.Models;
using RetakesAllocator.Modules.Weapons;
using RetakesAllocator.Modules.Votes;

using static RetakesAllocator.Modules.RetakeCapability;
using static RetakesAllocator.Modules.Database;
using static RetakesAllocator.Modules.Utils;
using static RetakesAllocator.Modules.Configs;
using static RetakesAllocator.Modules.Handlers.Commands;
using static RetakesAllocator.Modules.Handlers.Events;
using static RetakesAllocator.Modules.Handlers.Listeners;
using static RetakesAllocator.Modules.Votes.Votes;
using static RetakesAllocator.Modules.Weapons.Allocator;

namespace RetakesAllocator.Modules;

[MinimumApiVersion(215)]
public class Core : BasePlugin
{
    public static Core Plugin = null!;

    public override string ModuleName => "[Retakes] Weapons Allocator";
    public override string ModuleVersion => "1.2.0";
    public override string ModuleAuthor => "Ravid & B3none";
    public override string ModuleDescription => "Weapons Allocator plugin for retakes";

    public static Config Config = null!;
    public static NadesConfig NadesConfig = null!;

    public static Database Db = null!;
    public static List<Player> Players = new();
    public static int RoundsCounter = 0;
    public static AsyncVoteManager currentVote = null!;
    public static ConVar mp_damage_headshot_only = null!;

    public override void Load(bool hotReload)
    {
        Plugin = this;

        mp_damage_headshot_only = ConVar.Find("mp_damage_headshot_only")!;

        if(mp_damage_headshot_only == null!)
        {
            ThrowError("Failed to find mp_damage_headshot_only");
            return;
        }

        LoadConfigs();

        Connect(SQL_ConnectCallback);

        RegisterCommands();
        RegisterEvents();
        RegisterListeners();

        RetakeCapability_OnLoad();

        if (hotReload)
        {
            Utilities.GetPlayers().ForEach(AddPlayerToList);
        }
    }

    public override void Unload(bool hotReload)
    {
        UnRegisterCommands();
        Votes_OnPluginUnload();
        Utilities.GetPlayers().ForEach(RemovePlayerFromList);

        RetakeCapability_OnUnload();
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
                var tsecondary = reader.GetInt32("t_secondary");
                var ctSecondary = reader.GetInt32("ct_secondary");
                var giveAwp = (GiveAwp)reader.GetInt32("give_awp");

                Player player = Players[data];

                player.WeaponsAllocator.PrimaryWeaponT = tPrimary > PrimaryT.Count ? 0 : tPrimary;
                player.WeaponsAllocator.PrimaryWeaponCt = ctPrimary > PrimaryCt.Count ? 0 : ctPrimary;
                player.WeaponsAllocator.SecondaryWeaponT = tsecondary > PistolsT.Count ? 0 : tsecondary;
                player.WeaponsAllocator.SecondaryWeaponCt = ctSecondary > PistolsCT.Count ? 0 : ctSecondary;
                player.WeaponsAllocator.GiveAwp = giveAwp;
            }
        }
        else
        {
            Player player = Players[data];

            Query(SQL_CheckForErrors, $"INSERT INTO `weapons` (`auth`, `name`) VALUES ('{player.GetSteamId2()}', '{EscapeString(player.GetName())}')");
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

            Votes.Config.LoadConfig();
        }

        Weapons.Config.LoadConfig();

        PrintToServer("Configs loaded");
    }
}
