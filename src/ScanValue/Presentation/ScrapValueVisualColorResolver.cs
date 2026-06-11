using Auuueser.ScanValue.Configuration;
using Auuueser.ScanValue.Core.Domain;
using Auuueser.ScanValue.Game;
using UnityEngine;

namespace Auuueser.ScanValue.Presentation;

internal static class ScrapValueVisualColorResolver
{
    public static Color ResolveLabelColor(TrackedScrapItem item, ModSettings settings)
    {
        var color = ResolveBaseColor(item, settings, settings.LabelColor);
        color.a = settings.LabelColor.a;
        return color;
    }

    public static Color ResolveHighlightColor(TrackedScrapItem item, ModSettings settings)
    {
        var color = ResolveBaseColor(item, settings, settings.HighlightColor);
        color.a = settings.Highlight.Alpha;
        return color;
    }

    public static Color ResolveHighlightColor(ScrapValueColorTier tier, ModSettings settings)
    {
        var color = ResolveBaseColor(tier, settings, settings.HighlightColor);
        color.a = settings.Highlight.Alpha;
        return color;
    }

    public static ScrapValueColorTier ResolveTier(TrackedScrapItem item, ModSettings settings)
    {
        return settings.ValueColors.ResolveTier(item.HasUnknownValue, item.ScrapValue);
    }

    private static Color ResolveBaseColor(TrackedScrapItem item, ModSettings settings, Color fallback)
    {
        if (!settings.ValueColors.Enabled)
        {
            return fallback;
        }

        return ResolveBaseColor(ResolveTier(item, settings), settings, fallback);
    }

    private static Color ResolveBaseColor(ScrapValueColorTier tier, ModSettings settings, Color fallback)
    {
        if (!settings.ValueColors.Enabled)
        {
            return fallback;
        }

        return tier switch
        {
            ScrapValueColorTier.Unknown => settings.UnknownValueColor,
            ScrapValueColorTier.Low => settings.LowValueColor,
            ScrapValueColorTier.Medium => settings.MediumValueColor,
            ScrapValueColorTier.High => settings.HighValueColor,
            ScrapValueColorTier.VeryHigh => settings.VeryHighValueColor,
            ScrapValueColorTier.Jackpot => settings.JackpotValueColor,
            _ => fallback,
        };
    }
}
