using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocator.Modules.Weapons;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

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
        // No AWP in warmup. This still runs there so the nade pool is refilled, but nobody wins the
        // roll for a round that is not being played.
        if (GetGameRules() is { WarmupPeriod: true })
        {
            foreach (var warmupPlayer in players)
            {
                warmupPlayer.WeaponsAllocator.ShouldGiveAwp = false;
            }

            return;
        }

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

    private Timer? _spawnTimer;

    /// <summary>
    /// Applies a saved loadout immediately, so the menu's result is visible now instead of next
    /// spawn - but only the slots that actually changed for the team this player is on.
    /// </summary>
    /// <remarks>
    /// The caller decides what changed, because only it knows what the values were before the edit.
    /// Editing the CT weapons while playing T changes nothing here, and changing only the rifle
    /// leaves the pistol in hand rather than re-issuing it.
    ///
    /// Grenades and armour are never re-issued: grenades come from a pool shared by everyone and
    /// refilled once per round, so a player could drain it by reopening the menu and saving.
    /// </remarks>
    public void ApplyLoadoutChange(bool primaryChanged, bool secondaryChanged)
    {
        if (!primaryChanged && !secondaryChanged)
        {
            return;
        }

        if (!IsValid() || !Controller.PawnIsAlive)
        {
            return;
        }

        // A pistol round and a vote both deliberately override the saved loadout, so applying it
        // now would hand out weapons this round is not supposed to allow. Warmup has no such rules.
        if (GetGameRules() is not { WarmupPeriod: true })
        {
            if (RoundsCounter < Core.Config.PistolRound.RoundAmount || CurrentVote != null!)
            {
                return;
            }
        }

        if (primaryChanged)
        {
            WeaponsAllocator.ReplacePrimary();
        }

        if (secondaryChanged)
        {
            WeaponsAllocator.ReplaceSecondary();
        }
    }

    public void CreateSpawnDelay()
    {
        // player_spawn fires more than once around a single spawn - on the pawn appearing and again
        // on team assignment. Each one used to queue its own timer, and since allocation only ever
        // gives and never strips, the extras stacked a second full loadout. Keep one timer.
        _spawnTimer?.Kill();
        _spawnTimer = Plugin.AddTimer(.1f, Timer_GiveWeapons);
    }

    private void Timer_GiveWeapons()
    {
        _spawnTimer = null;

        if (!IsValid())
        {
            return;
        }

        // Every branch below only ever gives, so clear first and allocating twice is harmless
        // instead of doubling. This is the one place all three allocation paths pass through.
        WeaponsAllocator.ClearAllocatedWeapons();

        // Warmup is not a round. Newer retakes builds spawn players during it, and neither of the
        // two things that shape a real round applies there: pistol rounds are counted from round 0
        // and a vote is for the match, so warmup hands out the player's configured loadout instead.
        // `is { }` keeps this safe if the game rules entity cannot be resolved.
        var warmup = GetGameRules() is { WarmupPeriod: true };

        if(!warmup && RoundsCounter < Core.Config.PistolRound.RoundAmount)
        {
            WeaponsAllocator.AllocatePistolRound();
            WeaponsAllocator.AllocateArmor(false);
            return;
        }

        if(warmup || CurrentVote == null!)
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
