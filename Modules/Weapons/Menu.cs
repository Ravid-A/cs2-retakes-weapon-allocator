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

    public static void OpenPistolRoundTMenu(CCSPlayerController player)
    {
        var centerHtmlMenu = new CenterHtmlMenu("[ T ] Pistol Round", Plugin);

        if(Core.Config.AddSkipOption)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenPistolRoundCTMenu(p));
        }

        foreach (var weapon in PistolsT)
        {
            centerHtmlMenu.AddMenuOption(weapon.DisplayName, (CCSPlayerController player, ChatMenuOption option) => OnPistolRoundTSelect(player, option));
        }

        MenuManager.OpenCenterHtmlMenu(Plugin, player, centerHtmlMenu);
    }

    private static void OnPistolRoundTSelect(CCSPlayerController player, ChatMenuOption? option)
    {
        if (option == null)
        {
            PrintToChat(player, $"{Prefix} You did not select a weapon!");
            return;
        }

        var playerObj = FindPlayer(player);

        if (playerObj == null!)
        {
            return;
        }

        PrintToChat(player, $"{Prefix} You selected {option.Text} as T Pistol Round!");
        playerObj.WeaponsAllocator.PistolRoundWeaponT = GetWeaponIndex(option.Text, WeaponType.PistolRoundT);

        OpenPistolRoundCTMenu(player);
    }

    public static void OpenPistolRoundCTMenu(CCSPlayerController player)
    {
        var centerHtmlMenu = new CenterHtmlMenu("[ CT ] Pistol Round", Plugin);

        if(Core.Config.AddSkipOption)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenTPrimaryMenu(p));
        }

        foreach (var weapon in PistolsCT)
        {
            centerHtmlMenu.AddMenuOption(weapon.DisplayName, (CCSPlayerController player, ChatMenuOption option) => OnPistolRoundCTSelect(player, option));
        }

        MenuManager.OpenCenterHtmlMenu(Plugin, player, centerHtmlMenu);
    }

    private static void OnPistolRoundCTSelect(CCSPlayerController player, ChatMenuOption? option)
    {
        if (option == null)
        {
            PrintToChat(player, $"{Prefix} You did not select a weapon!");
            return;
        }

        var playerObj = FindPlayer(player);

        if (playerObj == null!)
        {
            return;
        }

        PrintToChat(player, $"{Prefix} You selected {option.Text} as CT Pistol Round!");
        playerObj.WeaponsAllocator.PistolRoundWeaponCt = GetWeaponIndex(option.Text, WeaponType.PistolRoundCt);

        OpenTPrimaryMenu(player);
    }

    public static void OpenTPrimaryMenu(CCSPlayerController player, bool showNext = true)
    {
        var centerHtmlMenu = new CenterHtmlMenu("[ T ] Primary Weapon", Plugin);

        if(Core.Config.AddSkipOption && showNext)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenCTPrimaryMenu(p));
        }

        foreach (var weapon in PrimaryT)
        {
            if (!HasAwpAccess(player) && weapon.Item.Equals("weapon_awp", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            centerHtmlMenu.AddMenuOption(weapon.DisplayName, (CCSPlayerController player, ChatMenuOption option) => OnTPrimarySelect(player, option, showNext));
        }

        MenuManager.OpenCenterHtmlMenu(Plugin, player, centerHtmlMenu);
    }

    private static void OnTPrimarySelect(CCSPlayerController player, ChatMenuOption? option, bool showNext)
    {
        if (option == null)
        {
            PrintToChat(player, $"{Prefix} You did not select a weapon!");
            return;
        }

        var playerObj = FindPlayer(player);

        if (playerObj == null!)
        {
            return;
        }

        PrintToChat(player, $"{Prefix} You selected {option.Text} as T Primary!");

        playerObj.WeaponsAllocator.PrimaryWeaponT = GetWeaponIndex(option.Text, WeaponType.PrimaryT);

        if(showNext)
        {
            OpenCTPrimaryMenu(player);
            return;
        }

        MenuManager.CloseActiveMenu(player);
    }

    public static void OpenCTPrimaryMenu(CCSPlayerController player, bool showNext = true)
    {
        var centerHtmlMenu = new CenterHtmlMenu("[ CT ] Primary Weapon", Plugin);

        if(Core.Config.AddSkipOption && showNext)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenSecondaryTMenu(p));
        }

        foreach (var weapon in PrimaryCt)
        {
            if (!HasAwpAccess(player) && weapon.Item.Equals("weapon_awp", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            centerHtmlMenu.AddMenuOption(weapon.DisplayName, (CCSPlayerController player, ChatMenuOption option) => OnCTPrimarySelect(player, option, showNext));
        }

        MenuManager.OpenCenterHtmlMenu(Plugin, player, centerHtmlMenu);
    }

    private static void OnCTPrimarySelect(CCSPlayerController player, ChatMenuOption? option, bool showNext)
    {
        if (option == null)
        {
            PrintToChat(player, $"{Prefix} You did not select a weapon!");
            return;
        }

        var playerObj = FindPlayer(player);

        if (playerObj == null!)
        {
            return;
        }

        PrintToChat(player, $"{Prefix} You selected {option.Text} as CT Primary!");

        playerObj.WeaponsAllocator.PrimaryWeaponCt = GetWeaponIndex(option.Text, WeaponType.PrimaryCt);

        if(showNext)
        {
            OpenSecondaryTMenu(player);
            return;
        }

        MenuManager.CloseActiveMenu(player);
    }

    public static void OpenSecondaryTMenu(CCSPlayerController player, bool showNext = true)
    {
        var centerHtmlMenu = new CenterHtmlMenu("[ T ] Secondary Weapon", Plugin);

        if(Core.Config.AddSkipOption && showNext)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenSecondaryCTMenu(p));
        }

        foreach (var weapon in PistolsT)
        {
            centerHtmlMenu.AddMenuOption(weapon.DisplayName, (CCSPlayerController player, ChatMenuOption option) => OnSecondaryTSelect(player, option, showNext));
        }

        MenuManager.OpenCenterHtmlMenu(Plugin, player, centerHtmlMenu);
    }

    private static void OnSecondaryTSelect(CCSPlayerController player, ChatMenuOption? option, bool showNext = true)
    {
        if (option == null)
        {
            PrintToChat(player, $"{Prefix} You did not select a weapon!");
            return;
        }

        var playerObj = FindPlayer(player);

        if (playerObj == null!)
        {
            return;
        }

        PrintToChat(player, $"{Prefix} You selected {option.Text} as T Secondary!");

        playerObj.WeaponsAllocator.SecondaryWeaponT = GetWeaponIndex(option.Text, WeaponType.SecondaryT);

        if(showNext)
        {
            OpenSecondaryCTMenu(player);
            return;
        }

        MenuManager.CloseActiveMenu(player);
    }

    public static void OpenSecondaryCTMenu(CCSPlayerController player, bool showNext = true)
    {
        var centerHtmlMenu = new CenterHtmlMenu("[ CT ] Secondary Weapon", Plugin);

        if(Core.Config.AddSkipOption && showNext)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenAwpMenuIfAllowed(p));
        }

        foreach (var weapon in PistolsCT)
        {
            centerHtmlMenu.AddMenuOption(weapon.DisplayName, (CCSPlayerController player, ChatMenuOption option) => OnSecondaryCTSelect(player, option, showNext));
        }

        MenuManager.OpenCenterHtmlMenu(Plugin, player, centerHtmlMenu);
    }

    private static void OnSecondaryCTSelect(CCSPlayerController player, ChatMenuOption? option, bool showNext = true)
    {
        if (option == null)
        {
            PrintToChat(player, $"{Prefix} You did not select a weapon!");
            return;
        }

        var playerObj = FindPlayer(player);

        if (playerObj == null!)
        {
            return;
        }

        PrintToChat(player, $"{Prefix} You selected {option.Text} as CT Secondary!");

        playerObj.WeaponsAllocator.SecondaryWeaponCt = GetWeaponIndex(option.Text, WeaponType.SecondaryCt);

        if(showNext)
        {
            OpenAwpMenuIfAllowed(player);
            return;
        }

        MenuManager.CloseActiveMenu(player);
    }

    private static void OpenAwpMenuIfAllowed(CCSPlayerController player)
    {
        if (HasAwpAccess(player))
        {
            OpenGiveAWPMenu(player);
            return;
        }

        MenuManager.CloseActiveMenu(player);
    }

    public static void OpenGiveAWPMenu(CCSPlayerController player)
    {
        if (!HasAwpAccess(player))
        {
            PrintToChat(player, $"{Prefix} The AWP menu is for VIP players only.");
            MenuManager.CloseActiveMenu(player);
            return;
        }

        var centerHtmlMenu = new CenterHtmlMenu("When To Get AWP", Plugin);

        centerHtmlMenu.AddMenuOption("Never", OnGiveAWPSelect);
        centerHtmlMenu.AddMenuOption("Sometimes", OnGiveAWPSelect);
        centerHtmlMenu.AddMenuOption("Always", OnGiveAWPSelect);

        MenuManager.OpenCenterHtmlMenu(Plugin, player, centerHtmlMenu);
    }

    private static void OnGiveAWPSelect(CCSPlayerController player, ChatMenuOption? option)
    {
        if (option == null)
        {
            PrintToChat(player, $"{Prefix} You did not select an option!");
            return;
        }

        var playerObj = FindPlayer(player);

        if (playerObj == null!)
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

        MenuManager.CloseActiveMenu(player);
    }

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
