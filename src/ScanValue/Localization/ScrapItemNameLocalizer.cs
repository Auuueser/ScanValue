using System;
using System.IO;
using Auuueser.ScanValue.Configuration;
using Auuueser.ScanValue.Core.Domain;
using Auuueser.ScanValue.Core.Localization;
using Auuueser.ScanValue.Game;
using BepInEx.Logging;

namespace Auuueser.ScanValue.Localization;

internal sealed class ScrapItemNameLocalizer
{
    private readonly ManualLogSource logger;
    private readonly ScrapNameTranslationMap translations = new ScrapNameTranslationMap();
    private readonly bool chineseProjectDetected;

    private ScrapItemNameLocalizer(ManualLogSource logger, bool chineseProjectDetected)
    {
        this.logger = logger;
        this.chineseProjectDetected = chineseProjectDetected;
    }

    public bool HasChineseDictionary => translations.HasEntries;

    public static ScrapItemNameLocalizer Load(ManualLogSource logger)
    {
        var files = ChineseProjectResourceLocator.FindDictionaryFiles(logger);
        var localizer = new ScrapItemNameLocalizer(logger, files.Count > 0);
        for (var index = 0; index < files.Count; index++)
        {
            localizer.LoadFile(files[index]);
        }

        if (localizer.translations.HasEntries)
        {
            logger.LogInfo($"ScanValue loaded {localizer.translations.Count} item-name translations from {files.Count} active LC Chinese Project resource(s).");
        }

        return localizer;
    }

    public ScrapItemResolvedNames ResolveNames(string currentItemName)
    {
        if (string.IsNullOrWhiteSpace(currentItemName))
        {
            return new ScrapItemResolvedNames(string.Empty, string.Empty);
        }

        var englishName = translations.ToEnglish(currentItemName);
        var chineseName = translations.ToChinese(englishName);
        if (string.Equals(chineseName, englishName, StringComparison.Ordinal))
        {
            chineseName = translations.ToChinese(currentItemName);
        }

        return new ScrapItemResolvedNames(englishName, chineseName);
    }

    public string? ResolveDisplayName(TrackedScrapItem item, ModSettings settings)
    {
        if (!settings.ItemNames.ShowItemNames)
        {
            return null;
        }

        return settings.ItemNames.Language switch
        {
            ScrapItemNameLanguage.Chinese => NonEmptyOrFallback(item.ChineseName, item.EnglishName),
            ScrapItemNameLanguage.English => NonEmptyOrFallback(item.EnglishName, item.ChineseName),
            _ => ShouldUseChinese()
                ? NonEmptyOrFallback(item.ChineseName, item.EnglishName)
                : NonEmptyOrFallback(item.EnglishName, item.ChineseName),
        };
    }

    private bool ShouldUseChinese()
    {
        return chineseProjectDetected && translations.HasEntries;
    }

    private void LoadFile(string path)
    {
        try
        {
            var text = File.ReadAllText(path);
            if (string.Equals(Path.GetFileName(path), "zh-CN.runtime.json", StringComparison.OrdinalIgnoreCase))
            {
                ScrapNameDictionaryParser.AddRuntimeJson(translations, text);
            }
            else
            {
                ScrapNameDictionaryParser.AddCfg(translations, text);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning($"ScanValue could not load item-name dictionary '{path}': {ex.Message}");
        }
    }

    private static string? NonEmptyOrFallback(string primary, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(primary))
        {
            return primary;
        }

        return string.IsNullOrWhiteSpace(fallback) ? null : fallback;
    }
}
