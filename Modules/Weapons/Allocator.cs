using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using RetakesAllocator.Modules.Models;
using RetakesAllocator.Modules.Votes;

using static RetakesAllocator.Modules.Weapons.Menu;
using static RetakesAllocator.Modules.Votes.Votes;

namespace RetakesAllocator.Modules.Weapons;

public enum GiveAwp
{
    Never,
    Sometimes,
    Always
}

public class Allocator(Player player)
{
    public static List<Weapon> PrimaryT = new()
    {
        new Weapon("weapon_ak47", "AK-47"),
        new Weapon("weapon_sg556", "SG 553")
    };

    public static List<Weapon> PrimaryCt = new()
    {
        new Weapon("weapon_m4a1", "M4A4"),
        new Weapon("weapon_m4a1_silencer", "M4A1-S"),
        new Weapon("weapon_aug", "AUG")
    };

    public static List<Weapon> PistolsT = new()
    {
        new Weapon("weapon_glock", "Glock-18"),
        new Weapon("weapon_p250", "P250"),
    };

    public static List<Weapon> PistolsCT = new()
    {
        new Weapon("weapon_usp_silencer", "USP-S"),
        new Weapon("weapon_p250", "P250"),
        new Weapon("weapon_hkp2000", "P2000")
    };

    private static Nades _ctNades = new();
    private static Nades _nades = new();

    private CCSPlayerController CCsPlayerController => player.Controller;

    public int PrimaryWeaponT = 0;
    public int PrimaryWeaponCt = 0;
    public int SecondaryWeaponT = 0;
    public int SecondaryWeaponCt = 0;

    public GiveAwp GiveAwp = GiveAwp.Never;
    public bool ShouldGiveAwp = false;

    public static void ResetNades()
    {
        _ctNades = new Nades(Core.NadesConfig.CtNades);
        _nades = new Nades(Core.NadesConfig.TNades);
    }

    public bool SetupGiveAwp()
    {
        var giveAwp = GiveAwp switch
        {
            GiveAwp.Always => true,
            GiveAwp.Never => false,
            _ => new Random().Next(0, 2) == 1
        };

        return giveAwp;
    }

    public void AllocateNades()
    {
        if (!player.Controller.IsValid)
        {
            return;
        }

        if (!CCsPlayerController.PawnIsAlive)
        {
            return;
        }

        switch (CCsPlayerController.Team)
        {
            case < CsTeam.Terrorist:
            case > CsTeam.CounterTerrorist:
            case CsTeam.Terrorist when !_nades.HasNades():
            case CsTeam.CounterTerrorist when !_ctNades.HasNades():
                return;
        }

        var nades = CCsPlayerController.Team == CsTeam.Terrorist ? _nades : _ctNades;

        CsItem grenade;
        do{
            grenade = SelectGrenade();
        }
        while(!CheckIfNadeAvailable(nades, grenade));

        nades.RemoveNade(grenade);

        CCsPlayerController.GiveNamedItem(grenade);
    }

    private bool CheckIfNadeAvailable(Nades nades, CsItem nade)
    {
        if (!player.Controller.IsValid)
        {
            return false;
        }

        if (!CCsPlayerController.PawnIsAlive)
        {
            return false;
        }

        if (CCsPlayerController.Team < CsTeam.Terrorist || CCsPlayerController.Team > CsTeam.CounterTerrorist)
        {
            return false;
        }

        return nade switch
        {
            CsItem.Flashbang => nades.HasFlashbangs(),
            CsItem.Molotov or CsItem.Incendiary => nades.HasMolotovs(),
            CsItem.HEGrenade => nades.HasHeGrenades(),
            CsItem.SmokeGrenade => nades.HasSmokes(),
            _ => false
        };
    }

    /// <summary>
    /// Empties the two slots allocation is about to refill, so giving a loadout twice leaves the
    /// player with one of each instead of two. Every allocation path only ever gives, so without
    /// this a duplicated spawn event stacks a second full loadout.
    /// </summary>
    /// <remarks>
    /// Deliberately NOT <c>RemoveWeapons()</c>: that also takes the C4, which the retakes plugin
    /// hands to a terrorist at round start, before this runs 0.1s after spawn. The bomb has a slot
    /// of its own and is never cleared here.
    ///
    /// The knife IS cleared, because all three paths hand one straight back - the only exception
    /// being a vote with <c>GiveKnife: false</c>, which now genuinely leaves the player without one
    /// rather than silently keeping the spawn knife as it did before.
    /// </remarks>
    public void ClearAllocatedWeapons()
    {
        if (!player.Controller.IsValid || !CCsPlayerController.PawnIsAlive)
        {
            return;
        }

        CCsPlayerController.RemoveItemBySlot(gear_slot_t.GEAR_SLOT_RIFLE);
        CCsPlayerController.RemoveItemBySlot(gear_slot_t.GEAR_SLOT_PISTOL);
        CCsPlayerController.RemoveItemBySlot(gear_slot_t.GEAR_SLOT_GRENADES);
        CCsPlayerController.RemoveItemBySlot(gear_slot_t.GEAR_SLOT_KNIFE);
    }

    private bool CanAllocate() =>
        player.Controller.IsValid
        && CCsPlayerController.PawnIsAlive
        && CCsPlayerController.Team is >= CsTeam.Terrorist and <= CsTeam.CounterTerrorist;

    /// <summary>
    /// Swaps just the primary for the one the loadout now names, leaving the pistol, knife,
    /// grenades and bomb untouched. Used when a saved loadout changed only that slot.
    /// </summary>
    /// <remarks>
    /// Does nothing while <see cref="ShouldGiveAwp"/> is set: that player's rifle this round came
    /// from the AWP roll and not from their loadout, so swapping it would quietly take the AWP away.
    /// </remarks>
    public void ReplacePrimary()
    {
        if (!CanAllocate() || ShouldGiveAwp)
        {
            return;
        }

        var item = CCsPlayerController.Team == CsTeam.Terrorist
            ? PrimaryT[PrimaryWeaponT].Item
            : PrimaryCt[PrimaryWeaponCt].Item;

        CCsPlayerController.RemoveItemBySlot(gear_slot_t.GEAR_SLOT_RIFLE);
        CCsPlayerController.GiveNamedItem(item);
    }

    /// <summary>As <see cref="ReplacePrimary"/>, for the pistol slot.</summary>
    public void ReplaceSecondary()
    {
        if (!CanAllocate() || ShouldGiveAwp)
        {
            return;
        }

        var item = CCsPlayerController.Team == CsTeam.Terrorist
            ? PistolsT[SecondaryWeaponT].Item
            : PistolsCT[SecondaryWeaponCt].Item;

        CCsPlayerController.RemoveItemBySlot(gear_slot_t.GEAR_SLOT_PISTOL);
        CCsPlayerController.GiveNamedItem(item);
    }

    public void AllocateArmor(bool giveFull = true)
    {
        if (!player.Controller.IsValid)
        {
            return;
        }

        if (!CCsPlayerController.PawnIsAlive)
        {
            return;
        }

        if (CCsPlayerController.Team < CsTeam.Terrorist || CCsPlayerController.Team > CsTeam.CounterTerrorist)
        {
            return;
        }

        CCsPlayerController.GiveNamedItem(giveFull ? CsItem.KevlarHelmet : CsItem.Kevlar);
    }

    public void Allocate()
    {
        if (!player.Controller.IsValid)
        {
            return;
        }

        if (!CCsPlayerController.PawnIsAlive)
        {
            return;
        }

        if (CCsPlayerController.Team < CsTeam.Terrorist || CCsPlayerController.Team > CsTeam.CounterTerrorist)
        {
            return;
        }

        string primary;
        if (ShouldGiveAwp)
        {
            primary = "weapon_awp";
        }
        else
        {
            primary = CCsPlayerController.Team == CsTeam.Terrorist ? PrimaryT[PrimaryWeaponT].Item : PrimaryCt[PrimaryWeaponCt].Item;
        }

        string secondary;

        if (ShouldGiveAwp)
        {
            secondary = "weapon_deagle";
        }
        else
        {
            secondary = CCsPlayerController.Team == CsTeam.Terrorist ? PistolsT[SecondaryWeaponT].Item : PistolsCT[SecondaryWeaponCt].Item;
        }

        CCsPlayerController.GiveNamedItem(primary);
        CCsPlayerController.GiveNamedItem(secondary);
        CCsPlayerController.GiveNamedItem(CsItem.Knife);

        if (CCsPlayerController.Team == CsTeam.CounterTerrorist)
        {
           GiveCtEquipment();
        }
    }

    public void AllocatePistolRound()
    {
        if (!player.Controller.IsValid)
        {
            return;
        }

        if (!CCsPlayerController.PawnIsAlive)
        {
            return;
        }

        if (CCsPlayerController.Team is < CsTeam.Terrorist or > CsTeam.CounterTerrorist)
        {
            return;
        }

        var secondary = (CCsPlayerController.Team == CsTeam.Terrorist) ? Core.Config.PistolRound.WeaponT : Core.Config.PistolRound.WeaponCt;

        CCsPlayerController.GiveNamedItem(secondary);
        CCsPlayerController.GiveNamedItem(CsItem.Knife);

        if (CCsPlayerController.Team == CsTeam.CounterTerrorist)
        {
           GiveCtEquipment();
        }
    }

    public void AllocateVote(Vote vote)
    {
         if (!player.Controller.IsValid)
         {
             return;
         }

         if (!CCsPlayerController.PawnIsAlive)
         {
             return;
         }

         if (CCsPlayerController.Team < CsTeam.Terrorist || CCsPlayerController.Team > CsTeam.CounterTerrorist)
         {
             return;
         }

         var weapons = CCsPlayerController.Team == CsTeam.Terrorist ? vote.WeaponsT : vote.WeaponsCt;

         if(weapons.Count > 1)
         {
             ShowWeaponSelectionMenu(CCsPlayerController, weapons, WeaponSelectionTime);
         } else {
             CCsPlayerController.GiveNamedItem("weapon_" + weapons.First());
         }

         if(vote.GiveKnife)
             CCsPlayerController.GiveNamedItem(CsItem.Knife);

         if (CCsPlayerController.Team == CsTeam.CounterTerrorist)
         {
             GiveCtEquipment();
         }
    }

    private CsItem SelectGrenade()
    {
        var grenade = CsItem.HEGrenade;

        var rand = new Random().Next(0,4);

        grenade = rand switch
        {
            0 => CsItem.HEGrenade,
            1 => CsItem.Flashbang,
            2 => CsItem.SmokeGrenade,
            3 => CCsPlayerController.Team == CsTeam.Terrorist ? CsItem.Molotov : CsItem.Incendiary,
            _ => grenade
        };

        return grenade;
    }

    private void GiveCtEquipment()
    {
        if (
            CCsPlayerController is { Team: CsTeam.CounterTerrorist, PlayerPawn.IsValid: true }
            && CCsPlayerController.PlayerPawn.Value != null
            && CCsPlayerController.PlayerPawn.Value.IsValid
            && CCsPlayerController.PlayerPawn.Value.ItemServices != null
        ) {
            var itemServices = new CCSPlayer_ItemServices(CCsPlayerController.PlayerPawn.Value.ItemServices.Handle)
            {
                HasDefuser = true
            };
        }
    }
}