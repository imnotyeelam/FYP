using System.Collections;
using Fyp.Development.Progress;
using TMPro;
using UnityEngine;

namespace Fyp.Development.Recursion
{
    /// <summary>Ruin 2-local wrapper for the existing return entrance; it does not alter RuinEntrance globally.</summary>
    public sealed class Ruin02ReturnGate : MonoBehaviour, ISharedInteractionTarget
    {
        public RuinEntrance returnEntrance;
        public FragmentProgressTracker progress;
        public TMP_Text lockedMessage;
        [Min(.5f)] public float messageSeconds = 2.5f;
        Coroutine clearMessage;

        public bool UsesSharedInteraction => true;
        public Transform InteractionOrigin => returnEntrance != null ? returnEntrance.transform : transform;
        public float SharedInteractionRange => returnEntrance != null ? returnEntrance.interactionDistance : 0f;
        public string SharedHint => "Press [E] to return to Hub";
        public bool CanSharedInteract => isActiveAndEnabled && returnEntrance != null && returnEntrance.player != null &&
                                         returnEntrance.destination != null && returnEntrance.teleportManager != null && progress != null;

        public void InteractFromShared()
        {
            if (!CanSharedInteract) return;
            if (!progress.HasFragment(FragmentProgressTracker.Ruin02))
            {
                ShowLockedMessage();
                return;
            }
            returnEntrance.teleportManager.TeleportTo(returnEntrance.destination);
        }

        void ShowLockedMessage()
        {
            if (lockedMessage == null) return;
            lockedMessage.text = "Collect the magic fragment first!";
            // This is the existing FragmentRewardToast.  Its Chomage font and visual styling
            // are intentionally owned by the shared UI, not by this gate.
            lockedMessage.gameObject.SetActive(true);
            lockedMessage.transform.parent.gameObject.SetActive(true);
            if (clearMessage != null) StopCoroutine(clearMessage);
            clearMessage = StartCoroutine(HideMessageAfterDelay());
        }

        IEnumerator HideMessageAfterDelay()
        {
            yield return new WaitForSecondsRealtime(messageSeconds);
            if (lockedMessage != null) lockedMessage.transform.parent.gameObject.SetActive(false);
            clearMessage = null;
        }
    }
}
