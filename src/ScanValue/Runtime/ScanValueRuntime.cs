using System;
using Auuueser.ScanValue.Configuration;
using Auuueser.ScanValue.Game;
using Auuueser.ScanValue.Localization;
using Auuueser.ScanValue.Presentation;
using BepInEx.Logging;
using UnityEngine;

namespace Auuueser.ScanValue.Runtime;

internal sealed class ScanValueRuntime : IDisposable
{
    private static GameObject? runtimeObject;
    private static ScrapVisibilityController? runtimeController;
    private static ScrapObjectRegistry? registry;
    private static ScrapObjectPatcher? patcher;
    private static VanillaScanPatcher? scanPatcher;
    private static ScrapPricePresenter? presenter;
    private static ScrapHighlightPresenter? highlightPresenter;
    private static ScrapItemNameLocalizer? nameLocalizer;

    private readonly GameObject host;
    private bool disposed;

    private ScanValueRuntime(GameObject host)
    {
        this.host = host;
    }

    public static ScanValueRuntime Start(ModConfig config, ManualLogSource logger)
    {
        if (runtimeObject == null)
        {
            runtimeObject = new GameObject("ScanValue.Runtime");
            runtimeObject.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(runtimeObject);
        }
        else if (!runtimeObject.activeSelf)
        {
            runtimeObject.SetActive(true);
        }

        nameLocalizer ??= ScrapItemNameLocalizer.Load(logger);
        registry ??= new ScrapObjectRegistry(logger, nameLocalizer);
        presenter ??= new ScrapPricePresenter(runtimeObject.transform, nameLocalizer);
        highlightPresenter ??= new ScrapHighlightPresenter();
        patcher ??= new ScrapObjectPatcher(registry, logger, ReapplyHighlightVisibility);

        runtimeController = runtimeObject.GetComponent<ScrapVisibilityController>();
        if (runtimeController == null)
        {
            runtimeController = runtimeObject.AddComponent<ScrapVisibilityController>();
        }

        runtimeController.Initialize(config, logger, registry, presenter, highlightPresenter, new LocalPlayerProvider());
        scanPatcher ??= new VanillaScanPatcher(runtimeController, logger);
        runtimeController.SetEnabled(true);
        logger.LogInfo("Runtime controller active.");

        return new ScanValueRuntime(runtimeObject);
    }

    private static void ReapplyHighlightVisibility(GrabbableObject item)
    {
        if (registry != null &&
            highlightPresenter != null &&
            registry.TryGet(item, out var tracked))
        {
            runtimeController?.MarkVanillaScanVisualStateDirty();
            if (item.isHeld || item.isHeldByEnemy || item.isPocketed || item.deactivated || item.itemUsedUp)
            {
                highlightPresenter.Hide(tracked);
                return;
            }

            highlightPresenter.ReapplyVisibilityState(tracked);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        patcher?.Dispose();
        patcher = null;
        scanPatcher?.Dispose();
        scanPatcher = null;
        registry?.Clear();
        registry = null;
        nameLocalizer = null;
        highlightPresenter?.HideAll();
        highlightPresenter = null;
        presenter = null;
        runtimeController = null;

        if (host != null)
        {
            UnityEngine.Object.Destroy(host);
        }

        if (runtimeObject == host)
        {
            runtimeObject = null;
        }
    }
}
