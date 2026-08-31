using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Cvars;
using Microsoft.Extensions.Logging;

using RetakesAllocator.Modules.Config;
using RetakesAllocator.Modules.Models;
using RetakesAllocator.Modules.Votes;
using RetakesAllocator.Modules.Weapons;

using static RetakesAllocator.Modules.RetakeCapability;
using static RetakesAllocator.Modules.Utils;
using static RetakesAllocator.Modules.Handlers.Commands;
using static RetakesAllocator.Modules.Handlers.Events;
using static RetakesAllocator.Modules.Handlers.Listeners;
using static RetakesAllocator.Modules.Votes.Votes;

namespace RetakesAllocator.Modules;

[MinimumApiVersion(373)]
public class Core : BasePlugin, IPluginConfig<RetakesAllocatorConfig>
{
    public static Core Plugin = null!;

    public override string ModuleName => "[Retakes] Weapons Allocator";
    public override string ModuleVersion => "3.2.4";
    public override string ModuleAuthor => "Ravid & B3none";
    public override string ModuleDescription => "Weapons Allocator plugin for retakes";

    public static RetakesAllocatorConfig Config = null!;

    // Required by IPluginConfig. Implemented explicitly so it does not collide with
    // the static Config field above; OnConfigParsed is what actually stores the config.
    RetakesAllocatorConfig IPluginConfig<RetakesAllocatorConfig>.Config { get; set; } = new();

    private static bool _loaded;
    public static NadesConfig NadesConfig = null!;

    public static WeaponStore Store = null!;
    public static List<Player> Players = new();
    public static int RoundsCounter = 0;
    public static AsyncVoteManager CurrentVote = null!;
    public static ConVar mp_damage_headshot_only = null!;

    public override void Load(bool hotReload)
    {
        Plugin = this;

        mp_damage_headshot_only = ConVar.Find("mp_damage_headshot_only")!;

        if(mp_damage_headshot_only == null!)
        {
            Plugin.Logger.LogError("Failed to find the mp_damage_headshot_only convar; headshot-only rounds will not work");
            return;
        }

        // Config is already loaded and applied by OnConfigParsed (called before Load).
        InitializeDatabase();

        RegisterCommands();
        RegisterEvents();
        RegisterListeners();

        Votes_OnConfigParsed(Config.Votes.WeaponSelectionTime, Config.Votes.RequiredPercentage);

        RetakeCapability_OnLoad();

        LoadoutPanel.Init();

        _loaded = true;

        if (hotReload)
        {
            Utilities.GetPlayers().ForEach(AddPlayerToList);
        }
    }

    public override void Unload(bool hotReload)
    {
        // _loaded is static, so it survives an unload/reload cycle within the same
        // process. Reset it here so the next Load's "register votes once" assumption
        // holds and OnConfigParsed doesn't register them early on the next load.
        _loaded = false;

        UnRegisterCommands();
        Votes_OnPluginUnload();
        Utilities.GetPlayers().ForEach(p => RemovePlayerFromList(p, flush: true));

        RetakeCapability_OnUnload();

        LoadoutPanel.Shutdown();
    }

    public static CCSGameRules GetGameRules()
    {
        var gameRulesEntities = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
        var gameRules = gameRulesEntities.First().GameRules;

        if(gameRules == null!)
        {
            Plugin.Logger.LogError("Failed to resolve the game rules entity");
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
            Plugin.Logger.LogInformation("Connected to the {Provider} database", Config.DbConnection.Provider);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError(e, "Failed to connect to the {Provider} database; check the DbConnection section of the config", Config.DbConnection.Provider);
        }
    }

    public void OnConfigParsed(RetakesAllocatorConfig config)
    {
        // OnConfigParsed runs before Load on first parse, and again on every
        // file-watch hot reload. Plugin may not be set yet on the first call, and
        // on a hot reload it may still point at the previous instance until Load
        // re-sets it, so we (re)assign it here before any Plugin.AddCommand runs.
        Plugin = this;
        Config = config;

        if (!Config.IsValid())
        {
            Plugin.Logger.LogError("Invalid configuration; check the DbConnection section of the config file");
            return;
        }

        ConfigApplier.Apply(Config);

        // On the initial parse, Load registers the vote commands once. On a hot
        // reload (after Load), re-register them to reflect any vote changes.
        if (_loaded)
        {
            Votes_OnConfigParsed(Config.Votes.WeaponSelectionTime, Config.Votes.RequiredPercentage);
        }
    }

    /// <summary>
    /// Re-applies the in-memory config and re-registers vote commands. Edits to the
    /// config file on disk are picked up automatically by CounterStrikeSharp's
    /// file-watch hot reload (which calls OnConfigParsed); this method re-applies the
    /// already-parsed config on demand (used by the reload command).
    /// </summary>
    public static void ReloadConfig()
    {
        ConfigApplier.Apply(Config);
        Votes_OnConfigParsed(Config.Votes.WeaponSelectionTime, Config.Votes.RequiredPercentage);

        // Weapon lists may have changed under a card that is already open.
        LoadoutPanel.RefreshOpen();

        Plugin.Logger.LogInformation("Configuration reloaded and applied");
    }
}
