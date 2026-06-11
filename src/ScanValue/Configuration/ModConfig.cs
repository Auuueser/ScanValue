using System;
using System.IO;
using Auuueser.ScanValue.Core.Configuration;
using Auuueser.ScanValue.Core.Domain;
using BepInEx.Configuration;
using UnityEngine;

namespace Auuueser.ScanValue.Configuration;

internal sealed class ModConfig
{
    private const float ConfigFilePollInterval = 0.5f;

    private readonly ConfigFile configFile;
    private readonly string configFilePath;
    private readonly ConfigEntry<bool> enabled;
    private readonly ConfigEntry<float> revealRadius;
    private readonly ConfigEntry<string> activationMode;
    private readonly ConfigEntry<float> scanRevealDurationSeconds;
    private readonly ConfigEntry<float> updateIntervalSeconds;
    private readonly ConfigEntry<bool> optimizeVanillaScan;
    private readonly ConfigEntry<int> maxVisibleLabels;
    private readonly ConfigEntry<float> heightOffset;
    private readonly ConfigEntry<float> worldScale;
    private readonly ConfigEntry<float> fontSize;
    private readonly ConfigEntry<string> labelColor;
    private readonly ConfigEntry<string> outlineColor;
    private readonly ConfigEntry<float> outlineWidth;
    private readonly ConfigEntry<bool> highlightEnabled;
    private readonly ConfigEntry<string> highlightColor;
    private readonly ConfigEntry<float> highlightAlpha;
    private readonly ConfigEntry<float> highlightWidth;
    private readonly ConfigEntry<int> maxHighlightedItems;
    private readonly ConfigEntry<bool> valueBasedColorsEnabled;
    private readonly ConfigEntry<int> lowValueMax;
    private readonly ConfigEntry<int> mediumValueMax;
    private readonly ConfigEntry<int> highValueMax;
    private readonly ConfigEntry<int> veryHighValueMax;
    private readonly ConfigEntry<string> unknownValueColor;
    private readonly ConfigEntry<string> lowValueColor;
    private readonly ConfigEntry<string> mediumValueColor;
    private readonly ConfigEntry<string> highValueColor;
    private readonly ConfigEntry<string> veryHighValueColor;
    private readonly ConfigEntry<string> jackpotValueColor;
    private readonly ConfigEntry<bool> showItemNames;
    private readonly ConfigEntry<string> itemNameLanguage;
    private readonly ConfigEntry<bool> debugEnabled;
    private readonly ConfigEntry<bool> debugDiagnosticsEnabled;
    private readonly ConfigEntry<bool> debugShowHeldItems;
    private readonly ConfigEntry<bool> debugShowZeroValueItems;
    private readonly ConfigEntry<bool> debugLogRegistrations;
    private readonly ConfigEntry<bool> debugShowCameraTestLabel;
    private readonly ConfigEntry<float> debugLogIntervalSeconds;
    private DateTime lastConfigWriteTimeUtc;
    private float nextConfigFilePollTime;

    private ModConfig(
        ConfigFile configFile,
        ConfigLanguage language,
        ConfigEntry<bool> enabled,
        ConfigEntry<float> revealRadius,
        ConfigEntry<string> activationMode,
        ConfigEntry<float> scanRevealDurationSeconds,
        ConfigEntry<float> updateIntervalSeconds,
        ConfigEntry<bool> optimizeVanillaScan,
        ConfigEntry<int> maxVisibleLabels,
        ConfigEntry<float> heightOffset,
        ConfigEntry<float> worldScale,
        ConfigEntry<float> fontSize,
        ConfigEntry<string> labelColor,
        ConfigEntry<string> outlineColor,
        ConfigEntry<float> outlineWidth,
        ConfigEntry<bool> showItemNames,
        ConfigEntry<string> itemNameLanguage,
        ConfigEntry<bool> highlightEnabled,
        ConfigEntry<string> highlightColor,
        ConfigEntry<float> highlightAlpha,
        ConfigEntry<float> highlightWidth,
        ConfigEntry<int> maxHighlightedItems,
        ConfigEntry<bool> valueBasedColorsEnabled,
        ConfigEntry<int> lowValueMax,
        ConfigEntry<int> mediumValueMax,
        ConfigEntry<int> highValueMax,
        ConfigEntry<int> veryHighValueMax,
        ConfigEntry<string> unknownValueColor,
        ConfigEntry<string> lowValueColor,
        ConfigEntry<string> mediumValueColor,
        ConfigEntry<string> highValueColor,
        ConfigEntry<string> veryHighValueColor,
        ConfigEntry<string> jackpotValueColor,
        ConfigEntry<bool> debugEnabled,
        ConfigEntry<bool> debugDiagnosticsEnabled,
        ConfigEntry<bool> debugShowHeldItems,
        ConfigEntry<bool> debugShowZeroValueItems,
        ConfigEntry<bool> debugLogRegistrations,
        ConfigEntry<bool> debugShowCameraTestLabel,
        ConfigEntry<float> debugLogIntervalSeconds)
    {
        this.configFile = configFile;
        configFilePath = configFile.ConfigFilePath;
        Language = language;
        Texts = ConfigTextCatalog.Get(language);
        this.enabled = enabled;
        this.revealRadius = revealRadius;
        this.activationMode = activationMode;
        this.scanRevealDurationSeconds = scanRevealDurationSeconds;
        this.updateIntervalSeconds = updateIntervalSeconds;
        this.optimizeVanillaScan = optimizeVanillaScan;
        this.maxVisibleLabels = maxVisibleLabels;
        this.heightOffset = heightOffset;
        this.worldScale = worldScale;
        this.fontSize = fontSize;
        this.labelColor = labelColor;
        this.outlineColor = outlineColor;
        this.outlineWidth = outlineWidth;
        this.showItemNames = showItemNames;
        this.itemNameLanguage = itemNameLanguage;
        this.highlightEnabled = highlightEnabled;
        this.highlightColor = highlightColor;
        this.highlightAlpha = highlightAlpha;
        this.highlightWidth = highlightWidth;
        this.maxHighlightedItems = maxHighlightedItems;
        this.valueBasedColorsEnabled = valueBasedColorsEnabled;
        this.lowValueMax = lowValueMax;
        this.mediumValueMax = mediumValueMax;
        this.highValueMax = highValueMax;
        this.veryHighValueMax = veryHighValueMax;
        this.unknownValueColor = unknownValueColor;
        this.lowValueColor = lowValueColor;
        this.mediumValueColor = mediumValueColor;
        this.highValueColor = highValueColor;
        this.veryHighValueColor = veryHighValueColor;
        this.jackpotValueColor = jackpotValueColor;
        this.debugEnabled = debugEnabled;
        this.debugDiagnosticsEnabled = debugDiagnosticsEnabled;
        this.debugShowHeldItems = debugShowHeldItems;
        this.debugShowZeroValueItems = debugShowZeroValueItems;
        this.debugLogRegistrations = debugLogRegistrations;
        this.debugShowCameraTestLabel = debugShowCameraTestLabel;
        this.debugLogIntervalSeconds = debugLogIntervalSeconds;

        Reload();
        SubscribeChanges();
        configFile.Save();
        RefreshLastWriteTime();
    }

    public ConfigLanguage Language { get; }

    public ConfigTexts Texts { get; }

    public ModSettings Current { get; private set; } = null!;

    public int SettingsVersion { get; private set; }

    public static ModConfig Bind(ConfigFile config, ConfigLanguage language)
    {
        var texts = ConfigTextCatalog.Get(language);
        return new ModConfig(
            config,
            language,
            config.Bind(texts.GeneralSection, "Enabled", true, texts.EnabledDescription),
            config.Bind(texts.VisibilitySection, "RevealRadius", 12f, texts.RevealRadiusDescription),
            config.Bind(texts.VisibilitySection, "ActivationMode", "VanillaScan", texts.ActivationModeDescription),
            config.Bind(texts.VisibilitySection, "ScanRevealDurationSeconds", 10f, texts.ScanRevealDurationSecondsDescription),
            config.Bind(texts.PerformanceSection, "UpdateIntervalSeconds", 0.12f, texts.UpdateIntervalSecondsDescription),
            config.Bind(texts.PerformanceSection, "OptimizeVanillaScan", true, texts.OptimizeVanillaScanDescription),
            config.Bind(texts.VisibilitySection, "MaxVisibleLabels", 64, texts.MaxVisibleLabelsDescription),
            config.Bind(texts.LabelSection, "HeightOffset", 0.85f, texts.HeightOffsetDescription),
            config.Bind(texts.LabelSection, "WorldScale", 0.18f, texts.WorldScaleDescription),
            config.Bind(texts.LabelSection, "FontSize", 3.5f, texts.FontSizeDescription),
            config.Bind(texts.LabelSection, "LabelColor", "#FFD447", texts.LabelColorDescription),
            config.Bind(texts.LabelSection, "OutlineColor", "#101010", texts.OutlineColorDescription),
            config.Bind(texts.LabelSection, "OutlineWidth", 0.25f, texts.OutlineWidthDescription),
            config.Bind(texts.LabelSection, "ShowItemNames", true, texts.ShowItemNamesDescription),
            config.Bind(texts.LabelSection, "ItemNameLanguage", "Auto", texts.ItemNameLanguageDescription),
            config.Bind(texts.HighlightSection, "EnableScanHighlight", true, texts.HighlightEnabledDescription),
            config.Bind(texts.HighlightSection, "HighlightColor", "#FFD447", texts.HighlightColorDescription),
            config.Bind(texts.HighlightSection, "HighlightAlpha", 0.35f, texts.HighlightAlphaDescription),
            config.Bind(texts.HighlightSection, "HighlightWidth", 0.035f, texts.HighlightWidthDescription),
            config.Bind(texts.HighlightSection, "MaxHighlightedItems", 64, texts.MaxHighlightedItemsDescription),
            config.Bind(texts.HighlightSection, "EnableValueBasedColors", true, texts.ValueBasedColorsEnabledDescription),
            config.Bind(texts.HighlightSection, "LowValueMax", 39, texts.LowValueMaxDescription),
            config.Bind(texts.HighlightSection, "MediumValueMax", 79, texts.MediumValueMaxDescription),
            config.Bind(texts.HighlightSection, "HighValueMax", 119, texts.HighValueMaxDescription),
            config.Bind(texts.HighlightSection, "VeryHighValueMax", 169, texts.VeryHighValueMaxDescription),
            config.Bind(texts.HighlightSection, "UnknownValueColor", "#FFD447", texts.UnknownValueColorDescription),
            config.Bind(texts.HighlightSection, "LowValueColor", "#7EC8FF", texts.LowValueColorDescription),
            config.Bind(texts.HighlightSection, "MediumValueColor", "#FFD447", texts.MediumValueColorDescription),
            config.Bind(texts.HighlightSection, "HighValueColor", "#FF9F1C", texts.HighValueColorDescription),
            config.Bind(texts.HighlightSection, "VeryHighValueColor", "#FF5A36", texts.VeryHighValueColorDescription),
            config.Bind(texts.HighlightSection, "JackpotValueColor", "#FF3FD4", texts.JackpotValueColorDescription),
            config.Bind(texts.DebugSection, "Enabled", false, texts.DebugEnabledDescription),
            config.Bind(texts.DebugSection, "DiagnosticsEnabled", false, texts.DebugDiagnosticsEnabledDescription),
            config.Bind(texts.DebugSection, "ShowHeldItems", false, texts.DebugShowHeldItemsDescription),
            config.Bind(texts.DebugSection, "ShowZeroValueItems", false, texts.DebugShowZeroValueItemsDescription),
            config.Bind(texts.DebugSection, "LogRegistrations", false, texts.DebugLogRegistrationsDescription),
            config.Bind(texts.DebugSection, "ShowCameraTestLabel", false, texts.DebugShowCameraTestLabelDescription),
            config.Bind(texts.DebugSection, "LogIntervalSeconds", 3f, texts.DebugLogIntervalSecondsDescription));
    }

    public void ReloadIfChangedOnDisk(float currentTime)
    {
        if (currentTime < nextConfigFilePollTime || !File.Exists(configFilePath))
        {
            return;
        }

        nextConfigFilePollTime = currentTime + ConfigFilePollInterval;
        var currentWriteTimeUtc = File.GetLastWriteTimeUtc(configFilePath);
        if (currentWriteTimeUtc <= lastConfigWriteTimeUtc)
        {
            return;
        }

        configFile.Reload();
        RefreshLastWriteTime();
    }

    private void Reload()
    {
        Current = new ModSettings(
            ScrapVisibilityOptions.Create(
                enabled.Value,
                revealRadius.Value,
                updateIntervalSeconds.Value,
                maxVisibleLabels.Value,
                activationMode.Value,
                scanRevealDurationSeconds.Value),
            ClampFinite(heightOffset.Value, 0.1f, 4f),
            ClampFinite(worldScale.Value, 0.03f, 1f),
            ClampFinite(fontSize.Value, 0.5f, 12f),
            ParseColor(labelColor.Value, new Color32(255, 212, 71, 255)),
            ParseColor(outlineColor.Value, new Color32(16, 16, 16, 255)),
            ClampFinite(outlineWidth.Value, 0f, 0.5f),
            ParseColor(highlightColor.Value, new Color32(255, 212, 71, 255)),
            ScrapHighlightOptions.Create(
                highlightEnabled.Value,
                highlightAlpha.Value,
                highlightWidth.Value,
                maxHighlightedItems.Value),
            ScrapValueColorOptions.Create(
                valueBasedColorsEnabled.Value,
                lowValueMax.Value,
                mediumValueMax.Value,
                highValueMax.Value,
                veryHighValueMax.Value),
            ParseColor(unknownValueColor.Value, new Color32(255, 212, 71, 255)),
            ParseColor(lowValueColor.Value, new Color32(126, 200, 255, 255)),
            ParseColor(mediumValueColor.Value, new Color32(255, 212, 71, 255)),
            ParseColor(highValueColor.Value, new Color32(255, 159, 28, 255)),
            ParseColor(veryHighValueColor.Value, new Color32(255, 90, 54, 255)),
            ParseColor(jackpotValueColor.Value, new Color32(255, 63, 212, 255)),
            ScrapItemNameOptions.Create(
                showItemNames.Value,
                itemNameLanguage.Value),
            ScrapScanPerformanceOptions.Create(optimizeVanillaScan.Value),
            ScrapDebugOptions.Create(
                debugEnabled.Value,
                debugDiagnosticsEnabled.Value,
                debugShowHeldItems.Value,
                debugShowZeroValueItems.Value,
                debugLogRegistrations.Value,
                debugShowCameraTestLabel.Value,
                debugLogIntervalSeconds.Value));
        SettingsVersion++;
    }

    private void SubscribeChanges()
    {
        configFile.SettingChanged += HandleSettingChanged;
        configFile.ConfigReloaded += HandleConfigReloaded;
    }

    private void HandleSettingChanged(object sender, SettingChangedEventArgs args)
    {
        Reload();
        RefreshLastWriteTime();
    }

    private void HandleConfigReloaded(object sender, EventArgs args)
    {
        Reload();
        RefreshLastWriteTime();
    }

    private void RefreshLastWriteTime()
    {
        if (File.Exists(configFilePath))
        {
            lastConfigWriteTimeUtc = File.GetLastWriteTimeUtc(configFilePath);
        }
    }

    private static float ClampFinite(float value, float min, float max)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            return min;
        }

        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    private static Color ParseColor(string value, Color fallback)
    {
        if (!string.IsNullOrWhiteSpace(value) && ColorUtility.TryParseHtmlString(value, out var color))
        {
            return color;
        }

        return fallback;
    }
}
