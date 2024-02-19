using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using RetakesAllocator.Modules.Models;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Utils;
using static RetakesAllocator.Modules.Weapons.Allocator;

namespace RetakesAllocator.Modules.Weapons;

public class Menu
{
    public static void OpenTPrimaryMenu(CCSPlayerController player)
    {
        CenterHtmlMenu centerHtmlMenu = new CenterHtmlMenu($"{PREFIX} Select a T Primary Weapon");

        if(Core.Config.AddSkipOption)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenCTPrimaryMenu(p));
        }

        foreach (Weapon weapon in PrimaryT)
        {
            centerHtmlMenu.AddMenuOption(weapon.DisplayName, OnTPrimarySelect);
        }

        MenuManager.OpenCenterHtmlMenu(Plugin, player, centerHtmlMenu);
    }

    private static void OnTPrimarySelect(CCSPlayerController player, ChatMenuOption option)
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

        OpenCTPrimaryMenu(player);
    }

    public static void OpenCTPrimaryMenu(CCSPlayerController player)
    {
        CenterHtmlMenu centerHtmlMenu = new CenterHtmlMenu($"{PREFIX} Select a CT Primary Weapon");

        if(Core.Config.AddSkipOption)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenSecondaryMenu(p));
        }

        foreach (Weapon weapon in PrimaryCt)
        {
            centerHtmlMenu.AddMenuOption(weapon.DisplayName, OnCTPrimarySelect);
        }

        MenuManager.OpenCenterHtmlMenu(Plugin, player, centerHtmlMenu);
    }

    private static void OnCTPrimarySelect(CCSPlayerController player, ChatMenuOption option)
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

        OpenSecondaryMenu(player);
    }

    private static void OpenSecondaryMenu(CCSPlayerController player)
    {
        CenterHtmlMenu centerHtmlMenu = new CenterHtmlMenu($"{PREFIX} Select a Secondary Weapon");

        if(Core.Config.AddSkipOption)
        {
            centerHtmlMenu.AddMenuOption("SKIP", (p, _) => OpenGiveAWPMenu(p));
        }

        foreach (Weapon weapon in Pistols)
        {
            centerHtmlMenu.AddMenuOption(weapon.DisplayName, OnSecondarySelect);
        }

        MenuManager.OpenCenterHtmlMenu(Plugin, player, centerHtmlMenu);
    }

    private static void OnSecondarySelect(CCSPlayerController player, ChatMenuOption option)
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

        OpenGiveAWPMenu(player);
    }

    private static void OpenGiveAWPMenu(CCSPlayerController player)
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

        PrintToChat(player, $"{PREFIX} You have finished setting up your weapons!");
        PrintToChat(player, $"{PREFIX} The weapons you have selected will be given to you at the start of the next round!");
    }
}