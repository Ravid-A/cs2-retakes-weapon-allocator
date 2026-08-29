using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
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

    public static void PrintToChat(CCSPlayerController controller, string msg)
    {
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

    public static Player FindPlayer(CCSPlayerController controller)
    {
        return Players.Find(player => player.PlayerIndex == controller.Index)!;
    }

    public static void ServerCommand(string command, params object[] args)
    {
        Server.ExecuteCommand(string.Format(command, args));
    }

    public static void AddPlayerToList(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || player.IsBot)
        {
            return;
        }

        if (FindPlayer(player) != null!)
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
        Players.Add(playerObj);

        // Read CounterStrikeSharp-bound values on the game thread before going async.
        var auth = playerObj.GetSteamId2();
        var name = playerObj.GetName();

        Task.Run(async () =>
        {
            try
            {
                var pref = await Store.GetUserAsync(auth);

                if (pref == null)
                {
                    await Store.CreateUserAsync(auth, name);
                }
                else
                {
                    // Apply to the player on the game thread. Skip if they
                    // disconnected while the DB load was in flight.
                    Server.NextFrame(() =>
                    {
                        if (!Players.Contains(playerObj))
                        {
                            return;
                        }

                        ApplyPreferences(playerObj, pref);
                    });
                }
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

        allocator.PrimaryWeaponT = pref.TPrimary > Allocator.PrimaryT.Count ? 0 : pref.TPrimary;
        allocator.PrimaryWeaponCt = pref.CtPrimary > Allocator.PrimaryCt.Count ? 0 : pref.CtPrimary;
        allocator.SecondaryWeaponT = pref.TSecondary > Allocator.PistolsT.Count ? 0 : pref.TSecondary;
        allocator.SecondaryWeaponCt = pref.CtSecondary > Allocator.PistolsCT.Count ? 0 : pref.CtSecondary;
        allocator.GiveAwp = (GiveAwp)pref.GiveAwp;
    }

    public static void RemovePlayerFromList(CCSPlayerController player, bool flush = false)
    {
        if (player == null || !player.IsValid || player.IsBot)
        {
            return;
        }

        var playerObj = FindPlayer(player);

        if (playerObj == null!)
        {
            return;
        }

        // Snapshot all values on the game thread before going async.
        var pref = new WeaponPreference
        {
            Auth = playerObj.GetSteamId2(),
            TPrimary = playerObj.WeaponsAllocator.PrimaryWeaponT,
            CtPrimary = playerObj.WeaponsAllocator.PrimaryWeaponCt,
            TSecondary = playerObj.WeaponsAllocator.SecondaryWeaponT,
            CtSecondary = playerObj.WeaponsAllocator.SecondaryWeaponCt,
            GiveAwp = (int)playerObj.WeaponsAllocator.GiveAwp,
        };

        Players.Remove(playerObj);

        if (flush)
        {
            // Plugin unload path: block so the save completes before teardown.
            try
            {
                Store.SaveUserAsync(pref).GetAwaiter().GetResult();
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
                await Store.SaveUserAsync(pref);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError(e, "Failed to save weapon preferences for {SteamId}", pref.Auth);
            }
        });
    }

    public static int GetRoundsAmount()
    {
        IEnumerable<CTeam> team = Utilities.FindAllEntitiesByDesignerName<CTeam>("cs_team");

        if (team.Count() == 0)
        {
            return 0;
        }

        int rounds = 0;

        foreach (var t in team)
        {
            rounds = t.Score;
        }

        return rounds;
    }

    public static CCSPlayerController[] ValidPlayers(bool considerBots = false)
    {
        //considerBots = true;
        return Utilities.GetPlayers()
        .Where(x => x.ReallyValid(considerBots))
        .Where(x => !x.IsHLTV)
        .Where(x => considerBots || !x.IsBot)
        .ToArray();
    }

    public static bool ReallyValid(this CCSPlayerController? player, bool considerBots = false)
    {
        return player is not null && player.IsValid && player.Connected == PlayerConnectedState.Connected &&
            (considerBots || (!player.IsBot && !player.IsHLTV));
    }

    public static int ValidPlayerCount(bool considerBots = false)
    {
        return ValidPlayers(considerBots).Length;
    }

    public static T GetRandomFromList<T>(this List<T> list)
    {
        int index = new Random().Next(list.Count);
        return list[index];
    }   
}
