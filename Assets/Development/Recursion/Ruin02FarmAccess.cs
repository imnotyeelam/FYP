using Fyp.Development.Progress;
using TMPro;
using UnityEngine;

namespace Fyp.Development.Recursion
{
    public sealed class Ruin02FarmAccess : MonoBehaviour, ISharedInteractionTarget, ISharedInteractionPriority
    {
        public bool isFarmReturn;
        public Ruin02PracticeController practice;
        public Ruin02AirRestoration restoration;
        public Ruin02HarvestConsole console;
        public Transform destination;
        public TeleportManager teleport;
        public TMP_Text toast;
        public float interactionDistance = 1.4f;
        public bool StaffCompleted => practice != null && practice.IsSolved && practice.Session != null &&
            practice.Session.Current.Frames.Count == 0 && restoration != null && !restoration.IsPlaying &&
            restoration.RestoredCount == 4 && restoration.IsStaffRestored(1) && restoration.IsStaffRestored(2) &&
            restoration.IsStaffRestored(3) && restoration.IsStaffRestored(4);
        public bool UsesSharedInteraction => true;
        public Transform InteractionOrigin => transform;
        public float SharedInteractionRange => interactionDistance;
        public int SharedInteractionPriority => 20;
        public bool CanSharedInteract => isActiveAndEnabled && teleport != null && teleport.player != null &&
            destination != null && console != null && !console.IsOpen && !console.simulation.IsRunning &&
            practice != null && !practice.IsOpen && !practice.IsCinematicActive &&
            CursorManager.allowCursorLock && Cursor.lockState == CursorLockMode.Locked &&
            Vector3.Distance(teleport.player.position, transform.position) <= interactionDistance;
        public string SharedHint => isFarmReturn ? "Press [E] to Return to Ritual Chamber" :
            StaffCompleted ? "Press [E] to Enter Magical Farm" : "Press [E] to Inspect Magical Farm Gateway";

        public void InteractFromShared()
        {
            if (!CanSharedInteract) return;
            if (!isFarmReturn && !StaffCompleted)
            {
                FragmentRewardToast.Show(toast, "Complete all four Staff Restorations and finish the return flow to enter Magical Farm.", 3.5f);
                return;
            }
            var player = teleport.player;
            var controller = player.GetComponent<CharacterController>();
            bool controllerBefore = controller != null && controller.enabled;
            Vector3 offset = destination.position - player.position;
            teleport.TeleportTo(destination);
            if (controller != null) controller.enabled = controllerBefore;
            player.GetComponent<PlayerMovement>()?.ResetVerticalVelocity();
            console.cameraFollow.transform.position += offset;
            Physics.SyncTransforms();
            InteractionHintManager.Instance?.HideAll();
        }
    }
}
