using Fyp.Development.Progress;
using TMPro;
using UnityEngine;

namespace Fyp.Development.Recursion
{
    /// <summary>Ruin 2-local wrapper for the existing return entrance; it does not alter RuinEntrance globally.</summary>
    public sealed class Ruin02ReturnGate : MonoBehaviour, ISharedInteractionTarget, ISharedInteractionPriority
    {
        public RuinEntrance returnEntrance;
        public FragmentProgressTracker progress;
        public TMP_Text lockedMessage;
        // The retired H4 entrance retains route/prerequisite references without receiving E input.
        public Ruin02FarmAccess farmAccess;
        [Min(.5f)] public float messageSeconds = 2.5f;

        public bool UsesSharedInteraction => true;
        public Transform InteractionOrigin => returnEntrance != null ? returnEntrance.transform : transform;
        public float SharedInteractionRange => returnEntrance != null ? returnEntrance.interactionDistance : 0f;
        public int SharedInteractionPriority => 20;
        public bool FarmCompleted => farmAccess != null && farmAccess.console != null &&
            farmAccess.console.simulation != null && farmAccess.console.simulation.IsComplete;
        public string SharedHint => progress != null && progress.HasFragment(FragmentProgressTracker.Ruin02)
            ? "Press [E] to Return to Hub"
            : farmAccess != null && farmAccess.StaffCompleted
                ? FarmCompleted ? "Press [E] to Check Return Requirements" : "Press [E] to Enter Magical Farm"
                : "Press [E] to Inspect Magic Rock";
        public bool CanSharedInteract => isActiveAndEnabled && returnEntrance != null && returnEntrance.player != null &&
            returnEntrance.destination != null && returnEntrance.teleportManager != null && progress != null &&
            farmAccess != null && farmAccess.destination != null && farmAccess.console != null &&
            farmAccess.console.simulation != null && !farmAccess.console.IsOpen && !farmAccess.console.simulation.IsRunning &&
            farmAccess.practice != null && !farmAccess.practice.IsOpen && !farmAccess.practice.IsCinematicActive &&
            CursorManager.allowCursorLock && Cursor.lockState == CursorLockMode.Locked &&
            Vector3.Distance(returnEntrance.player.position, InteractionOrigin.position) <= SharedInteractionRange;

        public void InteractFromShared()
        {
            if (!CanSharedInteract) return;
            if (progress.HasFragment(FragmentProgressTracker.Ruin02))
            {
                TeleportSafely(returnEntrance.destination);
                return;
            }
            if (!farmAccess.StaffCompleted)
            {
                FragmentRewardToast.Show(lockedMessage, "Restore all four staffs and finish the restoration first!", messageSeconds);
                return;
            }
            if (!FarmCompleted)
            {
                TeleportSafely(farmAccess.destination);
                return;
            }
            ShowLockedMessage();
        }

        void TeleportSafely(Transform destination)
        {
            var player = returnEntrance.player;
            var controller = player.GetComponent<CharacterController>();
            bool enabledBefore = controller != null && controller.enabled;
            Vector3 offset = destination.position - player.position;
            returnEntrance.teleportManager.TeleportTo(destination);
            if (controller != null) controller.enabled = enabledBefore;
            player.GetComponent<PlayerMovement>()?.ResetVerticalVelocity();
            if (farmAccess.console.cameraFollow != null)
            {
                farmAccess.console.cameraFollow.transform.position += offset;
            }
            Physics.SyncTransforms();
            InteractionHintManager.Instance?.HideAll();
        }

        void ShowLockedMessage()
        {
            FragmentRewardToast.Show(lockedMessage, "Collect the magic fragment first!", messageSeconds);
        }
    }
}
