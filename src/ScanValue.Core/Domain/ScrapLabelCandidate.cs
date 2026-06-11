namespace Auuueser.ScanValue.Core.Domain;

public readonly record struct ScrapLabelCandidate(
    int Id,
    float ScreenX,
    float ScreenY,
    float DistanceToCamera,
    int ScrapValue,
    bool WasVisible);
