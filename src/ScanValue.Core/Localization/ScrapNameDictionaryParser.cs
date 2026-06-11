using System;
using System.Collections.Generic;
using System.Globalization;

namespace Auuueser.ScanValue.Core.Localization;

public static class ScrapNameDictionaryParser
{
    public static ScrapNameTranslationMap ParseCfg(string text)
    {
        var map = new ScrapNameTranslationMap();
        AddCfg(map, text);
        return map;
    }

    public static void AddCfg(ScrapNameTranslationMap map, string? text)
    {
        if (map == null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        for (var index = 0; index < lines.Length; index++)
        {
            AddCfgLine(map, lines[index]);
        }
    }

    public static ScrapNameTranslationMap ParseRuntimeJson(string text)
    {
        var map = new ScrapNameTranslationMap();
        AddRuntimeJson(map, text);
        return map;
    }

    public static void AddRuntimeJson(ScrapNameTranslationMap map, string? text)
    {
        if (map == null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        foreach (var jsonObject in EnumerateJsonObjects(text))
        {
            if (!TryGetJsonStringProperty(jsonObject, "source", out var source))
            {
                continue;
            }

            TryGetJsonStringProperty(jsonObject, "target", out var target);
            TryGetJsonStringProperty(jsonObject, "mode", out var mode);

            if (string.Equals(mode, "skip", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mode, "regex-preserve", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var mappedTarget = string.Equals(mode, "preserve", StringComparison.OrdinalIgnoreCase)
                ? source
                : target;
            if (!string.IsNullOrWhiteSpace(mappedTarget))
            {
                map.Add(source, mappedTarget);
            }
        }
    }

    private static void AddCfgLine(ScrapNameTranslationMap map, string rawLine)
    {
        var line = rawLine.Trim();
        if (line.Length == 0 ||
            line.StartsWith("#", StringComparison.Ordinal) ||
            line.StartsWith("//", StringComparison.Ordinal) ||
            line.StartsWith("/*", StringComparison.Ordinal) ||
            IsRegexCfgLine(line))
        {
            return;
        }

        var separator = FindCfgEntrySeparator(line);
        if (separator <= 0)
        {
            return;
        }

        var source = UnescapeCfgValue(line.Substring(0, separator).Trim());
        var target = UnescapeCfgValue(line.Substring(separator + 1).Trim());
        map.Add(source, target);
    }

    private static bool IsRegexCfgLine(string line)
    {
        return line.StartsWith("r:\"", StringComparison.Ordinal) ||
               line.StartsWith("sr:\"", StringComparison.Ordinal) ||
               line.StartsWith("rex:", StringComparison.Ordinal);
    }

    private static int FindCfgEntrySeparator(string line)
    {
        var escaped = false;
        var inRichTextTag = false;
        for (var index = 0; index < line.Length; index++)
        {
            var ch = line[index];
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (ch == '\\')
            {
                escaped = true;
                continue;
            }

            if (ch == '<')
            {
                inRichTextTag = true;
                continue;
            }

            if (inRichTextTag)
            {
                if (ch == '>')
                {
                    inRichTextTag = false;
                }

                continue;
            }

            if (ch == '=')
            {
                return index;
            }
        }

        return -1;
    }

    private static string UnescapeCfgValue(string value)
    {
        return value
            .Replace("\\n", "\n", StringComparison.Ordinal)
            .Replace("\\r", "\r", StringComparison.Ordinal)
            .Replace("\\t", "\t", StringComparison.Ordinal)
            .Replace("\\=", "=", StringComparison.Ordinal)
            .Replace("\\\"", "\"", StringComparison.Ordinal);
    }

    private static IEnumerable<string> EnumerateJsonObjects(string text)
    {
        var depth = 0;
        var starts = new List<int>();
        var inString = false;
        var escaped = false;
        for (var index = 0; index < text.Length; index++)
        {
            var ch = text[index];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (ch == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (ch == '"')
            {
                inString = true;
                continue;
            }

            if (ch == '{')
            {
                starts.Add(index);
                depth++;
                continue;
            }

            if (ch != '}' || depth == 0)
            {
                continue;
            }

            var start = starts[starts.Count - 1];
            starts.RemoveAt(starts.Count - 1);
            depth--;
            if (depth >= 1)
            {
                yield return text.Substring(start, index - start + 1);
            }
        }
    }

    private static bool TryGetJsonStringProperty(string jsonObject, string propertyName, out string value)
    {
        value = string.Empty;
        var search = "\"" + propertyName + "\"";
        var cursor = jsonObject.IndexOf(search, StringComparison.Ordinal);
        if (cursor < 0)
        {
            return false;
        }

        cursor += search.Length;
        while (cursor < jsonObject.Length && char.IsWhiteSpace(jsonObject[cursor]))
        {
            cursor++;
        }

        if (cursor >= jsonObject.Length || jsonObject[cursor] != ':')
        {
            return false;
        }

        cursor++;
        while (cursor < jsonObject.Length && char.IsWhiteSpace(jsonObject[cursor]))
        {
            cursor++;
        }

        if (cursor >= jsonObject.Length || jsonObject[cursor] != '"')
        {
            return false;
        }

        cursor++;
        var output = new List<char>();
        var escaped = false;
        for (; cursor < jsonObject.Length; cursor++)
        {
            var ch = jsonObject[cursor];
            if (escaped)
            {
                if (ch == 'u' && cursor + 4 < jsonObject.Length &&
                    ushort.TryParse(jsonObject.Substring(cursor + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                {
                    output.Add((char)code);
                    cursor += 4;
                }
                else
                {
                    output.Add(ch switch
                    {
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        '"' => '"',
                        '\\' => '\\',
                        '/' => '/',
                        _ => ch,
                    });
                }

                escaped = false;
                continue;
            }

            if (ch == '\\')
            {
                escaped = true;
                continue;
            }

            if (ch == '"')
            {
                value = new string(output.ToArray());
                return true;
            }

            output.Add(ch);
        }

        return false;
    }
}
