using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Utils;

namespace RetakesAllocator.Modules.Weapons;

public class Menu
{
    private static readonly Dictionary<CCSPlayerController, Timer> Timers = new();

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

    public static void ShowWeaponSelectionMenu(CCSPlayerController player, List<string> weapons, int time)
    {
        var centerHtmlMenu = new CenterHtmlMenu($"Select a weapon [{time} Seconds Left]", Plugin);

        foreach (var weapon in weapons)
        {
            centerHtmlMenu.AddMenuOption(WeaponToDisplayName(weapon), (p, c) => OnWeaponSelect(p, c, weapon));
        }

        MenuManager.OpenCenterHtmlMenu(Plugin, player, centerHtmlMenu);
        
        Timers.Clear();

        var timer = Plugin.AddTimer(1f, () => Countdown(centerHtmlMenu, player, weapons, time));
        Timers.Add(player, timer);
    }

    private static void OnWeaponSelect(CCSPlayerController player, ChatMenuOption? option, string weapon)
    {
        if (option == null)
        {
            PrintToChat(player, $"{Prefix} You did not select a weapon!");
            return;
        }

        PrintToChat(player, $"{Prefix} You selected {WeaponToDisplayName(weapon)} as your weapon!");
        player.GiveNamedItem("weapon_" + weapon);

        if (Timers.TryGetValue(player, out var timer))
        {
            timer.Kill();
            Timers.Remove(player);
        }

        MenuManager.CloseActiveMenu(player);
    }

    public static void Countdown(CenterHtmlMenu menu, CCSPlayerController player, List<string> weapons, int seconds)
    {
        menu.Title = $"Select a weapon [{--seconds} Seconds Left]";

        if (seconds == 0)
        {
            GiveRandomWeapon(player, weapons);
            return;
        }
        
        MenuManager.OpenCenterHtmlMenu(Plugin, player, menu);

        var timer = Plugin.AddTimer(1f, () => Countdown(menu, player, weapons, seconds));
        Timers[player] = timer;
    }

    public static void GiveRandomWeapon(CCSPlayerController player, List<string> weapons)
    {   
        var weapon = weapons[new Random().Next(0, weapons.Count)];

        PrintToChat(player, $"{Prefix} You have'nt selected a weapon, giving you a random weapon!");
        PrintToChat(player, $"{Prefix} You received a {WeaponToDisplayName(weapon)}!");
        player.GiveNamedItem("weapon_" + weapon);

        MenuManager.CloseActiveMenu(player);
    }

    private static string WeaponToDisplayName(string weapon)
    {
        return Weapons.TryGetValue(weapon, out var name) ? name : weapon;
    }
}