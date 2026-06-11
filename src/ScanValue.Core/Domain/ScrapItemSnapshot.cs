namespace Auuueser.ScanValue.Core.Domain;

public readonly record struct ScrapItemSnapshot(
    int ScrapValue,
    float DistanceSquaredToPlayer,
    bool IsHeld,
    bool IsHeldByEnemy,
    bool IsDeactivated,
    bool IsPocketed,
    bool IsUsedUp);
