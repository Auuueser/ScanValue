namespace Auuueser.ScanValue.Core.Domain;

public sealed class ScrapDebugOptions
{
    private const float MinLogIntervalSeconds = 0.25f;
    private const float MaxLogIntervalSeconds = 30f;

    private ScrapDebugOptions(
        bool enabled,
        bool diagnosticsEnabled,
        bool showHeldItems,
        bool showZeroValueItems,
        bool logRegistrations,
        bool showCameraTestLabel,
        float logIntervalSeconds)
    {
        Enabled = enabled;
        DiagnosticsEnabled = diagnosticsEnabled;
        ShowHeldItems = showHeldItems;
        ShowZeroValueItems = showZeroValueItems;
        LogRegistrations = logRegistrations;
        ShowCameraTestLabel = showCameraTestLabel;
        LogIntervalSeconds = logIntervalSeconds;
    }

    public static ScrapDebugOptions Disabled { get; } = Create(
        enabled: false,
        diagnosticsEnabled: false,
        showHeldItems: false,
        showZeroValueItems: false,
        logRegistrations: false,
        showCameraTestLabel: false,
        logIntervalSeconds: 3f);

    public bool Enabled { get; }

    public bool DiagnosticsEnabled { get; }

    public bool ShowHeldItems { get; }

    public bool ShowZeroValueItems { get; }

    public bool LogRegistrations { get; }

    public bool ShowCameraTestLabel { get; }

    public float LogIntervalSeconds { get; }

    public bool ShouldLogDiagnostics => Enabled && DiagnosticsEnabled;

    public bool ShouldLogRegistrations => Enabled && LogRegistrations;

    public bool ShouldShowCameraTestLabel => Enabled && ShowCameraTestLabel;

    public bool LogVisibilitySummary => DiagnosticsEnabled;

    public static ScrapDebugOptions Create(
        bool enabled,
        bool diagnosticsEnabled,
        bool showHeldItems,
        bool showZeroValueItems,
        bool logRegistrations,
        bool showCameraTestLabel,
        float logIntervalSeconds)
    {
        return new ScrapDebugOptions(
            enabled,
            diagnosticsEnabled,
            showHeldItems,
            showZeroValueItems,
            logRegistrations,
            showCameraTestLabel,
            ClampFinite(logIntervalSeconds, MinLogIntervalSeconds, MaxLogIntervalSeconds));
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
}

