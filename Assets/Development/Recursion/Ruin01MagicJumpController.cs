using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Fyp.Development.Recursion
{
    /// <summary>Recursive magical leaps with controller-driven movement and one animation cycle per segment.</summary>
    public sealed class Ruin01MagicJumpController : MonoBehaviour, ISharedInteractionTarget, ISharedInteractionPriority
    {
        public BridgePracticeController bridgePractice;
        public Transform player;
        public PlayerMovement playerMovement;
        public CameraFollow cameraFollow;
        public CharacterController characterController;
        public Animator animator;
        public AnimationClip runningJumpClip;
        public Transform jumpStartPoint;
        public MeshCollider jumpStartSurface;
        public Transform interactionAnchor;
        public Ruin01JumpGuidedSimulation jumpGuide;
        public Transform[] destinations;
        public Ruin01MagicJumpTrigger[] magicTriggers;
        public GameObject panel;
        public TMP_InputField input;
        public Button castButton;
        public Button closeButton;
        public TMP_Text statusText;
        [Tooltip("Established AI feedback pipeline after a failed attempt (local text is the offline fallback).")]
        public ChallengeAIFeedback aiFeedback;
        [Min(.2f)] public float secondsPerLeap = .8f;
        [Min(1f)] public float interactionDistance = 3f;
        [Min(0f)] public float leapArcHeight = .8f;
        [Min(0f)] public float failureFeedbackSeconds = .85f;
        [Min(.1f)] public float platformPauseSeconds = .9f;
        [Min(0f)] public float animationBlendSeconds = .06f;

        bool open, playing, completed, developerPrerequisitesUnlocked;
        bool movementBefore, cameraBefore, controllerBefore, cursorBefore, allowBefore, rootMotionBefore;
        CursorLockMode lockBefore;
        GameObject selectionBefore;
        Coroutine playback;
        TrailRenderer jumpTrail;
        Material jumpTrailMaterial;
        int requested, calls, performed, currentN, attempts, animationPlayCount, animationActivationCount;
        static readonly int LocomotionState = Animator.StringToHash("Base Layer.Locomotion");
        static readonly int RunningJumpState = Animator.StringToHash("Base Layer.Running Jump");
        static readonly int RunningJumpShortState = Animator.StringToHash("Running Jump");
        static readonly int MagicJumpSpeed = Animator.StringToHash("MagicJumpSpeed");

        public bool JumpChallengeCompleted => completed;
        public event Action LandedSuccessfully;
        public bool IsOpen => open;
        public bool IsPlaying => playing;
        public int CallCount => calls;
        public int PerformedJumps => performed;
        public int CurrentN => currentN;
        public int AttemptCount => attempts;
        public bool DeveloperPrerequisitesUnlocked => developerPrerequisitesUnlocked;
        public int AnimationPlayCount => animationPlayCount;
        public int AnimationActivationCount => animationActivationCount;
        public int RunningJumpStateHash => RunningJumpState;
        public bool IsJumpAnimationActive => animator != null &&
            (IsRunningJump(animator.GetCurrentAnimatorStateInfo(0)) ||
             (animator.IsInTransition(0) && IsRunningJump(animator.GetNextAnimatorStateInfo(0))));
        public bool UsesSharedInteraction => true;
        // Prefer the rock's local action only within its range; the existing Hub return stays registered.
        public int SharedInteractionPriority => 100;
        public Transform InteractionOrigin => interactionAnchor;
        public float SharedInteractionRange => interactionDistance;
        public bool CanSharedInteract => enabled && isActiveAndEnabled && !open && !playing && !completed &&
            (jumpGuide == null || !jumpGuide.IsOpen) &&
            bridgePractice != null && (developerPrerequisitesUnlocked ||
                (bridgePractice.IsComplete && bridgePractice.GuidedLessonCompleted)) &&
            player != null && jumpStartPoint != null && interactionAnchor != null &&
            Vector3.Distance(player.position, interactionAnchor.position) <= interactionDistance &&
            IsPlayerOnJumpStartSurface() && CursorManager.allowCursorLock;
        public string SharedHint => "Press [E] to Cast Recursive Magic Jump";

        void Awake()
        {
            if (bridgePractice == null || player == null || playerMovement == null || cameraFollow == null ||
                characterController == null || animator == null || runningJumpClip == null ||
                !runningJumpClip.isHumanMotion || jumpStartPoint == null || jumpStartSurface == null || interactionAnchor == null ||
                !jumpStartSurface.enabled || jumpStartSurface.isTrigger || destinations == null || destinations.Length != 5 ||
                Array.Exists(destinations, t => t == null) || magicTriggers == null || magicTriggers.Length != 5 ||
                Array.Exists(magicTriggers, t => t == null) || panel == null || input == null || castButton == null ||
                closeButton == null || statusText == null)
            {
                Debug.LogError("Ruin 1 magic jump needs explicit bridge, player, route and panel references.", this);
                enabled = false;
                return;
            }
            panel.SetActive(false);
            HideAllPlatforms();
            animator.applyRootMotion = false;
            castButton.onClick.AddListener(Submit);
            closeButton.onClick.AddListener(Close);
        }

        public void InteractFromShared()
        {
            if (CanSharedInteract) Open();
        }

        bool IsPlayerOnJumpStartSurface()
        {
            if (player == null || characterController == null || jumpStartSurface == null ||
                !jumpStartSurface.enabled || jumpStartSurface.isTrigger) return false;
            float probeHeight = Mathf.Max(1.5f, characterController.stepOffset + 1f);
            var ray = new Ray(player.position + Vector3.up * probeHeight, Vector3.down);
            return jumpStartSurface.Raycast(ray, out var hit, probeHeight + characterController.stepOffset + .75f) &&
                Vector3.Angle(hit.normal, Vector3.up) <= characterController.slopeLimit;
        }

        public void Open()
        {
            if (!CanSharedInteract) return;
            if (jumpGuide != null && !jumpGuide.IsCompleted && !developerPrerequisitesUnlocked)
            {
                jumpGuide.Open();
                return;
            }
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
            input.text = "";
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
                const string rangeHint = "Enter one whole number from 1 to 6.";
                SetStatus(rangeHint);
                // A non-empty rejected entry (e.g. 0, 7 or "magicJump(5)") is a learning moment; blank stays local.
                if (aiFeedback != null && !string.IsNullOrWhiteSpace(input.text))
                    aiFeedback.Request(JumpContext(input.text.Trim(), int.TryParse(input.text, out int bad) ?
                            "out-of-range n (" + bad + "); valid entries are 1 to 6" : "not a whole number",
                            "Entry check", "no attempt started"),
                        rangeHint, (text, live) => { if (open && !playing) SetStatus(text); });
                return;
            }
            requested = n;
            calls = 0;
            performed = 0;
            currentN = n;
            attempts++;
            playing = true;
            rootMotionBefore = animator.applyRootMotion;
            animator.applyRootMotion = false;
            HideAllPlatforms();
            input.gameObject.SetActive(false);
            castButton.gameObject.SetActive(false);
            if (CursorManager.Instance != null) CursorManager.Instance.SetUIOpen(false);
            else { CursorManager.allowCursorLock = true; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            cameraFollow.enabled = cameraBefore;
            characterController.enabled = false;
            ClearJumpTrail();
            player.position = jumpStartPoint.position;
            var forward = destinations[0].position - jumpStartPoint.position;
            forward.y = 0;
            player.rotation = Quaternion.LookRotation(forward, Vector3.up);
            playback = StartCoroutine(RunAttempt());
        }

        IEnumerator RunAttempt()
        {
            yield return MagicJump(requested);
            HideAllPlatforms();
            RestoreLocomotionAnimation();
            if (requested == 5 && performed == 5)
            {
                PlaceSafely(destinations[4], false);
                completed = true;
                SetStatus("Jump challenge complete; safe landing reached.");
                playing = false;
                playback = null;
                Close();
                LandedSuccessfully?.Invoke();
                yield break;
            }
            SetStatus(requested < destinations.Length
                ? "Not quite there! Your magic stopped before reaching the destination. Try a different n."
                : "Too much magic! The spell would carry you beyond the safe route. Try a different n.");
            yield return new WaitForSeconds(failureFeedbackSeconds);
            ClearJumpTrail();
            PlaceSafely(jumpStartPoint, true);
            FinishAttemptAnimation();
            cameraFollow.enabled = false;
            if (CursorManager.Instance != null) CursorManager.Instance.SetUIOpen(true);
            else { CursorManager.allowCursorLock = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            input.gameObject.SetActive(true);
            castButton.gameObject.SetActive(true);
            playing = false;
            playback = null;
            string localHint = requested < destinations.Length
                ? "Not quite there! Your magic stopped before reaching the destination. Try a different n."
                : "Too much magic! The spell would carry you beyond the safe route. Try a different n.";
            SetStatus(localHint);
            if (aiFeedback != null)
                aiFeedback.Request(JumpContext(requested.ToString(),
                        requested < destinations.Length ? "fell short (too few leaps)" : "overshot (too many leaps)",
                        "Attempt result", $"n = {requested}, leaps performed = {performed}, recursive calls = {calls}"),
                    localHint, (text, live) => { if (open && !playing) SetStatus(text); });
            input.Select();
            input.ActivateInputField();
        }

        // AI context only: never includes the destination count or the required n (kept as protectedAnswer for leak filtering).
        ChallengeFeedbackRequest JumpContext(string answer, string errorType, string phase, string parameters) =>
            new ChallengeFeedbackRequest {
                challengeId = "Ruin01Jump", ruinId = "Ruin01", playerAnswer = answer, errorType = errorType, phase = phase,
                question = "Choose the starting n so magicJump(n) makes exactly enough leaps to reach the far side.",
                parameters = parameters, currentStep = attempts,
                callStack = phase == "Attempt result" ? $"descended magicJump({requested}) to magicJump(0), then {performed} calls returned with a leap; stack now empty" : "(empty)",
                protectedAnswer = destinations != null ? destinations.Length.ToString() : null };

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
                SetStatus("The spell reached the end of the safe route. Extra movement was prevented.");
                yield return new WaitForSeconds(.55f);
                yield break;
            }
            int index = performed;
            var from = index == 0 ? jumpStartPoint.position : destinations[index - 1].position;
            var to = destinations[index].position;
            SetStatus("Returning from magicJump(" + n + "): completing the next leap.");
            PlayRunningJumpAnimation();
            BeginJumpTrail();
            yield return Travel(from, to, secondsPerLeap);
            if (jumpTrail != null) jumpTrail.emitting = false;
            RestoreLocomotionAnimation();
            performed++;
            HideAllPlatforms();
            magicTriggers[index].RevealAtFeet(player.position);
            SetStatus(performed + " leaps completed. Standing on a temporary Magic Platform.");
            yield return new WaitForSeconds(platformPauseSeconds);
            magicTriggers[index].Hide();
        }

        void BeginJumpTrail()
        {
            if (jumpTrail == null)
            {
                var shader = Resources.Load<Shader>("Ruin01JumpTrail");
                if (shader == null || !shader.isSupported) return;
                jumpTrailMaterial = new Material(shader) { name = "Ruin 1 Magic Jump Trail" };
                jumpTrail = Ruin01JumpTrail.Create(animator, jumpTrailMaterial);
            }
            jumpTrail.Clear();
            jumpTrail.enabled = true;
            jumpTrail.emitting = true;
        }

        internal void ClearJumpTrail()
        {
            if (jumpTrail == null) return;
            jumpTrail.emitting = false;
            jumpTrail.Clear();
            jumpTrail.enabled = false;
        }

        void HideAllPlatforms()
        {
            if (magicTriggers == null) return;
            foreach (var trigger in magicTriggers) if (trigger != null) trigger.Hide();
        }

        void PlayRunningJumpAnimation()
        {
            animator.applyRootMotion = false;
            float cycleSeconds = Mathf.Max(.01f, secondsPerLeap);
            animator.SetFloat(MagicJumpSpeed, Mathf.Max(.01f, runningJumpClip.length / cycleSeconds));
            animator.CrossFadeInFixedTime(RunningJumpState, animationBlendSeconds, 0, 0f);
            animationPlayCount++;
        }

        static bool IsRunningJump(AnimatorStateInfo state)
        {
            return state.fullPathHash == RunningJumpState || state.shortNameHash == RunningJumpShortState;
        }

        void RestoreLocomotionAnimation()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.CrossFadeInFixedTime(LocomotionState, animationBlendSeconds, 0, 0f);
        }

        void FinishAttemptAnimation()
        {
            RestoreLocomotionAnimation();
            if (animator != null) animator.applyRootMotion = rootMotionBefore;
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
            bool animationObserved = false;
            var direction = to - from;
            direction.y = 0;
            if (direction.sqrMagnitude > .01f) player.rotation = Quaternion.LookRotation(direction);
            while (elapsed < seconds)
            {
                if (IsJumpAnimationActive) animationObserved = true;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                float ease = t * t * (3f - 2f * t);
                player.position = Vector3.Lerp(from, to, ease) + Vector3.up *
                    (leapArcHeight * Mathf.Sin(Mathf.PI * t));
                yield return null;
            }
            player.position = to;
            if (IsJumpAnimationActive) animationObserved = true;
            if (animationObserved) animationActivationCount++;
        }

        void SetStatus(string message)
        {
            if (statusText != null) statusText.text = "Current n: " + currentN +
                "   Calls executed: " + calls + "   Leaps completed: " + performed + "\n" + message;
        }

        public void SetDeveloperPrerequisiteOverride(bool unlocked)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying || playing) return;
            developerPrerequisitesUnlocked = unlocked;
#endif
        }

        public void ResetForDeveloperTesting()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying || playing) return;
            if (open) Close();
            ClearJumpTrail();
            completed = false;
            requested = calls = performed = currentN = attempts = 0;
            animationPlayCount = 0;
            animationActivationCount = 0;
            HideAllPlatforms();
            FinishAttemptAnimation();
#endif
        }

        public void Close()
        {
            if (!open || playing) return;
            aiFeedback?.Cancel();
            open = false;
            if (panel != null) panel.SetActive(false);
            if (CursorManager.Instance != null) CursorManager.Instance.SetUIOpen(false);
            CursorManager.allowCursorLock = allowBefore;
            Cursor.lockState = lockBefore;
            Cursor.visible = cursorBefore;
            if (playerMovement != null) playerMovement.enabled = movementBefore;
            if (cameraFollow != null) cameraFollow.enabled = cameraBefore;
            if (characterController != null) characterController.enabled = controllerBefore;
            FinishAttemptAnimation();
            EventSystem.current?.SetSelectedGameObject(selectionBefore);
        }

        void OnDisable()
        {
            ClearJumpTrail();
            HideAllPlatforms();
            FinishAttemptAnimation();
            if (!open) return;
            if (playback != null) StopCoroutine(playback);
            playing = false;
            if (!completed && jumpStartPoint != null && player != null) PlaceSafely(jumpStartPoint, true);
            Close();
        }

        void OnDestroy()
        {
            if (jumpTrail != null) Destroy(jumpTrail.gameObject);
            if (jumpTrailMaterial != null) Destroy(jumpTrailMaterial);
            if (castButton != null) castButton.onClick.RemoveListener(Submit);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }
    }
}
