using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using PanoramaManager;
using RetakesAllocator.Modules.Models;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Utils;

namespace RetakesAllocator.Modules.Weapons;

/// <summary>
/// The single Panorama card that replaces the five chained CenterHtmlMenu screens.
///
/// A CS2 client renders a layout it already has on disk; the server can only write strings into
/// dialog variables and toggle classes on panels by id. So every tile the menu could ever need
/// already exists in the layout, and opening the menu means filling {s:pt1}..{s:sc8} from the
/// Weapons config, collapsing the tiles past the end of each configured list, and marking one
/// tile per row.
///
/// Edits live in a <see cref="Draft"/> until SAVE: closing the card, or a round restart, discards
/// them. The old menus wrote each pick straight into the allocator as it was made.
/// </summary>
public static class LoadoutPanel
{
    private const string LayoutPath = "panorama/layout/custom_game/alloc_menu.vxml_c";

    /// <summary>
    /// Tiles per row. MUST match --tiles in hud/build_layout.py: the layout physically has no more
    /// panels than this and the server cannot create them, so a longer configured list is
    /// truncated here rather than half-drawn in game.
    /// </summary>
    private const int Slots = 8;

    private static readonly string[] Prefixes = ["pt", "pc", "st", "sc"];

    private static PanelHandle? _panel;

    /// <summary>Unsaved edits. A present entry means the card is open for that player.</summary>
    private static readonly Dictionary<int, Draft> Drafts = new();

    /// <summary>
    /// The icon class currently applied to each tile, per player, for the current open session
    /// only. Classes only toggle, so a config reload that puts a different weapon in a slot has to
    /// take the stale icon off before putting the new one on - otherwise the tile wears both.
    /// </summary>
    private static readonly Dictionary<int, Dictionary<string, string>> AppliedIcons = new();

    private sealed class Draft
    {
        public int PrimaryT;
        public int PrimaryCt;
        public int SecondaryT;
        public int SecondaryCt;
        public GiveAwp Awp;
        public bool Dirty;

        /// <summary>
        /// What was saved last, so a save can tell which slots actually changed and re-issue only
        /// those. Re-baselined after each save, so saving twice does not re-apply the first edit.
        /// </summary>
        public int SavedPrimaryT;
        public int SavedPrimaryCt;
        public int SavedSecondaryT;
        public int SavedSecondaryCt;
    }

    public static void Init()
    {
        try
        {
            Panorama.Init(Plugin);

            _panel = Panorama.Spawn(LayoutPath, new LayoutContract
            {
                RootPanelId = "AllocMenu",
                RowCount = 0,          // not a row list - every panel is driven by hand
                RevealClass = "show",
                HiddenClass = "hidden",
                ActiveClass = "sel",
                CloseButtonId = "exit",
                CaptureInput = true,
                TitleVar = "title",
                SubtitleVar = "tag",
            });

            _panel.Title = "Retakes · Loadout";
            _panel.Subtitle = TriggerHint();
            _panel.SetVariable("awphint", "How often you take the AWP when you win the roll.");
            _panel.OnEvent += OnEvent;

            Plugin.Logger.LogInformation("Loadout panel spawned from {Layout}", LayoutPath);
        }
        catch (Exception e)
        {
            // A missing layout or an unresolved signature must not take the plugin with it -
            // allocation works fine without a menu. Open() reports it to the player instead.
            _panel = null;
            Plugin.Logger.LogError(e, "Failed to spawn the loadout panel; the loadout menu will be unavailable");
        }
    }

    public static void Shutdown()
    {
        _panel?.Dispose();
        _panel = null;
        Drafts.Clear();
        AppliedIcons.Clear();
        Panorama.Shutdown();
    }

    public static void Open(CCSPlayerController player)
    {
        if (_panel == null)
        {
            PrintToChat(player, $"{Prefix} The loadout menu is unavailable - check the server console.");
            return;
        }

        var playerObj = FindPlayer(player);

        if (playerObj == null!)
        {
            return;
        }

        var allocator = playerObj.WeaponsAllocator;

        var draft = new Draft
        {
            PrimaryT = allocator.PrimaryWeaponT,
            PrimaryCt = allocator.PrimaryWeaponCt,
            SecondaryT = allocator.SecondaryWeaponT,
            SecondaryCt = allocator.SecondaryWeaponCt,
            Awp = allocator.GiveAwp,
        };

        ClampToLists(draft);

        draft.SavedPrimaryT = draft.PrimaryT;
        draft.SavedPrimaryCt = draft.PrimaryCt;
        draft.SavedSecondaryT = draft.SecondaryT;
        draft.SavedSecondaryCt = draft.SecondaryCt;

        Drafts[player.Slot] = draft;

        // Open BEFORE rendering. Per-player writes made while the panel is not yet open for that
        // player never reach them - only global state survives - so rendering first draws an empty
        // card and, worse, leaves AppliedIcons believing it already applied every icon class, which
        // makes the icons permanently missing rather than merely late.
        _panel.Open(player);

        // Since PanoramaManager 0.3 Open strips every class the library ever turned on for this
        // slot, so the tiles are bare now whatever this record says. Forget it before drawing or
        // the diff below skips every icon it believes is still applied.
        AppliedIcons.Remove(player.Slot);
        Render(player, draft);
    }

    public static void OnPlayerDisconnect(int slot)
    {
        // The library scrubs the slot's classes itself on leave and on the next Open, so nothing
        // recorded here can outlive the session it was drawn in.
        Drafts.Remove(slot);
        AppliedIcons.Remove(slot);
    }

    /// <summary>Redraws every open card. Used after a config reload changes the weapon lists.</summary>
    public static void RefreshOpen()
    {
        if (_panel == null)
        {
            return;
        }

        foreach (var (slot, draft) in Drafts.ToArray())
        {
            var player = Utilities.GetPlayerFromSlot(slot);

            if (player == null || !player.IsValid)
            {
                OnPlayerDisconnect(slot);
                continue;
            }

            ClampToLists(draft);
            Render(player, draft);
        }
    }

    private static void OnEvent(PanelEvent e)
    {
        // A round restart destroys the layout entity. The library rebuilds it and restores its own
        // rows, title and handle-level variables, but everything written per player is ours to put
        // back - it never saw what those ids meant.
        if (e.Action == PanelAction.Restored)
        {
            // The entity is a new one, so nothing we ever applied is on it. Drop the icon record
            // before redrawing or the diff will skip every class it thinks is already set.
            AppliedIcons.Clear();
            RefreshOpen();
            return;
        }

        if (e.Player == null || !e.Player.IsValid)
        {
            return;
        }

        if (e.Action == PanelAction.Close)
        {
            OnPlayerDisconnect(e.Player.Slot);
            return;
        }

        if (!Drafts.TryGetValue(e.Player.Slot, out var draft))
        {
            return;
        }

        var id = e.ElementId ?? string.Empty;

        if (id == "save")
        {
            Save(e.Player, draft);
            return;
        }

        if (id.StartsWith("awp") && int.TryParse(id[3..], out var option)
            && option >= 1 && option <= 3)
        {
            draft.Awp = (GiveAwp)(option - 1);
            draft.Dirty = true;
            Render(e.Player, draft);
            return;
        }

        if (id.Length > 2 && Prefixes.Contains(id[..2]) && int.TryParse(id[2..], out var tile))
        {
            var prefix = id[..2];
            var index = tile - 1;

            // A click can only come from a tile the layout drew, but the configured list may have
            // shrunk since. Never index a list with a number that arrived from a client.
            if (index < 0 || index >= ListFor(prefix).Count)
            {
                return;
            }

            SetSelection(draft, prefix, index);
            draft.Dirty = true;
            Render(e.Player, draft);
        }
    }

    private static void Save(CCSPlayerController player, Draft draft)
    {
        var playerObj = FindPlayer(player);

        if (playerObj == null!)
        {
            return;
        }

        var allocator = playerObj.WeaponsAllocator;

        allocator.PrimaryWeaponT = draft.PrimaryT;
        allocator.PrimaryWeaponCt = draft.PrimaryCt;
        allocator.SecondaryWeaponT = draft.SecondaryT;
        allocator.SecondaryWeaponCt = draft.SecondaryCt;
        allocator.GiveAwp = draft.Awp;

        // Hand the new loadout over now rather than at the next spawn - a menu whose result only
        // shows up a round later is hard to tell apart from one that did not save. Only the slots
        // that changed for the team being played are re-issued: editing the CT columns while on T
        // touches nothing, and changing only the rifle leaves the pistol in hand.
        var terrorist = player.Team == CsTeam.Terrorist;

        playerObj.ApplyLoadoutChange(
            primaryChanged: terrorist
                ? draft.PrimaryT != draft.SavedPrimaryT
                : draft.PrimaryCt != draft.SavedPrimaryCt,
            secondaryChanged: terrorist
                ? draft.SecondaryT != draft.SavedSecondaryT
                : draft.SecondaryCt != draft.SavedSecondaryCt);

        draft.SavedPrimaryT = draft.PrimaryT;
        draft.SavedPrimaryCt = draft.PrimaryCt;
        draft.SavedSecondaryT = draft.SecondaryT;
        draft.SavedSecondaryCt = draft.SecondaryCt;

        draft.Dirty = false;

        // Read the CounterStrikeSharp-bound values on the game thread before going async.
        var pref = new WeaponPreference
        {
            Auth = playerObj.GetSteamId2(),
            TPrimary = draft.PrimaryT,
            CtPrimary = draft.PrimaryCt,
            TSecondary = draft.SecondaryT,
            CtSecondary = draft.SecondaryCt,
            GiveAwp = (int)draft.Awp,
        };

        var slot = player.Slot;

        // Close on save. The picks are already live on the allocator, so the card has nothing left
        // to show, and the confirmation belongs in chat where it survives the panel going away.
        _panel!.Close(player);
        PrintToChat(player, $"{Prefix} Loadout saved.");

        Task.Run(async () =>
        {
            try
            {
                await Store.SaveUserAsync(pref);
                return;
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError(e, "Failed to save weapon preferences for {SteamId}", pref.Auth);
            }

            // Native calls are not thread-safe: come back to the game thread to talk to the player.
            // Only the failure is reported here - success was already confirmed above, and making
            // players wait on a database round trip to see it would be worse.
            Server.NextFrame(() =>
            {
                var current = Utilities.GetPlayerFromSlot(slot);

                if (current == null || !current.IsValid)
                {
                    return;
                }

                PrintToChat(current, $"{Prefix} Your loadout could not be stored - it applies to this session only.");
            });
        });
    }

    private static void Render(CCSPlayerController player, Draft draft)
    {
        if (_panel == null)
        {
            return;
        }

        if (!AppliedIcons.TryGetValue(player.Slot, out var icons))
        {
            icons = AppliedIcons[player.Slot] = new Dictionary<string, string>();
        }

        foreach (var prefix in Prefixes)
        {
            var list = ListFor(prefix);
            var selected = GetSelection(draft, prefix);

            for (var i = 0; i < Slots; i++)
            {
                var id = $"{prefix}{i + 1}";
                var configured = i < list.Count;

                _panel.SetVariableFor(player, id, configured ? list[i].DisplayName : string.Empty);
                _panel.SetClassFor(player, id, "hidden", !configured);
                _panel.SetClassFor(player, id, "sel", configured && i == selected);

                var icon = configured ? IconClass(list[i].Item) : null;

                if (icons.TryGetValue(id, out var applied) && applied != icon)
                {
                    _panel.SetClassFor(player, id, applied, false);
                    icons.Remove(id);
                }

                if (icon != null && !icons.ContainsKey(id))
                {
                    _panel.SetClassFor(player, id, icon, true);
                    icons[id] = icon;
                }
            }
        }

        for (var option = 0; option < 3; option++)
        {
            _panel.SetClassFor(player, $"awp{option + 1}", "sel", (int)draft.Awp == option);
        }

        _panel.SetVariableFor(player, "status", draft.Dirty ? "Unsaved changes" : "Saved to your profile");
    }

    /// <summary>
    /// The icon class is the item name minus its weapon_ prefix - which is also the name of the
    /// CS2 icon file - so weapon_icons.css needs no edit when a server developer changes
    /// Config.Weapons.
    /// </summary>
    private static string IconClass(string item) =>
        "wi-" + (item.StartsWith("weapon_") ? item["weapon_".Length..] : item);

    private static string TriggerHint()
    {
        var word = Core.Config.TriggerWords.FirstOrDefault();
        return string.IsNullOrWhiteSpace(word) ? "!guns" : $"!{word}";
    }

    private static List<Weapon> ListFor(string prefix) => prefix switch
    {
        "pt" => Allocator.PrimaryT,
        "pc" => Allocator.PrimaryCt,
        "st" => Allocator.PistolsT,
        _ => Allocator.PistolsCT,
    };

    private static int GetSelection(Draft draft, string prefix) => prefix switch
    {
        "pt" => draft.PrimaryT,
        "pc" => draft.PrimaryCt,
        "st" => draft.SecondaryT,
        _ => draft.SecondaryCt,
    };

    private static void SetSelection(Draft draft, string prefix, int index)
    {
        switch (prefix)
        {
            case "pt": draft.PrimaryT = index; break;
            case "pc": draft.PrimaryCt = index; break;
            case "st": draft.SecondaryT = index; break;
            default: draft.SecondaryCt = index; break;
        }
    }

    /// <summary>Drops a selection a shorter list no longer has, the way ApplyPreferences does.</summary>
    private static void ClampToLists(Draft draft)
    {
        if (draft.PrimaryT >= Allocator.PrimaryT.Count) draft.PrimaryT = 0;
        if (draft.PrimaryCt >= Allocator.PrimaryCt.Count) draft.PrimaryCt = 0;
        if (draft.SecondaryT >= Allocator.PistolsT.Count) draft.SecondaryT = 0;
        if (draft.SecondaryCt >= Allocator.PistolsCT.Count) draft.SecondaryCt = 0;
    }
}
