using System;
using System.Collections.Generic;

namespace Auuueser.ScanValue.Core.Localization;

public sealed class ScrapNameTranslationMap
{
    private readonly Dictionary<string, string> englishToChinese = new Dictionary<string, string>(256, StringComparer.Ordinal);
    private readonly Dictionary<string, string> chineseToEnglish = new Dictionary<string, string>(256, StringComparer.Ordinal);

    public int Count => englishToChinese.Count;

    public bool HasEntries => englishToChinese.Count > 0;

    public void Add(string? source, string? target)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
        {
            return;
        }

        var normalizedSource = source.Trim();
        var normalizedTarget = target.Trim();
        if (normalizedSource.Length == 0 || normalizedTarget.Length == 0)
        {
            return;
        }

        if (!englishToChinese.ContainsKey(normalizedSource))
        {
            englishToChinese.Add(normalizedSource, normalizedTarget);
        }

        if (!chineseToEnglish.ContainsKey(normalizedTarget))
        {
            chineseToEnglish.Add(normalizedTarget, normalizedSource);
        }
    }

    public string ToChinese(string currentName)
    {
        return englishToChinese.TryGetValue(currentName, out var translated) ? translated : currentName;
    }

    public string ToEnglish(string currentName)
    {
        return chineseToEnglish.TryGetValue(currentName, out var source) ? source : currentName;
    }
}
