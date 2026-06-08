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
