using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using Microsoft.Extensions.Logging;
using static RetakesAllocator.Modules.Core;
using RetakesAllocator.Modules.Weapons;
using Player = RetakesAllocator.Modules.Models.Player;

namespace RetakesAllocator.Modules;

public static class Utils
{
    // Populated by ConfigApplier.Apply once the config is parsed.
    public static string Prefix { get; set; } = string.Empty;
    public static string PrefixCon { get; set; } = string.Empty;

    /// <summary>
    /// Chat trigger words, pre-hashed by <see cref="Config.ConfigApplier"/>. The say
    /// listener runs for every chat message on the server, so this must be an O(1)
    /// lookup rather than a LINQ scan over a string[].
    /// </summary>
    public static HashSet<string> TriggerWords { get; set; } =
        new(StringComparer.OrdinalIgnoreCase) { "guns", "gun", "weapon", "weapons" };

    public static void PrintToChat(CCSPlayerController? controller, string msg)
    {
        if (controller is null || !controller.IsValid)
        {
            return;
        }

        controller.PrintToChat(msg);
    }

    public static void PrintToChatAll(string msg)
    {
        Server.PrintToChatAll(msg);
    }

    public static void ReplyToCommand(CommandInfo commandInfo, string msg)
    {
        commandInfo.ReplyToCommand(msg);
    }

    /// <summary>O(1) lookup of the tracked player for a controller, or null.</summary>
    public static Player? FindPlayer(CCSPlayerController? controller)
    {
        if (controller is null || !controller.IsValid)
        {
            return null;
        }

        return Players.GetValueOrDefault(controller.Slot);
    }

    public static bool HasAwpAccess(CCSPlayerController? player)
    {
        var permission = Core.Config.AwpPermission.Trim();

        if (permission.Length == 0)
        {
            return true;
        }

        if (player is null || !player.IsValid)
        {
            return false;
        }

        if (!permission.StartsWith('@'))
        {
            permission = $"@{permission}";
        }

        return AdminManager.PlayerHasPermissions(player, permission)
               || AdminManager.GetPlayerAdminData(player.AuthorizedSteamID) != null;
    }

    public static void ServerCommand(string command, params object[] args)
    {
        Server.ExecuteCommand(string.Format(command, args));
    }

    public static void AddPlayerToList(CCSPlayerController? player)
    {
        if (player is null || !player.IsValid || player.IsBot || player.IsHLTV)
        {
            return;
        }

        if (Players.ContainsKey(player.Slot))
        {
            return;
        }

        // Steam authorization may not be complete yet (e.g. during map start or
        // hot reload). Without it we can't resolve the SteamID, so defer adding
        // the player until OnClientAuthorized fires for them.
        if (player.AuthorizedSteamID == null)
        {
            return;
        }

        var playerObj = new Player(player);
        Players[playerObj.Slot] = playerObj;

        // Still connected across a map change: keep what they chose on the last map rather
        // than re-reading a database row that may not have caught up yet.
        if (TakeCarriedPreferences(playerObj.GetSteamId2()) is { } carried)
        {
            ApplyPreferences(playerObj, carried);
            return;
        }

        if (Store is null)
        {
            // The database failed to initialise; run with in-memory defaults.
            return;
        }

        // Read CounterStrikeSharp-bound values on the game thread before going async.
        var auth = playerObj.GetSteamId2();
        var name = playerObj.GetName();

        if (auth.Length == 0)
        {
            return;
        }

        var slot = playerObj.Slot;
        var store = Store;

        Task.Run(async () =>
        {
            try
            {
                var pref = await store.GetUserAsync(auth);

                if (pref == null)
                {
                    await store.CreateUserAsync(auth, name);
                    return;
                }

                // Apply to the player on the game thread. Skip if they
                // disconnected while the DB load was in flight.
                Server.NextFrame(() =>
                {
                    if (!Players.TryGetValue(slot, out var tracked) || !ReferenceEquals(tracked, playerObj))
                    {
                        return;
                    }

                    ApplyPreferences(playerObj, pref);
                });
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError(e, "Failed to load weapon preferences for {SteamId}", auth);
            }
        });
    }

    private static void ApplyPreferences(Player playerObj, WeaponPreference pref)
    {
        var allocator = playerObj.WeaponsAllocator;

        allocator.PrimaryWeaponT = SafeWeaponIndex(pref.TPrimary, Allocator.PrimaryT.Count);
        allocator.PrimaryWeaponCt = SafeWeaponIndex(pref.CtPrimary, Allocator.PrimaryCt.Count);
        allocator.SecondaryWeaponT = SafeWeaponIndex(pref.TSecondary, Allocator.PistolsT.Count);
        allocator.SecondaryWeaponCt = SafeWeaponIndex(pref.CtSecondary, Allocator.PistolsCT.Count);
        allocator.PistolRoundWeaponT = SafeWeaponIndex(pref.TPistolRound, Allocator.PistolsT.Count);
        allocator.PistolRoundWeaponCt = SafeWeaponIndex(pref.CtPistolRound, Allocator.PistolsCT.Count);
        allocator.GiveAwp = (GiveAwp)pref.GiveAwp;
    }

    private static int SafeWeaponIndex(int index, int count)
    {
        return index < 0 || index >= count ? 0 : index;
    }

    public static void RemovePlayerFromList(CCSPlayerController? player, bool flush = false)
    {
        if (player is null || player.IsBot || player.IsHLTV)
        {
            return;
        }

        // Deliberately not gated on IsValid: on disconnect the controller may already
        // be torn down and we still want the tracked entry (and its preferences) gone.
        if (!Players.TryGetValue(player.Slot, out var playerObj))
        {
            return;
        }

        RemoveTrackedPlayer(playerObj, flush);
    }

    /// <summary>
    /// Drops a tracked player and persists their preferences. <paramref name="flush"/>
    /// blocks on the write (used by the plugin unload path); otherwise it is fire and
    /// forget on the thread pool so the game thread never waits on the database.
    /// </summary>
    public static void RemoveTrackedPlayer(Player playerObj, bool flush = false)
    {
        Players.Remove(playerObj.Slot);

        if (Store is null)
        {
            return;
        }

        // Snapshot all values on the game thread before going async.
        var pref = SnapshotPreferences(playerObj);

        if (pref is null)
        {
            // Never authorized (or already gone) — nothing we can key a row on.
            return;
        }

        var store = Store;

        if (flush)
        {
            // Plugin unload path: block so the save completes before teardown.
            try
            {
                store.SaveUserAsync(pref).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError(e, "Failed to save weapon preferences for {SteamId}", pref.Auth);
            }
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                await store.SaveUserAsync(pref);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError(e, "Failed to save weapon preferences for {SteamId}", pref.Auth);
            }
        });
    }

    /// <summary>
    /// Preferences of the players who were connected when the map changed, keyed by SteamID2.
    /// Players stay connected through a map change, so no disconnect save runs for them, and
    /// OnMapStart used to drop their in-memory choices and reload the stale database row.
    /// </summary>
    private static readonly Dictionary<string, WeaponPreference> CarriedPreferences = new(StringComparer.Ordinal);

    private static long _carriedAt;

    // Long enough for a slow client to load the next map; after that the saved row is current.
    private const long CarryLifetimeMs = 180_000;

    /// <summary>
    /// Called on map start before the tracked players are cleared: saves every tracked
    /// player's preferences and keeps them in memory for when that player is re-added.
    /// </summary>
    public static void CarryPreferencesOverMapChange()
    {
        CarriedPreferences.Clear();
        _carriedAt = Environment.TickCount64;

        foreach (var tracked in Players.Values.ToArray())
        {
            var pref = SnapshotPreferences(tracked);

            if (pref is null)
            {
                continue;
            }

            CarriedPreferences[pref.Auth] = pref;

            var store = Store;

            if (store is null)
            {
                continue;
            }

            Task.Run(async () =>
            {
                try
                {
                    await store.SaveUserAsync(pref);
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogError(e, "Failed to save weapon preferences for {SteamId}", pref.Auth);
                }
            });
        }
    }

    private static WeaponPreference? TakeCarriedPreferences(string auth)
    {
        if (auth.Length == 0 || !CarriedPreferences.Remove(auth, out var carried))
        {
            return null;
        }

        return Environment.TickCount64 - _carriedAt <= CarryLifetimeMs ? carried : null;
    }

    private static WeaponPreference? SnapshotPreferences(Player playerObj)
    {
        var auth = playerObj.GetSteamId2();

        if (auth.Length == 0)
        {
            return null;
        }

        var allocator = playerObj.WeaponsAllocator;

        return new WeaponPreference
        {
            Auth = auth,
            Name = playerObj.GetName(),
            TPrimary = allocator.PrimaryWeaponT,
            CtPrimary = allocator.PrimaryWeaponCt,
            TSecondary = allocator.SecondaryWeaponT,
            CtSecondary = allocator.SecondaryWeaponCt,
            TPistolRound = allocator.PistolRoundWeaponT,
            CtPistolRound = allocator.PistolRoundWeaponCt,
            GiveAwp = (int)allocator.GiveAwp,
        };
    }

    public static CCSPlayerController[] ValidPlayers(bool considerBots = false)
    {
        return Utilities.GetPlayers()
        .Where(x => x.ReallyValid(considerBots))
        .Where(x => !x.IsHLTV)
        .Where(x => considerBots || !x.IsBot)
        .ToArray();
    }

    public static bool ReallyValid(this CCSPlayerController? player, bool considerBots = false)
    {
        return player is not null && player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected &&
            (considerBots || (!player.IsBot && !player.IsHLTV));
    }

    /// <summary>
    /// Counts connected players without building the intermediate list/array that
    /// <see cref="ValidPlayers"/> allocates — this is read repeatedly per vote message.
    /// </summary>
    public static int ValidPlayerCount(bool considerBots = false)
    {
        var count = 0;
        var maxPlayers = Server.MaxPlayers;

        for (var slot = 0; slot < maxPlayers; slot++)
        {
            var controller = Utilities.GetPlayerFromSlot(slot);

            if (controller.ReallyValid(considerBots))
            {
                count++;
            }
        }

        return count;
    }

    public static T GetRandomFromList<T>(this List<T> list)
    {
        return list[Random.Shared.Next(list.Count)];
    }
}
