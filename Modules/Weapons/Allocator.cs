using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using RetakesAllocator.Modules.Models;
using RetakesAllocator.Modules.Votes;

using static RetakesAllocator.Modules.Weapons.Menu;
using static RetakesAllocator.Modules.Utils;
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
    public enum WeaponType
    {
        PrimaryT,
        PrimaryCt,
        SecondaryT,
        SecondaryCt,
        PistolRoundT,
        PistolRoundCt
    };

    public static List<Weapon> PrimaryT = new()
    {
        new Weapon("weapon_ak47", "AK-47"),
        new Weapon("weapon_galilar", "Galil"),
        new Weapon("weapon_sg556", "SG 553")
    };

    public static List<Weapon> PrimaryCt = new()
    {
        new Weapon("weapon_m4a1_silencer", "M4A1-S"),
        new Weapon("weapon_m4a1", "M4A4"),
        new Weapon("weapon_famas", "FAMAS"),
        new Weapon("weapon_aug", "AUG")
    };

    public static List<Weapon> PistolsT = new()
    {
        new Weapon("weapon_glock", "Glock-18"),
        new Weapon("weapon_deagle", "Desert Eagle"),
        new Weapon("weapon_p250", "P250"),
        new Weapon("weapon_tec9", "Tec-9"),
        new Weapon("weapon_elite", "Dual Berettas"),
        new Weapon("weapon_cz75a", "CZ75"),
        new Weapon("weapon_revolver", "Revolver")
    };

    public static List<Weapon> PistolsCT = new()
    {
        new Weapon("weapon_usp_silencer", "USP-S"),
        new Weapon("weapon_deagle", "Desert Eagle"),
        new Weapon("weapon_p250", "P250"),
        new Weapon("weapon_fiveseven", "Five-SeveN"),
        new Weapon("weapon_elite", "Dual Berettas"),
        new Weapon("weapon_cz75a", "CZ75"),
        new Weapon("weapon_revolver", "Revolver"),
        new Weapon("weapon_hkp2000", "P2000")
    };

    private const string AwpItem = "weapon_awp";

    private static Nades _ctNades = new();
    private static Nades _nades = new();

    public int PrimaryWeaponT = 0;
    public int PrimaryWeaponCt = 0;
    public int SecondaryWeaponT = 0;
    public int SecondaryWeaponCt = 0;
    public int PistolRoundWeaponT = GetWeaponIndex(Core.Config?.PistolRound.WeaponT ?? "weapon_glock", PistolsT);
    public int PistolRoundWeaponCt = GetWeaponIndex(Core.Config?.PistolRound.WeaponCt ?? "weapon_usp_silencer", PistolsCT);

    public GiveAwp GiveAwp = GiveAwp.Never;
    public bool ShouldGiveAwp = false;

    public static void ResetNades()
    {
        if (Core.NadesConfig is null)
        {
            return;
        }

        _ctNades = new Nades(Core.NadesConfig.CtNades);
        _nades = new Nades(Core.NadesConfig.TNades);
    }

    private static int GetWeaponIndex(string item, List<Weapon> weapons)
    {
        var index = weapons.FindIndex(w => w.Item == item);
        return index < 0 ? 0 : index;
    }

    public static int GetWeaponIndex(string weapon, WeaponType type)
    {
        var weapons = type switch
        {
            WeaponType.PrimaryT => PrimaryT,
            WeaponType.PrimaryCt => PrimaryCt,
            WeaponType.SecondaryT or WeaponType.PistolRoundT => PistolsT,
            WeaponType.SecondaryCt or WeaponType.PistolRoundCt => PistolsCT,
            _ => null
        };

        return weapons?.FindIndex(w => w.DisplayName == weapon) ?? -1;
    }

    /// <summary>
    /// Resolves a configured weapon list entry defensively. A config hot reload can
    /// shrink or empty a list while a stored preference index still points past its
    /// end, which used to throw out of the spawn path and skip the whole allocation.
    /// </summary>
    private static string? PickWeapon(List<Weapon> weapons, int index)
    {
        if (weapons.Count == 0)
        {
            return null;
        }

        if (index < 0 || index >= weapons.Count)
        {
            index = 0;
        }

        return weapons[index].Item;
    }

    /// <summary>The correct default knife for the team. `weapon_knife` is the CT model.</summary>
    private static CsItem KnifeFor(CsTeam team) => team == CsTeam.Terrorist ? CsItem.KnifeT : CsItem.KnifeCT;

    private static bool IsPlayingTeam(CsTeam team) => team is CsTeam.Terrorist or CsTeam.CounterTerrorist;

    /// <summary>
    /// Returns the controller when it is valid, alive and on a playing team; null
    /// otherwise. Resolving it once per allocation avoids repeating the entity lookup
    /// and the same three guards at the top of every method.
    /// </summary>
    private CCSPlayerController? GetAllocatableController()
    {
        var controller = player.Controller;

        if (controller is null || !controller.IsValid || !controller.PawnIsAlive)
        {
            return null;
        }

        return IsPlayingTeam(controller.Team) ? controller : null;
    }

    public bool SetupGiveAwp()
    {
        if (!HasAwpAccess(player.Controller))
        {
            return false;
        }

        return GiveAwp switch
        {
            GiveAwp.Always => true,
            GiveAwp.Never => false,
            _ => Random.Shared.Next(0, 2) == 1
        };
    }

    public void AllocateNades()
    {
        var controller = GetAllocatableController();

        if (controller is null)
        {
            return;
        }

        var team = controller.Team;
        var nades = team == CsTeam.Terrorist ? _nades : _ctNades;

        var grenade = SelectGrenade(nades, team);

        if (grenade is null)
        {
            return;
        }

        nades.RemoveNade(grenade.Value);
        controller.GiveNamedItem(grenade.Value);
    }

    public void AllocateArmor(bool giveFull = true)
    {
        var controller = GetAllocatableController();

        if (controller is null)
        {
            return;
        }

        controller.GiveNamedItem(giveFull ? CsItem.KevlarHelmet : CsItem.Kevlar);
    }

    public void Allocate()
    {
        var controller = GetAllocatableController();

        if (controller is null)
        {
            return;
        }

        var team = controller.Team;
        var isT = team == CsTeam.Terrorist;

        string? primary;
        string? secondary;

        if (ShouldGiveAwp)
        {
            primary = AwpItem;
            secondary = "weapon_deagle";
        }
        else
        {
            primary = GetPrimaryWeapon(team);
            secondary = isT
                ? PickWeapon(PistolsT, SecondaryWeaponT)
                : PickWeapon(PistolsCT, SecondaryWeaponCt);
        }

        if (primary is not null)
        {
            controller.GiveNamedItem(primary);
        }

        if (secondary is not null)
        {
            controller.GiveNamedItem(secondary);
        }

        controller.GiveNamedItem(KnifeFor(team));

        if (!isT)
        {
            GiveCtEquipment(controller);
        }
    }

    private string? GetPrimaryWeapon(CsTeam team)
    {
        var isT = team == CsTeam.Terrorist;
        var weapons = isT ? PrimaryT : PrimaryCt;
        var primary = PickWeapon(weapons, isT ? PrimaryWeaponT : PrimaryWeaponCt);

        if (primary is null)
        {
            return null;
        }

        if (!primary.Equals(AwpItem, StringComparison.OrdinalIgnoreCase) || HasAwpAccess(player.Controller))
        {
            return primary;
        }

        return weapons.Find(w => !w.Item.Equals(AwpItem, StringComparison.OrdinalIgnoreCase))?.Item
            ?? (isT ? "weapon_ak47" : "weapon_m4a1");
    }

    public void AllocatePistolRound()
    {
        var controller = GetAllocatableController();

        if (controller is null)
        {
            return;
        }

        var team = controller.Team;
        var isT = team == CsTeam.Terrorist;

        var secondary = isT
            ? PickWeapon(PistolsT, PistolRoundWeaponT)
            : PickWeapon(PistolsCT, PistolRoundWeaponCt);

        if (secondary is not null)
        {
            controller.GiveNamedItem(secondary);
        }

        controller.GiveNamedItem(KnifeFor(team));

        if (!isT)
        {
            GiveCtEquipment(controller);
        }
    }

    public void AllocateVote(Vote vote)
    {
        var controller = GetAllocatableController();

        if (controller is null)
        {
            return;
        }

        var team = controller.Team;
        var isT = team == CsTeam.Terrorist;

        var weapons = isT ? vote.WeaponsT : vote.WeaponsCt;

        if (!HasAwpAccess(controller))
        {
            weapons = weapons.Where(w => !w.Equals("awp", StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (weapons.Count > 1)
        {
            ShowWeaponSelectionMenu(controller, weapons, WeaponSelectionTime);
        }
        else if (weapons.Count == 1)
        {
            controller.GiveNamedItem("weapon_" + weapons[0]);
        }

        if (vote.GiveKnife)
        {
            controller.GiveNamedItem(KnifeFor(team));
        }

        if (!isT)
        {
            GiveCtEquipment(controller);
        }
    }

    /// <summary>
    /// Picks uniformly from the grenade types the team kit still has left. The old
    /// implementation drew a random type and retried until it happened to hit an
    /// available one, which could spin for many iterations (and allocated a new
    /// <c>Random</c> plus re-validated the player on every attempt).
    /// </summary>
    private static CsItem? SelectGrenade(Nades nades, CsTeam team)
    {
        Span<CsItem> available = stackalloc CsItem[4];
        var count = 0;

        if (nades.HasHeGrenades())
        {
            available[count++] = CsItem.HEGrenade;
        }

        if (nades.HasFlashbangs())
        {
            available[count++] = CsItem.Flashbang;
        }

        if (nades.HasSmokes())
        {
            available[count++] = CsItem.SmokeGrenade;
        }

        if (nades.HasMolotovs())
        {
            available[count++] = team == CsTeam.Terrorist ? CsItem.Molotov : CsItem.Incendiary;
        }

        return count == 0 ? null : available[Random.Shared.Next(count)];
    }

    private static void GiveCtEquipment(CCSPlayerController controller)
    {
        var pawn = controller.PlayerPawn.Value;

        if (pawn is null || !pawn.IsValid || pawn.ItemServices is null)
        {
            return;
        }

        new CCSPlayer_ItemServices(pawn.ItemServices.Handle)
        {
            HasDefuser = true
        };
    }
}
