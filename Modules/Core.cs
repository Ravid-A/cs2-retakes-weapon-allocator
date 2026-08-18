using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Cvars;
using Microsoft.Extensions.Logging;

using RetakesAllocator.Modules.Config;
using RetakesAllocator.Modules.Models;
using RetakesAllocator.Modules.Votes;

using static RetakesAllocator.Modules.RetakeCapability;
using static RetakesAllocator.Modules.Utils;
using static RetakesAllocator.Modules.Handlers.Commands;
using static RetakesAllocator.Modules.Handlers.Events;
using static RetakesAllocator.Modules.Handlers.Listeners;
using static RetakesAllocator.Modules.Votes.Votes;

namespace RetakesAllocator.Modules;

[MinimumApiVersion(360)]
public class Core : BasePlugin, IPluginConfig<RetakesAllocatorConfig>
{
    public static Core Plugin = null!;

    public override string ModuleName => "[Retakes] Weapons Allocator";
    public override string ModuleVersion => "2.0.2";
    public override string ModuleAuthor => "Ravid & B3none";
    public override string ModuleDescription => "Weapons Allocator plugin for retakes";

    public static RetakesAllocatorConfig Config = null!;

    // Required by IPluginConfig. Implemented explicitly so it does not collide with
    // the static Config field above; OnConfigParsed is what actually stores the config.
    RetakesAllocatorConfig IPluginConfig<RetakesAllocatorConfig>.Config { get; set; } = new();

    private static bool _loaded;
    public static NadesConfig NadesConfig = null!;

    public static WeaponStore Store = null!;

    /// <summary>
    /// Tracked players keyed by <see cref="CCSPlayerController.Slot"/>. A dictionary
    /// (instead of the old list + linear scan) keeps player lookups O(1); they happen
    /// on every spawn, every chat message and every menu callback, so at 20+ slots the
    /// scan was pure overhead.
    /// </summary>
    public static readonly Dictionary<int, Player> Players = new();

    public static int RoundsCounter = 0;
    public static AsyncVoteManager CurrentVote = null!;
    public static ConVar? mp_damage_headshot_only;

    // Resolving the game rules entity means walking the whole active entity list and
    // reading every entity's designer name across the native boundary. That used to
    // happen on every single player spawn; cache it for the lifetime of the map.
    private static CCSGameRules? _gameRules;

    public override void Load(bool hotReload)
    {
        Plugin = this;

        mp_damage_headshot_only = ConVar.Find("mp_damage_headshot_only");

        if (mp_damage_headshot_only is null)
        {
            // Not fatal: only headshot-only vote rounds depend on it. Previously this
            // returned early and left the plugin loaded but with nothing registered.
            Logger.LogWarning("Failed to find the mp_damage_headshot_only convar; headshot-only rounds will not work");
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
            InvalidateGameRules();
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

        // Iterate the tracked players rather than the live controller list: a player
        // whose controller has already gone away still has preferences worth flushing.
        foreach (var player in Players.Values.ToArray())
        {
            RemoveTrackedPlayer(player, flush: true);
        }

        Players.Clear();
        InvalidateGameRules();

        RetakeCapability_OnUnload();
    }

    /// <summary>
    /// The cached <c>cs_gamerules</c> entity, resolved lazily and re-resolved if the
    /// cached handle goes stale. Null when it cannot be found (e.g. mid map change).
    /// </summary>
    public static CCSGameRules? GameRules
    {
        get
        {
            if (_gameRules is not null && _gameRules.Handle != IntPtr.Zero)
            {
                return _gameRules;
            }

            foreach (var proxy in Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules"))
            {
                if (proxy is null || !proxy.IsValid || proxy.GameRules is null)
                {
                    continue;
                }

                _gameRules = proxy.GameRules;
                return _gameRules;
            }

            _gameRules = null;
            return null;
        }
    }

    /// <summary>Drops the cached game rules pointer; called on map start/end.</summary>
    public static void InvalidateGameRules()
    {
        _gameRules = null;
    }

    /// <summary>
    /// True while the server is in warmup. Falls back to false when the game rules
    /// entity cannot be resolved, so allocation keeps working instead of throwing.
    /// </summary>
    public static bool IsWarmup => GameRules?.WarmupPeriod ?? false;

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
        Plugin.Logger.LogInformation("Configuration reloaded and applied");
    }
}
