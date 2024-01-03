using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;

using WeaponsAllocator;
using static WeaponsAllocator.Functions;

using static Weapons.Allocator;

namespace Weapons;

public class WeaponsMenu
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

        player_obj.inGunMenu = false;
    }

    public static void OpenTPrimaryMenu(CCSPlayerController player)
    {
        ChatMenu menu = new ChatMenu($"{PREFIX} Select a T Primary Weapon");

        foreach (Weapon weapon in primary_t)
        {
            menu.AddMenuOption(weapon.display_name, OnTPrimarySelect);
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

        player_obj.weaponsAllocator.primaryWeapon_t = GetWeaponIndex(option.Text, WeaponType.PRIMARY_T);

        OpenCTPrimaryMenu(player);
    }

    public static void OpenCTPrimaryMenu(CCSPlayerController player)
    {
        ChatMenu menu = new ChatMenu($"{PREFIX} Select a CT Primary Weapon");

        foreach (Weapon weapon in primary_ct)
        {
            menu.AddMenuOption(weapon.display_name, OnCTPrimarySelect);
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

        Player player_obj = FindPlayer(player);

        if (player_obj == null!)
        {
            return;
        }

        PrintToChat(player, $"{PREFIX} You selected {option.Text} as CT Primary!");

        player_obj.weaponsAllocator.primaryWeapon_ct = GetWeaponIndex(option.Text, WeaponType.PRIMARY_CT);

        OpenSecondaryMenu(player);
    }

    public static void OpenSecondaryMenu(CCSPlayerController player)
    {
        ChatMenu menu = new ChatMenu($"{PREFIX} Select a Secondary Weapon");

        foreach (Weapon weapon in pistols)
        {
            menu.AddMenuOption(weapon.display_name, OnSecondarySelect);
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

        player_obj.weaponsAllocator.secondaryWeapon = GetWeaponIndex(option.Text, WeaponType.SECONDARY);

        OpenGiveAWPMenu(player);
    }

    public static void OpenGiveAWPMenu(CCSPlayerController player)
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

        Player player_obj = FindPlayer(player);

        if (player_obj == null!)
        {
            return;
        }

        PrintToChat(player, $"{PREFIX} You selected {option.Text} as when to give the AWP!");

        switch (option.Text)
        {
            case "Never":
                player_obj.weaponsAllocator.giveAWP = GiveAWP.NEVER;
                break;
            case "Sometimes":
                player_obj.weaponsAllocator.giveAWP = GiveAWP.SOMETIMES;
                break;
            case "Always":
                player_obj.weaponsAllocator.giveAWP = GiveAWP.ALWAYS;
                break;
        }

        PrintToChat(player, $"{PREFIX} You have finished setting up your weapons!");
        PrintToChat(player, $"{PREFIX} The weapons you have selected will be given to you at the start of the next round!");

        player_obj.inGunMenu = false;
    }
}