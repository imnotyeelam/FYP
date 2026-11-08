using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Fyp.Development.Recursion
{
    /// <summary>F2-A only: five scripted magical leaps, with no animation or reward integration.</summary>
    public sealed class Ruin01MagicJumpController : MonoBehaviour, ISharedInteractionTarget
    {
        public BridgePracticeController bridgePractice;
        public Transform player;
        public PlayerMovement playerMovement;
        public CameraFollow cameraFollow;
        public CharacterController characterController;
        public Transform jumpStartPoint;
        public Transform[] destinations;
        public Ruin01MagicJumpTrigger[] magicTriggers;
        public GameObject panel;
        public TMP_InputField input;
        public Button castButton;
        public Button closeButton;
        public TMP_Text statusText;
        [Min(.2f)] public float secondsPerLeap = .8f;
        [Min(1f)] public float interactionDistance = 3f;
        [Min(0f)] public float leapArcHeight = .8f;
        [Min(0f)] public float failureFeedbackSeconds = .85f;

        bool open, playing, completed, movementBefore, cameraBefore, controllerBefore, cursorBefore, allowBefore;
        CursorLockMode lockBefore;
        GameObject selectionBefore;
        Coroutine playback;
        int requested, calls, performed, currentN, attempts;

        public bool JumpChallengeCompleted => completed;
        public bool IsOpen => open;
        public bool IsPlaying => playing;
        public int CallCount => calls;
        public int PerformedJumps => performed;
        public int CurrentN => currentN;
        public int AttemptCount => attempts;
        public bool UsesSharedInteraction => true;
        public Transform InteractionOrigin => jumpStartPoint;
        public float SharedInteractionRange => interactionDistance;
        public bool CanSharedInteract => enabled && isActiveAndEnabled && !open && !playing && !completed &&
            bridgePractice != null && bridgePractice.IsComplete && bridgePractice.GuidedLessonCompleted &&
            player != null && jumpStartPoint != null &&
            Vector3.Distance(player.position, jumpStartPoint.position) <= interactionDistance && CursorManager.allowCursorLock;
        public string SharedHint => "Press [E] to Cast Recursive Magic Jump";

        void Awake()
        {
            if (bridgePractice == null || player == null || playerMovement == null || cameraFollow == null ||
                characterController == null || jumpStartPoint == null || destinations == null || destinations.Length != 5 ||
                Array.Exists(destinations, t => t == null) || magicTriggers == null || magicTriggers.Length != 5 ||
                Array.Exists(magicTriggers, t => t == null) || panel == null || input == null || castButton == null ||
                closeButton == null || statusText == null)
            {
                Debug.LogError("Ruin 1 magic jump needs explicit bridge, player, route and panel references.", this);
                enabled = false;
                return;
            }
            panel.SetActive(false);
            castButton.onClick.AddListener(Submit);
            closeButton.onClick.AddListener(Close);
        }

        public void InteractFromShared()
        {
            if (CanSharedInteract) Open();
        }

        public void Open()
        {
            if (!CanSharedInteract) return;
            open = true;
            movementBefore = playerMovement.enabled;
            cameraBefore = cameraFollow.enabled;
            controllerBefore = characterController.enabled;
            lockBefore = Cursor.lockState;
            cursorBefore = Cursor.visible;
            allowBefore = CursorManager.allowCursorLock;
            selectionBefore = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            playerMovement.enabled = false;
            cameraFollow.enabled = false;
            InteractionHintManager.Instance?.HideAll();
            if (CursorManager.Instance != null) CursorManager.Instance.SetUIOpen(true);
            else { CursorManager.allowCursorLock = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            panel.SetActive(true);
            input.gameObject.SetActive(true);
            castButton.gameObject.SetActive(true);
            input.text = "5";
            SetStatus("Enter n (1–6). magicJump(n) calls itself until n=0, then makes one leap per returning call.");
            input.Select();
            input.ActivateInputField();
        }

        void Update()
        {
            if (!open || playing || Keyboard.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame) Close();
            else if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
                Submit();
        }

        public void Submit()
        {
            if (!open || playing) return;
            if (!int.TryParse(input.text, out int n) || n < 1 || n > 6)
            {
                SetStatus("Enter one whole number from 1 to 6.");
                return;
            }
            requested = n;
            calls = 0;
            performed = 0;
            currentN = n;
            attempts++;
            playing = true;
            input.gameObject.SetActive(false);
            castButton.gameObject.SetActive(false);
            if (CursorManager.Instance != null) CursorManager.Instance.SetUIOpen(false);
            else { CursorManager.allowCursorLock = true; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            cameraFollow.enabled = cameraBefore;
            characterController.enabled = false;
            player.position = jumpStartPoint.position;
            var forward = destinations[0].position - jumpStartPoint.position;
            forward.y = 0;
            player.rotation = Quaternion.LookRotation(forward, Vector3.up);
            playback = StartCoroutine(RunAttempt());
        }

        IEnumerator RunAttempt()
        {
            yield return MagicJump(requested);
            if (requested == 5 && performed == 5)
            {
                PlaceSafely(destinations[4], false);
                completed = true;
                SetStatus("Five calls returned. Jump challenge complete; safe landing reached.");
                playing = false;
                playback = null;
                Close();
                yield break;
            }
            SetStatus(requested < 5
                ? "Insufficient distance: " + performed + " of 5 leaps. Teleporting to the start for retry."
                : "Overshoot: call 6 would pass the landing. Teleporting to the start for retry.");
            yield return new WaitForSeconds(failureFeedbackSeconds);
            PlaceSafely(jumpStartPoint, true);
            cameraFollow.enabled = false;
            if (CursorManager.Instance != null) CursorManager.Instance.SetUIOpen(true);
            else { CursorManager.allowCursorLock = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            input.gameObject.SetActive(true);
            castButton.gameObject.SetActive(true);
            playing = false;
            playback = null;
            SetStatus(requested < 5
                ? "Insufficient distance: " + performed + " of 5 leaps. Enter n and retry."
                : "Overshoot avoided: n=6 needs one leap too many. Enter n and retry.");
            input.Select();
            input.ActivateInputField();
        }

        // Each invocation adds one stack frame. The n=0 frame does not move the player.
        IEnumerator MagicJump(int n)
        {
            currentN = n;
            if (n == 0)
            {
                SetStatus("magicJump(0): base case. Return through the call stack.");
                yield return new WaitForSeconds(.3f);
                yield break;
            }
            calls++;
            SetStatus("magicJump(" + n + "): call magicJump(" + (n - 1) + "). Calls: " + calls + ".");
            yield return new WaitForSeconds(.18f);
            yield return MagicJump(n - 1);
            currentN = n;
            if (performed >= destinations.Length)
            {
                SetStatus("magicJump(" + n + "): no sixth destination. Overshoot stopped before movement.");
                yield return new WaitForSeconds(.55f);
                yield break;
            }
            int index = performed;
            var from = index == 0 ? jumpStartPoint.position : destinations[index - 1].position;
            var to = destinations[index].position;
            SetStatus("Returning from magicJump(" + n + "): leap " + (index + 1) + "/5. Calls: " + calls + ".");
            yield return Travel(from, to, secondsPerLeap);
            performed++;
            magicTriggers[index].Pulse();
        }

        void PlaceSafely(Transform destination, bool shiftCamera)
        {
            characterController.enabled = false;
            Vector3 offset = destination.position - player.position;
            player.position = destination.position;
            if (shiftCamera) cameraFollow.transform.position += offset;
            playerMovement.ResetVerticalVelocity();
            characterController.enabled = controllerBefore;
        }

        IEnumerator Travel(Vector3 from, Vector3 to, float seconds)
        {
            float elapsed = 0;
            var direction = to - from;
            direction.y = 0;
            if (direction.sqrMagnitude > .01f) player.rotation = Quaternion.LookRotation(direction);
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                float ease = t * t * (3f - 2f * t);
                player.position = Vector3.Lerp(from, to, ease) + Vector3.up *
                    (leapArcHeight * Mathf.Sin(Mathf.PI * t));
                yield return null;
            }
            player.position = to;
        }

        void SetStatus(string message)
        {
            if (statusText != null) statusText.text = "n=" + currentN + "   Calls=" + calls +
                "   Leaps=" + performed + "/5\n" + message;
        }

        public void Close()
        {
            if (!open || playing) return;
            open = false;
            panel.SetActive(false);
            if (CursorManager.Instance != null) CursorManager.Instance.SetUIOpen(false);
            CursorManager.allowCursorLock = allowBefore;
            Cursor.lockState = lockBefore;
            Cursor.visible = cursorBefore;
            playerMovement.enabled = movementBefore;
            cameraFollow.enabled = cameraBefore;
            characterController.enabled = controllerBefore;
            EventSystem.current?.SetSelectedGameObject(selectionBefore);
        }

        void OnDisable()
        {
            if (!open) return;
            if (playback != null) StopCoroutine(playback);
            playing = false;
            if (!completed && jumpStartPoint != null && player != null) PlaceSafely(jumpStartPoint, true);
            Close();
        }

        void OnDestroy()
        {
            if (castButton != null) castButton.onClick.RemoveListener(Submit);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }
    }
}
