using System.Collections.Generic;
using RetakesAllocator.Modules;
using RetakesAllocator.Modules.Config;
using RetakesAllocator.Modules.Weapons;
using RetakesAllocator.Modules.Models;
using RetakesAllocator.Modules.Votes;
using Xunit;

using VotesClass = RetakesAllocator.Modules.Votes.Votes;

namespace RetakesAllocator.Tests;

/// <summary>
/// Tests for <see cref="ConfigApplier"/>. Implements <see cref="IDisposable"/> to
/// restore the shared static state after each test so other test classes see
/// canonical defaults regardless of execution order.
/// Placed in the "StaticState" collection so it runs sequentially with other
/// classes that read/write the same static fields.
/// </summary>
[Collection("StaticState")]
public class ConfigApplierTests : IDisposable
{
    // Snapshots of the live static state taken BEFORE any test mutates it.
    // Restoring from these snapshots is source-of-truth-agnostic: if the real
    // defaults ever change, teardown automatically tracks them.
    private readonly List<Weapon> _savedPrimaryT;
    private readonly List<Weapon> _savedPrimaryCt;
    private readonly List<Weapon> _savedPistolsT;
    private readonly List<Weapon> _savedPistolsCT;
    private readonly List<Vote> _savedVotes;
    private readonly int _savedWeaponSelectionTime;
    private readonly int _savedRequired;
    private readonly NadesConfig _savedNades;
    private readonly string _savedPrefix;
    private readonly string _savedPrefixCon;
    private readonly HashSet<string> _savedTriggerWords;

    public ConfigApplierTests()
    {
        _savedPrimaryT = new List<Weapon>(Allocator.PrimaryT);
        _savedPrimaryCt = new List<Weapon>(Allocator.PrimaryCt);
        _savedPistolsT = new List<Weapon>(Allocator.PistolsT);
        _savedPistolsCT = new List<Weapon>(Allocator.PistolsCT);
        _savedVotes = new List<Vote>(VotesClass.WeaponVotes);
        _savedWeaponSelectionTime = VotesClass.WeaponSelectionTime;
        _savedRequired = VotesClass.RequiredPercentage;
        _savedNades = Core.NadesConfig;
        _savedPrefix = Utils.Prefix;
        _savedPrefixCon = Utils.PrefixCon;
        _savedTriggerWords = Utils.TriggerWords;
    }

    public void Dispose()
    {
        // Restore weapon lists in-place so list instances are preserved.
        Allocator.PrimaryT.Clear();
        Allocator.PrimaryT.AddRange(_savedPrimaryT);
        Allocator.PrimaryCt.Clear();
        Allocator.PrimaryCt.AddRange(_savedPrimaryCt);
        Allocator.PistolsT.Clear();
        Allocator.PistolsT.AddRange(_savedPistolsT);
        Allocator.PistolsCT.Clear();
        Allocator.PistolsCT.AddRange(_savedPistolsCT);

        VotesClass.WeaponVotes.Clear();
        VotesClass.WeaponVotes.AddRange(_savedVotes);
        VotesClass.WeaponSelectionTime = _savedWeaponSelectionTime;
        VotesClass.RequiredPercentage = _savedRequired;

        Core.NadesConfig = _savedNades;
        Utils.Prefix = _savedPrefix;
        Utils.PrefixCon = _savedPrefixCon;
        Utils.TriggerWords = _savedTriggerWords;
    }

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
                CtNades = new Nades { Flashbangs = 9 },
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

        Assert.Equal("[P]", Utils.Prefix);
        Assert.Equal("[C]", Utils.PrefixCon);

        Assert.Single(Allocator.PrimaryT);
        Assert.Equal("weapon_ak47", Allocator.PrimaryT[0].Item);
        Assert.Single(Allocator.PrimaryCt);
        Assert.Single(Allocator.PistolsT);
        Assert.Single(Allocator.PistolsCT);

        Assert.Equal(9, Core.NadesConfig.CtNades.Flashbangs);
        Assert.Equal(8, Core.NadesConfig.TNades.Flashbangs);

        Assert.Equal(42, VotesClass.RequiredPercentage);
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

    [Fact]
    public void Apply_BuildsACaseInsensitiveTriggerWordSet()
    {
        ConfigApplier.Apply(new RetakesAllocatorConfig
        {
            TriggerWords = new[] { "guns", "  Weapons  ", "", "   " },
        });

        // Empty/whitespace entries are dropped and the rest are trimmed, so the say
        // listener never has to normalise anything per chat message.
        Assert.Equal(2, Utils.TriggerWords.Count);
        Assert.Contains("guns", Utils.TriggerWords);
        Assert.Contains("Weapons", Utils.TriggerWords);
        Assert.Contains("GUNS", Utils.TriggerWords);
        Assert.Contains("weapons", Utils.TriggerWords);
        Assert.DoesNotContain("gun", Utils.TriggerWords);
    }
}
