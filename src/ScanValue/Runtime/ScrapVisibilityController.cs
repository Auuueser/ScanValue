using System.Collections.Generic;
using Auuueser.ScanValue.Configuration;
using Auuueser.ScanValue.Core.Domain;
using Auuueser.ScanValue.Game;
using Auuueser.ScanValue.Presentation;
using BepInEx.Logging;
using UnityEngine;

namespace Auuueser.ScanValue.Runtime;

internal sealed class ScrapVisibilityController : MonoBehaviour
{
    private const int VisualPrewarmBatchSize = 4;
    private const int LayoutCandidateMargin = 24;
    private const int HighlightPrewarmBatchSize = 2;
    private const int HighlightPrewarmCandidateChecks = 16;

    private ModConfig config = null!;
    private ManualLogSource logger = null!;
    private ScrapObjectRegistry registry = null!;
    private ScrapPricePresenter presenter = null!;
    private ScrapHighlightPresenter highlightPresenter = null!;
    private LocalPlayerProvider localPlayerProvider = null!;
    private ScrapDiagnostics diagnostics = null!;
    private ScrapVisibilityRules rules = null!;
    private ScrapVisibilityRules highlightRules = null!;
    private readonly ScrapLabelLayoutResolver layoutResolver = new ScrapLabelLayoutResolver(ScrapLabelLayoutOptions.Default);
    private readonly List<TrackedScrapItem> preLayoutItems = new List<TrackedScrapItem>(128);
    private readonly List<float> preLayoutDistanceSquares = new List<float>(128);
    private readonly List<bool> preLayoutWasVisible = new List<bool>(128);
    private readonly List<TrackedScrapItem> layoutItems = new List<TrackedScrapItem>(128);
    private readonly List<ScrapLabelCandidate> layoutCandidates = new List<ScrapLabelCandidate>(128);
    private readonly List<ScrapLabelPlacement> layoutPlacements = new List<ScrapLabelPlacement>(128);
    private readonly List<Vector3> layoutAnchors = new List<Vector3>(128);
    private readonly List<TrackedScrapItem> visibleVanillaScanItems = new List<TrackedScrapItem>(32);
    private readonly List<TrackedScrapItem> incomingVanillaScanItems = new List<TrackedScrapItem>(32);
    private int settingsVersion = -1;
    private float nextRefreshTime;
    private float nextPrewarmTime;
    private float nextDebugHeartbeatLogTime;
    private int nextHighlightPrewarmIndex;
    private bool vanillaScanHighlightRefreshRequired = true;
    private bool initialized;

    public void Initialize(
        ModConfig config,
        ManualLogSource logger,
        ScrapObjectRegistry registry,
        ScrapPricePresenter presenter,
        ScrapHighlightPresenter highlightPresenter,
        LocalPlayerProvider localPlayerProvider)
    {
        this.config = config;
        this.logger = logger;
        this.registry = registry;
        this.presenter = presenter;
        this.highlightPresenter = highlightPresenter;
        this.localPlayerProvider = localPlayerProvider;
        diagnostics = new ScrapDiagnostics(logger);
        initialized = true;
        RefreshSettingsIfNeeded();
    }

    public void SetEnabled(bool enabled)
    {
        gameObject.SetActive(enabled);
    }

    public bool ShouldSuppressVanillaScanElements => initialized && config.Current.Visibility.Enabled;

    public bool ShouldOptimizeVanillaScan => initialized && config.Current.ScanPerformance.OptimizeVanillaScan;

    public void MarkVanillaScanVisualStateDirty()
    {
        if (!initialized)
        {
            return;
        }

        vanillaScanHighlightRefreshRequired = true;
        nextRefreshTime = 0f;
    }

    public void SetVisibleVanillaScanNodes(Dictionary<RectTransform, ScanNodeProperties> scanNodes)
    {
        if (!initialized)
        {
            return;
        }

        if (scanNodes == null)
        {
            ClearVisibleVanillaScanNodes();
            return;
        }

        var settings = config.Current;
        if (!settings.Visibility.Enabled || settings.Visibility.ActivationMode != ScrapRevealActivationMode.VanillaScan)
        {
            ClearVisibleVanillaScanNodes();
            highlightPresenter.HideAll();
            return;
        }

        incomingVanillaScanItems.Clear();
        var valueChanged = false;
        foreach (var pair in scanNodes)
        {
            var scanNode = pair.Value;
            if (scanNode == null || scanNode.nodeType != 2)
            {
                continue;
            }

            if (!registry.TryGetByScanNode(scanNode, out var tracked))
            {
                continue;
            }

            if (!TryApplyScanNodeValue(scanNode, tracked, out var itemValueChanged))
            {
                continue;
            }

            if (itemValueChanged)
            {
                valueChanged = true;
            }

            incomingVanillaScanItems.Add(tracked);
        }

        var scanItemsChanged = !VanillaScanItemsMatch();
        if (scanItemsChanged)
        {
            visibleVanillaScanItems.Clear();
            visibleVanillaScanItems.AddRange(incomingVanillaScanItems);
            nextRefreshTime = 0f;
            vanillaScanHighlightRefreshRequired = true;
        }

        if (valueChanged)
        {
            nextRefreshTime = 0f;
        }

        if (scanItemsChanged || valueChanged || vanillaScanHighlightRefreshRequired)
        {
            RefreshHighlightedVanillaScanItems(settings, incomingVanillaScanItems);
            vanillaScanHighlightRefreshRequired = false;
        }
    }

    public void ClearVisibleVanillaScanNodes()
    {
        if (!initialized)
        {
            return;
        }

        highlightPresenter.HideAll();
        if (visibleVanillaScanItems.Count == 0)
        {
            return;
        }

        visibleVanillaScanItems.Clear();
        nextRefreshTime = 0f;
        if (config.Current.Visibility.ActivationMode == ScrapRevealActivationMode.VanillaScan)
        {
            presenter.HideScrapLabels();
        }
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        config.ReloadIfChangedOnDisk(Time.realtimeSinceStartup);
        RefreshSettingsIfNeeded();

        var settings = config.Current;
        if (!settings.Visibility.Enabled)
        {
            highlightPresenter.HideAll();
            presenter.HideAll();
            LogDebugHeartbeatIfDue(settings, null);
            return;
        }

        var usesVanillaScanNodes = settings.Visibility.ActivationMode == ScrapRevealActivationMode.VanillaScan;
        PrewarmScanVisualCachesIfDue(settings, usesVanillaScanNodes);
        if (usesVanillaScanNodes && visibleVanillaScanItems.Count == 0 &&
            !settings.Debug.ShouldShowCameraTestLabel &&
            !settings.Debug.ShouldLogDiagnostics)
        {
            highlightPresenter.HideAll();
            presenter.HideDebugTestLabel();
            presenter.HideScrapLabels();
            return;
        }

        if (!localPlayerProvider.TryGet(out var playerPosition, out var playerCamera, out var failureReason))
        {
            highlightPresenter.HideAll();
            presenter.HideDebugTestLabel();
            presenter.HideAll();
            diagnostics.LogNoCameraIfDue(settings.Debug, registry.Count, failureReason);
            LogDebugHeartbeatIfDue(settings, null);
            return;
        }

        if (settings.Debug.ShouldShowCameraTestLabel)
        {
            presenter.ShowDebugTestLabel(playerCamera);
        }
        else
        {
            presenter.HideDebugTestLabel();
        }

        LogDebugHeartbeatIfDue(settings, playerCamera);

        if (usesVanillaScanNodes && visibleVanillaScanItems.Count == 0)
        {
            highlightPresenter.HideAll();
            presenter.HideScrapLabels();
            return;
        }

        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + settings.Visibility.UpdateIntervalSeconds;
        RefreshVisibleLabels(settings, playerPosition, playerCamera, usesVanillaScanNodes);
    }

    private void RefreshSettingsIfNeeded()
    {
        if (settingsVersion == config.SettingsVersion)
        {
            return;
        }

        settingsVersion = config.SettingsVersion;
        rules = new ScrapVisibilityRules(config.Current.Visibility, config.Current.Debug);
        highlightRules = new ScrapVisibilityRules(config.Current.Visibility, ScrapDebugOptions.Disabled);
        layoutResolver.UpdateOptions(ScrapLabelLayoutOptions.ForItemNames(config.Current.ItemNames.ShowItemNames));
        registry.SetDebugOptions(
            config.Current.Debug.Enabled && config.Current.Debug.LogRegistrations,
            config.Current.Debug.Enabled && config.Current.Debug.ShowZeroValueItems);
        presenter.ApplyStyle(config.Current);
        highlightPresenter.ApplyStyle(config.Current);
        vanillaScanHighlightRefreshRequired = true;
        if (!config.Current.Visibility.Enabled ||
            !config.Current.Highlight.Enabled ||
            config.Current.Visibility.ActivationMode != ScrapRevealActivationMode.VanillaScan)
        {
            visibleVanillaScanItems.Clear();
            highlightPresenter.HideAll();
        }

        nextRefreshTime = 0f;
        nextPrewarmTime = 0f;
        if (config.Current.Debug.ShouldLogDiagnostics)
        {
            logger.LogInfo("ScanValue settings reloaded.");
        }
    }

    private void LogDebugHeartbeatIfDue(ModSettings settings, Camera? camera)
    {
        var debug = settings.Debug;
        if (!debug.ShouldLogDiagnostics || Time.unscaledTime < nextDebugHeartbeatLogTime)
        {
            return;
        }

        nextDebugHeartbeatLogTime = Time.unscaledTime + debug.LogIntervalSeconds;
        var cameraName = camera != null ? camera.name : "none";
        logger.LogInfo(
            $"ScanValue debug heartbeat: enabled={settings.Visibility.Enabled} camera='{cameraName}' registered={registry.Count} " +
            $"active={presenter.ActiveCount} testLabel={presenter.DebugTestLabelVisible}");
    }

    private void PrewarmScanVisualCachesIfDue(ModSettings settings, bool usesVanillaScanNodes)
    {
        if (!usesVanillaScanNodes || visibleVanillaScanItems.Count != 0)
        {
            return;
        }

        if (Time.unscaledTime < nextPrewarmTime)
        {
            return;
        }

        nextPrewarmTime = Time.unscaledTime + settings.Visibility.UpdateIntervalSeconds;
        presenter.Prewarm(settings.Visibility.MaxVisibleLabels, VisualPrewarmBatchSize);
        PrewarmHighlightProxies(settings, HighlightPrewarmBatchSize);
    }

    private void PrewarmHighlightProxies(ModSettings settings, int maxCreatedCount)
    {
        if (!settings.Highlight.Enabled || registry.Count == 0 || maxCreatedCount <= 0)
        {
            return;
        }

        var attempts = registry.Count < HighlightPrewarmCandidateChecks
            ? registry.Count
            : HighlightPrewarmCandidateChecks;
        var created = 0;
        for (var attempt = 0; attempt < attempts && created < maxCreatedCount; attempt++)
        {
            if (nextHighlightPrewarmIndex >= registry.Count)
            {
                nextHighlightPrewarmIndex = 0;
            }

            var tracked = registry[nextHighlightPrewarmIndex];
            nextHighlightPrewarmIndex++;
            if (!tracked.IsAlive)
            {
                continue;
            }

            var snapshot = CreateVanillaScanSnapshot(tracked);
            if (!highlightRules.ShouldShowVanillaScanned(snapshot))
            {
                continue;
            }

            if (highlightPresenter.Prewarm(tracked, settings))
            {
                created++;
            }
        }
    }

    private void RefreshVisibleLabels(ModSettings settings, Vector3 playerPosition, Camera playerCamera, bool usesVanillaScanNodes)
    {
        preLayoutItems.Clear();
        preLayoutDistanceSquares.Clear();
        preLayoutWasVisible.Clear();
        layoutItems.Clear();
        layoutCandidates.Clear();
        layoutAnchors.Clear();

        var visibleCount = 0;
        var aliveCount = 0;
        var hiddenNoValue = 0;
        var hiddenState = 0;
        var hiddenDistance = 0;
        var hiddenBudget = 0;
        var hiddenOverlap = 0;
        var debug = settings.Debug;
        var collectDebugCounts = debug.ShouldLogDiagnostics;
        var preLayoutCandidateLimit = settings.Visibility.MaxVisibleLabels + LayoutCandidateMargin;
        var preLayoutWorstIndex = -1;
        presenter.BeginFrame();

        var sourceCount = usesVanillaScanNodes ? visibleVanillaScanItems.Count : registry.Count;
        for (var index = 0; index < sourceCount; index++)
        {
            var tracked = usesVanillaScanNodes ? visibleVanillaScanItems[index] : registry[index];
            if (!tracked.IsAlive)
            {
                continue;
            }

            aliveCount++;
            var item = tracked.Item;
            var delta = tracked.Transform.position - playerPosition;
            var scrapValue = usesVanillaScanNodes ? tracked.ScrapValue : item.scrapValue;
            var hasUnknownScanValue = usesVanillaScanNodes && tracked.HasUnknownValue;
            var snapshot = CreateItemSnapshot(tracked, hasUnknownScanValue ? 1 : scrapValue, delta.sqrMagnitude);

            var shouldShow = usesVanillaScanNodes ? rules.ShouldShowVanillaScanned(snapshot) : rules.ShouldShow(snapshot);
            if (!shouldShow)
            {
                if (collectDebugCounts)
                {
                    if (snapshot.ScrapValue <= 0 && !debug.ShowZeroValueItems)
                    {
                        hiddenNoValue++;
                    }
                    else if (snapshot.IsDeactivated || snapshot.IsUsedUp ||
                             ((snapshot.IsHeld || snapshot.IsHeldByEnemy || snapshot.IsPocketed) && !debug.ShowHeldItems))
                    {
                        hiddenState++;
                    }
                    else
                    {
                        hiddenDistance++;
                    }
                }

                continue;
            }

            if (!usesVanillaScanNodes)
            {
                tracked.RefreshValue(scrapValue);
            }

            var wasVisible = presenter.IsActive(tracked);
            if (preLayoutItems.Count < preLayoutCandidateLimit)
            {
                AddPreLayoutCandidate(tracked, snapshot.DistanceSquaredToPlayer, wasVisible);
                if (preLayoutItems.Count == preLayoutCandidateLimit)
                {
                    preLayoutWorstIndex = FindLowestPreLayoutPriorityIndex();
                }
            }
            else
            {
                hiddenBudget++;
                var incoming = new ScrapLabelPreCandidate(
                    tracked.RegistrationId,
                    snapshot.DistanceSquaredToPlayer,
                    tracked.ScrapValue,
                    wasVisible);
                var currentWorst = CreatePreLayoutCandidate(preLayoutWorstIndex);
                if (ScrapLabelPreCandidatePriority.IsHigherPriority(incoming, currentWorst))
                {
                    ReplacePreLayoutCandidate(preLayoutWorstIndex, tracked, snapshot.DistanceSquaredToPlayer, wasVisible);
                    preLayoutWorstIndex = FindLowestPreLayoutPriorityIndex();
                }
            }
        }

        for (var index = 0; index < preLayoutItems.Count; index++)
        {
            var tracked = preLayoutItems[index];
            var anchor = tracked.GetLabelAnchor(settings.HeightOffset);
            var screen = playerCamera.WorldToScreenPoint(anchor);
            if (screen.z <= 0f)
            {
                hiddenDistance++;
                continue;
            }

            layoutItems.Add(tracked);
            layoutAnchors.Add(anchor);
            layoutCandidates.Add(new ScrapLabelCandidate(tracked.RegistrationId, screen.x, screen.y, screen.z, tracked.ScrapValue, preLayoutWasVisible[index]));
        }

        layoutResolver.ResolveInto(layoutCandidates, settings.Visibility.MaxVisibleLabels, layoutPlacements);
        for (var index = 0; index < layoutPlacements.Count; index++)
        {
            var placement = layoutPlacements[index];
            if (!placement.IsVisible)
            {
                hiddenOverlap++;
                continue;
            }

            var worldPosition = presenter.ResolvePlacedWorldPosition(playerCamera, layoutAnchors[index], placement);
            presenter.Show(layoutItems[index], worldPosition, playerCamera, settings);
            visibleCount++;
        }

        presenter.EndFrame();

        diagnostics.LogScanIfDue(debug, playerCamera, new ScrapDiagnosticCounters
        {
            Registered = registry.Count,
            Alive = aliveCount,
            Candidates = layoutCandidates.Count,
            Shown = visibleCount,
            HiddenNoValue = hiddenNoValue,
            HiddenState = hiddenState,
            HiddenDistance = hiddenDistance,
            HiddenBudget = hiddenBudget,
            HiddenOverlap = hiddenOverlap,
            PoolActive = presenter.ActiveCount,
            PoolIdle = presenter.IdleCount,
            TestLabel = presenter.DebugTestLabelVisible,
        });
    }

    private void RefreshHighlightedVanillaScanItems(ModSettings settings, IReadOnlyList<TrackedScrapItem> sourceItems)
    {
        if (!settings.Highlight.Enabled)
        {
            highlightPresenter.HideAll();
            return;
        }

        highlightPresenter.BeginFrame();
        var limit = sourceItems.Count < settings.Highlight.MaxHighlightedItems
            ? sourceItems.Count
            : settings.Highlight.MaxHighlightedItems;
        for (var index = 0; index < limit; index++)
        {
            var tracked = sourceItems[index];
            if (!tracked.IsAlive)
            {
                continue;
            }

            var snapshot = CreateVanillaScanSnapshot(tracked);
            if (!highlightRules.ShouldShowVanillaScanned(snapshot))
            {
                continue;
            }

            highlightPresenter.Show(tracked);
        }

        highlightPresenter.EndFrame();
    }

    private static ScrapItemSnapshot CreateVanillaScanSnapshot(TrackedScrapItem tracked)
    {
        return CreateItemSnapshot(
            tracked,
            tracked.HasUnknownValue ? 1 : tracked.ScrapValue,
            distanceSquaredToPlayer: 0f);
    }

    private static ScrapItemSnapshot CreateItemSnapshot(TrackedScrapItem tracked, int scrapValue, float distanceSquaredToPlayer)
    {
        var item = tracked.Item;
        return new ScrapItemSnapshot(
            scrapValue,
            distanceSquaredToPlayer,
            item.isHeld,
            item.isHeldByEnemy,
            item.deactivated,
            item.isPocketed,
            item.itemUsedUp);
    }

    private void AddPreLayoutCandidate(TrackedScrapItem tracked, float distanceSquaredToPlayer, bool wasVisible)
    {
        preLayoutItems.Add(tracked);
        preLayoutDistanceSquares.Add(distanceSquaredToPlayer);
        preLayoutWasVisible.Add(wasVisible);
    }

    private static bool TryApplyScanNodeValue(ScanNodeProperties scanNode, TrackedScrapItem tracked, out bool valueChanged)
    {
        valueChanged = false;
        if (ScrapScanValueText.HasUnknownValue(scanNode.subText))
        {
            valueChanged = !tracked.HasUnknownValue;
            tracked.RefreshUnknownValue();
            return true;
        }

        if (!TryResolveKnownScanNodeValue(scanNode, tracked, out var scrapValue))
        {
            return false;
        }

        valueChanged = tracked.HasUnknownValue || tracked.ScrapValue != scrapValue;
        tracked.RefreshValue(scrapValue);
        return true;
    }

    private static bool TryResolveKnownScanNodeValue(ScanNodeProperties scanNode, TrackedScrapItem tracked, out int scrapValue)
    {
        scrapValue = 0;
        if (scanNode.scrapValue > 0)
        {
            scrapValue = scanNode.scrapValue;
            return true;
        }

        if (ScrapScanValueText.TryParseKnownValue(scanNode.subText, out scrapValue))
        {
            return true;
        }

        if (tracked.Item.scrapValue > 0)
        {
            scrapValue = tracked.Item.scrapValue;
            return true;
        }

        scrapValue = scanNode.scrapValue;
        return true;
    }

    private bool VanillaScanItemsMatch()
    {
        if (visibleVanillaScanItems.Count != incomingVanillaScanItems.Count)
        {
            return false;
        }

        for (var index = 0; index < visibleVanillaScanItems.Count; index++)
        {
            if (visibleVanillaScanItems[index] != incomingVanillaScanItems[index])
            {
                return false;
            }
        }

        return true;
    }

    private void ReplacePreLayoutCandidate(int index, TrackedScrapItem tracked, float distanceSquaredToPlayer, bool wasVisible)
    {
        preLayoutItems[index] = tracked;
        preLayoutDistanceSquares[index] = distanceSquaredToPlayer;
        preLayoutWasVisible[index] = wasVisible;
    }

    private int FindLowestPreLayoutPriorityIndex()
    {
        var worstIndex = 0;
        var worstCandidate = CreatePreLayoutCandidate(worstIndex);
        for (var index = 1; index < preLayoutItems.Count; index++)
        {
            var current = CreatePreLayoutCandidate(index);
            if (ScrapLabelPreCandidatePriority.Compare(current, worstCandidate) > 0)
            {
                worstIndex = index;
                worstCandidate = current;
            }
        }

        return worstIndex;
    }

    private ScrapLabelPreCandidate CreatePreLayoutCandidate(int index)
    {
        var tracked = preLayoutItems[index];
        return new ScrapLabelPreCandidate(
            tracked.RegistrationId,
            preLayoutDistanceSquares[index],
            tracked.ScrapValue,
            preLayoutWasVisible[index]);
    }
}
