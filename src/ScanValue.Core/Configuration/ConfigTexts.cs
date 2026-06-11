namespace Auuueser.ScanValue.Core.Configuration;

public sealed class ConfigTexts
{
    public ConfigTexts(
        string generalSection,
        string visibilitySection,
        string performanceSection,
        string labelSection,
        string highlightSection,
        string debugSection,
        string enabledDescription,
        string revealRadiusDescription,
        string activationModeDescription,
        string scanRevealDurationSecondsDescription,
        string updateIntervalSecondsDescription,
        string maxVisibleLabelsDescription,
        string heightOffsetDescription,
        string worldScaleDescription,
        string fontSizeDescription,
        string labelColorDescription,
        string outlineColorDescription,
        string outlineWidthDescription,
        string highlightEnabledDescription,
        string highlightColorDescription,
        string highlightAlphaDescription,
        string highlightWidthDescription,
        string maxHighlightedItemsDescription,
        string debugEnabledDescription,
        string debugDiagnosticsEnabledDescription,
        string debugShowHeldItemsDescription,
        string debugShowZeroValueItemsDescription,
        string debugLogRegistrationsDescription,
        string debugShowCameraTestLabelDescription,
        string debugLogIntervalSecondsDescription,
        string showItemNamesDescription = "Show the item name above the scrap value label. Applied immediately.",
        string itemNameLanguageDescription = "Item-name language: Auto, Chinese, or English. Applied immediately.",
        string valueBasedColorsEnabledDescription = "Color scrap labels and scan outlines by the scanned scrap value. Applied immediately.",
        string lowValueMaxDescription = "Maximum value for the low-value color tier. Applied immediately.",
        string mediumValueMaxDescription = "Maximum value for the medium-value color tier. Applied immediately.",
        string highValueMaxDescription = "Maximum value for the high-value color tier. Applied immediately.",
        string veryHighValueMaxDescription = "Maximum value for the very-high-value color tier. Values above this use the jackpot color. Applied immediately.",
        string unknownValueColorDescription = "HTML color for unknown scan values such as docked Apparatus ???. Applied immediately.",
        string lowValueColorDescription = "HTML color for low-value scrap labels and scan outlines. Applied immediately.",
        string mediumValueColorDescription = "HTML color for medium-value scrap labels and scan outlines. Applied immediately.",
        string highValueColorDescription = "HTML color for high-value scrap labels and scan outlines. Applied immediately.",
        string veryHighValueColorDescription = "HTML color for very-high-value scrap labels and scan outlines. Applied immediately.",
        string jackpotValueColorDescription = "HTML color for jackpot-value scrap labels and scan outlines. Applied immediately.",
        string optimizeVanillaScanDescription = "Optimize the vanilla right-click scan by caching scan UI components and avoiding repeated text rebuilds. Applied immediately.")
    {
        GeneralSection = generalSection;
        VisibilitySection = visibilitySection;
        PerformanceSection = performanceSection;
        LabelSection = labelSection;
        HighlightSection = highlightSection;
        DebugSection = debugSection;
        EnabledDescription = enabledDescription;
        RevealRadiusDescription = revealRadiusDescription;
        ActivationModeDescription = activationModeDescription;
        ScanRevealDurationSecondsDescription = scanRevealDurationSecondsDescription;
        UpdateIntervalSecondsDescription = updateIntervalSecondsDescription;
        MaxVisibleLabelsDescription = maxVisibleLabelsDescription;
        HeightOffsetDescription = heightOffsetDescription;
        WorldScaleDescription = worldScaleDescription;
        FontSizeDescription = fontSizeDescription;
        LabelColorDescription = labelColorDescription;
        OutlineColorDescription = outlineColorDescription;
        OutlineWidthDescription = outlineWidthDescription;
        HighlightEnabledDescription = highlightEnabledDescription;
        HighlightColorDescription = highlightColorDescription;
        HighlightAlphaDescription = highlightAlphaDescription;
        HighlightWidthDescription = highlightWidthDescription;
        MaxHighlightedItemsDescription = maxHighlightedItemsDescription;
        ShowItemNamesDescription = showItemNamesDescription;
        ItemNameLanguageDescription = itemNameLanguageDescription;
        ValueBasedColorsEnabledDescription = valueBasedColorsEnabledDescription;
        LowValueMaxDescription = lowValueMaxDescription;
        MediumValueMaxDescription = mediumValueMaxDescription;
        HighValueMaxDescription = highValueMaxDescription;
        VeryHighValueMaxDescription = veryHighValueMaxDescription;
        UnknownValueColorDescription = unknownValueColorDescription;
        LowValueColorDescription = lowValueColorDescription;
        MediumValueColorDescription = mediumValueColorDescription;
        HighValueColorDescription = highValueColorDescription;
        VeryHighValueColorDescription = veryHighValueColorDescription;
        JackpotValueColorDescription = jackpotValueColorDescription;
        OptimizeVanillaScanDescription = optimizeVanillaScanDescription;
        DebugEnabledDescription = debugEnabledDescription;
        DebugDiagnosticsEnabledDescription = debugDiagnosticsEnabledDescription;
        DebugShowHeldItemsDescription = debugShowHeldItemsDescription;
        DebugShowZeroValueItemsDescription = debugShowZeroValueItemsDescription;
        DebugLogRegistrationsDescription = debugLogRegistrationsDescription;
        DebugShowCameraTestLabelDescription = debugShowCameraTestLabelDescription;
        DebugLogIntervalSecondsDescription = debugLogIntervalSecondsDescription;
    }

    public string GeneralSection { get; }

    public string VisibilitySection { get; }

    public string PerformanceSection { get; }

    public string LabelSection { get; }

    public string HighlightSection { get; }

    public string DebugSection { get; }

    public string EnabledDescription { get; }

    public string RevealRadiusDescription { get; }

    public string ActivationModeDescription { get; }

    public string ScanRevealDurationSecondsDescription { get; }

    public string UpdateIntervalSecondsDescription { get; }

    public string MaxVisibleLabelsDescription { get; }

    public string HeightOffsetDescription { get; }

    public string WorldScaleDescription { get; }

    public string FontSizeDescription { get; }

    public string LabelColorDescription { get; }

    public string OutlineColorDescription { get; }

    public string OutlineWidthDescription { get; }

    public string HighlightEnabledDescription { get; }

    public string HighlightColorDescription { get; }

    public string HighlightAlphaDescription { get; }

    public string HighlightWidthDescription { get; }

    public string MaxHighlightedItemsDescription { get; }

    public string ShowItemNamesDescription { get; }

    public string ItemNameLanguageDescription { get; }

    public string ValueBasedColorsEnabledDescription { get; }

    public string LowValueMaxDescription { get; }

    public string MediumValueMaxDescription { get; }

    public string HighValueMaxDescription { get; }

    public string VeryHighValueMaxDescription { get; }

    public string UnknownValueColorDescription { get; }

    public string LowValueColorDescription { get; }

    public string MediumValueColorDescription { get; }

    public string HighValueColorDescription { get; }

    public string VeryHighValueColorDescription { get; }

    public string JackpotValueColorDescription { get; }

    public string OptimizeVanillaScanDescription { get; }

    public string DebugEnabledDescription { get; }

    public string DebugDiagnosticsEnabledDescription { get; }

    public string DebugShowHeldItemsDescription { get; }

    public string DebugShowZeroValueItemsDescription { get; }

    public string DebugLogRegistrationsDescription { get; }

    public string DebugShowCameraTestLabelDescription { get; }

    public string DebugLogIntervalSecondsDescription { get; }
}
