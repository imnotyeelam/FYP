using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fyp.Development.Progress
{
    /// <summary>Scene-session fragment state. Other ruins can use the same explicit instance.</summary>
    public sealed class FragmentProgressTracker : MonoBehaviour
    {
        public const string Ruin01 = "Ruin01";
        public const string Ruin02 = "Ruin02";
        public const string Ruin03 = "Ruin03";

        readonly HashSet<string> collected = new HashSet<string>(StringComparer.Ordinal);
        public event Action<string> FragmentCollected;
        public event Action AllFragmentsCollected;

        public int CollectedCount => collected.Count;
        public bool AllThreeCollected => collected.Count == 3;
        public bool HasFragment(string id) => id != null && collected.Contains(id);
        public static bool IsKnownFragment(string id) => id == Ruin01 || id == Ruin02 || id == Ruin03;

        public bool TryCollect(string id)
        {
            if (!IsKnownFragment(id) || !collected.Add(id)) return false;
            FragmentCollected?.Invoke(id);
            if (collected.Count == 3) AllFragmentsCollected?.Invoke();
            return true;
        }
    }
}
