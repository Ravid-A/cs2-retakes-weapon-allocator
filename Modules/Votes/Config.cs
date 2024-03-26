using System.Text.Json;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Configs;
using static RetakesAllocator.Modules.Votes.Logic;

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

        WeaponVotes.Clear();

        var config = JsonSerializer.Deserialize<VotesConfig>(File.ReadAllText(configPath))!;

        foreach (var vote in config.Votes)
        {
            WeaponVotes.Add(vote);
        }

    }

    private static void CreateConfig(string configPath, List<Vote> votes)
    {
        var config = new VotesConfig(votes);

        File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
    }
}

class VotesConfig
{
    public List<Vote> Votes { get; set; } = new();

    public VotesConfig(List<Vote> votes)
    {
        foreach (Vote vote in votes)
        {
            Votes.Add(vote);
        }
    }
}