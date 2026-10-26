using UnityEngine;

namespace Fyp.Development.Recursion
{
    /// <summary>Explicit references for one bridge; no execution or progression controller.</summary>
    [DisallowMultipleComponent]
    public sealed class FloatingBridgeMotionRig : MonoBehaviour
    {
        public Transform fixedStartingSection;
        public FloatingIslandMotion placeStone3;
        public FloatingIslandMotion placeStone2;
        public FloatingIslandMotion placeStone1;
        public Transform destinationIsland;

        public bool TryGetSection(int parameter, out FloatingIslandMotion section)
        {
            section = parameter == 3 ? placeStone3 : parameter == 2 ? placeStone2 : parameter == 1 ? placeStone1 : null;
            return section != null;
        }
    }
}
