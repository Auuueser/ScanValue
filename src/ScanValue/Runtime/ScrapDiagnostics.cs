using Auuueser.ScanValue.Core.Domain;
using BepInEx.Logging;
using UnityEngine;

namespace Auuueser.ScanValue.Runtime;

internal sealed class ScrapDiagnostics
{
    private readonly ManualLogSource logger;
    private float nextLogTime;

    public ScrapDiagnostics(ManualLogSource logger)
    {
        this.logger = logger;
    }

    public void LogNoCameraIfDue(ScrapDebugOptions debug, int registered, string reason)
    {
        if (!ShouldLog(debug))
        {
            return;
        }

        logger.LogInfo($"ScanValue diagnostics: camera=none registered={registered} reason={reason}");
    }

    public void LogScanIfDue(ScrapDebugOptions debug, Camera camera, ScrapDiagnosticCounters counters)
    {
        if (!ShouldLog(debug))
        {
            return;
        }

        logger.LogInfo(
            $"ScanValue diagnostics: camera={camera.name} registered={counters.Registered} alive={counters.Alive} candidates={counters.Candidates} shown={counters.Shown} " +
            $"hiddenNoValue={counters.HiddenNoValue} hiddenState={counters.HiddenState} hiddenDistance={counters.HiddenDistance} hiddenBudget={counters.HiddenBudget} hiddenOverlap={counters.HiddenOverlap} " +
            $"poolActive={counters.PoolActive} poolIdle={counters.PoolIdle} testLabel={counters.TestLabel}");
    }

    private bool ShouldLog(ScrapDebugOptions debug)
    {
        if (!debug.ShouldLogDiagnostics || Time.realtimeSinceStartup < nextLogTime)
        {
            return false;
        }

        nextLogTime = Time.realtimeSinceStartup + debug.LogIntervalSeconds;
        return true;
    }
}

internal struct ScrapDiagnosticCounters
{
    public int Registered;
    public int Alive;
    public int Candidates;
    public int Shown;
    public int HiddenNoValue;
    public int HiddenState;
    public int HiddenDistance;
    public int HiddenBudget;
    public int HiddenOverlap;
    public int PoolActive;
    public int PoolIdle;
    public bool TestLabel;
}
