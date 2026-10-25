using System;

namespace Fyp.Development.Recursion
{
    // No Unity dependency: a checkpoint fully determines the bridge's semantic state.
    public readonly struct BridgeSnapshotProjection
    {
        public int PlacedCount { get; }
        public int ReturningStone { get; }
        public bool Completed { get; }

        public BridgeSnapshotProjection(RecursionStep step, int stoneCount)
        {
            if (stoneCount < 1) throw new ArgumentOutOfRangeException(nameof(stoneCount));
            if (step.ActionCount < 0 || step.ActionCount > stoneCount)
                throw new ArgumentException("The lesson has more placement actions than configured bridge stones.");
            PlacedCount = step.ActionCount;
            Completed = step.Phase == RecursionPhase.Complete;
            bool returning = step.Phase == RecursionPhase.Return || step.Phase == RecursionPhase.Resume;
            ReturningStone = returning && step.Parameter.HasValue && PlacedCount == stoneCount
                ? Math.Max(0, Math.Min(stoneCount - 1, stoneCount - Math.Max(1, step.Parameter.Value))) : -1;
        }

        public bool IsPlaced(int index) => index >= 0 && index < PlacedCount;
    }
}
