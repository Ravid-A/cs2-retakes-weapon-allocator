using System.Text.Json;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Configs;
using static RetakesAllocator.Modules.Votes.Votes;

namespace RetakesAllocator.Modules.Votes;

public class Config
{
    private const string votes_path = "votes.json";

    public static void LoadConfig()
    {
        LoadVotes();
    }

    private static void LoadVotes()
    {
        string configPath = Path.Combine(Plugin.ModuleDirectory, ConfigDirectory, votes_path);

        if (!File.Exists(configPath))
        {
            CreateConfig(configPath, WeaponVotes);
            return;
        }

        var config = JsonSerializer.Deserialize<VotesConfig>(File.ReadAllText(configPath))!;

        if(config.Votes.Count == 0)
        {
            CreateConfig(configPath, WeaponVotes);
            return;
        }

        WeaponVotes.Clear();

        foreach (var vote in config.Votes)
        {
            WeaponVotes.Add(vote);
        }

        Votes_OnConfigParsed(config.WeaponSelectionTime, config.RequiredPrecentage);
    }

    private static void CreateConfig(string configPath, List<Vote> votes)
    {
        var config = new VotesConfig(votes);

        File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));

        Votes_OnConfigParsed(config.WeaponSelectionTime, config.RequiredPrecentage);
    }
}

class VotesConfig
{
    public int RequiredPrecentage { get; set; } = 60;
    public int WeaponSelectionTime { get; set; } = 5;
    public List<Vote> Votes { get; set; } = new();

    public VotesConfig(List<Vote> votes)
    {
        foreach (Vote vote in votes)
        {
            Votes.Add(vote);
        }
        RequiredPrecentage = 60;
        WeaponSelectionTime = 5;
    }
}