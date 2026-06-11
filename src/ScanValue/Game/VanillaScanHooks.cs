using System.Collections.Generic;
using Auuueser.ScanValue.Runtime;
using BepInEx.Logging;
using GameNetcodeStuff;
using UnityEngine;

namespace Auuueser.ScanValue.Game;

internal static class VanillaScanHooks
{
    private static ScrapVisibilityController? controller;
    private static ManualLogSource? logger;

    public static void Initialize(ScrapVisibilityController activeController, ManualLogSource activeLogger)
    {
        controller = activeController;
        logger = activeLogger;
    }

    public static void Clear(ScrapVisibilityController activeController)
    {
        if (controller == activeController)
        {
            controller = null;
            logger = null;
            VanillaScanPerformanceOptimizer.Clear();
        }
    }

    public static void AfterStart(HUDManager __instance)
    {
        if (controller?.ShouldOptimizeVanillaScan == true)
        {
            VanillaScanPerformanceOptimizer.Prewarm(__instance);
        }
    }

    public static bool BeforeUpdateScanNodes(
        HUDManager __instance,
        PlayerControllerB playerScript,
        RaycastHit[] ___scanNodesHit,
        RectTransform[] ___scanElements,
        Dictionary<RectTransform, ScanNodeProperties> ___scanNodes,
        List<ScanNodeProperties> ___nodesOnScreen,
        Terminal ___terminalScript,
        ref float ___updateScanInterval,
        ref int ___scannedScrapNum,
        ref int ___totalScrapScanned,
        ref int ___totalScrapScannedDisplayNum,
        ref float ___addToDisplayTotalInterval,
        float ___playerPingingScan)
    {
        if (controller?.ShouldOptimizeVanillaScan != true)
        {
            return true;
        }

        try
        {
            VanillaScanPerformanceOptimizer.UpdateScanNodesOptimized(
                __instance,
                playerScript,
                ___scanNodesHit,
                ___scanElements,
                ___scanNodes,
                ___nodesOnScreen,
                ___terminalScript,
                ref ___updateScanInterval,
                ref ___scannedScrapNum,
                ref ___totalScrapScanned,
                ref ___totalScrapScannedDisplayNum,
                ref ___addToDisplayTotalInterval,
                ___playerPingingScan,
                controller?.ShouldSuppressVanillaScanElements == true);
            return false;
        }
        catch (System.Exception ex)
        {
            logger?.LogWarning($"ScanValue vanilla scan optimizer failed; falling back to vanilla scan update. {ex.Message}");
            return true;
        }
    }

    public static void AfterUpdateScanNodes(
        Dictionary<RectTransform, ScanNodeProperties> ___scanNodes,
        RectTransform[] ___scanElements)
    {
        controller?.SetVisibleVanillaScanNodes(___scanNodes);
        if (controller?.ShouldSuppressVanillaScanElements == true)
        {
            SuppressVanillaScrapScanElements(___scanNodes, ___scanElements);
        }
    }

    public static void AfterDisableAllScanElements(HUDManager __instance)
    {
        controller?.ClearVisibleVanillaScanNodes();
        if (controller?.ShouldOptimizeVanillaScan == true)
        {
            VanillaScanPerformanceOptimizer.ParkAll(__instance);
        }
    }

    public static void LogPatchFailure(string methodName)
    {
        logger?.LogWarning($"Could not patch HUDManager.{methodName}; vanilla-scan scrap labels may not mirror the scan UI.");
    }

    private static void SuppressVanillaScrapScanElements(
        Dictionary<RectTransform, ScanNodeProperties> scanNodes,
        RectTransform[] scanElements)
    {
        if (scanNodes == null || scanElements == null)
        {
            return;
        }

        for (var index = 0; index < scanElements.Length; index++)
        {
            var scanElement = scanElements[index];
            if (scanElement == null ||
                !scanElement.gameObject.activeSelf ||
                !scanNodes.TryGetValue(scanElement, out var scanNode) ||
                scanNode == null ||
                scanNode.nodeType != 2)
            {
                continue;
            }

            HideVanillaScanElement(scanElement);
        }
    }

    private static void HideVanillaScanElement(RectTransform scanElement)
    {
        if (controller?.ShouldOptimizeVanillaScan == true)
        {
            VanillaScanPerformanceOptimizer.ParkElement(scanElement);
            return;
        }

        scanElement.gameObject.SetActive(false);
    }
}
