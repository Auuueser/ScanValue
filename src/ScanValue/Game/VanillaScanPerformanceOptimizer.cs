using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using TMPro;
using UnityEngine;

namespace Auuueser.ScanValue.Game;

internal static class VanillaScanPerformanceOptimizer
{
    private const int ScanNodeLayerMask = 4194304;
    private const int ScanLineOfSightMask = 134217984;
    private const float AssignScanIntervalSeconds = 0.25f;
    private const string WarmupText = "Value: $0123456789???";

    private static readonly Vector3 ParkedScanElementPosition = new Vector3(-400f, 0f, 0f);
    private static readonly Dictionary<HUDManager, HudScanCache> Caches = new Dictionary<HUDManager, HudScanCache>(2);

    public static void Clear()
    {
        Caches.Clear();
    }

    public static void Prewarm(HUDManager hud)
    {
        if (hud == null || hud.scanElements == null)
        {
            return;
        }

        var cache = GetOrCreateCache(hud, hud.scanElements);
        cache.PrewarmTextMeshes();
        ParkElements(hud.scanElements);
    }

    public static void ParkAll(HUDManager hud)
    {
        if (hud == null || hud.scanElements == null)
        {
            return;
        }

        var cache = GetOrCreateCache(hud, hud.scanElements);
        cache.ResetNodeState();
        ParkElements(hud.scanElements);
    }

    public static void ParkElement(RectTransform element)
    {
        if (element == null)
        {
            return;
        }

        if (element.gameObject.activeSelf && element.position == ParkedScanElementPosition)
        {
            return;
        }

        element.position = ParkedScanElementPosition;
        if (!element.gameObject.activeSelf)
        {
            element.gameObject.SetActive(true);
        }
    }

    public static void UpdateScanNodesOptimized(
        HUDManager hud,
        PlayerControllerB playerScript,
        RaycastHit[] scanNodesHit,
        RectTransform[] scanElements,
        Dictionary<RectTransform, ScanNodeProperties> scanNodes,
        List<ScanNodeProperties> nodesOnScreen,
        Terminal terminalScript,
        ref float updateScanInterval,
        ref int scannedScrapNum,
        ref int totalScrapScanned,
        ref int totalScrapScannedDisplayNum,
        ref float addToDisplayTotalInterval,
        float playerPingingScan,
        bool suppressScrapScanElements)
    {
        if (hud == null ||
            playerScript == null ||
            scanNodesHit == null ||
            scanElements == null ||
            scanNodes == null ||
            nodesOnScreen == null)
        {
            return;
        }

        var cache = GetOrCreateCache(hud, scanElements);
        if (updateScanInterval <= 0f)
        {
            updateScanInterval = AssignScanIntervalSeconds;
            AssignNewNodesOptimized(
                cache,
                playerScript,
                scanNodesHit,
                scanElements,
                scanNodes,
                nodesOnScreen,
                ref scannedScrapNum,
                ref totalScrapScanned,
                playerPingingScan);
        }

        updateScanInterval -= Time.deltaTime;
        var foundScrap = false;
        var screenCornersReady = false;

        for (var index = 0; index < scanElements.Length; index++)
        {
            var scanElement = scanElements[index];
            if (scanElement == null)
            {
                continue;
            }

            if (scanNodes.Count == 0 ||
                !scanNodes.TryGetValue(scanElement, out var node) ||
                node == null)
            {
                var hiddenElementCache = cache.Get(index, scanElement);
                hiddenElementCache.LastNode = null;
                scanNodes.Remove(scanElement);
                ParkElement(scanElement);
                continue;
            }

            if (NodeIsNotVisible(scanElement, node, scanNodes, cache.NodesOnScreenSet, ref totalScrapScanned))
            {
                var hiddenElementCache = cache.Get(index, scanElement);
                hiddenElementCache.LastNode = null;
                continue;
            }

            var elementCache = cache.Get(index, scanElement);
            if (node.nodeType == 2)
            {
                foundScrap = true;
                if (suppressScrapScanElements)
                {
                    elementCache.LastNode = null;
                    ParkElement(scanElement);
                    continue;
                }
            }

            if (!scanElement.gameObject.activeSelf)
            {
                scanElement.gameObject.SetActive(true);
            }

            if (elementCache.LastNode != node)
            {
                elementCache.LastNode = node;
                elementCache.Animator?.SetInteger("colorNumber", node.nodeType);
                AttemptScanNewCreature(hud, terminalScript, node.creatureScanID);
            }

            SetTextIfChanged(elementCache, node);
            var viewport = playerScript.gameplayCamera.WorldToViewportPoint(node.transform.position);
            if (viewport.x > 1f || viewport.x < 0f || viewport.y > 1f || viewport.y < 0f)
            {
                ParkElement(scanElement);
                continue;
            }

            if (!screenCornersReady)
            {
                hud.playerScreenRectTransform.GetWorldCorners(hud.playerScreenCorners);
                screenCornersReady = true;
            }

            scanElement.position = ResolveScanElementPosition(hud, viewport);
        }

        if (!foundScrap)
        {
            totalScrapScanned = 0;
            totalScrapScannedDisplayNum = 0;
            addToDisplayTotalInterval = 0.35f;
        }

        hud.scanInfoAnimator.SetBool("display", scannedScrapNum >= 2 && foundScrap);
    }

    private static void AssignNewNodesOptimized(
        HudScanCache cache,
        PlayerControllerB playerScript,
        RaycastHit[] scanNodesHit,
        RectTransform[] scanElements,
        Dictionary<RectTransform, ScanNodeProperties> scanNodes,
        List<ScanNodeProperties> nodesOnScreen,
        ref int scannedScrapNum,
        ref int totalScrapScanned,
        float playerPingingScan)
    {
        var cameraTransform = playerScript.gameplayCamera.transform;
        var ray = new Ray(cameraTransform.position + cameraTransform.forward * 20f, cameraTransform.forward);
        var hitCount = Physics.SphereCastNonAlloc(ray, 20f, scanNodesHit, 80f, ScanNodeLayerMask);
        if (hitCount > scanElements.Length)
        {
            hitCount = scanElements.Length;
        }

        nodesOnScreen.Clear();
        cache.NodesOnScreenSet.Clear();
        cache.AssignedNodesSet.Clear();
        foreach (var pair in scanNodes)
        {
            if (pair.Value != null)
            {
                cache.AssignedNodesSet.Add(pair.Value);
            }
        }

        scannedScrapNum = 0;
        for (var index = 0; index < hitCount; index++)
        {
            var hitTransform = scanNodesHit[index].transform;
            if (hitTransform == null ||
                !hitTransform.TryGetComponent<ScanNodeProperties>(out var node) ||
                !MeetsScanNodeRequirements(node, playerScript))
            {
                continue;
            }

            if (node.nodeType == 2)
            {
                scannedScrapNum++;
            }

            if (cache.NodesOnScreenSet.Add(node))
            {
                nodesOnScreen.Add(node);
            }

            if (playerPingingScan >= 0f)
            {
                AssignNodeToUIElement(node, scanElements, scanNodes, cache.AssignedNodesSet, ref totalScrapScanned);
            }
        }
    }

    private static bool MeetsScanNodeRequirements(ScanNodeProperties node, PlayerControllerB playerScript)
    {
        if (node == null)
        {
            return false;
        }

        var delta = playerScript.transform.position - node.transform.position;
        var distanceSquared = delta.sqrMagnitude;
        var maxRangeSquared = node.maxRange * node.maxRange;
        var minRangeSquared = node.minRange * node.minRange;
        if (distanceSquared >= maxRangeSquared || distanceSquared <= minRangeSquared)
        {
            return false;
        }

        var viewport = playerScript.gameplayCamera.WorldToViewportPoint(node.transform.position);
        if (viewport.z <= 0f || viewport.x > 1f || viewport.x < 0f || viewport.y > 1f || viewport.y < 0f)
        {
            return false;
        }

        if (!node.requiresLineOfSight)
        {
            return true;
        }

        return !Physics.Linecast(
            playerScript.gameplayCamera.transform.position,
            node.transform.position,
            ScanLineOfSightMask,
            QueryTriggerInteraction.Ignore);
    }

    private static bool NodeIsNotVisible(
        RectTransform scanElement,
        ScanNodeProperties node,
        Dictionary<RectTransform, ScanNodeProperties> scanNodes,
        HashSet<ScanNodeProperties> nodesOnScreenSet,
        ref int totalScrapScanned)
    {
        if (nodesOnScreenSet.Contains(node))
        {
            return false;
        }

        if (node.nodeType == 2)
        {
            totalScrapScanned = Mathf.Clamp(totalScrapScanned - node.scrapValue, 0, 100000);
        }

        ParkElement(scanElement);
        scanNodes.Remove(scanElement);
        return true;
    }

    private static void AssignNodeToUIElement(
        ScanNodeProperties node,
        RectTransform[] scanElements,
        Dictionary<RectTransform, ScanNodeProperties> scanNodes,
        HashSet<ScanNodeProperties> assignedNodesSet,
        ref int totalScrapScanned)
    {
        if (!assignedNodesSet.Add(node))
        {
            return;
        }

        for (var index = 0; index < scanElements.Length; index++)
        {
            var scanElement = scanElements[index];
            if (scanElement == null || scanNodes.ContainsKey(scanElement))
            {
                continue;
            }

            scanNodes.Add(scanElement, node);
            if (node.nodeType == 2)
            {
                totalScrapScanned += node.scrapValue;
            }

            return;
        }
    }

    private static void AttemptScanNewCreature(HUDManager hud, Terminal terminalScript, int enemyID)
    {
        if (enemyID == -1 ||
            terminalScript == null ||
            terminalScript.scannedEnemyIDs.Contains(enemyID))
        {
            return;
        }

        hud.ScanNewCreatureServerRpc(enemyID);
    }

    private static Vector3 ResolveScanElementPosition(HUDManager hud, Vector3 viewport)
    {
        var corners = hud.playerScreenCorners;
        if (!IngamePlayerSettings.Instance.flipCamera)
        {
            return Vector3.Lerp(
                Vector3.Lerp(corners[0], corners[3], viewport.x),
                Vector3.Lerp(corners[1], corners[2], viewport.x),
                viewport.y);
        }

        return Vector3.Lerp(
            Vector3.Lerp(corners[3], corners[0], viewport.x),
            Vector3.Lerp(corners[2], corners[1], viewport.x),
            viewport.y);
    }

    private static void SetTextIfChanged(ScanElementCache cache, ScanNodeProperties node)
    {
        var texts = cache.TextComponents;
        if (texts.Length <= 1)
        {
            return;
        }

        var header = node.headerText ?? string.Empty;
        if (!string.Equals(cache.LastHeader, header, StringComparison.Ordinal))
        {
            cache.LastHeader = header;
            texts[0].text = header;
        }

        var subText = node.subText ?? string.Empty;
        if (!string.Equals(cache.LastSubText, subText, StringComparison.Ordinal))
        {
            cache.LastSubText = subText;
            texts[1].text = subText;
        }
    }

    private static HudScanCache GetOrCreateCache(HUDManager hud, RectTransform[] scanElements)
    {
        if (!Caches.TryGetValue(hud, out var cache))
        {
            cache = new HudScanCache();
            Caches.Add(hud, cache);
        }

        cache.Refresh(scanElements);
        return cache;
    }

    private sealed class HudScanCache
    {
        private RectTransform[]? scanElements;
        private ScanElementCache[] elements = Array.Empty<ScanElementCache>();

        public HashSet<ScanNodeProperties> NodesOnScreenSet { get; } = new HashSet<ScanNodeProperties>();

        public HashSet<ScanNodeProperties> AssignedNodesSet { get; } = new HashSet<ScanNodeProperties>();

        public void Refresh(RectTransform[] currentScanElements)
        {
            if (ReferenceEquals(scanElements, currentScanElements) && elements.Length == currentScanElements.Length)
            {
                return;
            }

            scanElements = currentScanElements;
            elements = new ScanElementCache[currentScanElements.Length];
            for (var index = 0; index < currentScanElements.Length; index++)
            {
                elements[index] = new ScanElementCache(currentScanElements[index]);
            }
        }

        public ScanElementCache Get(int index, RectTransform scanElement)
        {
            var element = elements[index];
            if (element.Element == scanElement)
            {
                return element;
            }

            element = new ScanElementCache(scanElement);
            elements[index] = element;
            return element;
        }

        public void PrewarmTextMeshes()
        {
            for (var index = 0; index < elements.Length; index++)
            {
                var texts = elements[index].TextComponents;
                for (var textIndex = 0; textIndex < texts.Length; textIndex++)
                {
                    var text = texts[textIndex];
                    if (text == null)
                    {
                        continue;
                    }

                    var previousText = text.text;
                    text.text = WarmupText;
                    text.ForceMeshUpdate(ignoreActiveState: true);
                    text.text = previousText;
                    text.ForceMeshUpdate(ignoreActiveState: true);
                }
            }
        }

        public void ResetNodeState()
        {
            for (var index = 0; index < elements.Length; index++)
            {
                elements[index].LastNode = null;
            }
        }
    }

    private sealed class ScanElementCache
    {
        public ScanElementCache(RectTransform element)
        {
            Element = element;
            if (element == null)
            {
                TextComponents = Array.Empty<TextMeshProUGUI>();
                return;
            }

            Animator = element.GetComponent<Animator>();
            TextComponents = element.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true);
        }

        public RectTransform Element { get; }

        public Animator? Animator { get; }

        public TextMeshProUGUI[] TextComponents { get; }

        public ScanNodeProperties? LastNode { get; set; }

        public string LastHeader { get; set; } = string.Empty;

        public string LastSubText { get; set; } = string.Empty;
    }

    private static void ParkElements(RectTransform[] scanElements)
    {
        for (var index = 0; index < scanElements.Length; index++)
        {
            ParkElement(scanElements[index]);
        }
    }
}
