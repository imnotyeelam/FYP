using TMPro;
using UnityEngine;

namespace Fyp.Development.Progress
{
    public sealed class FinalPortalInteraction : MonoBehaviour, ISharedInteractionTarget
    {
        public PortalRestorationController restoration;
        public TMP_Text toast;
        [Min(1f)] public float interactionDistance = 5f;

        FragmentProgressTracker Progress => restoration.fragmentProgress;
        public bool UsesSharedInteraction => true;
        public Transform InteractionOrigin => transform;
        public float SharedInteractionRange => interactionDistance;
        public bool CanSharedInteract => isActiveAndEnabled && restoration != null &&
            restoration.isActiveAndEnabled && restoration.requireFinalPortalFragments &&
            Progress != null && !restoration.HasStartedRestoration;
        public string SharedHint => Progress.HasFinalPortalFragments
            ? $"Final Portal · {Progress.FinalPortalProgress} fragments — Press [E] to restore"
            : $"Final Portal · {Progress.FinalPortalProgress} fragments — Press [E] for requirements";

        public void InteractFromShared()
        {
            if (!CanSharedInteract) return;
            if (Progress.HasFinalPortalFragments)
            {
                restoration.RestoreMagic();
                return;
            }

            bool hasCalling = Progress.HasFragment(FragmentProgressTracker.Ruin01);
            bool hasReturning = Progress.HasFragment(FragmentProgressTracker.Ruin02);
            string missing = hasCalling ? "Fragment of Returning Magic (Ruin 2)" :
                hasReturning ? "Fragment of Calling (Ruin 1)" :
                "Fragment of Calling (Ruin 1) and Fragment of Returning Magic (Ruin 2)";
            FragmentRewardToast.Show(toast, $"Final Portal · {Progress.FinalPortalProgress}\nMissing: {missing}", 4f);
        }
    }
}
