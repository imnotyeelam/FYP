using UnityEngine;

namespace Fyp.Development.Recursion
{
    // Future crystal/echo views can consume the same immutable execution snapshots.
    public abstract class RecursionWorldView : MonoBehaviour
    {
        public abstract void ApplySnapshot(RecursionStep step, bool immediate);
        public virtual void FinishMotion() { }
    }
}
