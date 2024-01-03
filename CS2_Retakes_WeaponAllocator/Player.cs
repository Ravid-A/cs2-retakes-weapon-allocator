using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;

using Weapons;

using static WeaponsAllocator.Core;

namespace WeaponsAllocator;

public class Player
{
    public CCSPlayerController player;

    private SteamID steamID;

    public Allocator weaponsAllocator;

    public bool inGunMenu = false;

    public Player(CCSPlayerController player, SteamID steamID)
    {
        this.player = player;
        this.steamID = steamID;
        weaponsAllocator = new Allocator(player);
    }

    public static void SetupPlayers(List<Player> players)
    {
        bool giveawp_t = true;
        bool giveawp_ct = true;

        foreach(Player player in players)
        {
            CsTeam team = player.GetTeam();
            bool giveawp = player.weaponsAllocator.SetupGiveAwp();

            player.weaponsAllocator.give_awp = false;

            if(giveawp)
            {
                if(team == CsTeam.Terrorist && giveawp_t)
                {
                    player.weaponsAllocator.give_awp = true;
                    giveawp_t = false;
                }

                if(team == CsTeam.CounterTerrorist && giveawp_ct)
                {
                    player.weaponsAllocator.give_awp = true;
                    giveawp_ct = false;
                }
            }
        }
    }

    public CsTeam GetTeam()
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

    public string GetSteamID2()
    {
        return steamID.SteamId2;
    }

    public string GetName()
    {
        if(player == null! || !player.IsValid)
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
        _plugin.AddTimer(.05f, Timer_GiveWeapons);
    }

    private void Timer_GiveWeapons()
    {
        if(!isLive())
        {
            return;
        }

        weaponsAllocator.Allocate();
    }
}