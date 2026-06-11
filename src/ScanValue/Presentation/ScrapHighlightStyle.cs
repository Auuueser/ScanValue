using Auuueser.ScanValue.Configuration;
using UnityEngine;

namespace Auuueser.ScanValue.Presentation;

internal readonly struct ScrapHighlightStyle
{
    public ScrapHighlightStyle(Color color, float width)
    {
        Color = color;
        Width = width;
    }

    public Color Color { get; }

    public float Width { get; }

    public static ScrapHighlightStyle FromSettings(ModSettings settings)
    {
        var color = settings.HighlightColor;
        color.a = settings.Highlight.Alpha;
        return new ScrapHighlightStyle(color, settings.Highlight.Width);
    }
}
