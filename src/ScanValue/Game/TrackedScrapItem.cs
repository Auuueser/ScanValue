using UnityEngine;
using Auuueser.ScanValue.Core.Domain;
using Auuueser.ScanValue.Localization;

namespace Auuueser.ScanValue.Game;

internal sealed class TrackedScrapItem
{
    private const float MinimumRendererTopClearance = 0.18f;
    private const float MaximumRendererTopClearance = 0.35f;

    public TrackedScrapItem(
        GrabbableObject item,
        Transform transform,
        int scrapValue,
        ScanNodeProperties? scanNode,
        Renderer[] renderers,
        ScrapObjectMarker marker,
        int index,
        int registrationId,
        ScrapItemResolvedNames names)
    {
        Item = item;
        Transform = transform;
        ScrapValue = scrapValue;
        ScanNode = scanNode;
        Renderers = renderers;
        Marker = marker;
        Index = index;
        RegistrationId = registrationId;
        EnglishName = names.EnglishName;
        ChineseName = names.ChineseName;
    }

    public GrabbableObject Item { get; }

    public Transform Transform { get; }

    public int ScrapValue { get; private set; }

    public string? ValueTextOverride { get; private set; }

    public bool HasUnknownValue => ValueTextOverride != null;

    public ScanNodeProperties? ScanNode { get; private set; }

    public Renderer[] Renderers { get; }

    public ScrapObjectMarker Marker { get; }

    public int Index { get; set; }

    public int RegistrationId { get; }

    public string EnglishName { get; private set; }

    public string ChineseName { get; private set; }

    public bool IsAlive => Item != null && Transform != null;

    public Vector3 GetLabelAnchor(float heightOffset)
    {
        var fallback = Transform.position + Vector3.up * heightOffset;
        if (!TryGetRendererBounds(out var bounds))
        {
            return fallback;
        }

        var clearance = Mathf.Clamp(heightOffset * 0.25f, MinimumRendererTopClearance, MaximumRendererTopClearance);
        var rendererTopAnchor = new Vector3(bounds.center.x, bounds.max.y + clearance, bounds.center.z);
        return rendererTopAnchor.y > fallback.y ? rendererTopAnchor : fallback;
    }

    public void RefreshValue(int value)
    {
        ScrapValue = value;
        ValueTextOverride = null;
    }

    public void RefreshUnknownValue()
    {
        ScrapValue = 0;
        ValueTextOverride = ScrapScanValueText.UnknownValue;
    }

    public void RefreshScanNode(ScanNodeProperties? scanNode)
    {
        ScanNode = scanNode;
    }

    public void RefreshNames(ScrapItemResolvedNames names)
    {
        EnglishName = names.EnglishName;
        ChineseName = names.ChineseName;
    }

    private bool TryGetRendererBounds(out Bounds combinedBounds)
    {
        combinedBounds = default;
        var hasBounds = false;
        for (var index = 0; index < Renderers.Length; index++)
        {
            var renderer = Renderers[index];
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            var rendererBounds = renderer.bounds;
            if (!IsFinite(rendererBounds.center) ||
                !IsFinite(rendererBounds.extents) ||
                rendererBounds.extents.sqrMagnitude <= 0.0001f)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = rendererBounds;
                hasBounds = true;
                continue;
            }

            combinedBounds.Encapsulate(rendererBounds);
        }

        return hasBounds;
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
