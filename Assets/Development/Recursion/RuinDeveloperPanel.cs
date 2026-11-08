using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Fyp.Development.Recursion
{
    /// <summary>Editor Play Mode shortcuts only. The component disables itself in every player build.</summary>
    public sealed class RuinDeveloperPanel : MonoBehaviour
    {
        public GameObject panel;
        public TMP_Text statusText;
        public UnityEngine.UI.Button hubButton;
        public UnityEngine.UI.Button ruin01Button;
        public UnityEngine.UI.Button ruin02Button;
        public UnityEngine.UI.Button unlockJumpButton;
        public UnityEngine.UI.Button resetJumpButton;
        public UnityEngine.UI.Button closeButton;
        public Transform player;
        public PlayerMovement playerMovement;
        public CameraFollow cameraFollow;
        public CharacterController characterController;
        public Transform mainHubSpawn;
        public Transform ruin01JumpStart;
        public Transform ruin02EntrySpawn;
        public Ruin01MagicJumpController jumpController;
        public InteractionHintManager interactionHints;

        bool open, movementBefore, cameraBefore, controllerBefore, cursorVisibleBefore, cursorAllowedBefore;
        CursorLockMode cursorLockBefore;
        GameObject selectionBefore;

        public bool IsOpen => open;
        public bool IsEditorOnlyAvailable => Application.isEditor;
        public bool CanOpen => Application.isEditor && enabled && isActiveAndEnabled && !open &&
            jumpController != null && !jumpController.IsOpen && !jumpController.IsPlaying &&
            playerMovement != null && playerMovement.enabled && cameraFollow != null && cameraFollow.enabled &&
            characterController != null && characterController.enabled && CursorManager.allowCursorLock &&
            Cursor.lockState == CursorLockMode.Locked && !HasOtherModal() && !HasActiveCinematic();

        void Awake()
        {
#if !UNITY_EDITOR
            if (panel != null) panel.SetActive(false);
            enabled = false;
#else
            if (panel == null || statusText == null || hubButton == null || ruin01Button == null ||
                ruin02Button == null || unlockJumpButton == null || resetJumpButton == null || closeButton == null ||
                player == null || playerMovement == null || cameraFollow == null || characterController == null ||
                mainHubSpawn == null || ruin01JumpStart == null || ruin02EntrySpawn == null ||
                jumpController == null || interactionHints == null)
            {
                Debug.LogError("Developer Mode needs explicit panel, player, spawn and jump references.", this);
                enabled = false;
                return;
            }
            panel.SetActive(false);
            hubButton.onClick.AddListener(TeleportToMainHub);
            ruin01Button.onClick.AddListener(TeleportToRuin01JumpStart);
            ruin02Button.onClick.AddListener(TeleportToRuin02Entry);
            unlockJumpButton.onClick.AddListener(UnlockRuin01JumpForTesting);
            resetJumpButton.onClick.AddListener(ResetRuin01JumpAttempt);
            closeButton.onClick.AddListener(Close);
#endif
        }

        void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying || Keyboard.current == null || !Keyboard.current.tabKey.wasPressedThisFrame) return;
            if (open) Close();
            else Open();
#endif
        }

        public void Open()
        {
#if UNITY_EDITOR
            if (!CanOpen) return;
            open = true;
            movementBefore = playerMovement.enabled;
            cameraBefore = cameraFollow.enabled;
            controllerBefore = characterController.enabled;
            cursorLockBefore = Cursor.lockState;
            cursorVisibleBefore = Cursor.visible;
            cursorAllowedBefore = CursorManager.allowCursorLock;
            selectionBefore = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            playerMovement.enabled = false;
            cameraFollow.enabled = false;
            interactionHints.HideAll();
            if (CursorManager.Instance != null) CursorManager.Instance.SetUIOpen(true);
            else
            {
                CursorManager.allowCursorLock = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            statusText.text = "Editor Play Mode shortcuts. No progression or rewards are changed.";
            panel.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(closeButton.gameObject);
#endif
        }

        public void Close()
        {
#if UNITY_EDITOR
            if (!open) return;
            open = false;
            panel.SetActive(false);
            RestoreControls();
#endif
        }

        public void TeleportToMainHub() => Teleport(mainHubSpawn, "Main Hub Player Spawn");
        public void TeleportToRuin01JumpStart() => Teleport(ruin01JumpStart, "Ruin 1 JumpStartPoint");
        public void TeleportToRuin02Entry() => Teleport(ruin02EntrySpawn, "Ruin 2 EntrySpawn");

        public void UnlockRuin01JumpForTesting()
        {
#if UNITY_EDITOR
            if (!CanUseShortcut()) return;
            jumpController.SetDeveloperPrerequisiteOverride(true);
            statusText.text = "Ruin 1 Jump unlocked for this Play session only. Bridge and lesson progress were not changed.";
#endif
        }

        public void ResetRuin01JumpAttempt()
        {
#if UNITY_EDITOR
            if (!CanUseShortcut()) return;
            jumpController.ResetForDeveloperTesting();
            statusText.text = "Ruin 1 Jump attempt reset for testing. No progression or rewards were granted.";
#endif
        }

        void Teleport(Transform destination, string label)
        {
#if UNITY_EDITOR
            if (destination == null || !CanUseShortcut()) return;
            characterController.enabled = false;
            Vector3 offset = destination.position - player.position;
            player.SetPositionAndRotation(destination.position, destination.rotation);
            cameraFollow.transform.position += offset;
            playerMovement.ResetVerticalVelocity();
            Physics.SyncTransforms();
            characterController.enabled = controllerBefore;
            statusText.text = "Teleported to " + label + ".";
            Close();
#endif
        }

        bool CanUseShortcut()
        {
            return open && jumpController != null && !jumpController.IsOpen && !jumpController.IsPlaying &&
                !HasOtherModal() && !HasActiveCinematic();
        }

        bool HasOtherModal()
        {
            if (interactionHints == null || interactionHints.modalPanels == null) return false;
            foreach (var modal in interactionHints.modalPanels)
                if (modal != null && modal != panel && modal.activeInHierarchy) return true;
            return false;
        }

        bool HasActiveCinematic()
        {
#if UNITY_EDITOR
            foreach (var practice in FindObjectsByType<Ruin02PracticeController>(FindObjectsSortMode.None))
                if (practice.IsCinematicActive) return true;
            foreach (var restoration in FindObjectsByType<Ruin02AirRestoration>(FindObjectsSortMode.None))
                if (restoration.IsPlaying) return true;
#endif
            return false;
        }

        void RestoreControls()
        {
            characterController.enabled = controllerBefore;
            playerMovement.enabled = movementBefore;
            cameraFollow.enabled = cameraBefore;
            if (CursorManager.Instance != null) CursorManager.Instance.SetUIOpen(false);
            CursorManager.allowCursorLock = cursorAllowedBefore;
            Cursor.lockState = cursorLockBefore;
            Cursor.visible = cursorVisibleBefore;
            EventSystem.current?.SetSelectedGameObject(selectionBefore);
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            if (open)
            {
                open = false;
                if (panel != null) panel.SetActive(false);
                RestoreControls();
            }
#endif
        }

        void OnDestroy()
        {
#if UNITY_EDITOR
            if (hubButton != null) hubButton.onClick.RemoveListener(TeleportToMainHub);
            if (ruin01Button != null) ruin01Button.onClick.RemoveListener(TeleportToRuin01JumpStart);
            if (ruin02Button != null) ruin02Button.onClick.RemoveListener(TeleportToRuin02Entry);
            if (unlockJumpButton != null) unlockJumpButton.onClick.RemoveListener(UnlockRuin01JumpForTesting);
            if (resetJumpButton != null) resetJumpButton.onClick.RemoveListener(ResetRuin01JumpAttempt);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
#endif
        }
    }
}
