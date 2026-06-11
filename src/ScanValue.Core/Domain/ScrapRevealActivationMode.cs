using System;

namespace Auuueser.ScanValue.Core.Domain;

public enum ScrapRevealActivationMode
{
    VanillaScan,
    Always,
}

public static class ScrapRevealActivationModeParser
{
    public static ScrapRevealActivationMode Parse(string? value)
    {
        if (string.Equals(value, nameof(ScrapRevealActivationMode.Always), StringComparison.OrdinalIgnoreCase))
        {
            return ScrapRevealActivationMode.Always;
        }

        return ScrapRevealActivationMode.VanillaScan;
    }
}
