using System;

namespace Auuueser.ScanValue.Core.Configuration;

public static class ChineseProjectDetection
{
    private const string CurrentKnownGuid = "cn.codex.v81testchn";
    private const string PackageName = "LC_Chinese_Project";
    private const string RepositoryUrl = "github.com/Auuueser/LC-Chinese-Project";

    public static bool IsChineseProjectPlugin(string? pluginGuid, string? pluginName, string? pluginLocation)
    {
        if (EqualsIgnoreCase(pluginGuid, CurrentKnownGuid))
        {
            return true;
        }

        if (ContainsIgnoreCase(pluginName, "LC Chinese Project") ||
            ContainsIgnoreCase(pluginName, "Chinese Project") ||
            ContainsIgnoreCase(pluginName, "V81 TEST CHN"))
        {
            return true;
        }

        return ContainsIgnoreCase(pluginLocation, PackageName) ||
               ContainsIgnoreCase(pluginLocation, "LC-Chinese-Project") ||
               ContainsIgnoreCase(pluginLocation, "LC Chinese Project");
    }

    public static bool ContainsChineseProjectManifestText(string? manifestText)
    {
        return ContainsIgnoreCase(manifestText, PackageName) ||
               ContainsIgnoreCase(manifestText, "LC Chinese Project") ||
               ContainsIgnoreCase(manifestText, "LC-Chinese-Project") ||
               ContainsIgnoreCase(manifestText, RepositoryUrl);
    }

    public static bool ContainsChineseConfigSections(string? configText)
    {
        return ContainsIgnoreCase(configText, "[通用]") ||
               ContainsIgnoreCase(configText, "[可见性]") ||
               ContainsIgnoreCase(configText, "[性能]") ||
               ContainsIgnoreCase(configText, "[标签]") ||
               ContainsIgnoreCase(configText, "[调试]");
    }

    private static bool EqualsIgnoreCase(string? value, string expected)
    {
        return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsIgnoreCase(string? value, string expected)
    {
        return value != null && value.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
