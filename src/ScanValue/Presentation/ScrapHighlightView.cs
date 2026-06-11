using System.Collections.Generic;
using Auuueser.ScanValue.Game;
using UnityEngine;
using UnityEngine.Rendering;

namespace Auuueser.ScanValue.Presentation;

internal sealed class ScrapHighlightView
{
    private const string ProxyName = "ScanValue.ScanHighlightProxy";

    private readonly List<GameObject> proxyObjects = new List<GameObject>(8);
    private readonly List<Renderer> proxyRenderers = new List<Renderer>(8);
    private readonly List<Material[]> proxyMaterialSlots = new List<Material[]>(8);
    private readonly List<Transform> proxyTransforms = new List<Transform>(8);
    private Material? currentMaterial;
    private bool visible;

    private ScrapHighlightView()
    {
    }

    public int FrameTouched { get; set; }

    public static ScrapHighlightView Create(TrackedScrapItem item, Material sharedMaterial, ScrapHighlightStyle style)
    {
        var view = new ScrapHighlightView();
        var renderers = item.Renderers;
        for (var index = 0; index < renderers.Length; index++)
        {
            var source = renderers[index];
            if (source == null)
            {
                continue;
            }

            if (source is MeshRenderer meshRenderer)
            {
                view.AddMeshProxy(meshRenderer, sharedMaterial);
                continue;
            }

            if (source is SkinnedMeshRenderer skinnedRenderer)
            {
                view.AddSkinnedProxy(skinnedRenderer, sharedMaterial);
            }
        }

        view.ApplyStyle(style);
        view.ApplyMaterial(sharedMaterial);
        view.SetVisible(false);
        return view;
    }

    public void ApplyStyle(ScrapHighlightStyle style)
    {
        var scale = Vector3.one * (1f + style.Width);
        for (var index = 0; index < proxyTransforms.Count; index++)
        {
            var proxy = proxyTransforms[index];
            if (proxy != null)
            {
                proxy.localScale = scale;
            }
        }
    }

    public void ApplyMaterial(Material sharedMaterial)
    {
        if (currentMaterial == sharedMaterial)
        {
            return;
        }

        currentMaterial = sharedMaterial;
        for (var rendererIndex = 0; rendererIndex < proxyRenderers.Count; rendererIndex++)
        {
            var proxy = proxyRenderers[rendererIndex];
            if (proxy == null)
            {
                continue;
            }

            var slots = proxyMaterialSlots[rendererIndex];
            for (var slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                slots[slotIndex] = sharedMaterial;
            }

            proxy.sharedMaterials = slots;
        }
    }

    public void SetVisible(bool value)
    {
        if (visible && value)
        {
            return;
        }

        visible = value;
        ReapplyVisibilityState();
    }

    public void ReapplyVisibilityState()
    {
        for (var index = 0; index < proxyRenderers.Count; index++)
        {
            var proxyObject = proxyObjects[index];
            if (proxyObject != null && proxyObject.activeSelf != visible)
            {
                proxyObject.SetActive(visible);
            }

            var proxy = proxyRenderers[index];
            if (proxy != null)
            {
                proxy.enabled = visible;
            }
        }
    }

    private void AddMeshProxy(MeshRenderer source, Material sharedMaterial)
    {
        var sourceFilter = source.GetComponent<MeshFilter>();
        if (sourceFilter == null || sourceFilter.sharedMesh == null)
        {
            return;
        }

        var proxyObject = CreateProxyObject(source.transform);
        var filter = proxyObject.AddComponent<MeshFilter>();
        filter.sharedMesh = sourceFilter.sharedMesh;

        var renderer = proxyObject.AddComponent<MeshRenderer>();
        var materialSlots = ConfigureRenderer(source, renderer, sharedMaterial);
        AddProxy(renderer, materialSlots);
    }

    private void AddSkinnedProxy(SkinnedMeshRenderer source, Material sharedMaterial)
    {
        if (source.sharedMesh == null)
        {
            return;
        }

        var proxyObject = CreateProxyObject(source.transform);
        var renderer = proxyObject.AddComponent<SkinnedMeshRenderer>();
        renderer.sharedMesh = source.sharedMesh;
        renderer.bones = source.bones;
        renderer.rootBone = source.rootBone;
        renderer.localBounds = source.localBounds;
        renderer.updateWhenOffscreen = source.updateWhenOffscreen;
        var materialSlots = ConfigureRenderer(source, renderer, sharedMaterial);
        AddProxy(renderer, materialSlots);
    }

    private static GameObject CreateProxyObject(Transform source)
    {
        var proxyObject = new GameObject(ProxyName);
        proxyObject.transform.SetParent(source, false);
        proxyObject.transform.localPosition = Vector3.zero;
        proxyObject.transform.localRotation = Quaternion.identity;
        proxyObject.layer = source.gameObject.layer;
        proxyObject.tag = "DoNotSet";
        proxyObject.SetActive(false);
        return proxyObject;
    }

    private static Material[] ConfigureRenderer(Renderer source, Renderer target, Material sharedMaterial)
    {
        var materialSlots = CreateSharedMaterials(source.sharedMaterials.Length, sharedMaterial);
        target.sharedMaterials = materialSlots;
        target.shadowCastingMode = ShadowCastingMode.Off;
        target.receiveShadows = false;
        target.enabled = false;
        return materialSlots;
    }

    private void AddProxy(Renderer renderer, Material[] materialSlots)
    {
        proxyObjects.Add(renderer.gameObject);
        proxyRenderers.Add(renderer);
        proxyMaterialSlots.Add(materialSlots);
        proxyTransforms.Add(renderer.transform);
    }

    private static Material[] CreateSharedMaterials(int count, Material sharedMaterial)
    {
        var safeCount = count > 0 ? count : 1;
        var slots = new Material[safeCount];
        for (var index = 0; index < slots.Length; index++)
        {
            slots[index] = sharedMaterial;
        }

        return slots;
    }
}
