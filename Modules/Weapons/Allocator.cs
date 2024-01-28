using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;

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
        new("weapon_sg553", "SG 553")
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

    private readonly CCSPlayerController _player;

    public int PrimaryWeaponT = 0;
    public int PrimaryWeaponCt = 0;
    public int SecondaryWeapon = 0;

    public GiveAwp GiveAwp = GiveAwp.Never;
    public bool ShouldGiveAwp = false;

    public Allocator(CCSPlayerController player)
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

    public void Allocate()
    {
        if (_player == null || !_player.IsValid)
        {
            return;
        }

        if (!_player.PawnIsAlive)
        {
            return;
        }

        if ((CsTeam)_player.TeamNum < CsTeam.Terrorist || (CsTeam)_player.TeamNum > CsTeam.CounterTerrorist)
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
            if ((CsTeam)_player.TeamNum == CsTeam.Terrorist)
            {
                primary = PrimaryT[PrimaryWeaponT].Item;
            }
            else
            {
                primary = PrimaryCt[PrimaryWeaponCt].Item;
            }
        }

        string secondary = Pistols[SecondaryWeapon].Item;

        _player.GiveNamedItem(primary);
        _player.GiveNamedItem(secondary);
        _player.GiveNamedItem(CsItem.Knife);

        CsItem grenade = SelectGrenade();
        _player.GiveNamedItem(grenade);

        if (_player.TeamNum == (byte)CsTeam.CounterTerrorist)
        {
           GiveCtEquipment();
        }

        if(Core.Config.GiveArmor)
            GiveArmor();
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
                grenade = (CsTeam)_player.TeamNum == CsTeam.Terrorist ? CsItem.Molotov : CsItem.Incendiary;
                break;
        }

        return grenade;
    }

    private void GiveCtEquipment()
    {
        _player.GiveNamedItem(CsItem.KevlarHelmet); 

        if (
            (CsTeam)_player.TeamNum == CsTeam.CounterTerrorist
            && _player.PlayerPawn.IsValid
            && _player.PlayerPawn.Value != null
            && _player.PlayerPawn.Value.IsValid
            && _player.PlayerPawn.Value.ItemServices != null
        ) {
            var itemServices = new CCSPlayer_ItemServices(_player.PlayerPawn.Value.ItemServices.Handle)
            {
                HasDefuser = true
            };
        }
    }

    private void GiveArmor()
    {
        _player.GiveNamedItem(CsItem.KevlarHelmet); 
    }
}