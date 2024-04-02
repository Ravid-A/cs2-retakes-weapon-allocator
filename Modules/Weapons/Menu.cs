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

    public static void OpenTPrimaryMenu(CCSPlayerController player, bool showNext = true)
    {
        CenterHtmlMenu centerHtmlMenu = new CenterHtmlMenu($"{PREFIX} Select a T Primary Weapon");

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
        CenterHtmlMenu centerHtmlMenu = new CenterHtmlMenu($"{PREFIX} Select a CT Primary Weapon");

        if(Core.Config.AddSkipOption && showNext)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenSecondaryMenu(p));
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
            OpenSecondaryMenu(player);
            return;
        }

        MenuManager.CloseActiveMenu(player);
    }

    public static void OpenSecondaryMenu(CCSPlayerController player, bool showNext = true)
    {
        CenterHtmlMenu centerHtmlMenu = new CenterHtmlMenu($"{PREFIX} Select a Secondary Weapon");

        if(Core.Config.AddSkipOption && showNext)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenGiveAWPMenu(p));
        }

        foreach (Weapon weapon in Pistols)
        {
            centerHtmlMenu.AddMenuOption(weapon.DisplayName, (CCSPlayerController player, ChatMenuOption option) => OnSecondarySelect(player, option, showNext));
        }

        MenuManager.OpenCenterHtmlMenu(Plugin, player, centerHtmlMenu);
    }

    private static void OnSecondarySelect(CCSPlayerController player, ChatMenuOption option, bool showNext = true)
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

        PrintToChat(player, $"{PREFIX} You selected {option.Text} as Secondary!");

        player_obj.WeaponsAllocator.SecondaryWeapon = GetWeaponIndex(option.Text, WeaponType.Secondary);

        if(showNext)
        {
            OpenGiveAWPMenu(player);
            return;
        }

        MenuManager.CloseActiveMenu(player);
    }

    public static void OpenGiveAWPMenu(CCSPlayerController player)
    {
        CenterHtmlMenu centerHtmlMenu = new CenterHtmlMenu($"{PREFIX} Select when to give the AWP");

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
        CenterHtmlMenu centerHtmlMenu = new CenterHtmlMenu($"Select a weapon [{time} Seconds Left]");

        foreach (string weapon in weapons)
        {
            centerHtmlMenu.AddMenuOption(weapon, OnWeaponSelect);
        }

        MenuManager.OpenCenterHtmlMenu(Plugin, player, centerHtmlMenu);
        
        timers.Clear();

        Timer timer = Plugin.AddTimer(1f, () => Countdown(centerHtmlMenu, player, weapons, time));
        timers.Add(player, timer);
    }

    private static void OnWeaponSelect(CCSPlayerController player, ChatMenuOption option)
    {
        if (option == null)
        {
            PrintToChat(player, $"{PREFIX} You did not select a weapon!");
            return;
        }

        PrintToChat(player, $"{PREFIX} You selected {option.Text} as your weapon!");
        player.GiveNamedItem("weapon_" + option.Text);

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
        PrintToChat(player, $"{PREFIX} You received a {weapon}!");
        player.GiveNamedItem("weapon_" + weapon);

        MenuManager.CloseActiveMenu(player);
    }
}