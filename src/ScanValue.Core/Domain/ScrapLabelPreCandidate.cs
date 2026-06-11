namespace Auuueser.ScanValue.Core.Domain;

public readonly record struct ScrapLabelPreCandidate(
    int Id,
    float DistanceSquaredToPlayer,
    int ScrapValue,
    bool WasVisible);
