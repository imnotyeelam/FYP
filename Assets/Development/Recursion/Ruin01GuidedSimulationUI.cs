using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Fyp.Development.Recursion
{
    /// <summary>Owns Ruin 1's standalone guided simulation modal and its explicit Finish gate.</summary>
    public sealed class Ruin01GuidedSimulationUI : MonoBehaviour
    {
        public GameObject panel;
        public SpellbookRecursionSimulation simulation;
        public Button finishButton;
        public MonoBehaviour playerMovement, cameraFollow;

        bool open, movementBefore, cameraBefore, oldVisible, oldAllow;
        CursorLockMode oldLock;
        GameObject oldSelection;

        public bool IsOpen => open;
        public bool GuidedLessonCompleted => simulation != null && simulation.IsCompleted;

        void Awake()
        {
            if(simulation != null)
            {
                simulation.CloseRequested = CloseEarly;
                simulation.FinalCheckpointReached += RefreshFinish;
            }
            if(finishButton != null) finishButton.onClick.AddListener(Finish);
            if(panel != null) panel.SetActive(false);
        }

        public void Open()
        {
            if(open || panel == null || simulation == null || !CursorManager.allowCursorLock) return;
            open = true;
            movementBefore = playerMovement != null && playerMovement.enabled;
            cameraBefore = cameraFollow != null && cameraFollow.enabled;
            oldLock = Cursor.lockState;
            oldVisible = Cursor.visible;
            oldAllow = CursorManager.allowCursorLock;
            oldSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if(playerMovement != null) playerMovement.enabled = false;
            if(cameraFollow != null) cameraFollow.enabled = false;
            InteractionHintManager.Instance?.HideAll();
            if(CursorManager.Instance != null) CursorManager.Instance.SetUIOpen(true);
            else { CursorManager.allowCursorLock = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            panel.SetActive(true);
            RefreshFinish();
        }

        void Update()
        {
            if(open && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CloseEarly();
        }

        void RefreshFinish()
        {
            if(finishButton != null)
                finishButton.gameObject.SetActive(open && simulation != null && simulation.HasReachedFinalCheckpoint && !simulation.IsCompleted);
        }

        public void Finish()
        {
            if(!open || simulation == null || !simulation.CompleteLesson()) return;
            CloseInternal();
        }

        public void CloseEarly()
        {
            if(open) CloseInternal();
        }

        void CloseInternal()
        {
            open = false;
            if(panel != null) panel.SetActive(false);
            if(CursorManager.Instance != null) CursorManager.Instance.SetUIOpen(false);
            CursorManager.allowCursorLock = oldAllow;
            Cursor.lockState = oldLock;
            Cursor.visible = oldVisible;
            if(playerMovement != null) playerMovement.enabled = movementBefore;
            if(cameraFollow != null) cameraFollow.enabled = cameraBefore;
            EventSystem.current?.SetSelectedGameObject(oldSelection);
        }

        void OnDisable()
        {
            if(open) CloseInternal();
        }
    }
}
