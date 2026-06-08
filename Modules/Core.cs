using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Cvars;

using RetakesAllocator.Modules.Models;
using RetakesAllocator.Modules.Weapons;
using RetakesAllocator.Modules.Votes;

using static RetakesAllocator.Modules.RetakeCapability;
using static RetakesAllocator.Modules.Utils;
using static RetakesAllocator.Modules.Handlers.Commands;
using static RetakesAllocator.Modules.Handlers.Events;
using static RetakesAllocator.Modules.Handlers.Listeners;
using static RetakesAllocator.Modules.Votes.Votes;
using static RetakesAllocator.Modules.Weapons.Allocator;

namespace RetakesAllocator.Modules;

[MinimumApiVersion(215)]
public class Core : BasePlugin, IPluginConfig<RetakesAllocatorConfig>
{
    public static Core Plugin = null!;

    public override string ModuleName => "[Retakes] Weapons Allocator";
    public override string ModuleVersion => "1.2.0";
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

        // Config is already loaded and applied by OnConfigParsed (called before Load).
        InitializeDatabase();

        RegisterCommands();
        RegisterEvents();
        RegisterListeners();

        Votes_OnConfigParsed(Config.Votes.WeaponSelectionTime, Config.Votes.RequiredPercentage);

        RetakeCapability_OnLoad();

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
            ThrowError("Invalid config, please check your config file.");
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
        PrintToServer("Config re-applied");
    }
}
