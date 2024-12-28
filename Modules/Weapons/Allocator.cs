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

public class Weapon
{
    public string Item { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public Weapon(string item, string displayName)
    {
        Item = item;
        DisplayName = displayName;
    }
}

public class Allocator
{
    public enum WeaponType
    {
        PrimaryT,
        PrimaryCt,
        SecondaryT,
        SecondaryCt
    };

    public static List<Weapon> PrimaryT = new()
    {
        new("weapon_ak47", "AK-47"),
        new("weapon_sg556", "SG 553")
    };

    public static List<Weapon> PrimaryCt = new()
    {
        new("weapon_m4a1", "M4A4"),
        new("weapon_m4a1_silencer", "M4A1-S"),
        new("weapon_aug", "AUG")
    };

    public static List<Weapon> PistolsT = new()
    {
        new("weapon_glock", "Glock-18"),
        new("weapon_p250", "P250"),
    };

    public static List<Weapon> PistolsCT = new()
    {
        new("weapon_usp_silencer", "USP-S"),
        new("weapon_p250", "P250"),
        new("weapon_hkp2000", "P2000")
    };

    public static Nades CTNades = new();
    public static Nades TNades = new();

    private readonly Player _player;

    private CCSPlayerController cCSPlayerController => _player.player;

    public int PrimaryWeaponT = 0;
    public int PrimaryWeaponCt = 0;
    public int SecondaryWeaponT = 0;
    public int SecondaryWeaponCt = 0;

    public GiveAwp GiveAwp = GiveAwp.Never;
    public bool ShouldGiveAwp = false;

    public Allocator(Player player)
    {
        _player = player;
    }

    public static void ResetNades()
    {
        CTNades = new Nades(Core.NadesConfig.CTNades);
        TNades = new Nades(Core.NadesConfig.TNades);
    }

    public static int GetWeaponIndex(string weapon, WeaponType type)
    {
        switch(type)
        {
            case WeaponType.PrimaryT:
                return PrimaryT.FindIndex(w => w.DisplayName == weapon);
            case WeaponType.PrimaryCt:
                return PrimaryCt.FindIndex(w => w.DisplayName == weapon);
            case WeaponType.SecondaryT:
                return PistolsT.FindIndex(w => w.DisplayName == weapon);
            case WeaponType.SecondaryCt:
                return PistolsCT.FindIndex(w => w.DisplayName == weapon);
        }

        return -1;
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
        if (_player == null || cCSPlayerController == null || !_player.player.IsValid)
        {
            return;
        }

        if (!cCSPlayerController.PawnIsAlive)
        {
            return;
        }

        if (cCSPlayerController.Team < CsTeam.Terrorist || cCSPlayerController.Team > CsTeam.CounterTerrorist)
        {
            return;
        }

        if((cCSPlayerController.Team == CsTeam.Terrorist && !TNades.HasNades()) || (cCSPlayerController.Team == CsTeam.CounterTerrorist && !CTNades.HasNades()))
        {
            return;
        }

        Nades nades = cCSPlayerController.Team == CsTeam.Terrorist ? TNades : CTNades;

        CsItem grenade;
        do{
            grenade = SelectGrenade();
        }
        while(!CheckIfNadeAvailable(nades, grenade));

        nades.RemoveNade(grenade);

        cCSPlayerController.GiveNamedItem(grenade);
    }

    public bool CheckIfNadeAvailable(Nades nades, CsItem nade)
    {
        if (_player == null || cCSPlayerController == null || !_player.player.IsValid)
        {
            return false;
        }

        if (!cCSPlayerController.PawnIsAlive)
        {
            return false;
        }

        if (cCSPlayerController.Team < CsTeam.Terrorist || cCSPlayerController.Team > CsTeam.CounterTerrorist)
        {
            return false;
        }

        switch(nade)
        {
            case CsItem.Flashbang:
            {
                return nades.HasFlashbangs();
            }
            case CsItem.Molotov or CsItem.Incendiary:
            {
                return nades.HasMolotovs();
            }
            case CsItem.HEGrenade:
            {
                return nades.HasHeGrenades();
            }
            case CsItem.SmokeGrenade:
            {
                return nades.HasSmokes();
            }
        }

        return false;
    }

    public void AllocateArmor(bool give_full = true)
    {
        if (_player == null || cCSPlayerController == null || !_player.player.IsValid)
        {
            return;
        }

        if (!cCSPlayerController.PawnIsAlive)
        {
            return;
        }

        if (cCSPlayerController.Team < CsTeam.Terrorist || cCSPlayerController.Team > CsTeam.CounterTerrorist)
        {
            return;
        }

        cCSPlayerController.GiveNamedItem(give_full ? CsItem.KevlarHelmet : CsItem.Kevlar);
    }

    public void Allocate()
    {
        if (_player == null || cCSPlayerController == null || !_player.player.IsValid)
        {
            return;
        }

        if (!cCSPlayerController.PawnIsAlive)
        {
            return;
        }

        if (cCSPlayerController.Team < CsTeam.Terrorist || cCSPlayerController.Team > CsTeam.CounterTerrorist)
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
            if (cCSPlayerController.Team == CsTeam.Terrorist)
            {
                primary = PrimaryT[PrimaryWeaponT].Item;
            }
            else
            {
                primary = PrimaryCt[PrimaryWeaponCt].Item;
            }
        }

        string secondary;

        if (ShouldGiveAwp)
        {
            secondary = "weapon_deagle";
        }
        else
        {
            if (cCSPlayerController.Team == CsTeam.Terrorist)
            {
                secondary = PistolsT[SecondaryWeaponT].Item;
            }
            else
            {
                secondary = PistolsCT[SecondaryWeaponCt].Item;
            }
        }

        cCSPlayerController.GiveNamedItem(primary);
        cCSPlayerController.GiveNamedItem(secondary);
        cCSPlayerController.GiveNamedItem(CsItem.Knife);

        if (cCSPlayerController.Team == CsTeam.CounterTerrorist)
        {
           GiveCtEquipment();
        }
    }

    public void AllocatePistolRound()
    {
        if (_player == null || cCSPlayerController == null || !_player.player.IsValid)
        {
            return;
        }

        if (!cCSPlayerController.PawnIsAlive)
        {
            return;
        }

        if (cCSPlayerController.Team < CsTeam.Terrorist || cCSPlayerController.Team > CsTeam.CounterTerrorist)
        {
            return;
        }

        string secondary = Core.Config.PistolRound.GetWeaponByTeam(cCSPlayerController.Team);

        cCSPlayerController.GiveNamedItem(secondary);
        cCSPlayerController.GiveNamedItem(CsItem.Knife);

        if (cCSPlayerController.Team == CsTeam.CounterTerrorist)
        {
           GiveCtEquipment();
        }
    }

    public void AllocateVote(Vote vote)
    {
         if (_player == null || cCSPlayerController == null || !_player.player.IsValid)
        {
            return;
        }

        if (!cCSPlayerController.PawnIsAlive)
        {
            return;
        }

        if (cCSPlayerController.Team < CsTeam.Terrorist || cCSPlayerController.Team > CsTeam.CounterTerrorist)
        {
            return;
        }

        List<string> weapons = cCSPlayerController.Team == CsTeam.Terrorist ? vote.weapons_t : vote.weapons_ct;

        if(weapons.Count > 1)
        {
            ShowWeaponSelectionMenu(cCSPlayerController, weapons, WeaponSelectionTime);
        } else {
            cCSPlayerController.GiveNamedItem("weapon_" + weapons.First());
        }

        if(vote.GiveKnife)
            cCSPlayerController.GiveNamedItem(CsItem.Knife);

        if (cCSPlayerController.Team == CsTeam.CounterTerrorist)
        {
           GiveCtEquipment();
        }
    }

    private CsItem SelectGrenade()
    {
        var grenade = CsItem.HEGrenade;

        var rand = new Random().Next(0,4);

        switch(rand)
        {
            case 0: 
                grenade = CsItem.HEGrenade;
                break;
            case 1:
                grenade = CsItem.Flashbang;
                break;
            case 2:
                grenade = CsItem.SmokeGrenade;
                break;
            case 3:
                grenade = cCSPlayerController.Team == CsTeam.Terrorist ? CsItem.Molotov : CsItem.Incendiary;
                break;
        }

        return grenade;
    }

    private void GiveCtEquipment()
    {
        if (
            cCSPlayerController.Team == CsTeam.CounterTerrorist
            && cCSPlayerController.PlayerPawn.IsValid
            && cCSPlayerController.PlayerPawn.Value != null
            && cCSPlayerController.PlayerPawn.Value.IsValid
            && cCSPlayerController.PlayerPawn.Value.ItemServices != null
        ) {
            var itemServices = new CCSPlayer_ItemServices(cCSPlayerController.PlayerPawn.Value.ItemServices.Handle)
            {
                HasDefuser = true
            };
        }
    }
}