namespace Auuueser.ScanValue.Core.Domain;

public sealed class ScrapScanPerformanceOptions
{
    private ScrapScanPerformanceOptions(bool optimizeVanillaScan)
    {
        OptimizeVanillaScan = optimizeVanillaScan;
    }

    public static ScrapScanPerformanceOptions Create(bool optimizeVanillaScan)
    {
        return new ScrapScanPerformanceOptions(optimizeVanillaScan);
    }

    public bool OptimizeVanillaScan { get; }
}
