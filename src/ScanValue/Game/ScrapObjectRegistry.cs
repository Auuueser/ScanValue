using System;
using System.Collections.Generic;
using Auuueser.ScanValue.Localization;
using BepInEx.Logging;
using UnityEngine;

namespace Auuueser.ScanValue.Game;

internal sealed class ScrapObjectRegistry
{
    private readonly ManualLogSource logger;
    private readonly ScrapItemNameLocalizer nameLocalizer;
    private readonly List<TrackedScrapItem> items = new List<TrackedScrapItem>(256);
    private readonly Dictionary<ScanNodeProperties, TrackedScrapItem> scanNodeLookup = new Dictionary<ScanNodeProperties, TrackedScrapItem>(256);
    private bool debugRegistrations;
    private bool debugTrackZeroValueItems;
    private int nextRegistrationId;

    public ScrapObjectRegistry(ManualLogSource logger, ScrapItemNameLocalizer nameLocalizer)
    {
        this.logger = logger;
        this.nameLocalizer = nameLocalizer;
    }

    public int Count => items.Count;

    public TrackedScrapItem this[int index] => items[index];

    public void SetDebugOptions(bool logRegistrations, bool trackZeroValueItems)
    {
        debugRegistrations = logRegistrations;
        debugTrackZeroValueItems = trackZeroValueItems;
    }

    public void Register(GrabbableObject item)
    {
        if (!ShouldTrack(item))
        {
            return;
        }

        var marker = item.gameObject.GetComponent<ScrapObjectMarker>();
        if (marker != null && marker.Registry == this && marker.TrackedItem != null)
        {
            marker.TrackedItem.RefreshValue(item.scrapValue);
            UpdateScanNode(marker.TrackedItem, FindScanNode(item));
            marker.TrackedItem.RefreshNames(nameLocalizer.ResolveNames(GetItemName(item)));
            return;
        }

        marker = item.gameObject.AddComponent<ScrapObjectMarker>();
        var scanNode = FindScanNode(item);
        var tracked = new TrackedScrapItem(
            item,
            item.transform,
            item.scrapValue,
            scanNode,
            FindRenderers(item),
            marker,
            items.Count,
            nextRegistrationId++,
            nameLocalizer.ResolveNames(GetItemName(item)));
        marker.Initialize(this, tracked);
        items.Add(tracked);
        AddScanNodeLookup(tracked);

        if (debugRegistrations)
        {
            logger.LogInfo($"ScanValue registered '{item.itemProperties.itemName}' value={item.scrapValue} count={items.Count}.");
        }
    }

    public void RefreshValue(GrabbableObject item)
    {
        if (item == null)
        {
            return;
        }

        var marker = item.gameObject.GetComponent<ScrapObjectMarker>();
        if (marker != null && marker.Registry == this && marker.TrackedItem != null)
        {
            if (!ShouldTrack(item))
            {
                UnregisterMarker(marker);
                return;
            }

            marker.TrackedItem.RefreshValue(item.scrapValue);
            UpdateScanNode(marker.TrackedItem, FindScanNode(item));
            marker.TrackedItem.RefreshNames(nameLocalizer.ResolveNames(GetItemName(item)));
            return;
        }

        Register(item);
    }

    public void Unregister(GrabbableObject item)
    {
        if (item == null)
        {
            return;
        }

        var marker = item.gameObject.GetComponent<ScrapObjectMarker>();
        if (marker != null)
        {
            UnregisterMarker(marker);
        }
    }

    public void UnregisterMarker(ScrapObjectMarker marker)
    {
        if (marker.Registry != this || marker.TrackedItem == null)
        {
            return;
        }

        RemoveAt(marker.TrackedItem.Index);
        marker.ClearRegistration();
    }

    public void Clear()
    {
        for (var index = items.Count - 1; index >= 0; index--)
        {
            items[index].Marker.ClearRegistration();
        }

        items.Clear();
        scanNodeLookup.Clear();
    }

    public bool TryGetByScanNode(ScanNodeProperties? scanNode, out TrackedScrapItem tracked)
    {
        if (scanNode != null && scanNodeLookup.TryGetValue(scanNode, out tracked))
        {
            return true;
        }

        tracked = null!;
        return false;
    }

    public bool TryGet(GrabbableObject item, out TrackedScrapItem tracked)
    {
        if (item != null)
        {
            var marker = item.gameObject.GetComponent<ScrapObjectMarker>();
            if (marker != null && marker.Registry == this && marker.TrackedItem != null)
            {
                tracked = marker.TrackedItem;
                return true;
            }
        }

        tracked = null!;
        return false;
    }

    private bool ShouldTrack(GrabbableObject item)
    {
        if (item == null || item.itemProperties == null)
        {
            return false;
        }

        return item.itemProperties.isScrap ||
               item.scrapValue > 0 ||
               debugTrackZeroValueItems;
    }

    private static string GetItemName(GrabbableObject item)
    {
        var itemName = item.itemProperties != null ? item.itemProperties.itemName : null;
        return string.IsNullOrWhiteSpace(itemName) ? item.name : itemName;
    }

    private static ScanNodeProperties? FindScanNode(GrabbableObject item)
    {
        return item.GetComponentInChildren<ScanNodeProperties>(includeInactive: true);
    }

    private static Renderer[] FindRenderers(GrabbableObject item)
    {
        return item != null
            ? item.GetComponentsInChildren<Renderer>(includeInactive: true)
            : Array.Empty<Renderer>();
    }

    private void UpdateScanNode(TrackedScrapItem tracked, ScanNodeProperties? scanNode)
    {
        if (tracked.ScanNode == scanNode)
        {
            return;
        }

        RemoveScanNodeLookup(tracked);
        tracked.RefreshScanNode(scanNode);
        AddScanNodeLookup(tracked);
    }

    private void AddScanNodeLookup(TrackedScrapItem tracked)
    {
        if (tracked.ScanNode != null)
        {
            scanNodeLookup[tracked.ScanNode] = tracked;
        }
    }

    private void RemoveScanNodeLookup(TrackedScrapItem tracked)
    {
        if (tracked.ScanNode == null)
        {
            return;
        }

        if (scanNodeLookup.TryGetValue(tracked.ScanNode, out var current) && current == tracked)
        {
            scanNodeLookup.Remove(tracked.ScanNode);
        }
    }

    private void RemoveAt(int index)
    {
        if (index < 0 || index >= items.Count)
        {
            logger.LogDebug($"Ignoring stale scrap registry index {index}.");
            return;
        }

        var lastIndex = items.Count - 1;
        RemoveScanNodeLookup(items[index]);
        if (index != lastIndex)
        {
            var moved = items[lastIndex];
            moved.Index = index;
            items[index] = moved;
        }

        items.RemoveAt(lastIndex);
    }
}
