using System.Collections.Generic;
using Auuueser.ScanValue.Configuration;
using Auuueser.ScanValue.Core.Domain;
using Auuueser.ScanValue.Game;
using UnityEngine;

namespace Auuueser.ScanValue.Presentation;

internal sealed class ScrapHighlightPresenter
{
    private readonly Dictionary<TrackedScrapItem, ScrapHighlightView> activeViews = new Dictionary<TrackedScrapItem, ScrapHighlightView>(128);
    private readonly Dictionary<TrackedScrapItem, ScrapHighlightView> cachedViews = new Dictionary<TrackedScrapItem, ScrapHighlightView>(128);
    private readonly Dictionary<ScrapValueColorTier, Material> tierMaterials = new Dictionary<ScrapValueColorTier, Material>(6);
    private readonly List<TrackedScrapItem> staleItems = new List<TrackedScrapItem>(128);
    private ScrapHighlightStyle style = new ScrapHighlightStyle(new Color32(255, 212, 71, 89), 0.035f);
    private Material? fallbackMaterial;
    private ModSettings? currentSettings;
    private int frameId;

    public int CachedCount => cachedViews.Count;

    public void ApplyStyle(ModSettings settings)
    {
        currentSettings = settings;
        style = ScrapHighlightStyle.FromSettings(settings);
        fallbackMaterial ??= ScrapHighlightMaterialFactory.Create(style);
        ScrapHighlightMaterialFactory.Apply(fallbackMaterial, style);

        foreach (var pair in tierMaterials)
        {
            ScrapHighlightMaterialFactory.Apply(pair.Value, CreateTierStyle(pair.Key, settings));
        }

        foreach (var pair in cachedViews)
        {
            var view = pair.Value;
            view.ApplyStyle(style);
            view.ApplyMaterial(ResolveMaterial(pair.Key, settings));
        }
    }

    public void BeginFrame()
    {
        frameId++;
    }

    public void Show(TrackedScrapItem item)
    {
        var material = currentSettings != null
            ? ResolveMaterial(item, currentSettings)
            : GetOrCreateFallbackMaterial();
        if (!cachedViews.TryGetValue(item, out var view))
        {
            view = ScrapHighlightView.Create(item, material, style);
            cachedViews.Add(item, view);
        }
        else
        {
            view.ApplyMaterial(material);
        }

        activeViews[item] = view;
        view.FrameTouched = frameId;
        view.SetVisible(true);
    }

    public bool Prewarm(TrackedScrapItem item, ModSettings settings)
    {
        if (item == null || !item.IsAlive)
        {
            return false;
        }

        if (cachedViews.ContainsKey(item))
        {
            return false;
        }

        var material = ResolveMaterial(item, settings);
        var view = ScrapHighlightView.Create(item, material, style);
        cachedViews.Add(item, view);
        return true;
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
            if (activeViews.TryGetValue(item, out var view))
            {
                activeViews.Remove(item);
                view.SetVisible(false);
            }
        }
    }

    public void HideAll()
    {
        if (activeViews.Count == 0)
        {
            return;
        }

        foreach (var view in activeViews.Values)
        {
            view.SetVisible(false);
        }

        activeViews.Clear();
    }

    public void Hide(TrackedScrapItem item)
    {
        if (activeViews.TryGetValue(item, out var view))
        {
            activeViews.Remove(item);
            view.SetVisible(false);
            return;
        }

        if (cachedViews.TryGetValue(item, out view))
        {
            view.SetVisible(false);
        }
    }

    public void ReapplyVisibilityState(TrackedScrapItem item)
    {
        if (cachedViews.TryGetValue(item, out var view))
        {
            view.ReapplyVisibilityState();
        }
    }

    private Material ResolveMaterial(TrackedScrapItem item, ModSettings settings)
    {
        if (!settings.ValueColors.Enabled)
        {
            return GetOrCreateFallbackMaterial();
        }

        var tier = ScrapValueVisualColorResolver.ResolveTier(item, settings);
        return GetOrCreateTierMaterial(tier, CreateTierStyle(tier, settings));
    }

    private Material GetOrCreateFallbackMaterial()
    {
        fallbackMaterial ??= ScrapHighlightMaterialFactory.Create(style);
        return fallbackMaterial;
    }

    private Material GetOrCreateTierMaterial(ScrapValueColorTier tier, ScrapHighlightStyle tierStyle)
    {
        if (tierMaterials.TryGetValue(tier, out var material))
        {
            return material;
        }

        material = ScrapHighlightMaterialFactory.Create(tierStyle);
        tierMaterials.Add(tier, material);
        return material;
    }

    private static ScrapHighlightStyle CreateTierStyle(ScrapValueColorTier tier, ModSettings settings)
    {
        return new ScrapHighlightStyle(
            ScrapValueVisualColorResolver.ResolveHighlightColor(tier, settings),
            settings.Highlight.Width);
    }
}
