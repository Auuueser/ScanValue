namespace Auuueser.ScanValue.Core.Domain;

public sealed class ScrapVisibilityRules
{
    private readonly ScrapVisibilityOptions options;

    public ScrapVisibilityRules(ScrapVisibilityOptions options)
        : this(options, ScrapDebugOptions.Disabled)
    {
    }

    public ScrapVisibilityRules(ScrapVisibilityOptions options, ScrapDebugOptions debug)
    {
        this.options = options;
        Debug = debug;
    }

    public ScrapDebugOptions Debug { get; }

    public bool ShouldShow(ScrapItemSnapshot item)
    {
        if (!ShouldShowVanillaScanned(item))
        {
            return false;
        }

        return item.DistanceSquaredToPlayer <= options.RevealRadiusSquared;
    }

    public bool ShouldShowVanillaScanned(ScrapItemSnapshot item)
    {
        if (!options.Enabled)
        {
            return false;
        }

        if (item.ScrapValue <= 0 && !(Debug.Enabled && Debug.ShowZeroValueItems))
        {
            return false;
        }

        if (item.IsDeactivated || item.IsUsedUp)
        {
            return false;
        }

        if ((item.IsHeld || item.IsHeldByEnemy || item.IsPocketed) && !(Debug.Enabled && Debug.ShowHeldItems))
        {
            return false;
        }

        return true;
    }
}
