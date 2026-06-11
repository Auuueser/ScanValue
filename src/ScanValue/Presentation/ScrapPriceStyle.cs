using Auuueser.ScanValue.Configuration;
using UnityEngine;

namespace Auuueser.ScanValue.Presentation;

internal sealed class ScrapPriceStyle
{
    public ScrapPriceStyle(float worldScale, float fontSize, Color labelColor, Color outlineColor, float outlineWidth)
    {
        WorldScale = worldScale;
        FontSize = fontSize;
        LabelColor = labelColor;
        OutlineColor = outlineColor;
        OutlineWidth = outlineWidth;
    }

    public float WorldScale { get; }

    public float FontSize { get; }

    public Color LabelColor { get; }

    public Color OutlineColor { get; }

    public float OutlineWidth { get; }

    public static ScrapPriceStyle FromSettings(ModSettings settings)
    {
        return new ScrapPriceStyle(
            settings.WorldScale,
            settings.FontSize,
            settings.LabelColor,
            settings.OutlineColor,
            settings.OutlineWidth);
    }
}

