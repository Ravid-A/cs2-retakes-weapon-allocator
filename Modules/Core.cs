using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Cvars;

using RetakesAllocator.Modules.Models;
using RetakesAllocator.Modules.Weapons;
using RetakesAllocator.Modules.Votes;

using static RetakesAllocator.Modules.RetakeCapability;
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

    public static WeaponStore Store = null!;
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

        InitializeDatabase();

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
        Utilities.GetPlayers().ForEach(p => RemovePlayerFromList(p, flush: true));

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

    private static void InitializeDatabase()
    {
        try
        {
            var provider = DatabaseProviderFactory.Create(Config.DbConnection, Plugin.ModuleDirectory);
            Store = new WeaponStore(provider);
            Store.InitializeAsync().GetAwaiter().GetResult();
            PrintToServer("Connected to database");
        }
        catch (Exception e)
        {
            ThrowError($"Failed to connect to database: {e.Message}");
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
