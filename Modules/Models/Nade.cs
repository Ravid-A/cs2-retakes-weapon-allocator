using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace RetakesAllocator.Modules.Models;

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
            // CsItem.Smoke and CsItem.SmokeGrenade are the same value.
            case CsItem.SmokeGrenade:
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
