using System.Text.Json;
using RetakesAllocator.Modules;
using RetakesAllocator.Modules.Config;
using Xunit;

namespace RetakesAllocator.Tests;

[Collection("StaticState")]
public class ConfigModelTests
{
    [Fact]
    public void Defaults_MatchExistingCanonicalValues()
    {
        var config = new RetakesAllocatorConfig();

        Assert.Equal("sqlite", config.DbConnection.Provider);
        Assert.False(config.AddSkipOption);
        Assert.Equal(string.Empty, config.AwpPermission);
        Assert.Equal(new[] { "guns", "gun", "weapon", "weapons" }, config.TriggerWords);

        // Weapon lists copied from the Allocator canonical defaults.
        Assert.Equal(3, config.Weapons.PrimaryT.Count);
        Assert.Equal("weapon_ak47", config.Weapons.PrimaryT[0].Item);
        Assert.Equal(4, config.Weapons.PrimaryCt.Count);
        Assert.Equal(7, config.Weapons.PistolsT.Count);
        Assert.Equal(8, config.Weapons.PistolsCt.Count);

        // Nades defaults.
        Assert.Equal(2, config.Nades.CtNades.Flashbangs);
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
            "\"ConfigVersion\"", "\"DbConnection\"", "\"Prefix\"", "\"PistolRound\"",
            "\"TriggerWords\"", "\"AddSkipOption\"", "\"AwpPermission\"",
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
