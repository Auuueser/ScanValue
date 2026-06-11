using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace Auuueser.ScanValue.Game;

internal sealed class ScrapObjectPatcher : IDisposable
{
    private readonly Harmony harmony;
    private readonly ScrapObjectRegistry registry;
    private readonly ManualLogSource logger;
    private int patchedCount;
    private bool disposed;

    public ScrapObjectPatcher(
        ScrapObjectRegistry registry,
        ManualLogSource logger,
        Action<GrabbableObject> meshVisibilityReapplier)
    {
        this.registry = registry;
        this.logger = logger;
        harmony = new Harmony(PluginInfo.PluginGuid);
        ScrapObjectHooks.Initialize(registry, logger, meshVisibilityReapplier);

        Patch(
            nameof(GrabbableObject.Start),
            postfixName: nameof(ScrapObjectHooks.AfterGrabbableStart));
        Patch(
            nameof(GrabbableObject.OnDestroy),
            prefixName: nameof(ScrapObjectHooks.BeforeGrabbableDestroy));
        Patch(
            nameof(GrabbableObject.SetScrapValue),
            postfixName: nameof(ScrapObjectHooks.AfterSetScrapValue));
        Patch(
            nameof(GrabbableObject.DestroyObjectInHand),
            postfixName: nameof(ScrapObjectHooks.AfterDestroyObjectInHand));
        Patch(
            nameof(GrabbableObject.EnableItemMeshes),
            postfixName: nameof(ScrapObjectHooks.AfterEnableItemMeshes));

        this.logger.LogInfo($"ScanValue patched {patchedCount} GrabbableObject methods.");
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        harmony.UnpatchSelf();
        ScrapObjectHooks.Clear(registry);
    }

    private void Patch(string originalName, string? prefixName = null, string? postfixName = null)
    {
        var original = AccessTools.DeclaredMethod(typeof(GrabbableObject), originalName);
        if (original == null)
        {
            ScrapObjectHooks.LogPatchFailure(originalName);
            return;
        }

        var prefix = CreateHarmonyMethod(prefixName);
        var postfix = CreateHarmonyMethod(postfixName);
        harmony.Patch(original, prefix, postfix);
        patchedCount++;
    }

    private static HarmonyMethod? CreateHarmonyMethod(string? methodName)
    {
        if (methodName == null)
        {
            return null;
        }

        MethodInfo? patch = AccessTools.DeclaredMethod(typeof(ScrapObjectHooks), methodName);
        return patch == null ? null : new HarmonyMethod(patch);
    }
}
