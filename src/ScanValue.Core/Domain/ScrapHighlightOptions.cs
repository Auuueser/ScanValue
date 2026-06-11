namespace Auuueser.ScanValue.Core.Domain;

public readonly record struct ScrapHighlightOptions(
    bool Enabled,
    float Alpha,
    float Width,
    int MaxHighlightedItems)
{
    private const float MinAlpha = 0f;
    private const float MaxAlpha = 1f;
    private const float MinWidth = 0f;
    private const float MaxWidth = 0.15f;
    private const int MinHighlightedItems = 1;
    private const int MaxHighlightedItemsLimit = 256;

    public static ScrapHighlightOptions Create(
        bool enabled,
        float alpha,
        float width,
        int maxHighlightedItems)
    {
        return new ScrapHighlightOptions(
            enabled,
            ClampFinite(alpha, MinAlpha, MaxAlpha),
            ClampFinite(width, MinWidth, MaxWidth),
            Clamp(maxHighlightedItems, MinHighlightedItems, MaxHighlightedItemsLimit));
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

        if (value > max)
        {
            return max;
        }

        return value;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }
}
