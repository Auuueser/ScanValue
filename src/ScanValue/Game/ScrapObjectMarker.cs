using UnityEngine;

namespace Auuueser.ScanValue.Game;

internal sealed class ScrapObjectMarker : MonoBehaviour
{
    public ScrapObjectRegistry? Registry { get; private set; }

    public TrackedScrapItem? TrackedItem { get; private set; }

    public void Initialize(ScrapObjectRegistry registry, TrackedScrapItem trackedItem)
    {
        Registry = registry;
        TrackedItem = trackedItem;
    }

    public void ClearRegistration()
    {
        Registry = null;
        TrackedItem = null;
    }

    private void OnDestroy()
    {
        Registry?.UnregisterMarker(this);
    }
}

