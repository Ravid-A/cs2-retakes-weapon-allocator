namespace RetakesAllocator.Modules;

/// <summary>
/// One row of the `weapons` table. Property names map to snake_case columns via
/// Dapper's MatchNamesWithUnderscores (configured in <see cref="WeaponStore"/>).
/// </summary>
public class WeaponPreference
{
    public string Auth { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int TPrimary { get; set; }
    public int CtPrimary { get; set; }
    public int TSecondary { get; set; }
    public int CtSecondary { get; set; }
    public int TPistolRound { get; set; }
    public int CtPistolRound { get; set; }
    public int GiveAwp { get; set; }
}
