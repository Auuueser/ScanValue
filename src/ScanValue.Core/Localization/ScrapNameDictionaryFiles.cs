using System;
using System.IO;

namespace Auuueser.ScanValue.Core.Localization;

public static class ScrapNameDictionaryFiles
{
    public static bool IsActiveDictionaryFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var fileName = Path.GetFileName(path);
        if (fileName.EndsWith(".old", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(fileName, "zh-CN.runtime.json", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(Path.GetExtension(fileName), ".cfg", StringComparison.OrdinalIgnoreCase);
    }
}
