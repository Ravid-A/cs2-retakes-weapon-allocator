using Xunit;
using RetakesAllocator.Modules;

namespace RetakesAllocator.Tests;

public class WeaponStoreTests
{
    [Fact]
    public async Task GetUserAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        using var db = new TempDb();

        var result = await db.Store.GetUserAsync("STEAM_1:0:000000");

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateUserAsync_ThenGetUserAsync_ReturnsDefaultsRow()
    {
        using var db = new TempDb();

        await db.Store.CreateUserAsync("STEAM_1:0:111111", "Alice");
        var result = await db.Store.GetUserAsync("STEAM_1:0:111111");

        Assert.NotNull(result);
        Assert.Equal("STEAM_1:0:111111", result!.Auth);
        Assert.Equal("Alice", result.Name);
        Assert.Equal(0, result.TPrimary);
        Assert.Equal(0, result.CtPrimary);
        Assert.Equal(0, result.TSecondary);
        Assert.Equal(0, result.CtSecondary);
        Assert.Equal(0, result.TPistolRound);
        Assert.Equal(0, result.CtPistolRound);
        Assert.Equal(0, result.GiveAwp);
    }

    [Fact]
    public async Task SaveUserAsync_PersistsAllPreferenceColumns()
    {
        using var db = new TempDb();
        await db.Store.CreateUserAsync("STEAM_1:0:222222", "Bob");

        var pref = new WeaponPreference
        {
            Auth = "STEAM_1:0:222222",
            TPrimary = 1,
            CtPrimary = 2,
            TSecondary = 1,
            CtSecondary = 2,
            TPistolRound = 1,
            CtPistolRound = 2,
            GiveAwp = 2,
        };
        await db.Store.SaveUserAsync(pref);

        var result = await db.Store.GetUserAsync("STEAM_1:0:222222");
        Assert.NotNull(result);
        Assert.Equal(1, result!.TPrimary);
        Assert.Equal(2, result.CtPrimary);
        Assert.Equal(1, result.TSecondary);
        Assert.Equal(2, result.CtSecondary);
        Assert.Equal(1, result.TPistolRound);
        Assert.Equal(2, result.CtPistolRound);
        Assert.Equal(2, result.GiveAwp);
    }

    [Fact]
    public async Task CreateUserAsync_EscapesNameWithQuote_NoInjection()
    {
        using var db = new TempDb();

        // A name that would break interpolated SQL; parameterization must handle it.
        await db.Store.CreateUserAsync("STEAM_1:0:333333", "Robert'); DROP TABLE weapons;--");

        var result = await db.Store.GetUserAsync("STEAM_1:0:333333");
        Assert.NotNull(result);
        Assert.Equal("Robert'); DROP TABLE weapons;--", result!.Name);
    }
}
