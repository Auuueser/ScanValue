using System;
using Auuueser.ScanValue.Core.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Auuueser.ScanValue.Presentation;

internal sealed class ScrapPriceView
{
    private const float PriceOnlyWidth = 240f;
    private const float PriceOnlyHeight = 72f;
    private const float NamedWidth = 300f;
    private const float NamedHeight = 112f;
    private const float WorldScaleMultiplier = 0.02f;
    private const float FontSizeMultiplier = 12f;
    private const float NameFontSizeMultiplier = 4.8f;
    private static readonly Vector3 ParkedWorldPosition = new Vector3(0f, -10000f, 0f);
    private static TMP_FontAsset? resolvedDefaultTextFont;
    private static TMP_FontAsset? resolvedChineseFallbackFont;
    private static bool defaultTextFontResolved;
    private static bool chineseFallbackFontResolved;

    private readonly GameObject gameObject;
    private readonly RectTransform root;
    private readonly RectTransform nameRect;
    private readonly RectTransform valueRect;
    private readonly TextMeshProUGUI nameText;
    private readonly TextMeshProUGUI valueText;
    private readonly TextMeshProUGUI anchorMarker;
    private readonly Canvas worldCanvas;
    private readonly ScrapPriceBillboard billboard;
    private Camera? lastAppliedCanvasCamera;
    private string? lastNameText;
    private string? lastValueText;
    private int lastValueNumber;
    private bool lastValueWasNumeric;
    private bool lastHasName;
    private bool layoutApplied;
    private Color lastValueColor;
    private bool valueColorApplied;
    private bool visible;
    private ScrapPriceStyle currentStyle = null!;

    private ScrapPriceView(
        GameObject gameObject,
        RectTransform root,
        RectTransform nameRect,
        RectTransform valueRect,
        TextMeshProUGUI nameText,
        TextMeshProUGUI valueText,
        TextMeshProUGUI anchorMarker,
        Canvas worldCanvas,
        ScrapPriceBillboard billboard)
    {
        this.gameObject = gameObject;
        this.root = root;
        this.nameRect = nameRect;
        this.valueRect = valueRect;
        this.nameText = nameText;
        this.valueText = valueText;
        this.anchorMarker = anchorMarker;
        this.worldCanvas = worldCanvas;
        this.billboard = billboard;
    }

    public int FrameTouched { get; set; }

    public static ScrapPriceView Create(Transform parent, ScrapPriceStyle style)
    {
        var gameObject = new GameObject("ScrapValueLabel");
        gameObject.transform.SetParent(parent, false);

        var billboard = gameObject.AddComponent<ScrapPriceBillboard>();
        var root = gameObject.AddComponent<RectTransform>();

        var worldCanvas = gameObject.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.overrideSorting = true;
        worldCanvas.sortingOrder = 600;
        gameObject.AddComponent<CanvasScaler>();

        var nameText = CreateText(root, "NameText", useChineseFallback: true);
        var valueText = CreateText(root, "PriceText", useChineseFallback: false);
        var anchorMarker = CreateAnchorMarker(root);
        SetLayerRecursively(gameObject, parent.gameObject.layer);

        var view = new ScrapPriceView(
            gameObject,
            root,
            (RectTransform)nameText.transform,
            (RectTransform)valueText.transform,
            nameText,
            valueText,
            anchorMarker,
            worldCanvas,
            billboard);
        view.ApplyStyle(style);
        view.SetVisible(false);
        return view;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string objectName, bool useChineseFallback)
    {
        var textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        var rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        textObject.AddComponent<CanvasRenderer>();

        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.richText = false;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.enableAutoSizing = true;
        text.fontStyle = FontStyles.Bold;

        ApplyTextFont(text, useChineseFallback);

        return text;
    }

    private static TextMeshProUGUI CreateAnchorMarker(Transform parent)
    {
        var markerObject = new GameObject("AnchorMarker");
        markerObject.transform.SetParent(parent, false);

        var rectTransform = markerObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.sizeDelta = new Vector2(28f, 18f);
        rectTransform.anchoredPosition = new Vector2(0f, -4f);

        markerObject.AddComponent<CanvasRenderer>();

        var marker = markerObject.AddComponent<TextMeshProUGUI>();
        marker.name = "AnchorMarker";
        marker.alignment = TextAlignmentOptions.Center;
        marker.enableWordWrapping = false;
        marker.richText = false;
        marker.raycastTarget = false;
        marker.text = "v";
        ApplyTextFont(marker, useChineseFallback: false);

        return marker;
    }

    public void ApplyStyle(ScrapPriceStyle style)
    {
        currentStyle = style;
        root.localScale = Vector3.one * (style.WorldScale * WorldScaleMultiplier);

        var priceFontSize = style.FontSize * FontSizeMultiplier;
        valueText.fontSize = priceFontSize;
        valueText.fontSizeMin = Mathf.Max(8f, priceFontSize * 0.55f);
        valueText.fontSizeMax = priceFontSize;
        valueText.outlineColor = style.OutlineColor;
        valueText.outlineWidth = style.OutlineWidth;

        var nameFontSize = style.FontSize * NameFontSizeMultiplier;
        nameText.fontSize = nameFontSize;
        nameText.fontSizeMin = Mathf.Max(7f, nameFontSize * 0.55f);
        nameText.fontSizeMax = nameFontSize;
        nameText.outlineColor = style.OutlineColor;
        nameText.outlineWidth = style.OutlineWidth;

        anchorMarker.fontSize = style.FontSize * 5f;
        anchorMarker.outlineColor = style.OutlineColor;
        anchorMarker.outlineWidth = style.OutlineWidth;
        ApplyValueColor(style.LabelColor, force: true);
        ApplyTextLayout(lastHasName);
    }

    public void ApplyValueColor(Color color)
    {
        ApplyValueColor(color, force: false);
    }

    private static void ApplyTextFont(TextMeshProUGUI text, bool useChineseFallback)
    {
        var font = useChineseFallback ? ResolveChineseFallbackFont() ?? ResolveDefaultTextFont() : ResolveDefaultTextFont();
        if (font != null)
        {
            text.font = font;
        }
    }

    private static TMP_FontAsset? ResolveDefaultTextFont()
    {
        if (defaultTextFontResolved)
        {
            return resolvedDefaultTextFont;
        }

        defaultTextFontResolved = true;
        resolvedDefaultTextFont = TMP_Settings.defaultFontAsset;
        if (resolvedDefaultTextFont != null)
        {
            return resolvedDefaultTextFont;
        }

        var builtinFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (builtinFont != null)
        {
            resolvedDefaultTextFont = TMP_FontAsset.CreateFontAsset(builtinFont);
        }

        return resolvedDefaultTextFont;
    }

    private static TMP_FontAsset? ResolveChineseFallbackFont()
    {
        if (chineseFallbackFontResolved)
        {
            return resolvedChineseFallbackFont;
        }

        chineseFallbackFontResolved = true;
        var fallbacks = TMP_Settings.fallbackFontAssets;
        if (fallbacks == null)
        {
            return null;
        }

        for (var index = 0; index < fallbacks.Count; index++)
        {
            var fallback = fallbacks[index];
            var name = fallback != null ? fallback.name : string.Empty;
            if (name.IndexOf("V81TestChn", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("zh-cn", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("tmp-font", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("NotoSansSC", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("SourceHan", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Chinese", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                resolvedChineseFallbackFont = fallback;
                return resolvedChineseFallbackFont;
            }
        }

        return resolvedChineseFallbackFont;
    }

    public void SetValue(int value)
    {
        SetNameAndValue(null, value);
    }

    public void SetNameAndValue(string? itemName, int value)
    {
        var hasName = !string.IsNullOrWhiteSpace(itemName);
        ApplyTextLayout(hasName);
        SetNameText(hasName ? itemName : null);
        SetValueText(value);
    }

    public void SetNameAndValue(string? itemName, string value)
    {
        var hasName = !string.IsNullOrWhiteSpace(itemName);
        ApplyTextLayout(hasName);
        SetNameText(hasName ? itemName : null);
        lastValueWasNumeric = false;
        SetValueText(value);
    }

    public void SetText(string value)
    {
        ApplyTextLayout(false);
        SetNameText(null);
        lastValueWasNumeric = false;
        SetValueText(value);
    }

    public void SetWorldPosition(Vector3 worldPosition, Camera camera)
    {
        root.position = worldPosition;
        ApplyCanvasCamera(camera);
    }

    public void SetVisible(bool visible)
    {
        if (this.visible == visible && gameObject.activeSelf && (visible || root.position == ParkedWorldPosition))
        {
            return;
        }

        this.visible = visible;
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        billboard.enabled = visible;
        if (!visible)
        {
            billboard.SetCamera(null);
            root.position = ParkedWorldPosition;
        }
    }

    private void ApplyTextLayout(bool hasName)
    {
        if (currentStyle == null)
        {
            return;
        }

        if (layoutApplied && lastHasName == hasName)
        {
            return;
        }

        layoutApplied = true;
        lastHasName = hasName;
        root.sizeDelta = hasName
            ? new Vector2(NamedWidth, NamedHeight)
            : new Vector2(PriceOnlyWidth, PriceOnlyHeight);

        nameText.gameObject.SetActive(hasName);
        if (hasName)
        {
            nameRect.anchorMin = new Vector2(0f, 0.56f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            valueRect.anchorMin = new Vector2(0f, 0.04f);
            valueRect.anchorMax = new Vector2(1f, 0.7f);
        }
        else
        {
            valueRect.anchorMin = Vector2.zero;
            valueRect.anchorMax = Vector2.one;
        }

        valueRect.offsetMin = Vector2.zero;
        valueRect.offsetMax = Vector2.zero;
    }

    private void SetNameText(string? value)
    {
        if (lastNameText == value)
        {
            return;
        }

        lastNameText = value;
        nameText.text = value ?? string.Empty;
    }

    private void SetValueText(int value)
    {
        if (lastValueWasNumeric && lastValueNumber == value)
        {
            return;
        }

        lastValueWasNumeric = true;
        lastValueNumber = value;
        SetValueText(ScrapPriceText.Format(value));
    }

    private void SetValueText(string value)
    {
        if (lastValueText == value)
        {
            return;
        }

        lastValueText = value;
        valueText.text = value;
    }

    private void ApplyValueColor(Color color, bool force)
    {
        if (!force && valueColorApplied && lastValueColor == color)
        {
            return;
        }

        valueColorApplied = true;
        lastValueColor = color;
        valueText.color = color;
        nameText.color = color;
        anchorMarker.color = color;
    }

    private void ApplyCanvasCamera(Camera camera)
    {
        billboard.SetCamera(camera);

        if (lastAppliedCanvasCamera == camera && worldCanvas.worldCamera == camera)
        {
            return;
        }

        worldCanvas.worldCamera = camera;
        lastAppliedCanvasCamera = camera;
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        var targetTransform = target.transform;
        for (var index = 0; index < targetTransform.childCount; index++)
        {
            SetLayerRecursively(targetTransform.GetChild(index).gameObject, layer);
        }
    }
}
