using System;
using System.Collections.Generic;
using System.IO;
using Auuueser.ScanValue.Core.Configuration;
using Auuueser.ScanValue.Core.Localization;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;

namespace Auuueser.ScanValue.Localization;

internal static class ChineseProjectResourceLocator
{
    public static IReadOnlyList<string> FindDictionaryFiles(ManualLogSource logger)
    {
        var result = new List<string>(8);
        foreach (var pluginDirectory in FindChineseProjectPluginDirectories(logger))
        {
            AddIfActive(result, Path.Combine(pluginDirectory, "V81TestChn", "translations-clean", "zh-CN.runtime.json"));
            AddIfActive(result, Path.Combine(pluginDirectory, "translations-clean", "zh-CN.runtime.json"));
            AddCfgDirectory(result, Path.Combine(pluginDirectory, "V81TestChn", "translations-clean", "cfg", "zh-CN"));
            AddCfgDirectory(result, Path.Combine(pluginDirectory, "translations-clean", "cfg", "zh-CN"));
            AddCfgDirectory(result, Path.Combine(pluginDirectory, "V81TestChn", "translations-clean", "split", "cfg", "zh-CN"));
            AddCfgDirectory(result, Path.Combine(pluginDirectory, "translations-clean", "split", "cfg", "zh-CN"));
        }

        return result;
    }

    private static IEnumerable<string> FindChineseProjectPluginDirectories(ManualLogSource logger)
    {
        foreach (var pluginInfoPair in Chainloader.PluginInfos)
        {
            var pluginInfo = pluginInfoPair.Value;
            if (pluginInfo?.Metadata == null ||
                !ChineseProjectDetection.IsChineseProjectPlugin(
                    pluginInfo.Metadata.GUID,
                    pluginInfo.Metadata.Name,
                    pluginInfo.Location))
            {
                continue;
            }

            var pluginDirectory = Path.GetDirectoryName(pluginInfo.Location);
            if (!string.IsNullOrEmpty(pluginDirectory))
            {
                yield return pluginDirectory;
            }
        }

        foreach (var manifestPath in FindChineseProjectManifestPaths(logger))
        {
            var directory = Path.GetDirectoryName(manifestPath);
            if (!string.IsNullOrEmpty(directory))
            {
                yield return directory;
            }
        }
    }

    private static IEnumerable<string> FindChineseProjectManifestPaths(ManualLogSource logger)
    {
        if (!Directory.Exists(Paths.PluginPath))
        {
            yield break;
        }

        IEnumerable<string> manifests;
        try
        {
            manifests = Directory.EnumerateFiles(Paths.PluginPath, "manifest.json", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Could not inspect plugin manifests for item-name dictionaries: {ex.Message}");
            yield break;
        }

        foreach (var manifestPath in manifests)
        {
            string manifestText;
            try
            {
                manifestText = File.ReadAllText(manifestPath);
            }
            catch
            {
                continue;
            }

            if (ChineseProjectDetection.ContainsChineseProjectManifestText(manifestText))
            {
                yield return manifestPath;
            }
        }
    }

    private static void AddCfgDirectory(List<string> result, string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var path in Directory.EnumerateFiles(directory, "*.cfg", SearchOption.TopDirectoryOnly))
        {
            AddIfActive(result, path);
        }
    }

    private static void AddIfActive(List<string> result, string path)
    {
        if (!File.Exists(path) || !ScrapNameDictionaryFiles.IsActiveDictionaryFile(path))
        {
            return;
        }

        if (!result.Contains(path))
        {
            result.Add(path);
        }
    }
}
