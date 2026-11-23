using System;
using System.IO;
using System.Linq;
using System.Text;
using Fyp.Development.Progress;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Fyp.Development.Recursion.Editor
{
    [InitializeOnLoad]
    public static class RuinJumpUIAndDeveloperPlayChecks
    {
        const string Key = "FYP.RuinJumpUIAndDeveloper.PlayChecks";
        const string Output = "Temp/RuinJumpUIAndDeveloper/PlayChecks.txt";
        static RuinDeveloperPanel developer;
        static Ruin01MagicJumpController jump;
        static Ruin01FragmentReward fragment;
        static FragmentProgressTracker progress;
        static CharacterController player;
        static Keyboard keyboard;
        static StringBuilder log;
        static Vector3 blockedPosition;
        static bool initialBridge, initialLesson, initialFragment;
        static int initialFragments, stage, assertions, errors;
        static double began, stageAt;

        static RuinJumpUIAndDeveloperPlayChecks()
        {
            EditorApplication.update += Tick;
            if (SessionState.GetBool(Key, false)) Application.logMessageReceived += OnLog;
        }

        [MenuItem("Tools/FYP/Ruin 1/Jump UI + Developer Mode/Run Targeted Play Checks")]
        public static void Run()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                scene.path != "Assets/Scenes/MagicWorld_Development.unity" || scene.isDirty)
                throw new InvalidOperationException("Run checks from the saved development scene in Edit Mode.");
            RuinJumpUIAndDeveloperTools.VerifyInstalled();
            developer = null; jump = null; fragment = null; progress = null; player = null; keyboard = null;
            stage = assertions = errors = 0; began = stageAt = 0;
            log = new StringBuilder("Ruin 1 Jump UI + Developer Mode actual Unity Play Mode\n");
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }

        static void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
            try
            {
                if (began == 0) { began = EditorApplication.timeSinceStartup; return; }
                if (EditorApplication.timeSinceStartup - began > 80)
                    throw new TimeoutException("Developer Mode check timed out at stage " + stage);
                if (developer == null)
                {
                    if (EditorApplication.timeSinceStartup - began < 1) return;
                    developer = UnityEngine.Object.FindObjectsByType<RuinDeveloperPanel>(FindObjectsSortMode.None).Single();
                    jump = developer.jumpController;
                    fragment = UnityEngine.Object.FindObjectsByType<Ruin01FragmentReward>(FindObjectsSortMode.None).Single();
                    progress = fragment.progress;
                    player = developer.characterController;
                    initialBridge = jump.bridgePractice.IsComplete;
                    initialLesson = jump.bridgePractice.GuidedLessonCompleted;
                    initialFragment = fragment.FragmentCollected;
                    initialFragments = progress.CollectedCount;
                    Check(developer.enabled && developer.IsEditorOnlyAvailable && !developer.IsOpen && !developer.panel.activeInHierarchy,
                        "editor-only Developer Panel starts hidden");
                    Check(!jump.DeveloperPrerequisitesUnlocked && !jump.CanSharedInteract,
                        "real jump prerequisites begin locked");
                    CheckNoSpoilers("initial hidden jump UI");
                    keyboard = InputSystem.AddDevice<Keyboard>("Ruin Developer QA Keyboard");
                    keyboard.MakeCurrent();
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    Next(1); return;
                }
                if (EditorApplication.timeSinceStartup - stageAt < .18) return;
                switch (stage)
                {
                    case 1:
                        Press(UnityEngine.InputSystem.Key.Tab); Next(2); return;
                    case 2:
                        Check(developer.IsOpen && developer.panel.activeInHierarchy, "real Tab opens Developer Mode");
                        Check(!developer.playerMovement.enabled && !developer.cameraFollow.enabled &&
                              !CursorManager.allowCursorLock && Cursor.visible && Cursor.lockState == CursorLockMode.None,
                            "Developer Mode pauses controls and releases cursor");
                        Release(); Next(3); return;
                    case 3:
                        Press(UnityEngine.InputSystem.Key.Tab); Next(4); return;
                    case 4:
                        Check(!developer.IsOpen && !developer.panel.activeInHierarchy && developer.playerMovement.enabled &&
                              developer.cameraFollow.enabled && player.enabled && CursorManager.allowCursorLock &&
                              Cursor.lockState == CursorLockMode.Locked,
                            "second real Tab closes panel and restores controls");
                        Release(); developer.Open();
                        blockedPosition = player.transform.position;
                        jump.panel.SetActive(true);
                        developer.TeleportToRuin02Entry();
                        Check(Vector3.Distance(player.transform.position, blockedPosition) < .01f && developer.IsOpen,
                            "another modal blocks a Developer teleport");
                        jump.panel.SetActive(false);
                        developer.UnlockRuin01JumpForTesting();
                        Check(jump.DeveloperPrerequisitesUnlocked && jump.bridgePractice.IsComplete == initialBridge &&
                              jump.bridgePractice.GuidedLessonCompleted == initialLesson,
                            "session unlock bypasses without changing real bridge or lesson progress");
                        developer.TeleportToRuin01JumpStart();
                        Next(5); return;
                    case 5:
                        CheckSafeTeleport(developer.ruin01JumpStart, "Ruin 1 JumpStartPoint");
                        Check(jump.CanSharedInteract, "session unlock makes the jump available only at its real start");
                        Press(UnityEngine.InputSystem.Key.E); Next(6); return;
                    case 6:
                        Check(jump.IsOpen && jump.panel.activeInHierarchy, "real E opens jump input after session unlock");
                        Check(string.IsNullOrEmpty(jump.input.text), "jump input is no longer prefilled with the answer");
                        CheckNoSpoilers("open jump input");
                        Release();
                        jump.input.text = "1";
                        jump.castButton.onClick.Invoke();
                        Check(jump.IsPlaying, "test attempt starts through the existing Cast button");
                        Next(7); return;
                    case 7:
                        CheckNoSpoilers("jump playback");
                        if (jump.IsPlaying) return;
                        Check(jump.statusText.text.Contains("Not quite there! Your magic stopped before reaching the destination. Try a different n."),
                            "insufficient-distance feedback matches the requested non-spoiler text");
                        Check(jump.statusText.text.Contains("Current n:") && jump.statusText.text.Contains("Calls executed:"),
                            "current n and executed call count remain visible");
                        CheckNoSpoilers("failed jump feedback");
                        jump.Close();
                        developer.Open();
                        developer.ResetRuin01JumpAttempt();
                        Check(jump.AttemptCount == 0 && !jump.JumpChallengeCompleted,
                            "Developer reset clears only the repeatable jump attempt");
                        developer.TeleportToMainHub();
                        Next(8); return;
                    case 8:
                        CheckSafeTeleport(developer.mainHubSpawn, "Main Hub Player Spawn");
                        developer.Open(); developer.TeleportToRuin02Entry(); Next(9); return;
                    case 9:
                        CheckSafeTeleport(developer.ruin02EntrySpawn, "Ruin 2 EntrySpawn");
                        Check(jump.bridgePractice.IsComplete == initialBridge &&
                              jump.bridgePractice.GuidedLessonCompleted == initialLesson &&
                              fragment.FragmentCollected == initialFragment && progress.CollectedCount == initialFragments,
                            "shortcuts do not change real progression or fragment collection");
                        Check(errors == 0, "no new Play Mode Console errors");
                        Finish(true); return;
                }
            }
            catch (Exception ex) { Finish(false, "stage=" + stage + "\n" + ex); }
        }

        static void CheckSafeTeleport(Transform destination, string label)
        {
            Check(!developer.IsOpen && !developer.panel.activeInHierarchy && player.enabled &&
                  developer.playerMovement.enabled && developer.cameraFollow.enabled && CursorManager.allowCursorLock,
                label + " restores player, camera and cursor control");
            Vector2 playerXZ = new Vector2(player.transform.position.x, player.transform.position.z);
            Vector2 destinationXZ = new Vector2(destination.position.x, destination.position.z);
            Check(Vector2.Distance(playerXZ, destinationXZ) < .2f &&
                  Mathf.Abs(player.transform.position.y - destination.position.y) < 1.5f &&
                  Vector3.Distance(developer.cameraFollow.transform.position, player.transform.position) < 10f,
                label + " uses the existing marker and keeps the camera nearby");
        }

        static void CheckNoSpoilers(string context)
        {
            string visible = string.Join("\n", jump.panel.GetComponentsInChildren<TMPro.TMP_Text>(true)
                .Select(x => x.text ?? ""));
            Check(!visible.Contains("/5") && !visible.Contains("of 5") &&
                  !visible.Contains("Five calls") && !visible.Contains("five leaps") &&
                  !visible.Contains("one leap too many") && !visible.Contains("no sixth"),
                context + " contains no target-count spoiler");
        }

        static void Press(UnityEngine.InputSystem.Key key)
        {
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        }

        static void Release()
        {
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        }

        static void Check(bool result, string message)
        {
            assertions++;
            if (!result) throw new InvalidOperationException("Jump UI + Developer Mode: " + message);
        }

        static void Next(int next)
        {
            stage = next;
            stageAt = EditorApplication.timeSinceStartup;
        }

        static void Finish(bool success, string reason = null)
        {
            Application.logMessageReceived -= OnLog;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            log ??= new StringBuilder("Ruin 1 Jump UI + Developer Mode actual Unity Play Mode\n");
            log.AppendLine((success ? "PASS" : "FAIL") + " assertions=" + assertions + " Play errors=" + errors);
            if (reason != null) log.AppendLine(reason);
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            File.WriteAllText(Output, log.ToString());
            if (success) Debug.Log(log.ToString()); else Debug.LogError(log.ToString());
            SessionState.SetBool(Key, false);
            EditorApplication.isPlaying = false;
        }
    }
}
