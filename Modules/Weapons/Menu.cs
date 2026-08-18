using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Utils;
using static RetakesAllocator.Modules.Weapons.Allocator;

namespace RetakesAllocator.Modules.Weapons;

public class Menu
{
    /// <summary>A weapon-vote selection still waiting on a player, keyed by slot.</summary>
    private sealed class PendingSelection
    {
        public required CenterHtmlMenu Menu { get; init; }
        public required List<string> Weapons { get; init; }
        public int SecondsLeft;
    }

    // One shared countdown for every player instead of a per-player timer that
    // re-armed itself every second. With 20+ players and a 5s window that was ~100
    // native timers per vote round; now it is exactly one repeating timer.
    private static readonly Dictionary<int, PendingSelection> Pending = new();
    private static readonly List<int> FinishedSlots = new();
    private static Timer? _countdownTimer;

    private static readonly Dictionary<string, string> Weapons = new Dictionary<string, string>()
    {
        {"deagle", "Desert Eagle"},
        {"elite", "Dual Berettas"},
        {"fiveseven", "Five-SeveN"},
        {"glock", "Glock-18"},
        {"tec9", "Tec-9"},
        {"hkp2000", "P2000"},
        {"p250", "P250"},
        {"usp_silencer", "USP-S"},
        {"cz75a", "CZ75-Auto"},
        {"revolver", "R8 Revolver"},
        {"xm1014", "XM1014"},
        {"mag7", "MAG-7"},
        {"sawedoff", "Sawed-Off"},
        {"nova", "Nova"},
        {"mac10", "MAC-10"},
        {"mp5sd", "MP5-SD"},
        {"p90", "P90"},
        {"ump45", "UMP-45"},
        {"bizon", "PP-Bizon"},
        {"mp7", "MP7"},
        {"mp9", "MP9"},
        {"ak47", "AK-47"},
        {"aug", "AUG"},
        {"famas", "FAMAS"},
        {"galilar", "Galil AR"},
        {"m4a4", "M4A4"},
        {"sg556", "SG 553"},
        {"m4a1_silencer", "M4A1-S"},
        {"m4a1", "M4A1"}, // Added M4A1
        {"m249", "M249"},
        {"negev", "Negev"},
        {"awp", "AWP"},
        {"scar20", "SCAR-20"},
        {"g3sg1", "G3SG1"},
        {"ssg08", "SSG 08"}
    };

    /// <summary>
    /// Opens a menu, properly closing whatever was open first.
    /// <para>
    /// This matters for performance: every <c>CenterHtmlMenuInstance</c> registers an
    /// <c>OnTick</c> listener in its constructor, and CounterStrikeSharp's
    /// <c>MenuManager.OpenCenterHtmlMenu</c> only calls <c>Reset()</c> on the previous
    /// instance — never <c>Close()</c> — so that listener is never unregistered.
    /// Without this, every menu step permanently added another per-tick delegate for
    /// the lifetime of the map.
    /// </para>
    /// </summary>
    private static void OpenMenu(CCSPlayerController? player, CenterHtmlMenu menu)
    {
        if (player is null || !player.IsValid)
        {
            return;
        }

        CloseMenu(player);
        MenuManager.OpenCenterHtmlMenu(Plugin, player, menu);
    }

    /// <summary>Closes the player's active menu and unregisters its per-tick listener.</summary>
    public static void CloseMenu(CCSPlayerController? player)
    {
        if (player is null || !player.IsValid)
        {
            return;
        }

        var active = MenuManager.GetActiveMenu(player);

        if (active is null)
        {
            return;
        }

        try
        {
            active.Close();
        }
        catch (Exception)
        {
            // Third-party instance without a Close override, or a controller that went
            // away mid-teardown. Either way the menu must still be dropped.
            MenuManager.CloseActiveMenu(player);
        }
    }

    public static void OpenPistolRoundTMenu(CCSPlayerController player)
    {
        var centerHtmlMenu = new CenterHtmlMenu("[ T ] Pistol Round", Plugin);

        if (Core.Config.AddSkipOption)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenPistolRoundCTMenu(p));
        }

        foreach (var weapon in PistolsT)
        {
            centerHtmlMenu.AddMenuOption(weapon.DisplayName, OnPistolRoundTSelect);
        }

        OpenMenu(player, centerHtmlMenu);
    }

    private static void OnPistolRoundTSelect(CCSPlayerController player, ChatMenuOption? option)
    {
        if (!TrySetPreference(player, option, WeaponType.PistolRoundT, "T Pistol Round"))
        {
            return;
        }

        OpenPistolRoundCTMenu(player);
    }

    public static void OpenPistolRoundCTMenu(CCSPlayerController player)
    {
        var centerHtmlMenu = new CenterHtmlMenu("[ CT ] Pistol Round", Plugin);

        if (Core.Config.AddSkipOption)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenTPrimaryMenu(p));
        }

        foreach (var weapon in PistolsCT)
        {
            centerHtmlMenu.AddMenuOption(weapon.DisplayName, OnPistolRoundCTSelect);
        }

        OpenMenu(player, centerHtmlMenu);
    }

    private static void OnPistolRoundCTSelect(CCSPlayerController player, ChatMenuOption? option)
    {
        if (!TrySetPreference(player, option, WeaponType.PistolRoundCt, "CT Pistol Round"))
        {
            return;
        }

        OpenTPrimaryMenu(player);
    }

    public static void OpenTPrimaryMenu(CCSPlayerController player, bool showNext = true)
    {
        var centerHtmlMenu = new CenterHtmlMenu("[ T ] Primary Weapon", Plugin);

        if (Core.Config.AddSkipOption && showNext)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenCTPrimaryMenu(p));
        }

        // Resolved once instead of per weapon: this walks the admin tables and reads
        // the player's authorized SteamID across the native boundary.
        var awpAccess = HasAwpAccess(player);

        foreach (var weapon in PrimaryT)
        {
            if (!awpAccess && weapon.Item.Equals("weapon_awp", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            centerHtmlMenu.AddMenuOption(weapon.DisplayName, (p, option) => OnTPrimarySelect(p, option, showNext));
        }

        OpenMenu(player, centerHtmlMenu);
    }

    private static void OnTPrimarySelect(CCSPlayerController player, ChatMenuOption? option, bool showNext)
    {
        if (!TrySetPreference(player, option, WeaponType.PrimaryT, "T Primary"))
        {
            return;
        }

        if (showNext)
        {
            OpenCTPrimaryMenu(player);
            return;
        }

        CloseMenu(player);
    }

    public static void OpenCTPrimaryMenu(CCSPlayerController player, bool showNext = true)
    {
        var centerHtmlMenu = new CenterHtmlMenu("[ CT ] Primary Weapon", Plugin);

        if (Core.Config.AddSkipOption && showNext)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenSecondaryTMenu(p));
        }

        var awpAccess = HasAwpAccess(player);

        foreach (var weapon in PrimaryCt)
        {
            if (!awpAccess && weapon.Item.Equals("weapon_awp", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            centerHtmlMenu.AddMenuOption(weapon.DisplayName, (p, option) => OnCTPrimarySelect(p, option, showNext));
        }

        OpenMenu(player, centerHtmlMenu);
    }

    private static void OnCTPrimarySelect(CCSPlayerController player, ChatMenuOption? option, bool showNext)
    {
        if (!TrySetPreference(player, option, WeaponType.PrimaryCt, "CT Primary"))
        {
            return;
        }

        if (showNext)
        {
            OpenSecondaryTMenu(player);
            return;
        }

        CloseMenu(player);
    }

    public static void OpenSecondaryTMenu(CCSPlayerController player, bool showNext = true)
    {
        var centerHtmlMenu = new CenterHtmlMenu("[ T ] Secondary Weapon", Plugin);

        if (Core.Config.AddSkipOption && showNext)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenSecondaryCTMenu(p));
        }

        foreach (var weapon in PistolsT)
        {
            centerHtmlMenu.AddMenuOption(weapon.DisplayName, (p, option) => OnSecondaryTSelect(p, option, showNext));
        }

        OpenMenu(player, centerHtmlMenu);
    }

    private static void OnSecondaryTSelect(CCSPlayerController player, ChatMenuOption? option, bool showNext = true)
    {
        if (!TrySetPreference(player, option, WeaponType.SecondaryT, "T Secondary"))
        {
            return;
        }

        if (showNext)
        {
            OpenSecondaryCTMenu(player);
            return;
        }

        CloseMenu(player);
    }

    public static void OpenSecondaryCTMenu(CCSPlayerController player, bool showNext = true)
    {
        var centerHtmlMenu = new CenterHtmlMenu("[ CT ] Secondary Weapon", Plugin);

        if (Core.Config.AddSkipOption && showNext)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenAwpMenuIfAllowed(p));
        }

        foreach (var weapon in PistolsCT)
        {
            centerHtmlMenu.AddMenuOption(weapon.DisplayName, (p, option) => OnSecondaryCTSelect(p, option, showNext));
        }

        OpenMenu(player, centerHtmlMenu);
    }

    private static void OnSecondaryCTSelect(CCSPlayerController player, ChatMenuOption? option, bool showNext = true)
    {
        if (!TrySetPreference(player, option, WeaponType.SecondaryCt, "CT Secondary"))
        {
            return;
        }

        if (showNext)
        {
            OpenAwpMenuIfAllowed(player);
            return;
        }

        CloseMenu(player);
    }

    /// <summary>
    /// Shared handler for the six preference menus. Returns false when the selection
    /// could not be applied (no option, untracked player, or a weapon that no longer
    /// exists because the config was reloaded while the menu was open — that used to
    /// store an index of -1 and throw on the next spawn).
    /// </summary>
    private static bool TrySetPreference(CCSPlayerController player, ChatMenuOption? option, WeaponType type, string label)
    {
        if (option == null)
        {
            PrintToChat(player, $"{Prefix} You did not select a weapon!");
            return false;
        }

        var playerObj = FindPlayer(player);

        if (playerObj is null)
        {
            return false;
        }

        var index = GetWeaponIndex(option.Text, type);

        if (index < 0)
        {
            PrintToChat(player, $"{Prefix} That weapon is no longer available.");
            return false;
        }

        var allocator = playerObj.WeaponsAllocator;

        switch (type)
        {
            case WeaponType.PrimaryT:
                allocator.PrimaryWeaponT = index;
                break;
            case WeaponType.PrimaryCt:
                allocator.PrimaryWeaponCt = index;
                break;
            case WeaponType.SecondaryT:
                allocator.SecondaryWeaponT = index;
                break;
            case WeaponType.SecondaryCt:
                allocator.SecondaryWeaponCt = index;
                break;
            case WeaponType.PistolRoundT:
                allocator.PistolRoundWeaponT = index;
                break;
            case WeaponType.PistolRoundCt:
                allocator.PistolRoundWeaponCt = index;
                break;
        }

        PrintToChat(player, $"{Prefix} You selected {option.Text} as {label}!");
        return true;
    }

    private static void OpenAwpMenuIfAllowed(CCSPlayerController player)
    {
        if (HasAwpAccess(player))
        {
            OpenGiveAWPMenu(player);
            return;
        }

        CloseMenu(player);
    }

    public static void OpenGiveAWPMenu(CCSPlayerController player)
    {
        if (!HasAwpAccess(player))
        {
            PrintToChat(player, $"{Prefix} The AWP menu is for VIP players only.");
            CloseMenu(player);
            return;
        }

        var centerHtmlMenu = new CenterHtmlMenu("When To Get AWP", Plugin);

        centerHtmlMenu.AddMenuOption("Never", OnGiveAWPSelect);
        centerHtmlMenu.AddMenuOption("Sometimes", OnGiveAWPSelect);
        centerHtmlMenu.AddMenuOption("Always", OnGiveAWPSelect);

        OpenMenu(player, centerHtmlMenu);
    }

    private static void OnGiveAWPSelect(CCSPlayerController player, ChatMenuOption? option)
    {
        if (option == null)
        {
            PrintToChat(player, $"{Prefix} You did not select an option!");
            return;
        }

        var playerObj = FindPlayer(player);

        if (playerObj is null)
        {
            return;
        }

        PrintToChat(player, $"{Prefix} You selected {option.Text} as when to give the AWP!");

        switch (option.Text)
        {
            case "Never":
                playerObj.WeaponsAllocator.GiveAwp = GiveAwp.Never;
                break;
            case "Sometimes":
                playerObj.WeaponsAllocator.GiveAwp = GiveAwp.Sometimes;
                break;
            case "Always":
                playerObj.WeaponsAllocator.GiveAwp = GiveAwp.Always;
                break;
        }

        CloseMenu(player);
    }

    public static void ShowWeaponSelectionMenu(CCSPlayerController player, List<string> weapons, int time)
    {
        if (player is null || !player.IsValid || weapons.Count == 0)
        {
            return;
        }

        if (time < 1)
        {
            time = 1;
        }

        var centerHtmlMenu = new CenterHtmlMenu($"Select a weapon [{time} Seconds Left]", Plugin);

        foreach (var weapon in weapons)
        {
            var choice = weapon;
            centerHtmlMenu.AddMenuOption(WeaponToDisplayName(choice), (p, _) => OnWeaponSelect(p, choice));
        }

        Pending[player.Slot] = new PendingSelection
        {
            Menu = centerHtmlMenu,
            Weapons = weapons,
            SecondsLeft = time,
        };

        OpenMenu(player, centerHtmlMenu);
        EnsureCountdownRunning();
    }

    private static void OnWeaponSelect(CCSPlayerController player, string weapon)
    {
        if (player is null || !player.IsValid)
        {
            return;
        }

        // Removing the entry here is what stops the countdown from also handing out a
        // random weapon. The old code stored the per-player timer in a dictionary that
        // was Clear()ed on every call, so all but the last player lost their handle and
        // got a second, random weapon when their countdown expired.
        Pending.Remove(player.Slot);

        PrintToChat(player, $"{Prefix} You selected {WeaponToDisplayName(weapon)} as your weapon!");
        player.GiveNamedItem("weapon_" + weapon);

        CloseMenu(player);

        if (Pending.Count == 0)
        {
            StopCountdown();
        }
    }

    private static void EnsureCountdownRunning()
    {
        _countdownTimer ??= Plugin.AddTimer(
            1f,
            CountdownTick,
            TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
    }

    private static void StopCountdown()
    {
        _countdownTimer?.Kill();
        _countdownTimer = null;
    }

    private static void CountdownTick()
    {
        if (Pending.Count == 0)
        {
            StopCountdown();
            return;
        }

        FinishedSlots.Clear();

        foreach (var (slot, selection) in Pending)
        {
            // One player throwing must not abort the tick for everybody else, or the
            // rest of the queue would never be drained and would be handed a second
            // random weapon on the next tick.
            try
            {
                var controller = Utilities.GetPlayerFromSlot(slot);

                if (controller is null || !controller.IsValid)
                {
                    FinishedSlots.Add(slot);
                    continue;
                }

                selection.SecondsLeft--;

                if (selection.SecondsLeft <= 0)
                {
                    FinishedSlots.Add(slot);
                    GiveRandomWeapon(controller, selection.Weapons);
                    continue;
                }

                // The live menu instance re-renders every tick from this Title, so there
                // is no need to reopen the menu (which is what leaked a per-tick listener
                // per player per second).
                selection.Menu.Title = $"Select a weapon [{selection.SecondsLeft} Seconds Left]";
            }
            catch (Exception e)
            {
                FinishedSlots.Add(slot);
                Plugin.Logger.LogError(e, "Weapon selection countdown failed for slot {Slot}", slot);
            }
        }

        foreach (var slot in FinishedSlots)
        {
            Pending.Remove(slot);
        }

        if (Pending.Count == 0)
        {
            StopCountdown();
        }
    }

    /// <summary>Drops all pending selections (round change, map change, unload).</summary>
    public static void ClearPendingSelections()
    {
        Pending.Clear();
        StopCountdown();
    }

    /// <summary>Drops a single player's pending selection (disconnect).</summary>
    public static void ClearPendingSelection(int slot)
    {
        if (Pending.Remove(slot) && Pending.Count == 0)
        {
            StopCountdown();
        }
    }

    public static void GiveRandomWeapon(CCSPlayerController player, List<string> weapons)
    {
        if (player is null || !player.IsValid || weapons.Count == 0)
        {
            return;
        }

        var weapon = weapons[Random.Shared.Next(weapons.Count)];

        PrintToChat(player, $"{Prefix} You haven't selected a weapon, giving you a random weapon!");
        PrintToChat(player, $"{Prefix} You received a {WeaponToDisplayName(weapon)}!");
        player.GiveNamedItem("weapon_" + weapon);

        CloseMenu(player);
    }

    private static string WeaponToDisplayName(string weapon)
    {
        return Weapons.TryGetValue(weapon, out var name) ? name : weapon;
    }
}
