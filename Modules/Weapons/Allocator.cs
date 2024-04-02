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
        Secondary
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

    public static List<Weapon> Pistols = new()
    {
        new("weapon_glock", "Glock-18"),
        new("weapon_usp_silencer", "USP-S"),
        new("weapon_p250", "P250"),
        new("weapon_tec9", "Tec-9"),
        new("weapon_fiveseven", "Five-Seven"),
        new("weapon_deagle", "Desert Eagle")
    };

    private readonly Player _player;

    private CCSPlayerController cCSPlayerController => _player.player;

    public int PrimaryWeaponT = 0;
    public int PrimaryWeaponCt = 0;
    public int SecondaryWeapon = 0;

    public GiveAwp GiveAwp = GiveAwp.Never;
    public bool ShouldGiveAwp = false;

    public Allocator(Player player)
    {
        _player = player;
    }

    public static int GetWeaponIndex(string weapon, WeaponType type)
    {
        switch(type)
        {
            case WeaponType.PrimaryT:
                return PrimaryT.FindIndex(w => w.DisplayName == weapon);
            case WeaponType.PrimaryCt:
                return PrimaryCt.FindIndex(w => w.DisplayName == weapon);
            case WeaponType.Secondary:
                return Pistols.FindIndex(w => w.DisplayName == weapon);
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

        CsItem grenade = SelectGrenade();
        cCSPlayerController.GiveNamedItem(grenade);
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

        string secondary = Pistols[SecondaryWeapon].Item;

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

    private void GiveArmor()
    {
        cCSPlayerController.GiveNamedItem(CsItem.KevlarHelmet); 
    }
}