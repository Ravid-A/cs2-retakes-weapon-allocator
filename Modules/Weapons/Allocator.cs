using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Entities;

namespace RetakesAllocator.Modules.Weapons;

public enum GiveAwp
{
    Never,
    Sometimes,
    Always
}

public class Weapon
{
    public readonly CsItem Item;
    public readonly string DisplayName;

    public Weapon(CsItem item, string displayName)
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

    public static readonly Weapon[] PrimaryT =
    {
        new(CsItem.AK47, "AK-47"),
        new(CsItem.SG553, "SG 553")
    };

    public static readonly Weapon[] PrimaryCt =
    {
        new(CsItem.M4A1, "M4A4"),
        new(CsItem.M4A1S, "M4A1-S"),
        new(CsItem.AUG, "AUG")
    };

    public static readonly Weapon[] Pistols = 
    {
        new(CsItem.Glock, "Glock-18"),
        new(CsItem.USPS, "USP-S"),
        new(CsItem.P250, "P250"),
        new(CsItem.Tec9, "Tec-9"),
        new(CsItem.FiveSeven, "Five-Seven"),
        new(CsItem.Deagle, "Desert Eagle")
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
                for(var i = 0; i < PrimaryT.Length; i++)
                {
                    if (PrimaryT[i].DisplayName == weapon)
                    {
                        return i;
                    }
                }
                break;
            
            case WeaponType.PrimaryCt:
                for(var i = 0; i < PrimaryCt.Length; i++)
                {
                    if (PrimaryCt[i].DisplayName == weapon)
                    {
                        return i;
                    }
                }
                break;
            
            case WeaponType.Secondary:
                for(var i = 0; i < Pistols.Length; i++)
                {
                    if (Pistols[i].DisplayName == weapon)
                    {
                        return i;
                    }
                }
                break;
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

        CsItem primary;
        if (ShouldGiveAwp)
        {
            primary = CsItem.AWP;
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

        var secondary = Pistols[SecondaryWeapon].Item;

        _player.GiveNamedItem(primary);
        _player.GiveNamedItem(secondary);
        _player.GiveNamedItem(CsItem.Knife);

        var grenade = SelectGrenade();
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