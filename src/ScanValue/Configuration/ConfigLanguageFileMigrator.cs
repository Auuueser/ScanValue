using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Auuueser.ScanValue.Core.Configuration;
using BepInEx.Logging;

namespace Auuueser.ScanValue.Configuration;

internal static class ConfigLanguageFileMigrator
{
    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static readonly SectionMap[] Sections =
    {
        new("General", "通用", new[] { "Enabled" }),
        new("Visibility", "可见性", new[] { "RevealRadius", "ActivationMode", "ScanRevealDurationSeconds", "MaxVisibleLabels" }),
        new("Performance", "性能", new[] { "UpdateIntervalSeconds", "OptimizeVanillaScan" }),
        new("Label", "标签", new[] { "HeightOffset", "WorldScale", "FontSize", "LabelColor", "OutlineColor", "OutlineWidth", "ShowItemNames", "ItemNameLanguage" }),
        new("Highlight", "高光", new[] { "EnableScanHighlight", "HighlightColor", "HighlightAlpha", "HighlightWidth", "MaxHighlightedItems" }),
        new("Debug", "调试", new[] { "Enabled", "DiagnosticsEnabled", "ShowHeldItems", "ShowZeroValueItems", "LogRegistrations", "ShowCameraTestLabel", "LogIntervalSeconds" }),
    };

    public static bool Normalize(string configFilePath, ConfigLanguage language, ManualLogSource logger)
    {
        if (string.IsNullOrEmpty(configFilePath) || !File.Exists(configFilePath))
        {
            return false;
        }

        string text;
        try
        {
            text = File.ReadAllText(configFilePath, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Could not inspect ScanValue config language: {ex.Message}");
            return false;
        }

        if (!ShouldNormalize(text, language))
        {
            return false;
        }

        var values = CaptureValues(text, language);
        if (values.Count == 0)
        {
            return false;
        }

        var normalized = BuildTargetLanguageConfig(values, language);
        try
        {
            File.WriteAllText(configFilePath, normalized, Utf8NoBom);
            logger.LogInfo($"ScanValue config language normalized to {language}; known values were preserved.");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Could not normalize ScanValue config language: {ex.Message}");
            return false;
        }
    }

    private static bool ShouldNormalize(string text, ConfigLanguage language)
    {
        for (var index = 0; index < Sections.Length; index++)
        {
            var section = Sections[index];
            var oppositeHeader = language == ConfigLanguage.Chinese
                ? section.EnglishHeader
                : section.ChineseHeader;
            if (ContainsHeader(text, oppositeHeader))
            {
                return true;
            }
        }

        return false;
    }

    private static Dictionary<ValueId, CapturedValue> CaptureValues(string text, ConfigLanguage language)
    {
        var values = new Dictionary<ValueId, CapturedValue>(64);
        SectionMap? currentSection = null;
        var currentSectionIndex = -1;
        var currentPriority = 0;
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var rawLine = lines[lineIndex];
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var sectionIndex = FindSectionIndex(line);
            if (sectionIndex >= 0)
            {
                currentSectionIndex = sectionIndex;
                currentSection = Sections[sectionIndex];
                currentPriority = currentSection.IsHeaderLanguage(line, language) ? 1 : 0;
                continue;
            }

            if (currentSection == null)
            {
                continue;
            }

            var equalsIndex = rawLine.IndexOf('=');
            if (equalsIndex <= 0)
            {
                continue;
            }

            var key = rawLine.Substring(0, equalsIndex).Trim();
            if (!currentSection.ContainsKey(key))
            {
                continue;
            }

            var value = rawLine.Substring(equalsIndex + 1).Trim();
            var id = new ValueId(currentSectionIndex, key);
            if (!values.TryGetValue(id, out var existing) || currentPriority >= existing.Priority)
            {
                values[id] = new CapturedValue(value, currentPriority);
            }
        }

        return values;
    }

    private static string BuildTargetLanguageConfig(Dictionary<ValueId, CapturedValue> values, ConfigLanguage language)
    {
        var builder = new StringBuilder(1024);
        for (var sectionIndex = 0; sectionIndex < Sections.Length; sectionIndex++)
        {
            var section = Sections[sectionIndex];
            builder.AppendLine(section.GetHeader(language));
            for (var keyIndex = 0; keyIndex < section.Keys.Length; keyIndex++)
            {
                var key = section.Keys[keyIndex];
                if (values.TryGetValue(new ValueId(sectionIndex, key), out var captured))
                {
                    builder.Append(key).Append(" = ").AppendLine(captured.Value);
                }
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static bool ContainsHeader(string text, string header)
    {
        return text.IndexOf("[" + header + "]", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static int FindSectionIndex(string line)
    {
        for (var index = 0; index < Sections.Length; index++)
        {
            if (Sections[index].MatchesHeader(line))
            {
                return index;
            }
        }

        return -1;
    }

    private readonly struct ValueId : IEquatable<ValueId>
    {
        private readonly int sectionIndex;
        private readonly string key;

        public ValueId(int sectionIndex, string key)
        {
            this.sectionIndex = sectionIndex;
            this.key = key;
        }

        public bool Equals(ValueId other)
        {
            return sectionIndex == other.sectionIndex && string.Equals(key, other.key, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is ValueId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (sectionIndex * 397) ^ StringComparer.Ordinal.GetHashCode(key);
            }
        }
    }

    private readonly struct CapturedValue
    {
        public CapturedValue(string value, int priority)
        {
            Value = value;
            Priority = priority;
        }

        public string Value { get; }

        public int Priority { get; }
    }

    private sealed class SectionMap
    {
        public SectionMap(string englishName, string chineseName, string[] keys)
        {
            EnglishHeader = "[" + englishName + "]";
            ChineseHeader = "[" + chineseName + "]";
            Keys = keys;
        }

        public string EnglishHeader { get; }

        public string ChineseHeader { get; }

        public string[] Keys { get; }

        public string GetHeader(ConfigLanguage language)
        {
            return language == ConfigLanguage.Chinese ? ChineseHeader : EnglishHeader;
        }

        public bool IsHeaderLanguage(string line, ConfigLanguage language)
        {
            return language == ConfigLanguage.Chinese
                ? string.Equals(line, ChineseHeader, StringComparison.OrdinalIgnoreCase)
                : string.Equals(line, EnglishHeader, StringComparison.OrdinalIgnoreCase);
        }

        public bool MatchesHeader(string line)
        {
            return string.Equals(line, EnglishHeader, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(line, ChineseHeader, StringComparison.OrdinalIgnoreCase);
        }

        public bool ContainsKey(string key)
        {
            for (var index = 0; index < Keys.Length; index++)
            {
                if (string.Equals(Keys[index], key, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
