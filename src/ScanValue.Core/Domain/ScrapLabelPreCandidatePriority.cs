using System;

namespace Auuueser.ScanValue.Core.Domain;

public static class ScrapLabelPreCandidatePriority
{
    private const float DistanceSquaredPriorityBucketSize = 0.25f;

    public static bool IsHigherPriority(ScrapLabelPreCandidate left, ScrapLabelPreCandidate right) =>
        Compare(left, right) < 0;

    public static int Compare(ScrapLabelPreCandidate left, ScrapLabelPreCandidate right)
    {
        var distance = GetDistancePriorityBucket(left.DistanceSquaredToPlayer)
            .CompareTo(GetDistancePriorityBucket(right.DistanceSquaredToPlayer));
        if (distance != 0)
        {
            return distance;
        }

        var value = right.ScrapValue.CompareTo(left.ScrapValue);
        if (value != 0)
        {
            return value;
        }

        var visible = right.WasVisible.CompareTo(left.WasVisible);
        if (visible != 0)
        {
            return visible;
        }

        return left.Id.CompareTo(right.Id);
    }

    private static int GetDistancePriorityBucket(float distanceSquared)
    {
        if (float.IsNaN(distanceSquared) || float.IsInfinity(distanceSquared))
        {
            return int.MaxValue;
        }

        return (int)MathF.Floor(distanceSquared / DistanceSquaredPriorityBucketSize);
    }
}
