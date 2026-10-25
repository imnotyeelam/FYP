using UnityEngine;

namespace Fyp.Development.Recursion
{
    public sealed class BridgeWorldView : RecursionWorldView
    {
        [Tooltip("Execution order: placeStone(3), placeStone(2), placeStone(1).")]
        public BridgeStoneView[] stones;
        public Renderer[] banksAndSteps;

        public override void ApplySnapshot(RecursionStep step, bool immediate)
        {
            if (stones == null || stones.Length == 0) return;
            var state = new BridgeSnapshotProjection(step, stones.Length);
            for (int i = 0; i < stones.Length; i++)
                if (stones[i] != null) stones[i].Apply(state.IsPlaced(i), state.ReturningStone == i,
                    state.Completed, immediate);
        }

        public override void FinishMotion()
        {
            if (stones != null) foreach (var stone in stones) if (stone != null) stone.Settle();
        }

        // Include both poses, so stepping does not cause the observation camera to zoom in/out.
        public Bounds ObservationBounds()
        {
            Bounds bounds = new Bounds(transform.position, Vector3.one);
            if (banksAndSteps != null)
                foreach (var renderer in banksAndSteps) if (renderer != null) bounds.Encapsulate(renderer.bounds);
            if (stones != null)
                foreach (var stone in stones)
                {
                    if (stone == null) continue;
                    Vector3 size = stone.transform.lossyScale;
                    if (stone.brokenPose != null) bounds.Encapsulate(new Bounds(stone.brokenPose.position, size));
                    if (stone.repairedPose != null) bounds.Encapsulate(new Bounds(stone.repairedPose.position, size));
                }
            return bounds;
        }
    }
}
