namespace Auuueser.ScanValue.Core.Domain;

public sealed class ScrapVisibilityOptions
{
    private const float MinRevealRadius = 1f;
    private const float MaxRevealRadius = 100f;
    private const float DefaultScanRevealDurationSeconds = 10f;
    private const float MinScanRevealDurationSeconds = 0.5f;
    private const float MaxScanRevealDurationSeconds = 60f;
    private const float MinUpdateIntervalSeconds = 0.05f;
    private const float MaxUpdateIntervalSeconds = 1f;
    private const int MinVisibleLabels = 1;
    private const int MaxVisibleLabelsLimit = 256;

    private ScrapVisibilityOptions(
        bool enabled,
        float revealRadius,
        ScrapRevealActivationMode activationMode,
        float scanRevealDurationSeconds,
        float updateIntervalSeconds,
        int maxVisibleLabels)
    {
        Enabled = enabled;
        RevealRadius = revealRadius;
        RevealRadiusSquared = revealRadius * revealRadius;
        ActivationMode = activationMode;
        ScanRevealDurationSeconds = scanRevealDurationSeconds;
        UpdateIntervalSeconds = updateIntervalSeconds;
        MaxVisibleLabels = maxVisibleLabels;
    }

    public bool Enabled { get; }

    public float RevealRadius { get; }

    public float RevealRadiusSquared { get; }

    public ScrapRevealActivationMode ActivationMode { get; }

    public float ScanRevealDurationSeconds { get; }

    public float UpdateIntervalSeconds { get; }

    public int MaxVisibleLabels { get; }

    public static ScrapVisibilityOptions Create(
        bool enabled,
        float revealRadius,
        float updateIntervalSeconds,
        int maxVisibleLabels,
        string activationMode = "VanillaScan",
        float scanRevealDurationSeconds = DefaultScanRevealDurationSeconds)
    {
        return new ScrapVisibilityOptions(
            enabled,
            ClampFinite(revealRadius, MinRevealRadius, MaxRevealRadius),
            ScrapRevealActivationModeParser.Parse(activationMode),
            ClampFiniteOrDefault(
                scanRevealDurationSeconds,
                MinScanRevealDurationSeconds,
                MaxScanRevealDurationSeconds,
                DefaultScanRevealDurationSeconds),
            ClampFinite(updateIntervalSeconds, MinUpdateIntervalSeconds, MaxUpdateIntervalSeconds),
            Clamp(maxVisibleLabels, MinVisibleLabels, MaxVisibleLabelsLimit));
    }

    private static float ClampFiniteOrDefault(float value, float min, float max, float fallback)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            return fallback;
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
