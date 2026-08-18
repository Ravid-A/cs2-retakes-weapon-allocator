using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesAllocator.Modules.Weapons;

using static RetakesAllocator.Modules.Core;

namespace RetakesAllocator.Modules.Models;

public class Player
{
    public readonly int Slot;

    // Kept for source compatibility; the entity index is simply the slot + 1.
    public int PlayerIndex => Slot + 1;

    private CCSPlayerController _controller;

    /// <summary>
    /// The player's controller. Cached: every read used to go through
    /// <c>Utilities.GetPlayerFromIndex</c>, which does a native entity lookup and
    /// allocates a fresh wrapper object. The allocator touches this a dozen times per
    /// spawn, so at 20+ players that was hundreds of interop calls and allocations per
    /// round. The handle is re-resolved automatically if it ever goes invalid.
    /// </summary>
    public CCSPlayerController Controller
    {
        get
        {
            if (_controller.IsValid)
            {
                return _controller;
            }

            var refreshed = Utilities.GetPlayerFromSlot(Slot);

            if (refreshed is not null)
            {
                _controller = refreshed;
            }

            return _controller;
        }
    }

    public readonly Allocator WeaponsAllocator;

    /// <summary>
    /// Captured at join time. Reading it from the controller on disconnect is not
    /// reliable — the entity is often already torn down by then, which used to throw
    /// a NullReferenceException on the save path and lose the player's preferences.
    /// </summary>
    private readonly string _steamId2;

    private string _name;

    public Player(CCSPlayerController player)
    {
        Slot = player.Slot;
        _controller = player;
        _steamId2 = player.AuthorizedSteamID?.SteamId2 ?? string.Empty;
        _name = player.PlayerName;
        WeaponsAllocator = new Allocator(this);
    }

    public static void SetupPlayers(IEnumerable<Player> players)
    {
        Player? candidateT = null;
        Player? candidateCt = null;
        var seenT = 0;
        var seenCt = 0;

        // Reservoir sampling: picks one uniformly random AWP holder per team in a
        // single pass, with no intermediate lists to allocate.
        foreach (var player in players)
        {
            player.WeaponsAllocator.ShouldGiveAwp = false;

            if (!player.WeaponsAllocator.SetupGiveAwp())
            {
                continue;
            }

            switch (player.GetTeam())
            {
                case CsTeam.Terrorist:
                    seenT++;
                    if (Random.Shared.Next(seenT) == 0)
                    {
                        candidateT = player;
                    }
                    break;
                case CsTeam.CounterTerrorist:
                    seenCt++;
                    if (Random.Shared.Next(seenCt) == 0)
                    {
                        candidateCt = player;
                    }
                    break;
            }
        }

        if (candidateT is not null)
        {
            candidateT.WeaponsAllocator.ShouldGiveAwp = true;
        }

        if (candidateCt is not null)
        {
            candidateCt.WeaponsAllocator.ShouldGiveAwp = true;
        }
    }

    public CsTeam GetTeam()
    {
        var controller = Controller;

        return controller.IsValid ? controller.Team : CsTeam.None;
    }

    public string GetSteamId2()
    {
        return _steamId2;
    }

    /// <summary>
    /// The player's current name, falling back to the last one we saw once the
    /// controller is gone (name changes are picked up on every valid read).
    /// </summary>
    public string GetName()
    {
        var controller = Controller;

        if (controller.IsValid)
        {
            _name = controller.PlayerName;
        }

        return _name;
    }

    public bool IsValid()
    {
        return Controller.IsValid;
    }

    /// <summary>
    /// Runs the allocation for this player. Called from the batched spawn queue in
    /// <see cref="Handlers.Events"/> rather than from a per-player timer.
    /// </summary>
    public void GiveWeapons()
    {
        if (!IsValid())
        {
            return;
        }

        if (RoundsCounter < Core.Config.PistolRound.RoundAmount)
        {
            WeaponsAllocator.AllocatePistolRound();
            WeaponsAllocator.AllocateArmor(false);
            return;
        }

        if (CurrentVote == null!)
        {
            WeaponsAllocator.Allocate();
            WeaponsAllocator.AllocateNades();
            WeaponsAllocator.AllocateArmor();
            return;
        }

        var vote = CurrentVote.Vote;

        if (vote.GiveArmor)
        {
            WeaponsAllocator.AllocateArmor(vote.GiveHelmet);
        }

        if (vote.GiveNades)
        {
            WeaponsAllocator.AllocateNades();
        }

        if (vote.GiveWeapons)
        {
            WeaponsAllocator.AllocateVote(vote);
            return;
        }

        WeaponsAllocator.Allocate();
    }
}
