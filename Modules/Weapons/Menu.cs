using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using RetakesAllocator.Modules.Models;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Utils;
using static RetakesAllocator.Modules.Weapons.Allocator;

namespace RetakesAllocator.Modules.Weapons;

public class Menu
{
    private static Dictionary<CCSPlayerController, Timer> timers = new();

    private static Dictionary<string, string> c2Weapons = new Dictionary<string, string>()
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

    public static void OpenTPrimaryMenu(CCSPlayerController player, bool showNext = true)
    {
        CenterHtmlMenu centerHtmlMenu = new CenterHtmlMenu($"{PREFIX} Select a T Primary Weapon", Plugin);

        if(Core.Config.AddSkipOption && showNext)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenCTPrimaryMenu(p));
        }

        foreach (Weapon weapon in PrimaryT)
        {
            centerHtmlMenu.AddMenuOption(weapon.DisplayName, (CCSPlayerController player, ChatMenuOption option) => OnTPrimarySelect(player, option, showNext));
        }

        MenuManager.OpenCenterHtmlMenu(Plugin, player, centerHtmlMenu);
    }

    private static void OnTPrimarySelect(CCSPlayerController player, ChatMenuOption option, bool showNext)
    {
        if (option == null)
        {
            PrintToChat(player, $"{PREFIX} You did not select a weapon!");
            return;
        }

        Player player_obj = FindPlayer(player);

        if (player_obj == null!)
        {
            return;
        }

        PrintToChat(player, $"{PREFIX} You selected {option.Text} as T Primary!");

        player_obj.WeaponsAllocator.PrimaryWeaponT = GetWeaponIndex(option.Text, WeaponType.PrimaryT);

        if(showNext)
        {
            OpenCTPrimaryMenu(player);
            return;
        }

        MenuManager.CloseActiveMenu(player);
    }

    public static void OpenCTPrimaryMenu(CCSPlayerController player, bool showNext = true)
    {
        CenterHtmlMenu centerHtmlMenu = new CenterHtmlMenu($"{PREFIX} Select a CT Primary Weapon", Plugin);

        if(Core.Config.AddSkipOption && showNext)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenSecondaryTMenu(p));
        }

        foreach (Weapon weapon in PrimaryCt)
        {
            centerHtmlMenu.AddMenuOption(weapon.DisplayName, (CCSPlayerController player, ChatMenuOption option) => OnCTPrimarySelect(player, option, showNext));
        }

        MenuManager.OpenCenterHtmlMenu(Plugin, player, centerHtmlMenu);
    }

    private static void OnCTPrimarySelect(CCSPlayerController player, ChatMenuOption option, bool showNext)
    {
        if (option == null)
        {
            PrintToChat(player, $"{PREFIX} You did not select a weapon!");
            return;
        }

        var player_obj = FindPlayer(player);

        if (player_obj == null!)
        {
            return;
        }

        PrintToChat(player, $"{PREFIX} You selected {option.Text} as CT Primary!");

        player_obj.WeaponsAllocator.PrimaryWeaponCt = GetWeaponIndex(option.Text, WeaponType.PrimaryCt);

        if(showNext)
        {
            OpenSecondaryTMenu(player);
            return;
        }

        MenuManager.CloseActiveMenu(player);
    }

    public static void OpenSecondaryTMenu(CCSPlayerController player, bool showNext = true)
    {
        CenterHtmlMenu centerHtmlMenu = new CenterHtmlMenu($"{PREFIX} Select a T Secondary Weapon", Plugin);

        if(Core.Config.AddSkipOption && showNext)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenSecondaryCTMenu(p));
        }

        foreach (Weapon weapon in PistolsT)
        {
            centerHtmlMenu.AddMenuOption(weapon.DisplayName, (CCSPlayerController player, ChatMenuOption option) => OnSecondaryTSelect(player, option, showNext));
        }

        MenuManager.OpenCenterHtmlMenu(Plugin, player, centerHtmlMenu);
    }

    private static void OnSecondaryTSelect(CCSPlayerController player, ChatMenuOption option, bool showNext = true)
    {
        if (option == null)
        {
            PrintToChat(player, $"{PREFIX} You did not select a weapon!");
            return;
        }

        Player player_obj = FindPlayer(player);

        if (player_obj == null!)
        {
            return;
        }

        PrintToChat(player, $"{PREFIX} You selected {option.Text} as T Secondary!");

        player_obj.WeaponsAllocator.SecondaryWeaponT = GetWeaponIndex(option.Text, WeaponType.SecondaryT);

        if(showNext)
        {
            OpenSecondaryCTMenu(player);
            return;
        }

        MenuManager.CloseActiveMenu(player);
    }

    public static void OpenSecondaryCTMenu(CCSPlayerController player, bool showNext = true)
    {
        CenterHtmlMenu centerHtmlMenu = new CenterHtmlMenu($"{PREFIX} Select a CT Secondary Weapon", Plugin);

        if(Core.Config.AddSkipOption && showNext)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenGiveAWPMenu(p));
        }

        foreach (Weapon weapon in PistolsCT)
        {
            centerHtmlMenu.AddMenuOption(weapon.DisplayName, (CCSPlayerController player, ChatMenuOption option) => OnSecondaryCTSelect(player, option, showNext));
        }

        MenuManager.OpenCenterHtmlMenu(Plugin, player, centerHtmlMenu);
    }

    private static void OnSecondaryCTSelect(CCSPlayerController player, ChatMenuOption option, bool showNext = true)
    {
        if (option == null)
        {
            PrintToChat(player, $"{PREFIX} You did not select a weapon!");
            return;
        }

        Player player_obj = FindPlayer(player);

        if (player_obj == null!)
        {
            return;
        }

        PrintToChat(player, $"{PREFIX} You selected {option.Text} as CT Secondary!");

        player_obj.WeaponsAllocator.SecondaryWeaponCt = GetWeaponIndex(option.Text, WeaponType.SecondaryCt);

        if(showNext)
        {
            OpenGiveAWPMenu(player);
            return;
        }

        MenuManager.CloseActiveMenu(player);
    }

    public static void OpenGiveAWPMenu(CCSPlayerController player)
    {
        CenterHtmlMenu centerHtmlMenu = new CenterHtmlMenu($"{PREFIX} Select when to give the AWP", Plugin);

        centerHtmlMenu.AddMenuOption("Never", OnGiveAWPSelect);
        centerHtmlMenu.AddMenuOption("Sometimes", OnGiveAWPSelect);
        centerHtmlMenu.AddMenuOption("Always", OnGiveAWPSelect);

        MenuManager.OpenCenterHtmlMenu(Plugin, player, centerHtmlMenu);
    }

    private static void OnGiveAWPSelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (option == null)
        {
            PrintToChat(player, $"{PREFIX} You did not select an option!");
            return;
        }

        var player_obj = FindPlayer(player);

        if (player_obj == null!)
        {
            return;
        }

        PrintToChat(player, $"{PREFIX} You selected {option.Text} as when to give the AWP!");

        switch (option.Text)
        {
            case "Never":
                player_obj.WeaponsAllocator.GiveAwp = GiveAwp.Never;
                break;
            case "Sometimes":
                player_obj.WeaponsAllocator.GiveAwp = GiveAwp.Sometimes;
                break;
            case "Always":
                player_obj.WeaponsAllocator.GiveAwp = GiveAwp.Always;
                break;
        }

        MenuManager.CloseActiveMenu(player);
    }

    public static void ShowWeaponSelectionMenu(CCSPlayerController player, List<string> weapons, int time)
    {
        CenterHtmlMenu centerHtmlMenu = new CenterHtmlMenu($"Select a weapon [{time} Seconds Left]", Plugin);

        foreach (string weapon in weapons)
        {
            centerHtmlMenu.AddMenuOption(WeaponToDisplayName(weapon), (p, c) => OnWeaponSelect(p, c, weapon));
        }

        MenuManager.OpenCenterHtmlMenu(Plugin, player, centerHtmlMenu);
        
        timers.Clear();

        Timer timer = Plugin.AddTimer(1f, () => Countdown(centerHtmlMenu, player, weapons, time));
        timers.Add(player, timer);
    }

    private static void OnWeaponSelect(CCSPlayerController player, ChatMenuOption option, string weapon)
    {
        if (option == null)
        {
            PrintToChat(player, $"{PREFIX} You did not select a weapon!");
            return;
        }

        PrintToChat(player, $"{PREFIX} You selected {WeaponToDisplayName(weapon)} as your weapon!");
        player.GiveNamedItem("weapon_" + weapon);

        if (timers.ContainsKey(player))
        {
            Timer timer = timers[player];
            timer.Kill();
            timers.Remove(player);
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

        Timer timer = Plugin.AddTimer(1f, () => Countdown(menu, player, weapons, seconds));
        timers[player] = timer;
    }

    public static void GiveRandomWeapon(CCSPlayerController player, List<string> weapons)
    {   
        string weapon = weapons[new Random().Next(0, weapons.Count)];

        PrintToChat(player, $"{PREFIX} You have'nt selected a weapon, giving you a random weapon!");
        PrintToChat(player, $"{PREFIX} You received a {WeaponToDisplayName(weapon)}!");
        player.GiveNamedItem("weapon_" + weapon);

        MenuManager.CloseActiveMenu(player);
    }

    private static string WeaponToDisplayName(string weapon)
    {
        if (c2Weapons.ContainsKey(weapon))
        {
            return c2Weapons[weapon];
        }

        return weapon;
    }
}