## cs2-retakes-weapon-allocator

WeaponsAllocator plugin for retakes written in C# (.NET 10) for CounterStrikeSharp.

## Retakes

This plugin runs alongside B3none's retakes implementation: https://github.com/b3none/cs2-retakes

## Requirements

- **CounterStrikeSharp on .NET 10** (API 1.0.373 or newer). The loadout menu is driven through
  [PanoramaManager](https://www.nuget.org/packages/PanoramaManager), which targets `net10.0`, and
  CounterStrikeSharp itself moved there at 1.0.373.
- **[MultiAddonManager](https://github.com/Source2ZE/MultiAddonManager)** — required for the
  loadout menu.
- **The HUD workshop addon:
  [Retakes Allocator - HUD](https://steamcommunity.com/sharedfiles/filedetails/?id=3792297574)**
  (`3792297574`).

The menu is a Panorama layout, which means it is drawn from a file on the **client**. The server
cannot send it: it can only point at a layout the client already has, then write text into it and
toggle classes on it. A CS2 server otherwise distributes only its map addon, so MultiAddonManager is
what gets the layout to players alongside the map.

Add the addon in `game/csgo/cfg/multiaddonmanager/multiaddonmanager.cfg`:

```
mm_extra_addons 3792297574
```

The addon is client-side content only, so `mm_client_extra_addons` also works and skips the
server-side mount, at the cost of applying only to new connections. Either list takes effect on a
map reload, and clients download the addon when they connect.

Without it the plugin still allocates weapons normally — it logs the failure at load and tells
players the menu is unavailable, rather than opening nothing.

> The addon carries the compiled layout, so **any change under `hud/` needs the addon republished**
> and re-downloaded by clients. Ship the plugin and the HUD together: the plugin drives panels by
> id, and an id it expects that the layout does not have fails silently on both sides.

### Branding

The official addon is branded: it ships our logo and colour scheme. If that does not suit
your server, everything needed to publish your own is in this repository — the layout and
stylesheets under `hud/` are the full source. Replace `logo.png`, adjust the palette in the
stylesheets to taste, build as described in [Building the layout](#building-the-layout), and
publish the result as your own workshop item. Point `mm_extra_addons` at it and the plugin
works with it unchanged — it only drives panel ids and dialog variables, and takes no
dependency on the artwork.

## Loadout menu

`css_guns`, `css_pistols` and `css_awp` all open the same Panorama card: primary, secondary and
AWP are three bands of one panel, each with a T row and a CT row, and nothing is written until
SAVE. Any of the configured `TriggerWords` in chat opens it too.

The card is a Panorama layout, so it lives on the **client**, not in the plugin. The plugin only
fills it in. The layout source is in `hud/` as `.xml` and `.css` - Source 2 keeps Panorama sources
under the plain extensions and the compiler emits `.vxml_c` / `.vcss_c` into the game tree:

```
python hud/build_layout.py --tiles 8   # regenerate the layout and its weapon-icon classes
python hud/check_contract.py           # assert the plugin and the layout still agree on every id
python hud/make_previews.py            # render it to hud/previews/ in a browser
```

`--tiles` is the ceiling on a configured weapon list *and* the card width: Panorama cannot wrap
and the server cannot create panels, so a list longer than the tile count is truncated. Build with
the number your `Weapons` config actually uses. Changing it means recompiling and republishing the
addon, not just the plugin.

### Building the layout

Compiling needs `resourcecompiler.exe`, which ships with CS2 in `game\bin\win64` — Workshop Tools
is a GUI over it, and is not required:

```
# stage hud/panorama into content\csgo_addons\retakes_allocator\, then
.\cs2-panorama-hud\scripts\build-hud.ps1 -Cs2Root "<CS2 install>" -Addon retakes_allocator
```

Sources are `.xml` and `.css`; the compiler emits `.vxml_c` and `.vcss_c` into the matching
`game\csgo_addons\...` tree. That compiled `panorama/` folder is what gets published to the
workshop addon. Publish it from Workshop Tools under the same `retakes_allocator` source folder.

## Config

The config is managed by CounterStrikeSharp and generated automatically on first
load at:

```
addons/counterstrikesharp/configs/plugins/RetakesAllocator/RetakesAllocator.json
```

Editing the file is picked up live via CounterStrikeSharp's config hot reload. The
in-game `css_weapons_reload` command (requires `@css/root`) re-applies the current
config on demand.

> **Upgrading from an older version:** configuration moved into this single
> CounterStrikeSharp-managed file. The previous `configs/retakes_allocator.json`,
> `configs/weapons/*.json`, and `configs/votes.json` files are no longer read.
> Re-enter your settings (database credentials, weapon lists, votes) in the new file.

### Database

The `DbConnection.Provider` field selects the database engine:

- `"sqlite"` — a local file (the default); only `SqlitePath` matters (resolved
  relative to the plugin's directory unless the path is absolute).
- `"mysql"` — MySQL/MariaDB; fill in `Host`, `Database`, `User`, `Password`, `Port`.

If the database can't be reached the plugin logs an error and keeps running, but
weapon preferences won't load or persist — so make sure `DbConnection` is correct.

### HUD logo

The logo in the loadout card header ships with the addon, painted as a background by
`alloc_menu.css` — there is no config for it.

To use your own, replace `hud/panorama/styles/custom_game/logo.png`, keep
`logo_png.vtex` beside it (that file is what tells resourcecompiler to compile the
PNG - there is no compiler for a bare `.png`), rebuild the addon and republish it.
The `_png` in that filename is load-bearing: a reference to `logo.png` is resolved
by the engine to `logo_png.vtex_c`, so the definition has to be named for the source
extension or the texture is compiled under a name nothing looks for. The
picture sits with the stylesheets rather than under `panorama/images` because
Workshop Tools packs only `panorama/layout` and `panorama/styles` - an image
anywhere else never reaches the published VPK. The box
is `38x20` in `alloc_menu.css`, sized to the shipped mark's aspect ratio; Panorama
scales the image to the panel, so a logo of another shape wants those two numbers
changed.

### Example config

This is the full default config, generated on first load:

```json
{
  "ConfigVersion": 1,
  "DbConnection": {
    "Provider": "sqlite",
    "Host": "",
    "Database": "",
    "User": "",
    "Password": "",
    "Port": 3306,
    "SqlitePath": "weapons.db"
  },
  "Prefix": {
    "Prefix": " [Retakes]",
    "PrefixCon": "[RetakesAllocator]"
  },
  "PistolRound": {
    "RoundAmount": 2,
    "WeaponT": "weapon_glock",
    "WeaponCt": "weapon_usp_silencer"
  },
  "TriggerWords": [ "guns", "gun", "weapon", "weapons" ],
  "Weapons": {
    "PrimaryT": [
      { "Item": "weapon_ak47", "DisplayName": "AK-47" },
      { "Item": "weapon_sg556", "DisplayName": "SG 553" }
    ],
    "PrimaryCt": [
      { "Item": "weapon_m4a1", "DisplayName": "M4A4" },
      { "Item": "weapon_m4a1_silencer", "DisplayName": "M4A1-S" },
      { "Item": "weapon_aug", "DisplayName": "AUG" }
    ],
    "PistolsT": [
      { "Item": "weapon_glock", "DisplayName": "Glock-18" },
      { "Item": "weapon_p250", "DisplayName": "P250" }
    ],
    "PistolsCt": [
      { "Item": "weapon_usp_silencer", "DisplayName": "USP-S" },
      { "Item": "weapon_p250", "DisplayName": "P250" },
      { "Item": "weapon_hkp2000", "DisplayName": "P2000" }
    ]
  },
  "Nades": {
    "CtNades": { "Flashbangs": 2, "Smokes": 1, "Molotovs": 1, "HeGrenades": 1 },
    "TNades": { "Flashbangs": 1, "Smokes": 1, "Molotovs": 1, "HeGrenades": 1 }
  },
  "Votes": {
    "RequiredPercentage": 60,
    "WeaponSelectionTime": 5,
    "Votes": [
      {
        "Command": "vp",
        "Description": "pistol only",
        "WeaponsT": [ "glock" ],
        "WeaponsCt": [ "usp_silencer" ],
        "OnlyHeadshots": false,
        "GiveWeapons": true,
        "GiveNades": true,
        "GiveKnife": true,
        "GiveArmor": true,
        "GiveHelmet": false
      },
      {
        "Command": "vawp",
        "Description": "awp only",
        "WeaponsT": [ "awp" ],
        "WeaponsCt": [ "awp" ],
        "OnlyHeadshots": false,
        "GiveWeapons": true,
        "GiveNades": true,
        "GiveKnife": true,
        "GiveArmor": true,
        "GiveHelmet": true
      }
    ]
  }
}
```

The default config also ships `vph` (pistols + headshots only), `vhs` (headshots
only) and `vrifles` (rifle only) votes — trimmed above for brevity.

For **MySQL**, set `"Provider": "mysql"` and fill in the connection fields; for
**SQLite**, set `"Provider": "sqlite"` and a `"SqlitePath"`.

## Weapons, Nades & Votes

Selectable weapons, grenade kits, and weapon-vote definitions all live in the single
config file above:

- **`Weapons`** — the four selectable lists (`PrimaryT`, `PrimaryCt`, `PistolsT`,
  `PistolsCt`). Each entry is an `Item` (the `weapon_*` class name) and the
  `DisplayName` shown in the in-game menu.
- **`Nades`** — per-team grenade kits (`CtNades` / `TNades`): how many of each
  grenade a player is given.
- **`Votes`** — each vote defines a chat command (`css_<Command>`), the weapons it
  grants per team (`WeaponsT` / `WeaponsCt`), and the flags `OnlyHeadshots`,
  `GiveWeapons`, `GiveNades`, `GiveKnife`, `GiveArmor`, `GiveHelmet`. Admins can
  force a vote with `css_force<Command>` (requires `@css/root`). `RequiredPercentage`
  is the share of players needed to pass a vote and `WeaponSelectionTime` is the
  per-weapon menu countdown in seconds.

Changes are applied on hot reload or via `css_weapons_reload`.

## Setup for development

Run `dotnet restore` (or `dotnet build`) in the repository root to install the
CounterStrikeSharp API and build the plugin. Run the tests with `dotnet test`.
