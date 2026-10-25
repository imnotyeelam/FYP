using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Fyp.Development.Recursion
{
    [DefaultExecutionOrder(-100)]
    public sealed class RuinRecursionVisualizer : MonoBehaviour, ISharedInteractionTarget
    {
        public Transform player;
        public Transform interactionPoint;
        public Canvas existingCanvas;
        public GameObject spellbookPanel;
        public GameObject portalChallengePanel;
        public float interactionDistance = 3f;
        public bool useSharedInteraction;

        [Header("World-first presentation")]
        public RecursionPanelView panelView;
        public RecursionWorldView[] worldViews;
        public Camera gameplayCamera;
        public Camera observationCamera;
        public BridgeWorldView observationBridge;
        public bool useObservationCamera = true;

        public bool UsesSharedInteraction => useSharedInteraction;
        public Transform InteractionOrigin => interactionPoint;
        public float SharedInteractionRange => interactionDistance;
        public bool CanSharedInteract => isActiveAndEnabled && panel != null && player != null &&
            interactionPoint != null && !open && !OtherModalOpen;
        public string SharedHint => "Press [E] to Observe Recursion";
        public void InteractFromShared()
        {
            if (useSharedInteraction && CanSharedInteract) Open();
        }

        private RecursionPlayback playback;
        private GameObject panel;
        private TMP_FontAsset font;
        private bool open, hintVisible, closeRequested;
        private CursorLockMode previousLock;
        private bool previousVisible, previousAllowLock;
        private GameObject previousSelection;
        private readonly List<Behaviour> suspended = new List<Behaviour>();
        private bool ownsPanel, cameraOverride;
        private bool oldGameplayCameraEnabled, oldObservationCameraEnabled;
        private Camera activeGameplayCamera;
        private Rect oldObservationRect;
        private float oldOrthographicSize;
        private Vector3 oldObservationPosition;
        private Quaternion oldObservationRotation;
        private int lastWidth, lastHeight;
        private readonly Vector3[] panelCorners = new Vector3[4];

        private void Start()
        {
            if (player == null || interactionPoint == null || existingCanvas == null)
            {
                Debug.LogError("Ruin recursion visualizer requires Player, InteractionPoint and the existing Canvas.", this);
                enabled = false;
                return;
            }
            font = InteractionHintManager.Instance != null && InteractionHintManager.Instance.hintText != null
                ? InteractionHintManager.Instance.hintText.font : TMP_Settings.defaultFontAsset;
            if (playback == null) playback = new RecursionPlayback(BridgeRecursionLesson.Create());
            if (panelView == null)
            {
                panelView = RecursionPanelView.Create(existingCanvas.transform, font);
                ownsPanel = true;
            }
            panel = panelView.gameObject;
            panelView.Initialize(NextStep, PreviousStep, Replay, Close, JumpToStep);
            panel.SetActive(false);
            Render(true);
        }

        public void SetTrace(RecursionTrace trace)
        {
            playback = new RecursionPlayback(trace);
            if (panel != null) Render(true);
        }

        private bool OtherModalOpen => !CursorManager.allowCursorLock ||
            (spellbookPanel != null && spellbookPanel.activeInHierarchy) ||
            (portalChallengePanel != null && portalChallengePanel.activeInHierarchy);

        private void Update()
        {
            if (panel == null) return;
            if (open)
            {
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) closeRequested = true;
                if (cameraOverride && (Screen.width != lastWidth || Screen.height != lastHeight)) FrameObservation();
                return;
            }
            if (useSharedInteraction) return;
            bool near = Vector3.Distance(player.position, interactionPoint.position) <= interactionDistance;
            bool available = near && !OtherModalOpen && Cursor.lockState == CursorLockMode.Locked;
            if (available && !hintVisible)
            {
                InteractionHintManager.Instance?.ShowHint(this, "Press [E] to Observe Recursion");
                hintVisible = true;
            }
            else if (!available && hintVisible) HideHint();
            if (available && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) Open();
        }

        // Let CursorManager consume Escape while the modal still owns the cursor.
        private void LateUpdate()
        {
            if (!closeRequested) return;
            closeRequested = false;
            Close();
        }

        public void Open()
        {
            if (panel == null || open || OtherModalOpen) return;
            previousLock = Cursor.lockState;
            previousVisible = Cursor.visible;
            previousAllowLock = CursorManager.allowCursorLock;
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            open = true;
            HideHint();
            InteractionHintManager.Instance?.HideAll();
            // Preserve enabled states; prevent a legacy E handler opening a second modal or teleporting.
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour.gameObject.scene != gameObject.scene || !behaviour.enabled) continue;
                if (behaviour is PlayerMovement || behaviour is CameraFollow ||
                    behaviour is KnowledgeInteractable || behaviour is RuinEntrance || behaviour is PortalInteraction)
                {
                    suspended.Add(behaviour);
                    behaviour.enabled = false;
                }
            }
            if (CursorManager.Instance != null) CursorManager.Instance.SetUIOpen(true);
            else
            {
                CursorManager.allowCursorLock = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            panel.SetActive(true);
            BeginObservation();
            Render(true);
            EventSystem.current?.SetSelectedGameObject(panelView.nextButton.gameObject);
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            closeRequested = false;
            if (panel != null) panel.SetActive(false);
            if (worldViews != null) foreach (var view in worldViews) if (view != null) view.FinishMotion();
            EndObservation();
            if (CursorManager.Instance != null) CursorManager.Instance.SetUIOpen(false);
            CursorManager.allowCursorLock = previousAllowLock;
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisible;
            foreach (var behaviour in suspended) if (behaviour != null) behaviour.enabled = true;
            suspended.Clear();
            EventSystem.current?.SetSelectedGameObject(previousSelection);
        }

        public void NextStep() { if (playback == null) return; playback.Next(); Render(false); }
        public void PreviousStep() { if (playback == null) return; playback.Previous(); Render(true); }
        public void Replay() { if (playback == null) return; playback.Replay(); Render(true); }
        // Zero-based checkpoint index; the editable slider displays 1..22 to the learner.
        public void JumpToStep(int index) { if (playback == null) return; playback.JumpTo(index); Render(true); }

        private void HideHint()
        {
            InteractionHintManager.Instance?.HideHint(this);
            hintVisible = false;
        }

        private void OnDisable() { Close(); HideHint(); }
        private void OnDestroy()
        {
            Close();
            if (panelView != null) panelView.Disconnect();
            if (ownsPanel && panel != null) Destroy(panel);
        }

        private void Render(bool immediate)
        {
            panelView.Present(playback);
            if (worldViews != null)
                foreach (var view in worldViews) if (view != null) view.ApplySnapshot(playback.Current, immediate);
        }

        private void BeginObservation()
        {
            if (!useObservationCamera || observationCamera == null) return;
            activeGameplayCamera = gameplayCamera != null ? gameplayCamera : Camera.main;
            if (activeGameplayCamera == observationCamera) return;
            oldGameplayCameraEnabled = activeGameplayCamera != null && activeGameplayCamera.enabled;
            oldObservationCameraEnabled = observationCamera.enabled;
            oldObservationRect = observationCamera.rect;
            oldOrthographicSize = observationCamera.orthographicSize;
            oldObservationPosition = observationCamera.transform.position;
            oldObservationRotation = observationCamera.transform.rotation;
            cameraOverride = true;
            if (activeGameplayCamera != null) activeGameplayCamera.enabled = false;
            observationCamera.enabled = true;
            FrameObservation();
        }

        private void FrameObservation()
        {
            lastWidth = Screen.width; lastHeight = Screen.height;
            ((RectTransform)panelView.transform).GetWorldCorners(panelCorners);
            // The existing Canvas is Screen Space Overlay; no new Canvas or global scaler is needed.
            float edge = RectTransformUtility.WorldToScreenPoint(null, panelCorners[0]).x;
            float width = Mathf.Clamp(edge / Mathf.Max(1, Screen.width), .25f, .95f);
            observationCamera.rect = new Rect(0, 0, width, 1);
            if (observationBridge == null || !observationCamera.orthographic) return;
            Bounds bounds = observationBridge.ObservationBounds();
            observationCamera.transform.LookAt(bounds.center);
            Vector3 right = observationCamera.transform.right, up = observationCamera.transform.up;
            Vector3 ext = bounds.extents;
            float horizontal = Mathf.Abs(right.x) * ext.x + Mathf.Abs(right.y) * ext.y + Mathf.Abs(right.z) * ext.z;
            float vertical = Mathf.Abs(up.x) * ext.x + Mathf.Abs(up.y) * ext.y + Mathf.Abs(up.z) * ext.z;
            float aspect = Mathf.Max(.1f, Screen.width * width / Mathf.Max(1f, Screen.height));
            observationCamera.orthographicSize = Mathf.Max(vertical, horizontal / aspect) * 1.12f;
        }

        private void EndObservation()
        {
            if (!cameraOverride) return;
            cameraOverride = false;
            if (observationCamera != null)
            {
                observationCamera.enabled = oldObservationCameraEnabled;
                observationCamera.rect = oldObservationRect;
                observationCamera.orthographicSize = oldOrthographicSize;
                observationCamera.transform.SetPositionAndRotation(oldObservationPosition, oldObservationRotation);
            }
            if (activeGameplayCamera != null) activeGameplayCamera.enabled = oldGameplayCameraEnabled;
        }
    }
}
