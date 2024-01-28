using System.Text.Json;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Configs;
using static RetakesAllocator.Modules.Weapons.Allocator;

namespace RetakesAllocator.Modules.Weapons;

public class Config
{
    private const string weapons_directory = "weapons";
    private const string tprimary_path = "primary_t.json";
    private const string ctprimary_path = "primary_ct.json";
    private const string pistols_path = "pistols.json";

    public static void LoadConfig()
    {
        CreateWeaponsDirectory();

        LoadTPrimary();
        LoadCTPrimary();
        LoadPistols();
    }

    private static void CreateWeaponsDirectory()
    {
        var weaponsDirectory = Path.Combine(Plugin.ModuleDirectory, ConfigDirectory, weapons_directory);

        if (!Directory.Exists(weaponsDirectory))
        {
            Directory.CreateDirectory(weaponsDirectory);
        }
    }

    private static void LoadTPrimary()
    {
        string configPath = Path.Combine(Plugin.ModuleDirectory, ConfigDirectory, weapons_directory, tprimary_path);

        if (!File.Exists(configPath))
        {
            CreateConfig(configPath, PrimaryT);
            return;
        }

        PrimaryT.Clear();

        var config = JsonSerializer.Deserialize<WeaponsConfig>(File.ReadAllText(configPath))!;

        foreach (var weapon in config.Weapons)
        {
            PrimaryT.Add(weapon);
        }

    }

    private static void LoadCTPrimary()
    {
        string configPath = Path.Combine(Plugin.ModuleDirectory, ConfigDirectory, weapons_directory, ctprimary_path);

        if (!File.Exists(configPath))
        {
            CreateConfig(configPath, PrimaryCt);
            return;
        }

        PrimaryCt.Clear();

        var config = JsonSerializer.Deserialize<WeaponsConfig>(File.ReadAllText(configPath))!;

        foreach (var weapon in config.Weapons)
        {
            PrimaryCt.Add(weapon);
        }
    }

    private static void LoadPistols()
    {
        string configPath = Path.Combine(Plugin.ModuleDirectory, ConfigDirectory, weapons_directory, pistols_path);

        if (!File.Exists(configPath))
        {
            CreateConfig(configPath, Pistols);
            return;
        }

        Pistols.Clear();

        var config = JsonSerializer.Deserialize<WeaponsConfig>(File.ReadAllText(configPath))!;

        foreach (var weapon in config.Weapons)
        {
            Pistols.Add(weapon);
        }
    }

    private static void CreateConfig(string configPath, List<Weapon> weapons)
    {
        var config = new WeaponsConfig(weapons);

        File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
    }
}

class WeaponsConfig
{
    public List<Weapon> Weapons { get; set; } = new();

    public WeaponsConfig(List<Weapon> weapons)
    {
        foreach (Weapon weapon in weapons)
        {
            Weapons.Add(weapon);
        }
    }
}