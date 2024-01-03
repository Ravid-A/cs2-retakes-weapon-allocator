using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using RetakesAllocator.Modules.Models;

using static RetakesAllocator.Modules.Utils;
using static RetakesAllocator.Modules.Weapons.Allocator;

namespace RetakesAllocator.Modules.Weapons;

public class Menu
{
    private static void OnSelectExit(CCSPlayerController player, ChatMenuOption option)
    {
        if (option == null)
        {
            PrintToChat(player, $"{PREFIX} You did not select an option!");
            return;
        }

        Player player_obj = FindPlayer(player);

        if (player_obj == null!)
        {
            return;
        }

        PrintToChat(player, $"{PREFIX} You have finished setting up your weapons!");
        PrintToChat(player, $"{PREFIX} The weapons you have selected will be given to you at the start of the next round!");

        player_obj.InGunMenu = false;
    }

    public static void OpenTPrimaryMenu(CCSPlayerController player)
    {
        ChatMenu menu = new ChatMenu($"{PREFIX} Select a T Primary Weapon");

        foreach (Weapon weapon in PrimaryT)
        {
            menu.AddMenuOption(weapon.DisplayName, OnTPrimarySelect);
        }

        menu.AddMenuOption("Exit", OnSelectExit);

        ChatMenus.OpenMenu(player, menu);
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
        ChatMenu menu = new ChatMenu($"{PREFIX} Select a CT Primary Weapon");

        foreach (Weapon weapon in PrimaryCt)
        {
            menu.AddMenuOption(weapon.DisplayName, OnCTPrimarySelect);
        }

        menu.AddMenuOption("Exit", OnSelectExit);

        ChatMenus.OpenMenu(player, menu);
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
        var menu = new ChatMenu($"{PREFIX} Select a Secondary Weapon");

        foreach (var weapon in Pistols)
        {
            menu.AddMenuOption(weapon.DisplayName, OnSecondarySelect);
        }

        menu.AddMenuOption("Exit", OnSelectExit);

        ChatMenus.OpenMenu(player, menu);
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
        ChatMenu menu = new ChatMenu($"{PREFIX} Select when to give the AWP");

        menu.AddMenuOption("Never", OnGiveAWPSelect);
        menu.AddMenuOption("Sometimes", OnGiveAWPSelect);
        menu.AddMenuOption("Always", OnGiveAWPSelect);

        menu.AddMenuOption("Exit", OnSelectExit);

        ChatMenus.OpenMenu(player, menu);
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

        player_obj.InGunMenu = false;
    }
}