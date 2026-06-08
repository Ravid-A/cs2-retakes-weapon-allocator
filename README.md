## cs2-retakes-weapon-allocator

WeaponsAllocator plugin for retakes written in C# (.NET 8) for CounterStrikeSharp.

## Retakes

This plugin runs alongside B3none's retakes implementation: https://github.com/b3none/cs2-retakes

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
  "AddSkipOption": true,
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
