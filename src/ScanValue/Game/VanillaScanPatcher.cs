using System;
using System.Reflection;
using Auuueser.ScanValue.Runtime;
using BepInEx.Logging;
using HarmonyLib;

namespace Auuueser.ScanValue.Game;

internal sealed class VanillaScanPatcher : IDisposable
{
    private const string StartMethodName = "Start";
    private const string UpdateScanNodesMethodName = "UpdateScanNodes";
    private const string DisableAllScanElementsMethodName = "DisableAllScanElements";

    private readonly Harmony harmony;
    private readonly ScrapVisibilityController controller;
    private bool disposed;

    public VanillaScanPatcher(ScrapVisibilityController controller, ManualLogSource logger)
    {
        this.controller = controller;
        harmony = new Harmony(PluginInfo.PluginGuid + ".scan");
        VanillaScanHooks.Initialize(controller, logger);

        Patch(StartMethodName, postfixName: nameof(VanillaScanHooks.AfterStart));
        PatchUpdateScanNodes();
        Patch(DisableAllScanElementsMethodName, nameof(VanillaScanHooks.AfterDisableAllScanElements));
        logger.LogInfo("ScanValue patched HUDManager scan nodes and vanilla scan performance.");
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        harmony.UnpatchSelf();
        VanillaScanHooks.Clear(controller);
    }

    private static MethodInfo RequiredHook(string methodName)
    {
        return AccessTools.DeclaredMethod(typeof(VanillaScanHooks), methodName) ??
            throw new MissingMethodException(typeof(VanillaScanHooks).FullName, methodName);
    }

    private void Patch(string originalName, string postfixName)
    {
        Patch(originalName, prefix: null, postfixName);
    }

    private void PatchUpdateScanNodes()
    {
        Patch(UpdateScanNodesMethodName,
            postfixName: nameof(VanillaScanHooks.AfterUpdateScanNodes),
            prefix: new HarmonyMethod(RequiredHook(nameof(VanillaScanHooks.BeforeUpdateScanNodes))));
    }

    private void Patch(string originalName, HarmonyMethod? prefix, string postfixName)
    {
        var original = AccessTools.DeclaredMethod(typeof(HUDManager), originalName);
        if (original == null)
        {
            VanillaScanHooks.LogPatchFailure(originalName);
            return;
        }

        var postfix = new HarmonyMethod(RequiredHook(postfixName));
        harmony.Patch(original, prefix: prefix, postfix: postfix);
    }
}
