# Config → IPluginConfig Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace all three hand-rolled JSON file readers (main config, weapon lists, votes) with a single CounterStrikeSharp `IPluginConfig<RetakesAllocatorConfig>` consolidated config, so the plugin no longer does any manual `File.ReadAllText` / `JsonSerializer` work.

**Architecture:** Introduce one `RetakesAllocatorConfig : BasePluginConfig` with a labeled JSON section per subsystem (`DbConnection`, `Prefix`, `PistolRound`, `GiveArmor`, `TriggerWords`, `AddSkipOption`, `Weapons`, `Nades`, `Votes`). `Core` implements `IPluginConfig<RetakesAllocatorConfig>`; CounterStrikeSharp loads/creates the file and calls `OnConfigParsed` (before `Load`, and again on file-watch hot reload). A pure `ConfigApplier.Apply(config)` copies config values into the existing static state the rest of the code already reads (`Utils.PREFIX`, `Core.NadesConfig`, `Allocator` weapon lists, `Votes` fields); vote chat-commands are (re)registered idempotently. The explicit reload command re-applies the in-memory config.

**Tech Stack:** C# / .NET 8, CounterStrikeSharp (`BasePluginConfig` / `IPluginConfig<T>`), System.Text.Json attributes (`[JsonPropertyName]`), xUnit.

---

## Important context for the implementer

**CounterStrikeSharp `IPluginConfig<T>` lifecycle:**
- A plugin implements `IPluginConfig<T>` where `T : BasePluginConfig, new()`.
- The framework reads/creates `addons/counterstrikesharp/configs/plugins/RetakesAllocator/RetakesAllocator.json`, deserializes into `T`, sets the interface `Config` property, and calls `OnConfigParsed(T config)`.
- `OnConfigParsed` is called **once before `Load(hotReload)`**, and **again whenever the file changes on disk** (file-watch hot reload).
- `BasePluginConfig` lives in `CounterStrikeSharp.API.Core` and provides `public int Version { get; set; }`.

**This is a breaking change for existing servers:** config moves from `…/plugins/RetakesAllocator/configs/retakes_allocator.json` (+ `configs/weapons/*.json` + `configs/votes.json`) to the single CounterStrikeSharp-managed file above. There is no automatic migration; Task 6 documents this.

**Name reconciliation:** the old static field `Core.Config` (type `Config`) is reused, but its **type changes** to `RetakesAllocatorConfig`. The interface's required instance `Config` property is implemented **explicitly** so it does not collide with the static field of the same name.

**Why a pure `ConfigApplier`:** keeping the config→state copy in a side-effect-free static method (no `Plugin.AddCommand`, no CounterStrikeSharp runtime calls) makes it unit-testable in the existing `tests/RetakesAllocator.Tests` project, which cannot host the game runtime.

## File Structure

**New files:**
- `Modules/Config/RetakesAllocatorConfig.cs` — the `BasePluginConfig` subclass + `WeaponsSection` + `VotesSection`.
- `Modules/Config/ConfigApplier.cs` — pure `Apply(RetakesAllocatorConfig)` copying config into static state.
- `Modules/Weapons/Nades.cs` — relocated `NadesConfig` + `Nades` model classes (Task 5).
- `tests/RetakesAllocator.Tests/ConfigModelTests.cs` — defaults / validity / JSON section-name tests.
- `tests/RetakesAllocator.Tests/ConfigApplierTests.cs` — apply-to-static-state tests.

**Modified files:**
- `Modules/Votes/Votes.cs` — extract idempotent `RegisterVoteCommands` / `UnregisterVoteCommands`.
- `Modules/Utils.cs` — `PREFIX`/`PREFIX_CON` default to empty (set by `ConfigApplier`).
- `Modules/Core.cs` — implement `IPluginConfig`, `OnConfigParsed`, retype `Config`, rewire `Load`/`Unload`/`LoadConfigs`.
- `Modules/Handlers/Listeners.cs` — `triggerWords` → `TriggerWords`.
- `Modules/Handlers/Commands.cs` — reload command message/flow (uses repurposed `LoadConfigs`).
- `Modules/Config.cs` — delete old `Config` + `Configs` classes (Task 5), keep model classes.

**Deleted files:**
- `Modules/Weapons/Config.cs` (reader + `WeaponsConfig`; models move to `Nades.cs`) — Task 5.
- `Modules/Votes/Config.cs` (reader + `VotesConfig`) — Task 5.

---

## Task 1: Consolidated config model

**Files:**
- Create: `Modules/Config/RetakesAllocatorConfig.cs`
- Test: `tests/RetakesAllocator.Tests/ConfigModelTests.cs`

The model reuses existing classes (`ConnectionConfig`, `PrefixConfig`, `PistolRoundConfig` from `Modules/Config.cs`; `Weapon`, `Allocator`, `NadesConfig` from `Modules/Weapons`; `Vote`, `Votes` from `Modules/Votes`). All are already public and in scope. The section defaults are read once from the existing canonical defaults (DRY) — at construction time (plugin startup, before any apply) those statics still hold their inline defaults.

- [ ] **Step 1: Write the failing tests**

Create `tests/RetakesAllocator.Tests/ConfigModelTests.cs`:

```csharp
using System.Text.Json;
using RetakesAllocator.Modules;
using Xunit;

namespace RetakesAllocator.Tests;

public class ConfigModelTests
{
    [Fact]
    public void Defaults_MatchExistingCanonicalValues()
    {
        var config = new RetakesAllocatorConfig();

        Assert.Equal("mysql", config.DbConnection.Provider);
        Assert.True(config.GiveArmor);
        Assert.True(config.AddSkipOption);
        Assert.Equal(new[] { "guns", "gun", "weapon", "weapons" }, config.TriggerWords);

        // Weapon lists copied from the Allocator canonical defaults.
        Assert.Equal(2, config.Weapons.PrimaryT.Count);
        Assert.Equal("weapon_ak47", config.Weapons.PrimaryT[0].Item);
        Assert.Equal(3, config.Weapons.PrimaryCt.Count);
        Assert.Equal(2, config.Weapons.PistolsT.Count);
        Assert.Equal(3, config.Weapons.PistolsCt.Count);

        // Nades defaults.
        Assert.Equal(2, config.Nades.CTNades.Flashbangs);
        Assert.Equal(1, config.Nades.TNades.Flashbangs);

        // Votes defaults.
        Assert.Equal(60, config.Votes.RequiredPercentage);
        Assert.Equal(5, config.Votes.WeaponSelectionTime);
        Assert.Equal(5, config.Votes.Votes.Count);
    }

    [Fact]
    public void IsValid_DelegatesToDbConnection()
    {
        var config = new RetakesAllocatorConfig();
        config.DbConnection = new ConnectionConfig { Provider = "sqlite", SqlitePath = "weapons.db" };
        Assert.True(config.IsValid());

        config.DbConnection = new ConnectionConfig { Provider = "sqlite", SqlitePath = "" };
        Assert.False(config.IsValid());
    }

    [Fact]
    public void Json_UsesLabeledSectionNames_AndRoundTrips()
    {
        var config = new RetakesAllocatorConfig();
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

        foreach (var section in new[]
        {
            "\"Version\"", "\"DbConnection\"", "\"Prefix\"", "\"PistolRound\"",
            "\"GiveArmor\"", "\"TriggerWords\"", "\"AddSkipOption\"",
            "\"Weapons\"", "\"Nades\"", "\"Votes\"",
        })
        {
            Assert.Contains(section, json);
        }

        var parsed = JsonSerializer.Deserialize<RetakesAllocatorConfig>(json)!;
        Assert.Equal(config.Votes.RequiredPercentage, parsed.Votes.RequiredPercentage);
        Assert.Equal(config.Weapons.PrimaryT[0].Item, parsed.Weapons.PrimaryT[0].Item);
        Assert.Equal(config.Votes.Votes[0].Command, parsed.Votes.Votes[0].Command);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj --filter ConfigModelTests`
Expected: FAIL to compile — `The type or namespace name 'RetakesAllocatorConfig' could not be found`.

- [ ] **Step 3: Create the config model**

Create `Modules/Config/RetakesAllocatorConfig.cs`:

```csharp
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using RetakesAllocator.Modules.Weapons;
using RetakesAllocator.Modules.Votes;
using VotesClass = RetakesAllocator.Modules.Votes.Votes;

namespace RetakesAllocator.Modules;

/// <summary>
/// Single consolidated plugin config, loaded and saved by CounterStrikeSharp's
/// IPluginConfig mechanism. Each subsystem gets its own labeled JSON section.
/// </summary>
public class RetakesAllocatorConfig : BasePluginConfig
{
    [JsonPropertyName("DbConnection")]
    public ConnectionConfig DbConnection { get; set; } = new();

    [JsonPropertyName("Prefix")]
    public PrefixConfig Prefix { get; set; } = new();

    [JsonPropertyName("PistolRound")]
    public PistolRoundConfig PistolRound { get; set; } = new();

    [JsonPropertyName("GiveArmor")]
    public bool GiveArmor { get; set; } = true;

    [JsonPropertyName("TriggerWords")]
    public string[] TriggerWords { get; set; } = { "guns", "gun", "weapon", "weapons" };

    [JsonPropertyName("AddSkipOption")]
    public bool AddSkipOption { get; set; } = true;

    [JsonPropertyName("Weapons")]
    public WeaponsSection Weapons { get; set; } = new();

    [JsonPropertyName("Nades")]
    public NadesConfig Nades { get; set; } = new();

    [JsonPropertyName("Votes")]
    public VotesSection Votes { get; set; } = new();

    public bool IsValid() => DbConnection.IsValid();
}

/// <summary>The four selectable weapon lists. Defaults mirror Allocator's canonical lists.</summary>
public class WeaponsSection
{
    [JsonPropertyName("PrimaryT")]
    public List<Weapon> PrimaryT { get; set; } = new(Allocator.PrimaryT);

    [JsonPropertyName("PrimaryCt")]
    public List<Weapon> PrimaryCt { get; set; } = new(Allocator.PrimaryCt);

    [JsonPropertyName("PistolsT")]
    public List<Weapon> PistolsT { get; set; } = new(Allocator.PistolsT);

    [JsonPropertyName("PistolsCt")]
    public List<Weapon> PistolsCt { get; set; } = new(Allocator.PistolsCT);
}

/// <summary>Vote definitions plus the vote tuning values. Defaults mirror Votes' canonical values.</summary>
public class VotesSection
{
    [JsonPropertyName("RequiredPercentage")]
    public int RequiredPercentage { get; set; } = VotesClass.RequiredPrecentage;

    [JsonPropertyName("WeaponSelectionTime")]
    public int WeaponSelectionTime { get; set; } = VotesClass.WeaponSelectionTime;

    [JsonPropertyName("Votes")]
    public List<Vote> Votes { get; set; } = new(VotesClass.WeaponVotes);
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj --filter ConfigModelTests`
Expected: PASS — 3 passed. (`Weapon` and `Vote` deserialize via their parameterized constructors, whose parameter names match their property names — System.Text.Json supports this.)

Also run `dotnet build RetakesAllocator.csproj` → `Build succeeded.` (new types are unused so far; nothing else changes.)

- [ ] **Step 5: Commit**

```bash
git add Modules/Config/RetakesAllocatorConfig.cs tests/RetakesAllocator.Tests/ConfigModelTests.cs
git commit -m "feat(config): add consolidated RetakesAllocatorConfig model"
```

---

## Task 2: Idempotent vote-command (de)registration

Vote chat-commands are registered per vote. To support hot reload (re-applying a changed vote list), registration must be repeatable without duplicating or leaking commands. This task extracts that into `RegisterVoteCommands` / `UnregisterVoteCommands` and routes the existing caller through them. Behavior is unchanged for the current (single load) path.

This code calls `Plugin.AddCommand` (runtime-coupled) so it is verified by build + reasoning, not a unit test.

**Files:**
- Modify: `Modules/Votes/Votes.cs:66-101`

- [ ] **Step 1: Replace `Votes_OnConfigParsed` and `Votes_OnPluginUnload`**

In `Modules/Votes/Votes.cs`, replace the existing `Votes_OnConfigParsed` method (currently lines 66-79):

```csharp
    public static void Votes_OnConfigParsed(int weaponSelectionTime, int requiredPrecentage)
    {
        WeaponSelectionTime = weaponSelectionTime;
        RequiredPrecentage = requiredPrecentage;
        
        foreach (var vote in WeaponVotes)
        {
            Plugin.AddCommand($"css_{vote.Command}", vote.Description, OnVoteCommand);
            Plugin.AddCommand($"css_force{vote.Command}", $"force {vote.Description}", OnForceVoteCommand);
            AsyncVoteManager voteManager = new(vote);

            VoteManagers.Add(voteManager);
        }
    }
```

with:

```csharp
    public static void Votes_OnConfigParsed(int weaponSelectionTime, int requiredPrecentage)
    {
        WeaponSelectionTime = weaponSelectionTime;
        RequiredPrecentage = requiredPrecentage;

        RegisterVoteCommands();
    }

    /// <summary>
    /// (Re)registers a chat command per vote in <see cref="WeaponVotes"/>. Idempotent:
    /// any previously registered vote commands are removed first, so this is safe to
    /// call again on a config hot reload.
    /// </summary>
    public static void RegisterVoteCommands()
    {
        UnregisterVoteCommands();

        foreach (var vote in WeaponVotes)
        {
            Plugin.AddCommand($"css_{vote.Command}", vote.Description, OnVoteCommand);
            Plugin.AddCommand($"css_force{vote.Command}", $"force {vote.Description}", OnForceVoteCommand);
            VoteManagers.Add(new AsyncVoteManager(vote));
        }
    }

    /// <summary>Removes every currently registered vote command and clears the managers.</summary>
    public static void UnregisterVoteCommands()
    {
        foreach (var voteManager in VoteManagers)
        {
            var command = voteManager.vote.Command;
            Plugin.RemoveCommand($"css_{command}", OnVoteCommand);
            Plugin.RemoveCommand($"css_force{command}", OnForceVoteCommand);
        }

        VoteManagers.Clear();
    }
```

- [ ] **Step 2: Route `Votes_OnPluginUnload` through `UnregisterVoteCommands`**

In `Modules/Votes/Votes.cs`, replace the existing `Votes_OnPluginUnload` method (currently lines 94-101):

```csharp
    public static void Votes_OnPluginUnload()
    {
        foreach (var vote in WeaponVotes)
        {
            Plugin.RemoveCommand($"css_{vote.Command}", OnVoteCommand);
            Plugin.RemoveCommand($"css_force{vote.Command}", OnForceVoteCommand);
        }
    }
```

with:

```csharp
    public static void Votes_OnPluginUnload()
    {
        UnregisterVoteCommands();
    }
```

- [ ] **Step 3: Build**

Run: `dotnet build RetakesAllocator.csproj`
Expected: `Build succeeded.` 0 errors. (`Votes_OnConfigParsed` is still called by the old `Votes/Config.cs` reader, which remains until Task 5; behavior is unchanged.)

- [ ] **Step 4: Run the full test suite (regression)**

Run: `dotnet test tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj`
Expected: PASS — all tests green (the DB tests + Task 1's config tests).

- [ ] **Step 5: Commit**

```bash
git add Modules/Votes/Votes.cs
git commit -m "refactor(votes): extract idempotent Register/UnregisterVoteCommands"
```

---

## Task 3: `ConfigApplier` — copy config into static state

`ConfigApplier.Apply` copies a `RetakesAllocatorConfig` into the static state the rest of the plugin already reads. It is **pure** (no `Plugin.AddCommand`, no runtime calls) so it can be unit-tested. Weapon lists are mutated in place (`Clear` + `AddRange`) to preserve the existing `Allocator` list instances that other code references by reference.

This task also changes `Utils.PREFIX`/`PREFIX_CON` to default to empty strings, because their current initializers read `Core.Config` (which is null until a config is applied) and would throw when the test touches them.

**Files:**
- Modify: `Modules/Utils.cs:12-13`
- Create: `Modules/Config/ConfigApplier.cs`
- Test: `tests/RetakesAllocator.Tests/ConfigApplierTests.cs`

- [ ] **Step 1: Make `Utils.PREFIX`/`PREFIX_CON` default to empty**

In `Modules/Utils.cs`, replace these two lines (currently lines 12-13):

```csharp
    public static string PREFIX { get; set; } = Core.Config.Prefix.Prefix;
    public static string PREFIX_CON { get; set; } = Core.Config.Prefix.PrefixCon;
```

with:

```csharp
    // Populated by ConfigApplier.Apply once the config is parsed.
    public static string PREFIX { get; set; } = string.Empty;
    public static string PREFIX_CON { get; set; } = string.Empty;
```

- [ ] **Step 2: Write the failing test**

Create `tests/RetakesAllocator.Tests/ConfigApplierTests.cs`:

```csharp
using System.Collections.Generic;
using RetakesAllocator.Modules;
using RetakesAllocator.Modules.Weapons;
using RetakesAllocator.Modules.Votes;
using Xunit;
using VotesClass = RetakesAllocator.Modules.Votes.Votes;

namespace RetakesAllocator.Tests;

public class ConfigApplierTests
{
    [Fact]
    public void Apply_CopiesEveryConfigSectionIntoStaticState()
    {
        var config = new RetakesAllocatorConfig
        {
            Prefix = new PrefixConfig { Prefix = "[P]", PrefixCon = "[C]" },
            Weapons = new WeaponsSection
            {
                PrimaryT = new List<Weapon> { new("weapon_ak47", "AK-47") },
                PrimaryCt = new List<Weapon> { new("weapon_m4a1", "M4A4") },
                PistolsT = new List<Weapon> { new("weapon_glock", "Glock-18") },
                PistolsCt = new List<Weapon> { new("weapon_usp_silencer", "USP-S") },
            },
            Nades = new NadesConfig
            {
                CTNades = new Nades { Flashbangs = 9 },
                TNades = new Nades { Flashbangs = 8 },
            },
            Votes = new VotesSection
            {
                RequiredPercentage = 42,
                WeaponSelectionTime = 7,
                Votes = new List<Vote> { new("xx", "x only", new(), new(), false, true) },
            },
        };

        ConfigApplier.Apply(config);

        Assert.Equal("[P]", Utils.PREFIX);
        Assert.Equal("[C]", Utils.PREFIX_CON);

        Assert.Single(Allocator.PrimaryT);
        Assert.Equal("weapon_ak47", Allocator.PrimaryT[0].Item);
        Assert.Single(Allocator.PrimaryCt);
        Assert.Single(Allocator.PistolsT);
        Assert.Single(Allocator.PistolsCT);

        Assert.Equal(9, Core.NadesConfig.CTNades.Flashbangs);
        Assert.Equal(8, Core.NadesConfig.TNades.Flashbangs);

        Assert.Equal(42, VotesClass.RequiredPrecentage);
        Assert.Equal(7, VotesClass.WeaponSelectionTime);
        Assert.Single(VotesClass.WeaponVotes);
        Assert.Equal("xx", VotesClass.WeaponVotes[0].Command);
    }

    [Fact]
    public void Apply_MutatesWeaponListInPlace_PreservingTheInstance()
    {
        var before = Allocator.PrimaryT;

        ConfigApplier.Apply(new RetakesAllocatorConfig
        {
            Weapons = new WeaponsSection
            {
                PrimaryT = new List<Weapon> { new("weapon_ak47", "AK-47") },
            },
        });

        // Same List<Weapon> object is reused, not replaced (other code holds this reference).
        Assert.Same(before, Allocator.PrimaryT);
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj --filter ConfigApplierTests`
Expected: FAIL to compile — `The name 'ConfigApplier' does not exist`.

- [ ] **Step 4: Implement `ConfigApplier`**

Create `Modules/Config/ConfigApplier.cs`:

```csharp
using RetakesAllocator.Modules.Weapons;
using VotesClass = RetakesAllocator.Modules.Votes.Votes;

namespace RetakesAllocator.Modules;

/// <summary>
/// Copies a parsed <see cref="RetakesAllocatorConfig"/> into the static state the
/// rest of the plugin reads. Pure data — registers no commands and touches no
/// CounterStrikeSharp runtime objects, so it is safe to unit test and to call on
/// every config (re)parse.
/// </summary>
public static class ConfigApplier
{
    public static void Apply(RetakesAllocatorConfig config)
    {
        Utils.PREFIX = config.Prefix.Prefix;
        Utils.PREFIX_CON = config.Prefix.PrefixCon;

        Core.NadesConfig = config.Nades;

        ReplaceContents(Allocator.PrimaryT, config.Weapons.PrimaryT);
        ReplaceContents(Allocator.PrimaryCt, config.Weapons.PrimaryCt);
        ReplaceContents(Allocator.PistolsT, config.Weapons.PistolsT);
        ReplaceContents(Allocator.PistolsCT, config.Weapons.PistolsCt);

        ReplaceContents(VotesClass.WeaponVotes, config.Votes.Votes);
        VotesClass.WeaponSelectionTime = config.Votes.WeaponSelectionTime;
        VotesClass.RequiredPrecentage = config.Votes.RequiredPercentage;
    }

    private static void ReplaceContents<T>(List<T> target, List<T> source)
    {
        target.Clear();
        target.AddRange(source);
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj --filter ConfigApplierTests`
Expected: PASS — 2 passed.

Run the full suite too: `dotnet test tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj` → all green. Then `dotnet build RetakesAllocator.csproj` → `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
git add Modules/Utils.cs Modules/Config/ConfigApplier.cs tests/RetakesAllocator.Tests/ConfigApplierTests.cs
git commit -m "feat(config): add pure ConfigApplier and default PREFIX to empty"
```

---

## Task 4: Wire `Core` to `IPluginConfig` (the switch)

This is the integration task. `Core` starts implementing `IPluginConfig<RetakesAllocatorConfig>`; `OnConfigParsed` stores the config, validates it, applies it, and re-registers vote commands on hot reload. The static `Core.Config` is retyped to `RetakesAllocatorConfig`. `Load` stops calling the old loaders and instead registers vote commands once. `LoadConfigs` is repurposed to re-apply the in-memory config (used by the reload command). The one `triggerWords` consumer is updated to `TriggerWords`.

After this task the old reader classes still exist but are no longer called (deleted in Task 5). This task cannot be unit-tested (it touches the plugin lifecycle); it is verified by build + the existing tests staying green.

**Files:**
- Modify: `Modules/Core.cs`
- Modify: `Modules/Handlers/Listeners.cs:57`

- [ ] **Step 1: Update `Core` class declaration and the `Config` field**

In `Modules/Core.cs`, change the class declaration (currently line 22):

```csharp
public class Core : BasePlugin
```

to:

```csharp
public class Core : BasePlugin, IPluginConfig<RetakesAllocatorConfig>
```

Then change the static config field (currently line 31):

```csharp
    public static Config Config = null!;
```

to:

```csharp
    public static RetakesAllocatorConfig Config = null!;

    // Required by IPluginConfig. Implemented explicitly so it does not collide with
    // the static Config field above; OnConfigParsed is what actually stores the config.
    RetakesAllocatorConfig IPluginConfig<RetakesAllocatorConfig>.Config { get; set; } = new();

    private static bool _loaded;
```

(`IPluginConfig` is in `CounterStrikeSharp.API.Core`, already imported via `using CounterStrikeSharp.API.Core;` at line 2.)

- [ ] **Step 2: Remove the old config-loading `using` and add `OnConfigParsed`**

In `Modules/Core.cs`, remove this line (currently line 12):

```csharp
using static RetakesAllocator.Modules.Configs;
```

Then add the `OnConfigParsed` method. Place it directly above the existing `LoadConfigs` method (currently at line 106):

```csharp
    public void OnConfigParsed(RetakesAllocatorConfig config)
    {
        // OnConfigParsed runs before Load on first parse, and again on every
        // file-watch hot reload. Plugin may not be set yet on the first call.
        Plugin = this;
        Config = config;

        if (!Config.IsValid())
        {
            ThrowError("Invalid config, please check your config file.");
            return;
        }

        ConfigApplier.Apply(Config);

        // On the initial parse, Load registers the vote commands once. On a hot
        // reload (after Load), re-register them to reflect any vote changes.
        if (_loaded)
        {
            Votes_OnConfigParsed(Config.Votes.WeaponSelectionTime, Config.Votes.RequiredPercentage);
        }
    }
```

Add `using static RetakesAllocator.Modules.Votes.Votes;` is already present (line 16), so `Votes_OnConfigParsed` resolves.

- [ ] **Step 3: Rewrite `Load` to drop the old loaders and register vote commands once**

In `Modules/Core.cs`, replace the body of `Load` (currently lines 40-66) — specifically the section from `LoadConfigs();` onward. Replace these lines (currently 52-65):

```csharp
        LoadConfigs();

        InitializeDatabase();

        RegisterCommands();
        RegisterEvents();
        RegisterListeners();

        RetakeCapability_OnLoad();

        if (hotReload)
        {
            Utilities.GetPlayers().ForEach(AddPlayerToList);
        }
```

with:

```csharp
        // Config is already loaded and applied by OnConfigParsed (called before Load).
        InitializeDatabase();

        RegisterCommands();
        RegisterEvents();
        RegisterListeners();

        Votes_OnConfigParsed(Config.Votes.WeaponSelectionTime, Config.Votes.RequiredPercentage);

        RetakeCapability_OnLoad();

        _loaded = true;

        if (hotReload)
        {
            Utilities.GetPlayers().ForEach(AddPlayerToList);
        }
```

- [ ] **Step 4: Repurpose `LoadConfigs` to re-apply the in-memory config**

In `Modules/Core.cs`, replace the entire `LoadConfigs` method (currently lines 106-126):

```csharp
    public static void LoadConfigs(bool fullReload = true)
    {
        CreateConfigsDirectory();

        if(fullReload)
        {
            Config = LoadConfig();

            if (!Config.IsValid())
            {
                ThrowError("Invalid config, please check your config file.");
                return;
            }

            Votes.Config.LoadConfig();
        }

        Weapons.Config.LoadConfig();

        PrintToServer("Configs loaded");
    }
```

with:

```csharp
    /// <summary>
    /// Re-applies the in-memory config and re-registers vote commands. Edits to the
    /// config file on disk are picked up automatically by CounterStrikeSharp's
    /// file-watch hot reload (which calls OnConfigParsed); this method re-applies the
    /// already-parsed config on demand (used by the reload command).
    /// </summary>
    public static void ReloadConfig()
    {
        ConfigApplier.Apply(Config);
        Votes_OnConfigParsed(Config.Votes.WeaponSelectionTime, Config.Votes.RequiredPercentage);
        PrintToServer("Config re-applied");
    }
```

(Note: `ReloadConfig` replaces `LoadConfigs`. The reload command is updated to call it in Task 4 Step 6.)

- [ ] **Step 5: Update the `triggerWords` consumer**

In `Modules/Handlers/Listeners.cs`, change line 57:

```csharp
        if(!Core.Config.triggerWords.Any(word => word.Equals(message)))
```

to:

```csharp
        if(!Core.Config.TriggerWords.Any(word => word.Equals(message)))
```

- [ ] **Step 6: Point the reload command at `ReloadConfig`**

In `Modules/Handlers/Commands.cs`, change line 207 (inside `ReloadCommand`):

```csharp
        LoadConfigs(false);
```

to:

```csharp
        ReloadConfig();
```

`ReloadConfig` is a static member of `Core`, and `Commands.cs` already has `using static RetakesAllocator.Modules.Core;` (line 5), so it resolves unqualified.

- [ ] **Step 7: Build and verify no stale references remain**

Run: `dotnet build RetakesAllocator.csproj`
Expected: `Build succeeded.` 0 errors.

If you see errors mentioning `LoadConfigs`, `Configs`, `CreateConfigsDirectory`, `triggerWords`, or `Core.Config` member access, a consumer was missed — fix it (the consumer list is in the plan's context section). The old reader classes (`Configs`, `Weapons.Config`, `Votes.Config`) are now uncalled but still compile; they are removed in Task 5.

- [ ] **Step 8: Run the full test suite**

Run: `dotnet test tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj`
Expected: PASS — all tests green.

- [ ] **Step 9: Commit**

```bash
git add Modules/Core.cs Modules/Handlers/Listeners.cs Modules/Handlers/Commands.cs
git commit -m "feat(config): load config via IPluginConfig and OnConfigParsed"
```

---

## Task 5: Delete the dead readers and relocate models

The three manual JSON readers are now unreferenced. Remove them. Keep the model classes still in use: `ConnectionConfig`/`PrefixConfig`/`PistolRoundConfig` (stay in `Modules/Config.cs`), and `NadesConfig`/`Nades` (move to their own file). Drop `WeaponsConfig` and `VotesConfig` (replaced by config sections).

**Files:**
- Modify: `Modules/Config.cs` (remove `Config` + `Configs` classes)
- Create: `Modules/Weapons/Nades.cs` (relocated `NadesConfig` + `Nades`)
- Delete: `Modules/Weapons/Config.cs`
- Delete: `Modules/Votes/Config.cs`

- [ ] **Step 1: Confirm the readers are unreferenced**

Run: `git grep -nE "Configs\.|Weapons\.Config\.|Votes\.Config\.|LoadConfig\(|WeaponsConfig|VotesConfig|CreateConfigsDirectory" -- 'Modules/**/*.cs'`
Expected: matches only inside the three files being deleted/edited (`Modules/Config.cs`, `Modules/Weapons/Config.cs`, `Modules/Votes/Config.cs`). If anything else matches, fix that reference before continuing.

- [ ] **Step 2: Remove the old `Config` and `Configs` classes from `Modules/Config.cs`**

In `Modules/Config.cs`, delete the entire `Config` class (currently lines 8-32) and the entire `Configs` static class (currently lines 97-133). Keep `ConnectionConfig`, `PrefixConfig`, and `PistolRoundConfig`. Also remove the now-unused `using System.Text.Json;` (line 1) and `using static RetakesAllocator.Modules.Core;` (line 4) — verify with the compiler in Step 6.

After this edit, the top of `Modules/Config.cs` should read:

```csharp
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesAllocator.Modules;

public class ConnectionConfig
{
```

(everything from `public class ConnectionConfig` downward is unchanged: `ConnectionConfig`, `PrefixConfig`, `PistolRoundConfig`.)

- [ ] **Step 3: Create `Modules/Weapons/Nades.cs` with the relocated models**

Create `Modules/Weapons/Nades.cs` with the `NadesConfig` and `Nades` classes (moved verbatim from `Modules/Weapons/Config.cs`):

```csharp
using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace RetakesAllocator.Modules.Weapons;

public class NadesConfig
{
    public Nades CTNades { get; set; } = new();
    public Nades TNades { get; set; } = new();

    public NadesConfig(Nades ctNades, Nades tnNades)
    {
        CTNades = ctNades;
        TNades = tnNades;
    }

    public NadesConfig()
    {
        CTNades = new Nades()
        {
            Flashbangs = 2,
            Smokes = 1,
            Molotovs = 1,
            HeGrenades = 1
        };

        TNades = new Nades()
        {
            Flashbangs = 1,
            Smokes = 1,
            Molotovs = 1,
            HeGrenades = 1
        };
    }
}

public class Nades
{
    public int Flashbangs { get; set; } = 0;
    public int Smokes { get; set; } = 0;
    public int Molotovs { get; set; } = 0;
    public int HeGrenades { get; set; } = 0;

    public Nades()
    {
    }

    public Nades(Nades nades)
    {
        Flashbangs = nades.Flashbangs;
        Smokes = nades.Smokes;
        Molotovs = nades.Molotovs;
        HeGrenades = nades.HeGrenades;
    }

    public bool HasNades()
    {
        return Flashbangs > 0 || Smokes > 0 || Molotovs > 0 || HeGrenades > 0;
    }

    public bool HasFlashbangs()
    {
        return Flashbangs > 0;
    }

    public bool HasSmokes()
    {
        return Smokes > 0;
    }

    public bool HasMolotovs()
    {
        return Molotovs > 0;
    }

    public bool HasHeGrenades()
    {
        return HeGrenades > 0;
    }

    public void RemoveNade(CsItem nade)
    {
        switch (nade)
        {
            case CsItem.Flashbang:
                Flashbangs--;
                break;
            case CsItem.Smoke:
                Smokes--;
                break;
            case CsItem.Molotov or CsItem.Incendiary:
                Molotovs--;
                break;
            case CsItem.HEGrenade:
                HeGrenades--;
                break;
        }
    }
}
```

- [ ] **Step 4: Delete the two reader files**

```bash
git rm Modules/Weapons/Config.cs Modules/Votes/Config.cs
```

- [ ] **Step 5: Remove the now-dangling `using static …Configs;` imports**

Two files imported the deleted `Configs` class via `using static RetakesAllocator.Modules.Configs;`:
- `Modules/Weapons/Config.cs` — already deleted.
- `Modules/Votes/Config.cs` — already deleted.

No other file imports `Configs` (verified in Step 1). If Step 6's build reports an unresolved `Configs` import anywhere, remove that `using static RetakesAllocator.Modules.Configs;` line.

- [ ] **Step 6: Build and test**

Run: `dotnet build RetakesAllocator.csproj`
Expected: `Build succeeded.` 0 errors. (If the compiler flags an unused `using` as an error it will name the file and line — remove that `using`. Unused usings are warnings, not errors, by default, so this should build clean.)

Run: `dotnet test tests/RetakesAllocator.Tests/RetakesAllocator.Tests.csproj`
Expected: PASS — all tests green.

- [ ] **Step 7: Commit**

```bash
git add Modules/Config.cs Modules/Weapons/Nades.cs Modules/Weapons/Config.cs Modules/Votes/Config.cs
git commit -m "refactor(config): delete manual JSON readers, relocate Nades models"
```

---

## Task 6: Documentation and sample config

Update the README to describe the single consolidated config file, its new location, and the breaking change for existing servers.

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Rewrite the Config sections of the README**

In `README.md`, replace everything from the `## Config` heading down to (but not including) the `## Weapons Config` heading with the following. (Read the file first to get the exact current bounds; the `## Config` section currently spans the intro paragraphs plus the MySQL/SQLite JSON examples.)

````markdown
## Config

The config is managed by CounterStrikeSharp and generated automatically on first
load at:

```
addons/counterstrikesharp/configs/plugins/RetakesAllocator/RetakesAllocator.json
```

> **Upgrading from an older version:** configuration moved into this single
> CounterStrikeSharp-managed file. The previous `configs/retakes_allocator.json`,
> `configs/weapons/*.json`, and `configs/votes.json` files are no longer read.
> Re-enter your settings (database credentials, weapon lists, votes) in the new
> file. Editing the file is picked up live via CounterStrikeSharp's config hot
> reload; the in-game `css_weapons_reload` command (requires `@css/root`)
> re-applies the current config.

The `DbConnection` section is generated empty for MySQL and the plugin will raise
an exception until it is filled in. The `Provider` field selects the database
engine: `"mysql"` (MySQL/MariaDB, the default) or `"sqlite"` (a local file, no
server required).

### Example Config

```json
{
  "Version": 1,
  "DbConnection": {
    "Provider": "mysql",
    "Host": "<HOST>",
    "Database": "<DB>",
    "User": "<USER>",
    "Password": "<PASSWORD>",
    "Port": 3306,
    "SqlitePath": "weapons.db"
  },
  "Prefix": {
    "Prefix": " [Retakes]",
    "PrefixCon": "[RetakesAllocator]"
  },
  "PistolRound": {
    "RoundAmount": 2,
    "weapon_t": "weapon_glock",
    "weapon_ct": "weapon_usp_silencer"
  },
  "GiveArmor": true,
  "TriggerWords": [ "guns", "gun", "weapon", "weapons" ],
  "AddSkipOption": true,
  "Weapons": {
    "PrimaryT": [ { "Item": "weapon_ak47", "DisplayName": "AK-47" } ],
    "PrimaryCt": [ { "Item": "weapon_m4a1", "DisplayName": "M4A4" } ],
    "PistolsT": [ { "Item": "weapon_glock", "DisplayName": "Glock-18" } ],
    "PistolsCt": [ { "Item": "weapon_usp_silencer", "DisplayName": "USP-S" } ]
  },
  "Nades": {
    "CTNades": { "Flashbangs": 2, "Smokes": 1, "Molotovs": 1, "HeGrenades": 1 },
    "TNades": { "Flashbangs": 1, "Smokes": 1, "Molotovs": 1, "HeGrenades": 1 }
  },
  "Votes": {
    "RequiredPercentage": 60,
    "WeaponSelectionTime": 5,
    "Votes": [
      {
        "Command": "vp",
        "Description": "pistol only",
        "weapons_t": [ "glock" ],
        "weapons_ct": [ "usp_silencer" ],
        "OnlyHeadshots": false,
        "GiveWeapons": true,
        "GiveKnife": true,
        "GiveArmor": true,
        "GiveHelmet": false
      }
    ]
  }
}
```

For **SQLite**, set `"Provider": "sqlite"` and a `"SqlitePath"`; the MySQL fields
are ignored.
````

- [ ] **Step 2: Remove the now-obsolete "Weapons Config" section**

In `README.md`, the `## Weapons Config` section describes the old `configs/weapons/` files, which no longer exist. Replace that section's body so it points at the consolidated config. Replace the `## Weapons Config` heading and its paragraph with:

```markdown
## Weapons, Nades & Votes

Selectable weapons, grenade kits, and weapon-vote definitions all live in the
`Weapons`, `Nades`, and `Votes` sections of the single config file shown above.
Edit them there; changes are applied on hot reload or via `css_weapons_reload`.
```

- [ ] **Step 3: Commit**

```bash
git add README.md
git commit -m "docs: document the consolidated IPluginConfig config file"
```

---

## Self-Review

**1. Spec coverage** — "replace the config logic to use BasePluginConfig and IPluginConfig instead of files and json reads," decisions: all three subsystems consolidated with labeled JSON sections; reload via CSS hot reload + an explicit command.
- `BasePluginConfig` subclass with labeled sections → Task 1 (`[JsonPropertyName]` on every section). ✓
- `IPluginConfig` + `OnConfigParsed` replace manual loading → Task 4. ✓
- All three readers removed (no `File.ReadAllText`/`JsonSerializer` left in plugin code) → Task 5 deletes `Configs`, `Weapons.Config`, `Votes.Config`. ✓
- Hot reload supported (OnConfigParsed re-applies + re-registers votes when `_loaded`) and explicit reload command kept (`ReloadConfig`) → Tasks 2 + 4. ✓
- Weapon lists / votes / nades / prefix / pistol round / db / trigger words / skip option all carried into static state → Task 3 `ConfigApplier`, verified by `ConfigApplierTests`. ✓

**2. Placeholder scan** — no TBD/TODO; every code step has full code; commands have explicit expected output. ✓

**3. Type consistency** —
- `RetakesAllocatorConfig` members (`DbConnection`, `Prefix`, `PistolRound`, `GiveArmor`, `TriggerWords`, `AddSkipOption`, `Weapons`, `Nades`, `Votes`, `IsValid()`) defined in Task 1 are exactly what Task 3 (`ConfigApplier`), Task 4 (`OnConfigParsed`/`ReloadConfig`/`Load`), and the `Listeners`/`Commands` consumers use. ✓
- `WeaponsSection` props `PrimaryT/PrimaryCt/PistolsT/PistolsCt` map to `Allocator.PrimaryT/PrimaryCt/PistolsT/PistolsCT` in `ConfigApplier`. ✓
- `VotesSection.RequiredPercentage`/`WeaponSelectionTime`/`Votes` map to `Votes.RequiredPrecentage`/`WeaponSelectionTime`/`WeaponVotes` (note the pre-existing misspelled static field name is preserved; only the JSON key is corrected). ✓
- `Votes.RegisterVoteCommands`/`UnregisterVoteCommands` (Task 2) are used by `Votes_OnConfigParsed`/`Votes_OnPluginUnload` and indirectly by Task 4. ✓
- `Core.ReloadConfig` (Task 4) is the method `Commands.ReloadCommand` calls (Task 4 Step 6). ✓
- `NadesConfig`/`Nades` relocated in Task 5 keep the same namespace (`RetakesAllocator.Modules.Weapons`) and members, so `Core.NadesConfig`, `Allocator.ResetNades`, and `ConfigApplier` references stay valid. ✓

**Gap check:** `Core.Config` retype (Task 4) requires every `Core.Config.X` consumer to have a matching member on `RetakesAllocatorConfig`. Verified against the grep in the context section: `DbConnection`, `Prefix.Prefix`/`PrefixCon`, `PistolRound.RoundAmount`/`GetWeaponByTeam`, `GiveArmor`, `AddSkipOption`, `triggerWords`→`TriggerWords`. All present (the only rename, `triggerWords`→`TriggerWords`, is handled in Task 4 Step 5). ✓
