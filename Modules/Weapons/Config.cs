using System.Text.Json;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Configs;
using static RetakesAllocator.Modules.Weapons.Allocator;
using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace RetakesAllocator.Modules.Weapons;

public class Config
{
    private const string weapons_directory = "weapons";
    private const string tprimary_path = "primary_t.json";
    private const string ctprimary_path = "primary_ct.json";
    private const string tpistols_path = "pistols_t.json";
    private const string ctpistols_path = "pistols_ct.json";
    private const string nades_path = "nades.json";

    public static void LoadConfig()
    {
        CreateWeaponsDirectory();

        LoadTPrimary();
        LoadCTPrimary();
        LoadTPistols();
        LoadCTPistols();
        LoadNades();
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

    private static void LoadTPistols()
    {
        string configPath = Path.Combine(Plugin.ModuleDirectory, ConfigDirectory, weapons_directory, tpistols_path);

        if (!File.Exists(configPath))
        {
            CreateConfig(configPath, PistolsT);
            return;
        }

        PistolsT.Clear();

        var config = JsonSerializer.Deserialize<WeaponsConfig>(File.ReadAllText(configPath))!;

        foreach (var weapon in config.Weapons)
        {
            PistolsT.Add(weapon);
        }
    }
    
    private static void LoadCTPistols()
    {
        string configPath = Path.Combine(Plugin.ModuleDirectory, ConfigDirectory, weapons_directory, ctpistols_path);

        if (!File.Exists(configPath))
        {
            CreateConfig(configPath, PistolsCT);
            return;
        }

        PistolsCT.Clear();

        var config = JsonSerializer.Deserialize<WeaponsConfig>(File.ReadAllText(configPath))!;

        foreach (var weapon in config.Weapons)
        {
            PistolsCT.Add(weapon);
        }
    }

    private static void LoadNades()
    {
        string configPath = Path.Combine(Plugin.ModuleDirectory, ConfigDirectory, weapons_directory, nades_path);

        if (!File.Exists(configPath))
        {
            CreateNadesConfig(configPath);
            return;
        }

        var config = JsonSerializer.Deserialize<NadesConfig>(File.ReadAllText(configPath))!;

        Core.NadesConfig = config;
    }

    private static void CreateConfig(string configPath, List<Weapon> weapons)
    {
        var config = new WeaponsConfig(weapons);

        File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void CreateNadesConfig(string configPath)
    {
        var config = new NadesConfig();

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