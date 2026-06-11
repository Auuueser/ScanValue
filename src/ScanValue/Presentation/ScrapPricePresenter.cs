using System.Collections.Generic;
using Auuueser.ScanValue.Configuration;
using Auuueser.ScanValue.Core.Domain;
using Auuueser.ScanValue.Game;
using Auuueser.ScanValue.Localization;
using UnityEngine;

namespace Auuueser.ScanValue.Presentation;

internal sealed class ScrapPricePresenter
{
    private readonly Transform parent;
    private readonly ScrapItemNameLocalizer nameLocalizer;
    private readonly Dictionary<TrackedScrapItem, ScrapPriceView> activeViews = new Dictionary<TrackedScrapItem, ScrapPriceView>(128);
    private readonly List<TrackedScrapItem> staleItems = new List<TrackedScrapItem>(128);
    private readonly Stack<ScrapPriceView> pool = new Stack<ScrapPriceView>(128);
    private ScrapPriceStyle style = new ScrapPriceStyle(0.18f, 3.5f, new Color32(255, 212, 71, 255), new Color32(16, 16, 16, 255), 0.25f);
    private ScrapPriceView? debugTestLabel;
    private int frameId;

    public ScrapPricePresenter(Transform parent, ScrapItemNameLocalizer nameLocalizer)
    {
        this.parent = parent;
        this.nameLocalizer = nameLocalizer;
    }

    public int ActiveCount => activeViews.Count;

    public int IdleCount => pool.Count;

    public bool DebugTestLabelVisible => debugTestLabel != null;

    public void ApplyStyle(ModSettings settings)
    {
        style = ScrapPriceStyle.FromSettings(settings);

        foreach (var pair in activeViews)
        {
            var view = pair.Value;
            view.ApplyStyle(style);
            view.ApplyValueColor(ScrapValueVisualColorResolver.ResolveLabelColor(pair.Key, settings));
        }

        foreach (var view in pool)
        {
            view.ApplyStyle(style);
            view.ApplyValueColor(style.LabelColor);
        }

        if (debugTestLabel != null)
        {
            debugTestLabel.ApplyStyle(style);
            debugTestLabel.ApplyValueColor(style.LabelColor);
        }
    }

    public void BeginFrame()
    {
        frameId++;
    }

    public bool IsActive(TrackedScrapItem item) => activeViews.ContainsKey(item);

    public void Show(TrackedScrapItem item, Vector3 worldPosition, Camera camera, ModSettings settings)
    {
        if (!activeViews.TryGetValue(item, out var view))
        {
            view = Rent();
            activeViews.Add(item, view);
        }

        view.FrameTouched = frameId;
        view.ApplyValueColor(ScrapValueVisualColorResolver.ResolveLabelColor(item, settings));
        var itemName = nameLocalizer.ResolveDisplayName(item, settings);
        if (item.ValueTextOverride != null)
        {
            view.SetNameAndValue(itemName, item.ValueTextOverride);
        }
        else
        {
            view.SetNameAndValue(itemName, item.ScrapValue);
        }

        view.SetWorldPosition(worldPosition, camera);
        view.SetVisible(true);
    }

    public void ShowDebugTestLabel(Camera camera)
    {
        if (debugTestLabel == null)
        {
            debugTestLabel = Rent();
        }

        var worldPosition = camera.transform.position +
            camera.transform.forward * 2.25f -
            camera.transform.up * 0.35f;

        debugTestLabel.FrameTouched = frameId;
        debugTestLabel.ApplyValueColor(style.LabelColor);
        debugTestLabel.SetText("$TEST");
        debugTestLabel.SetWorldPosition(worldPosition, camera);
        debugTestLabel.SetVisible(true);
    }

    public void HideDebugTestLabel()
    {
        if (debugTestLabel == null)
        {
            return;
        }

        Return(debugTestLabel);
        debugTestLabel = null;
    }

    public Vector3 ResolvePlacedWorldPosition(Camera camera, Vector3 anchor, ScrapLabelPlacement placement)
    {
        var originalScreen = camera.WorldToScreenPoint(anchor);
        return camera.ScreenToWorldPoint(new Vector3(placement.ScreenX, placement.ScreenY, originalScreen.z));
    }

    public void EndFrame()
    {
        staleItems.Clear();

        foreach (var pair in activeViews)
        {
            if (pair.Value.FrameTouched != frameId)
            {
                staleItems.Add(pair.Key);
            }
        }

        for (var index = 0; index < staleItems.Count; index++)
        {
            var item = staleItems[index];
            var view = activeViews[item];
            activeViews.Remove(item);
            Return(view);
        }
    }

    public void HideAll()
    {
        HideDebugTestLabel();
        HideScrapLabels();
    }

    public void HideScrapLabels()
    {
        if (activeViews.Count == 0)
        {
            return;
        }

        staleItems.Clear();

        foreach (var item in activeViews.Keys)
        {
            staleItems.Add(item);
        }

        for (var index = 0; index < staleItems.Count; index++)
        {
            var item = staleItems[index];
            var view = activeViews[item];
            activeViews.Remove(item);
            Return(view);
        }
    }

    public void Prewarm(int targetCount, int maxCreatedCount)
    {
        if (targetCount <= 0 || maxCreatedCount <= 0)
        {
            return;
        }

        for (var created = 0; created < maxCreatedCount && pool.Count < targetCount; created++)
        {
            var view = ScrapPriceView.Create(parent, style);
            view.SetVisible(false);
            pool.Push(view);
        }
    }

    private ScrapPriceView Rent()
    {
        if (pool.Count > 0)
        {
            return pool.Pop();
        }

        return ScrapPriceView.Create(parent, style);
    }

    private void Return(ScrapPriceView view)
    {
        view.SetVisible(false);
        pool.Push(view);
    }
}

