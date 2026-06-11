using Auuueser.ScanValue.Configuration;
using Auuueser.ScanValue.Runtime;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace Auuueser.ScanValue;

[BepInPlugin(PluginInfo.PluginGuid, PluginInfo.PluginName, PluginInfo.PluginVersion)]
[BepInProcess("Lethal Company.exe")]
public sealed class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; } = null!;

    private ScanValueRuntime? runtime;
    private bool applicationQuitting;

    private void Awake()
    {
        Log = Logger;

        var language = ChineseProjectLanguageDetector.Detect(Config, Logger);
        if (ConfigLanguageFileMigrator.Normalize(Config.ConfigFilePath, language, Logger))
        {
            Config.Reload();
        }

        var config = ModConfig.Bind(Config, language);
        runtime = ScanValueRuntime.Start(config, Logger);

        Logger.LogInfo($"{PluginInfo.PluginName} {PluginInfo.PluginVersion} loaded.");
    }

    private void OnDestroy()
    {
        if (!applicationQuitting && Application.isPlaying)
        {
            Logger.LogWarning("ScanValue received early OnDestroy while the application is still running; preserving runtime.");
            return;
        }

        DisposeRuntime();
    }

    private void OnApplicationQuit()
    {
        applicationQuitting = true;
        DisposeRuntime();
    }

    private void DisposeRuntime()
    {
        runtime?.Dispose();
        runtime = null;
    }
}
