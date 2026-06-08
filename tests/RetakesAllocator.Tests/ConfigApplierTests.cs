using System.Collections.Generic;
using RetakesAllocator.Modules;
using RetakesAllocator.Modules.Weapons;
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
    // Canonical weapon lists captured ONCE before any test runs, by reading the
    // field initializers in Allocator. We use the hard-coded defaults here so the
    // restore is independent of whatever state Allocator is in at class-load time.
    private static readonly List<Weapon> _canonicalPrimaryT = new()
    {
        new("weapon_ak47", "AK-47"),
        new("weapon_sg556", "SG 553"),
    };
    private static readonly List<Weapon> _canonicalPrimaryCt = new()
    {
        new("weapon_m4a1", "M4A4"),
        new("weapon_m4a1_silencer", "M4A1-S"),
        new("weapon_aug", "AUG"),
    };
    private static readonly List<Weapon> _canonicalPistolsT = new()
    {
        new("weapon_glock", "Glock-18"),
        new("weapon_p250", "P250"),
    };
    private static readonly List<Weapon> _canonicalPistolsCT = new()
    {
        new("weapon_usp_silencer", "USP-S"),
        new("weapon_p250", "P250"),
        new("weapon_hkp2000", "P2000"),
    };
    private static readonly List<Vote> _canonicalVotes = new()
    {
        new("vp", "pistol only", new() { "glock" }, new() { "usp_silencer" }, false, true, true, true, false),
        new("vph", "pistol only with headshots only", new() { "glock" }, new() { "usp_silencer" }, true, true),
        new("vhs", "headshots only", new(), new(), true, false),
        new("vawp", "awp only", new() { "awp" }, new() { "awp" }, false, true),
        new("vrifles", "rifle only", new() { "ak47", "galilar" }, new() { "m4a1", "m4a1_silencer" }, false, true),
    };
    private const int CanonicalRequiredPrecentage = 60;
    private const int CanonicalWeaponSelectionTime = 5;

    public void Dispose()
    {
        // Restore weapon lists in-place so list instances are preserved.
        Allocator.PrimaryT.Clear();
        Allocator.PrimaryT.AddRange(_canonicalPrimaryT);
        Allocator.PrimaryCt.Clear();
        Allocator.PrimaryCt.AddRange(_canonicalPrimaryCt);
        Allocator.PistolsT.Clear();
        Allocator.PistolsT.AddRange(_canonicalPistolsT);
        Allocator.PistolsCT.Clear();
        Allocator.PistolsCT.AddRange(_canonicalPistolsCT);

        VotesClass.WeaponVotes.Clear();
        VotesClass.WeaponVotes.AddRange(_canonicalVotes);
        VotesClass.RequiredPrecentage = CanonicalRequiredPrecentage;
        VotesClass.WeaponSelectionTime = CanonicalWeaponSelectionTime;

        Utils.PREFIX = string.Empty;
        Utils.PREFIX_CON = string.Empty;
        Core.NadesConfig = null!;
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
