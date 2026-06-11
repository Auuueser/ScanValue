using Auuueser.ScanValue.Core.Domain;
using UnityEngine;

namespace Auuueser.ScanValue.Configuration;

internal sealed class ModSettings
{
    public ModSettings(
        ScrapVisibilityOptions visibility,
        float heightOffset,
        float worldScale,
        float fontSize,
        Color labelColor,
        Color outlineColor,
        float outlineWidth,
        Color highlightColor,
        ScrapHighlightOptions highlight,
        ScrapValueColorOptions valueColors,
        Color unknownValueColor,
        Color lowValueColor,
        Color mediumValueColor,
        Color highValueColor,
        Color veryHighValueColor,
        Color jackpotValueColor,
        ScrapItemNameOptions itemNames,
        ScrapScanPerformanceOptions scanPerformance,
        ScrapDebugOptions debug)
    {
        Visibility = visibility;
        HeightOffset = heightOffset;
        WorldScale = worldScale;
        FontSize = fontSize;
        LabelColor = labelColor;
        OutlineColor = outlineColor;
        OutlineWidth = outlineWidth;
        HighlightColor = highlightColor;
        Highlight = highlight;
        ValueColors = valueColors;
        UnknownValueColor = unknownValueColor;
        LowValueColor = lowValueColor;
        MediumValueColor = mediumValueColor;
        HighValueColor = highValueColor;
        VeryHighValueColor = veryHighValueColor;
        JackpotValueColor = jackpotValueColor;
        ItemNames = itemNames;
        ScanPerformance = scanPerformance;
        Debug = debug;
    }

    public ScrapVisibilityOptions Visibility { get; }

    public float HeightOffset { get; }

    public float WorldScale { get; }

    public float FontSize { get; }

    public Color LabelColor { get; }

    public Color OutlineColor { get; }

    public float OutlineWidth { get; }

    public Color HighlightColor { get; }

    public ScrapHighlightOptions Highlight { get; }

    public ScrapValueColorOptions ValueColors { get; }

    public Color UnknownValueColor { get; }

    public Color LowValueColor { get; }

    public Color MediumValueColor { get; }

    public Color HighValueColor { get; }

    public Color VeryHighValueColor { get; }

    public Color JackpotValueColor { get; }

    public ScrapItemNameOptions ItemNames { get; }

    public ScrapScanPerformanceOptions ScanPerformance { get; }

    public ScrapDebugOptions Debug { get; }
}
