using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace Weapons;

public enum GiveAWP
{
    NEVER,
    SOMETIMES,
    ALWAYS
}

public class Weapon
{
    public CsItem item;
    public string display_name = string.Empty;

    public Weapon(CsItem item, string display_name)
    {
        this.item = item;
        this.display_name = display_name;
    }
}

public class Allocator
{
    public enum WeaponType
    {
        PRIMARY_T,
        PRIMARY_CT,
        SECONDARY
    };

    public static Weapon[] primary_t =
    {
        new Weapon(CsItem.AK47, "AK-47"),
        new Weapon(CsItem.SG553, "SG 553")
    };

    public static Weapon[] primary_ct =
    {
        new Weapon(CsItem.M4A1, "M4A4"),
        new Weapon(CsItem.M4A1S, "M4A1-S"),
        new Weapon(CsItem.AUG, "AUG")
    };

    public static Weapon[] pistols = 
    {
        new Weapon(CsItem.Glock, "Glock-18"),
        new Weapon(CsItem.USPS, "USP-S"),
        new Weapon(CsItem.P250, "P250"),
        new Weapon(CsItem.Tec9, "Tec-9"),
        new Weapon(CsItem.FiveSeven, "Five-Seven"),
        new Weapon(CsItem.Deagle, "Desert Eagle")
    };

    public CCSPlayerController player;

    public int primaryWeapon_t = 0;
    public int primaryWeapon_ct = 0;
    public int secondaryWeapon = 0;

    public GiveAWP giveAWP = GiveAWP.NEVER;
    public bool give_awp = false;

    public char nades = '\0';

    public Allocator(CCSPlayerController player)
    {
        this.player = player;
    }

    public static int GetWeaponIndex(string weapon, WeaponType type)
    {
        switch(type)
        {
            case WeaponType.PRIMARY_T:
            {
                for(int i = 0; i < primary_t.Length; i++)
                {
                    if(primary_t[i].display_name == weapon)
                    {
                        return i;
                    }
                }
                break;
            }
            case WeaponType.PRIMARY_CT:
            {
                for(int i = 0; i < primary_ct.Length; i++)
                {
                    if(primary_ct[i].display_name == weapon)
                    {
                        return i;
                    }
                }
                break;
            }
            case WeaponType.SECONDARY:
            {
                for(int i = 0; i < pistols.Length; i++)
                {
                    if(pistols[i].display_name == weapon)
                    {
                        return i;
                    }
                }
                break;
            }
        }

        return -1;
    }

    public bool SetupGiveAwp()
    {
        bool give_awp;
        if (giveAWP == GiveAWP.ALWAYS)
        {
            give_awp = true;
        }
        else if (giveAWP == GiveAWP.NEVER)
        {
            give_awp = false;
        }
        else
        {
            give_awp = new Random().Next(0, 2) == 1;
        }

        return give_awp;
    }

    public void Allocate()
    {
        if (player == null || !player.IsValid)
        {
            return;
        }

        if(!player.PawnIsAlive)
        {
            return;
        }

        if(player.TeamNum < (byte)CsTeam.Terrorist || player.TeamNum > (byte)CsTeam.CounterTerrorist)
        {
            return;
        }

        CsItem primary;
        if (give_awp)
        {
            primary = CsItem.AWP;
        }
        else
        {
            if(player.TeamNum == (byte)CsTeam.Terrorist)
            {
                primary = primary_t[primaryWeapon_t].item;
            }else{
                primary = primary_ct[primaryWeapon_ct].item;
            }
        }

        CsItem secondary = pistols[secondaryWeapon].item;

        player.RemoveWeapons();
        player.GiveNamedItem(primary);
        player.GiveNamedItem(secondary);
        player.GiveNamedItem(CsItem.Knife);

        CsItem nade = SelectNade();
        player.GiveNamedItem(nade);

        if(player.TeamNum == (byte)CsTeam.CounterTerrorist)
        {
           GiveCTEquipment();
        }
    }

    private CsItem SelectNade()
    {
        CsItem nade = CsItem.HEGrenade;

        int rand = new Random().Next(0,4);

        switch(rand)
        {
            case 0: 
                nade = CsItem.HEGrenade;
                break;
            case 1:
                nade = CsItem.Flashbang;
                break;
            case 2:
                nade = CsItem.SmokeGrenade;
                break;
            case 3:
                nade = (CsTeam)player.TeamNum == CsTeam.Terrorist ? CsItem.Molotov : CsItem.Incendiary;
                break;
        }

        return nade;
    }

    private void GiveCTEquipment()
    {
         player.GiveNamedItem(CsItem.KevlarHelmet);

             if (
            (CsTeam)player.TeamNum == CsTeam.CounterTerrorist
            && player.PlayerPawn.IsValid
            && player.PlayerPawn.Value != null
            && player.PlayerPawn.Value.IsValid
            && player.PlayerPawn.Value.ItemServices != null
            ) {

            var itemServices = new CCSPlayer_ItemServices(player.PlayerPawn.Value.ItemServices.Handle)
            {
                HasDefuser = true
            };
        }
    }
}