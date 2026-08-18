using System.Collections.Generic;
using RetakesAllocator.Modules.Models;
using RetakesAllocator.Modules.Weapons;
using Xunit;

namespace RetakesAllocator.Tests;

/// <summary>
/// <see cref="Allocator.GetWeaponIndex(string, Allocator.WeaponType)"/> is what the
/// weapon menus write into a player's stored preference. It returns -1 for anything
/// it does not recognise (which happens when the config is reloaded while a menu is
/// open), so callers must not store the result blindly.
/// </summary>
[Collection("StaticState")]
public class AllocatorWeaponIndexTests
{
    [Theory]
    [InlineData("AK-47", Allocator.WeaponType.PrimaryT, 0)]
    [InlineData("SG 553", Allocator.WeaponType.PrimaryT, 2)]
    [InlineData("M4A1-S", Allocator.WeaponType.PrimaryCt, 0)]
    [InlineData("Glock-18", Allocator.WeaponType.SecondaryT, 0)]
    [InlineData("Glock-18", Allocator.WeaponType.PistolRoundT, 0)]
    [InlineData("USP-S", Allocator.WeaponType.SecondaryCt, 0)]
    [InlineData("USP-S", Allocator.WeaponType.PistolRoundCt, 0)]
    public void GetWeaponIndex_ResolvesKnownDisplayNames(string display, Allocator.WeaponType type, int expected)
    {
        Assert.Equal(expected, Allocator.GetWeaponIndex(display, type));
    }

    [Theory]
    [InlineData(Allocator.WeaponType.PrimaryT)]
    [InlineData(Allocator.WeaponType.PrimaryCt)]
    [InlineData(Allocator.WeaponType.SecondaryT)]
    [InlineData(Allocator.WeaponType.SecondaryCt)]
    [InlineData(Allocator.WeaponType.PistolRoundT)]
    [InlineData(Allocator.WeaponType.PistolRoundCt)]
    public void GetWeaponIndex_ReturnsMinusOne_ForUnknownWeapon(Allocator.WeaponType type)
    {
        Assert.Equal(-1, Allocator.GetWeaponIndex("Not A Real Gun", type));
    }

    [Fact]
    public void GetWeaponIndex_ReturnsMinusOne_WhenListWasEmptiedByAConfigReload()
    {
        var saved = new List<Weapon>(Allocator.PrimaryT);

        try
        {
            Allocator.PrimaryT.Clear();
            Assert.Equal(-1, Allocator.GetWeaponIndex("AK-47", Allocator.WeaponType.PrimaryT));
        }
        finally
        {
            Allocator.PrimaryT.Clear();
            Allocator.PrimaryT.AddRange(saved);
        }
    }
}
