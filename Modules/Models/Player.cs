using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocator.Modules.Votes;
using RetakesAllocator.Modules.Weapons;

using static RetakesAllocator.Modules.Core;

namespace RetakesAllocator.Modules.Models;

public class Player
{
    public int playerIndex;
    public CCSPlayerController player => Utilities.GetPlayerFromIndex(playerIndex)!;

    public readonly Allocator WeaponsAllocator;

    public Player(CCSPlayerController player)
    {
        playerIndex = (int)player.Index;
        WeaponsAllocator = new Allocator(this);
    }

    public static void SetupPlayers(List<Player> players)
    {
        List<Player> players_t = new();
        List<Player> players_ct = new();

        foreach(var player in players)
        {
            var team = player.GetTeam();
            var giveAwp = player.WeaponsAllocator.SetupGiveAwp();

            player.WeaponsAllocator.ShouldGiveAwp = false;

            if (giveAwp)
            {
                if (team == CsTeam.Terrorist )
                {
                    players_t.Add(player);
                }

                if (team == CsTeam.CounterTerrorist)
                {
                    players_ct.Add(player);
                }
            }
        }

        if(0 < players_t.Count)
        {
            Player player_t = Utils.GetRandomFromList(players_t);
            player_t.WeaponsAllocator.ShouldGiveAwp = true;
        }
        
        if(0 < players_ct.Count)
        {  
            Player player_ct = Utils.GetRandomFromList(players_ct);
            player_ct.WeaponsAllocator.ShouldGiveAwp = true;
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
        if(RoundsCounter < Core.Config.PistolRound.RoundAmount)
        {
            WeaponsAllocator.AllocatePistolRound();
            WeaponsAllocator.AllocateArmor(false);
            return;
        }

        if(currentVote == null!)
        {
            WeaponsAllocator.Allocate();
            WeaponsAllocator.AllocateNades();
            WeaponsAllocator.AllocateArmor();
            return;
        }

        Vote vote = currentVote.vote;

        if(vote.GiveArmor)
        {
            WeaponsAllocator.AllocateArmor(vote.GiveHelmet);
        }

        if(vote.GiveNades)
        {
            WeaponsAllocator.AllocateNades();
        }

        if(vote.GiveWeapons)
        {
            WeaponsAllocator.AllocateVote(vote);
            return;
        }

        WeaponsAllocator.Allocate();
    }
}