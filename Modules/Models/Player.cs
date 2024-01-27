using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocator.Modules.Weapons;

using static RetakesAllocator.Modules.Core;

namespace RetakesAllocator.Modules.Models;

public class Player
{
    public CCSPlayerController player;

    private SteamID _steamId;

    public readonly Allocator WeaponsAllocator;

    public Player(CCSPlayerController player, SteamID steamId)
    {
        this.player = player;
        _steamId = steamId;
        WeaponsAllocator = new Allocator(player);
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
            team = (CsTeam)player.TeamNum;
        } 
        catch
        {
            team = CsTeam.None;
        }

        return team;
    }

    public string GetSteamId2()
    {
        return _steamId.SteamId2;
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
        if (!IsLive())
        {
            return;
        }

        WeaponsAllocator.Allocate();
    }
}