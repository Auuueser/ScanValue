namespace Auuueser.ScanValue.Core.Domain;

public readonly record struct ScrapValueColorOptions(
    bool Enabled,
    int LowValueMax,
    int MediumValueMax,
    int HighValueMax,
    int VeryHighValueMax)
{
    private const int MinThreshold = 0;
    private const int MaxThreshold = 9999;

    public static ScrapValueColorOptions Create(
        bool enabled,
        int lowValueMax,
        int mediumValueMax,
        int highValueMax,
        int veryHighValueMax)
    {
        var low = Clamp(lowValueMax, MinThreshold, MaxThreshold);
        var medium = Clamp(mediumValueMax, low, MaxThreshold);
        var high = Clamp(highValueMax, medium, MaxThreshold);
        var veryHigh = Clamp(veryHighValueMax, high, MaxThreshold);
        return new ScrapValueColorOptions(enabled, low, medium, high, veryHigh);
    }

    public ScrapValueColorTier ResolveTier(bool hasUnknownValue, int value)
    {
        if (hasUnknownValue)
        {
            return ScrapValueColorTier.Unknown;
        }

        if (value <= LowValueMax)
        {
            return ScrapValueColorTier.Low;
        }

        if (value <= MediumValueMax)
        {
            return ScrapValueColorTier.Medium;
        }

        if (value <= HighValueMax)
        {
            return ScrapValueColorTier.High;
        }

        return value <= VeryHighValueMax
            ? ScrapValueColorTier.VeryHigh
            : ScrapValueColorTier.Jackpot;
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
