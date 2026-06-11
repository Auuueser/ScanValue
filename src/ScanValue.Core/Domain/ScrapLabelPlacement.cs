using System;

namespace Auuueser.ScanValue.Core.Domain;

public readonly record struct ScrapLabelPlacement(
    int Id,
    bool IsVisible,
    float ScreenX,
    float ScreenY,
    float Width,
    float Height,
    int SlotIndex)
{
    public bool Overlaps(ScrapLabelPlacement other, float minGap)
    {
        if (!IsVisible || !other.IsVisible)
        {
            return false;
        }

        var halfWidth = Width * 0.5f;
        var halfHeight = Height * 0.5f;
        var otherHalfWidth = other.Width * 0.5f;
        var otherHalfHeight = other.Height * 0.5f;

        return MathF.Abs(ScreenX - other.ScreenX) < halfWidth + otherHalfWidth + minGap &&
               MathF.Abs(ScreenY - other.ScreenY) < halfHeight + otherHalfHeight + minGap;
    }
}
