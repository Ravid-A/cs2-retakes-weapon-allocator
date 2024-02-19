using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocator.Modules.Weapons;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Utils;

namespace RetakesAllocator.Modules.Models;

public class Player
{
    public int playerIndex;
    public CCSPlayerController player => Utilities.GetPlayerFromIndex(playerIndex);

    public readonly Allocator WeaponsAllocator;

    public Player(CCSPlayerController player)
    {
        playerIndex = (int)player.Index;
        WeaponsAllocator = new Allocator(this);
    }

    public static void SetupPlayers(List<Player> players)
    {
        var giveAwpT = true;
        var giveAwpCt = true;

        foreach(var player in players)
        {
            var team = player.GetTeam();
            var giveAwp = player.WeaponsAllocator.SetupGiveAwp();

            player.WeaponsAllocator.ShouldGiveAwp = false;

            if (giveAwp)
            {
                if (team == CsTeam.Terrorist && giveAwpT)
                {
                    player.WeaponsAllocator.ShouldGiveAwp = true;
                    giveAwpT = false;
                }

                if (team == CsTeam.CounterTerrorist && giveAwpCt)
                {
                    player.WeaponsAllocator.ShouldGiveAwp = true;
                    giveAwpCt = false;
                }
            }
        }
    }

    private CsTeam GetTeam()
    {
        CsTeam team;

        try
        {
            team = player.Team;
        } 
        catch
        {
            team = CsTeam.None;
        }

        return team;
    }

    public string GetSteamId2()
    {
        return player.AuthorizedSteamID!.SteamId2;
    }

    public string GetName()
    {
        if (player == null! || !player.IsValid)
        {
            return string.Empty;
        }

        return player.PlayerName;
    }

    public bool IsValid()
    {
        return !(player == null! || !player.IsValid);
    }

    public void CreateSpawnDelay()
    {
        Plugin.AddTimer(.1f, Timer_GiveWeapons);
    }

    private void Timer_GiveWeapons()
    {
        WeaponsAllocator.Allocate();
    }
}