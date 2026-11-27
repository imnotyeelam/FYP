using Fyp.Development.Progress;
using TMPro;
using UnityEngine;

namespace Fyp.Development.Recursion
{
    /// <summary>
    /// Presentation only: names the original Ruin 2 stone by its current role.
    /// Fragment collected -> return stone; farm complete but fragment not yet collected -> collect prompt;
    /// otherwise -> farm entrance. Farm completion alone never shows the return name.
    /// Reads Ruin02ReturnGate/tracker state; never changes routing or progression.
    /// </summary>
    public sealed class Ruin02ReturnStoneLabel : MonoBehaviour
    {
        public Ruin02ReturnGate gate;
        public FragmentProgressTracker progress;
        public TMP_Text label;
        public string entranceText = "Magic Farm Entrance";
        public string collectText = "Collect the Magic Fragment";
        public string returnText = "Magic Return Stone";

        void LateUpdate()
        {
            if (label == null) return;
            bool collected = progress != null && progress.HasFragment(FragmentProgressTracker.Ruin02);
            bool farmDone = gate != null && gate.FarmCompleted;
            string wanted = collected ? returnText : farmDone ? collectText : entranceText;
            if (label.text != wanted) label.text = wanted;
        }
    }
}
