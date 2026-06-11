namespace Auuueser.ScanValue.Core.Domain;

public readonly record struct ScrapLabelLayoutOptions(
    bool Enabled,
    float LabelWidth,
    float LabelHeight,
    float MinGap,
    int MaxOffsetSlots)
{
    public static ScrapLabelLayoutOptions Default { get; } = Create(
        enabled: true,
        labelWidth: 72f,
        labelHeight: 26f,
        minGap: 6f,
        maxOffsetSlots: 6);

    public static ScrapLabelLayoutOptions ForItemNames(bool showItemNames)
    {
        return showItemNames
            ? Create(
                enabled: true,
                labelWidth: 220f,
                labelHeight: 96f,
                minGap: 10f,
                maxOffsetSlots: 6)
            : Default;
    }

    public static ScrapLabelLayoutOptions Create(
        bool enabled,
        float labelWidth,
        float labelHeight,
        float minGap,
        int maxOffsetSlots)
    {
        return new ScrapLabelLayoutOptions(
            enabled,
            ClampFinite(labelWidth, 24f, 240f),
            ClampFinite(labelHeight, 12f, 120f),
            ClampFinite(minGap, 0f, 64f),
            maxOffsetSlots < 0 ? 0 : maxOffsetSlots);
    }

    private static float ClampFinite(float value, float min, float max)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            return min;
        }

        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }
}
