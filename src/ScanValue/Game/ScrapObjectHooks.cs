using System;
using BepInEx.Logging;

namespace Auuueser.ScanValue.Game;

internal static class ScrapObjectHooks
{
    private static ScrapObjectRegistry? registry;
    private static ManualLogSource? logger;
    private static Action<GrabbableObject>? meshVisibilityReapplier;

    public static void Initialize(
        ScrapObjectRegistry activeRegistry,
        ManualLogSource activeLogger,
        Action<GrabbableObject> activeMeshVisibilityReapplier)
    {
        registry = activeRegistry;
        logger = activeLogger;
        meshVisibilityReapplier = activeMeshVisibilityReapplier;
    }

    public static void Clear(ScrapObjectRegistry activeRegistry)
    {
        if (registry == activeRegistry)
        {
            registry = null;
            logger = null;
            meshVisibilityReapplier = null;
        }
    }

    public static void AfterGrabbableStart(GrabbableObject __instance)
    {
        registry?.Register(__instance);
    }

    public static void BeforeGrabbableDestroy(GrabbableObject __instance)
    {
        registry?.Unregister(__instance);
    }

    public static void AfterSetScrapValue(GrabbableObject __instance)
    {
        registry?.RefreshValue(__instance);
    }

    public static void AfterDestroyObjectInHand(GrabbableObject __instance)
    {
        registry?.Unregister(__instance);
    }

    public static void AfterEnableItemMeshes(GrabbableObject __instance)
    {
        meshVisibilityReapplier?.Invoke(__instance);
    }

    public static void LogPatchFailure(string methodName)
    {
        logger?.LogWarning($"Could not patch GrabbableObject.{methodName}; scrap value labels may miss late-spawned items.");
    }
}
