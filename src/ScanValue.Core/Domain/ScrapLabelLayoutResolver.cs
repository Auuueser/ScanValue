using System;
using System.Collections.Generic;

namespace Auuueser.ScanValue.Core.Domain;

public sealed class ScrapLabelLayoutResolver
{
    private const float DistancePriorityBucketSize = 0.25f;

    private static readonly (float X, float Y)[] OffsetSlots =
    {
        (0f, 0f),
        (-0.65f, 0.65f),
        (0.65f, 0.65f),
        (0f, 1.25f),
        (-1.05f, 0f),
        (1.05f, 0f),
        (0f, 1.9f),
    };

    private ScrapLabelLayoutOptions options;
    private readonly List<ScrapLabelPlacement> accepted = new();
    private readonly IndexPriorityComparer indexPriorityComparer = new();
    private int[] sortedIndexes = Array.Empty<int>();
    private int[] distanceBuckets = Array.Empty<int>();

    public ScrapLabelLayoutResolver(ScrapLabelLayoutOptions options)
    {
        this.options = options;
    }

    public void UpdateOptions(ScrapLabelLayoutOptions options)
    {
        this.options = options;
    }

    public ScrapLabelPlacement[] Resolve(IReadOnlyList<ScrapLabelCandidate> candidates, int maxVisibleLabels)
    {
        if (candidates == null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        var results = new List<ScrapLabelPlacement>(candidates.Count);
        ResolveInto(candidates, maxVisibleLabels, results);
        return results.ToArray();
    }

    public void ResolveInto(IReadOnlyList<ScrapLabelCandidate> candidates, int maxVisibleLabels, List<ScrapLabelPlacement> results)
    {
        if (candidates == null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        if (results == null)
        {
            throw new ArgumentNullException(nameof(results));
        }

        accepted.Clear();
        results.Clear();
        EnsureSortBufferCapacity(candidates.Count);
        if (results.Capacity < candidates.Count)
        {
            results.Capacity = candidates.Count;
        }

        for (var index = 0; index < candidates.Count; index++)
        {
            results.Add(default);
            sortedIndexes[index] = index;
            distanceBuckets[index] = GetDistancePriorityBucket(candidates[index].DistanceToCamera);
        }

        indexPriorityComparer.Reset(candidates, distanceBuckets);
        try
        {
            Array.Sort(sortedIndexes, 0, candidates.Count, indexPriorityComparer);
            var visibleCount = 0;

            for (var order = 0; order < candidates.Count; order++)
            {
                var sourceIndex = sortedIndexes[order];
                var candidate = candidates[sourceIndex];
                if (visibleCount >= maxVisibleLabels)
                {
                    results[sourceIndex] = Hidden(candidate);
                    continue;
                }

                if (!options.Enabled)
                {
                    var placement = Visible(candidate, candidate.ScreenX, candidate.ScreenY, slotIndex: 0);
                    results[sourceIndex] = placement;
                    accepted.Add(placement);
                    visibleCount++;
                    continue;
                }

                var placed = false;
                var slotLimit = Math.Min(options.MaxOffsetSlots, OffsetSlots.Length - 1);
                for (var slotIndex = 0; slotIndex <= slotLimit; slotIndex++)
                {
                    var slot = OffsetSlots[slotIndex];
                    var screenX = candidate.ScreenX + slot.X * (options.LabelWidth + options.MinGap);
                    var screenY = candidate.ScreenY + slot.Y * (options.LabelHeight + options.MinGap);
                    var placement = Visible(candidate, screenX, screenY, slotIndex);
                    if (OverlapsAccepted(placement))
                    {
                        continue;
                    }

                    results[sourceIndex] = placement;
                    accepted.Add(placement);
                    visibleCount++;
                    placed = true;
                    break;
                }

                if (!placed)
                {
                    results[sourceIndex] = Hidden(candidate);
                }
            }
        }
        finally
        {
            accepted.Clear();
            indexPriorityComparer.Clear();
        }
    }

    private void EnsureSortBufferCapacity(int candidateCount)
    {
        if (sortedIndexes.Length >= candidateCount)
        {
            return;
        }

        Array.Resize(ref sortedIndexes, candidateCount);
        Array.Resize(ref distanceBuckets, candidateCount);
    }

    private bool OverlapsAccepted(ScrapLabelPlacement placement)
    {
        for (var index = 0; index < accepted.Count; index++)
        {
            if (placement.Overlaps(accepted[index], options.MinGap))
            {
                return true;
            }
        }

        return false;
    }

    private ScrapLabelPlacement Visible(ScrapLabelCandidate candidate, float screenX, float screenY, int slotIndex)
    {
        return new ScrapLabelPlacement(
            candidate.Id,
            IsVisible: true,
            screenX,
            screenY,
            options.LabelWidth,
            options.LabelHeight,
            slotIndex);
    }

    private static ScrapLabelPlacement Hidden(ScrapLabelCandidate candidate)
    {
        return new ScrapLabelPlacement(
            candidate.Id,
            IsVisible: false,
            candidate.ScreenX,
            candidate.ScreenY,
            Width: 0f,
            Height: 0f,
            SlotIndex: -1);
    }

    private static int ComparePriority(
        ScrapLabelCandidate left,
        ScrapLabelCandidate right,
        int leftDistanceBucket,
        int rightDistanceBucket)
    {
        var distance = leftDistanceBucket.CompareTo(rightDistanceBucket);
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

    private static int GetDistancePriorityBucket(float distance)
    {
        if (float.IsNaN(distance))
        {
            return int.MaxValue;
        }

        return (int)MathF.Floor(distance / DistancePriorityBucketSize);
    }

    private sealed class IndexPriorityComparer : IComparer<int>
    {
        private IReadOnlyList<ScrapLabelCandidate>? candidates;
        private int[]? distanceBuckets;

        public void Reset(IReadOnlyList<ScrapLabelCandidate> candidates, int[] distanceBuckets)
        {
            this.candidates = candidates;
            this.distanceBuckets = distanceBuckets;
        }

        public void Clear()
        {
            candidates = null;
            distanceBuckets = null;
        }

        public int Compare(int leftIndex, int rightIndex)
        {
            var currentCandidates = candidates!;
            var currentDistanceBuckets = distanceBuckets!;
            return ComparePriority(
                currentCandidates[leftIndex],
                currentCandidates[rightIndex],
                currentDistanceBuckets[leftIndex],
                currentDistanceBuckets[rightIndex]);
        }
    }
}
