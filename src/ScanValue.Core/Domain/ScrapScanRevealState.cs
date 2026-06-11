namespace Auuueser.ScanValue.Core.Domain;

public sealed class ScrapScanRevealState
{
    public void Trigger(ScrapVisibilityOptions options, float currentTime)
    {
    }

    public void Reset()
    {
    }

    public bool IsActive(ScrapVisibilityOptions options, float currentTime)
    {
        return options.ActivationMode == ScrapRevealActivationMode.Always;
    }
}
