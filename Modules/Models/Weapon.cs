namespace RetakesAllocator.Modules.Models;

public class Weapon(string item, string displayName)
{
    public string Item { get; set; } = item;
    public string DisplayName { get; set; } = displayName;
}