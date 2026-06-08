using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocator.Modules.Weapons;

using static RetakesAllocator.Modules.Core;

namespace RetakesAllocator.Modules.Models;

public class Player
{
    public int PlayerIndex;
    public CCSPlayerController Controller => Utilities.GetPlayerFromIndex(PlayerIndex)!;

    public readonly Allocator WeaponsAllocator;

    public Player(CCSPlayerController player)
    {
        PlayerIndex = (int)player.Index;
        WeaponsAllocator = new Allocator(this);
    }

    public static void SetupPlayers(List<Player> players)
    {
        List<Player> playersT = new();
        List<Player> playersCt = new();

        foreach(var player in players)
        {
            var team = player.GetTeam();
            var giveAwp = player.WeaponsAllocator.SetupGiveAwp();

            player.WeaponsAllocator.ShouldGiveAwp = false;

            if (giveAwp)
            {
                if (team == CsTeam.Terrorist )
                {
                    playersT.Add(player);
                }

                if (team == CsTeam.CounterTerrorist)
                {
                    playersCt.Add(player);
                }
            }
        }

        if(0 < playersT.Count)
        {
            Player playerT = Utils.GetRandomFromList(playersT);
            playerT.WeaponsAllocator.ShouldGiveAwp = true;
        }

        if(0 < playersCt.Count)
        {
            Player playerCt = Utils.GetRandomFromList(playersCt);
            playerCt.WeaponsAllocator.ShouldGiveAwp = true;
        }
    }

    private CsTeam GetTeam()
    {
        CsTeam team;

        try
        {
            team = Controller.Team;
        }
        catch
        {
            team = CsTeam.None;
        }

        return team;
    }

    public string GetSteamId2()
    {
        return Controller.AuthorizedSteamID!.SteamId2;
    }

    public string GetName()
    {
        if (Controller == null! || !Controller.IsValid)
        {
            return string.Empty;
        }

        return Controller.PlayerName;
    }

    public bool IsValid()
    {
        return !(Controller == null! || !Controller.IsValid);
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

        if(CurrentVote == null!)
        {
            WeaponsAllocator.Allocate();
            WeaponsAllocator.AllocateNades();
            WeaponsAllocator.AllocateArmor();
            return;
        }

        var vote = CurrentVote.Vote;

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
