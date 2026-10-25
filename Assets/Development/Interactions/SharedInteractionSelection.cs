using System;
using System.Collections.Generic;

public readonly struct SharedInteractionCandidate
{
    public readonly bool Eligible;
    public readonly float DistanceSquared;
    public readonly float Range;

    public SharedInteractionCandidate(bool eligible, float distanceSquared, float range)
    {
        Eligible = eligible;
        DistanceSquared = distanceSquared;
        Range = range;
    }
}

public static class SharedInteractionSelection
{
    // Stable ties use Inspector list order; a disabled/unavailable target cannot mask another.
    public static int Nearest(IReadOnlyList<SharedInteractionCandidate> candidates)
    {
        int selected = -1;
        float best = float.PositiveInfinity;
        for (int i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            if (!c.Eligible || c.Range < 0 || float.IsNaN(c.Range) || float.IsInfinity(c.Range) ||
                c.DistanceSquared < 0 || float.IsNaN(c.DistanceSquared) || float.IsInfinity(c.DistanceSquared) ||
                c.DistanceSquared > c.Range * c.Range) continue;
            if (c.DistanceSquared < best) { best = c.DistanceSquared; selected = i; }
        }
        return selected;
    }
}

public sealed class SharedInteractionPressGate
{
    private int consumedFrame = -1;
    public bool TryConsume(int frame, bool pressed)
    {
        if (!pressed || frame == consumedFrame) return false;
        consumedFrame = frame;
        return true;
    }
}
