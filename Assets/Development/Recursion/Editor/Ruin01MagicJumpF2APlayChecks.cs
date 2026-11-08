using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Fyp.Development.Recursion.Editor
{
    [InitializeOnLoad]
    public static class Ruin01MagicJumpF2APlayChecks
    {
        const string Key = "FYP.Ruin01.JumpF2A.PlayChecks";
        const string Output = "Temp/Ruin01JumpF2A/PlayChecks.txt";
        static Ruin01MagicJumpController jump;
        static BridgePracticeController practice;
        static Ruin01GuidedSimulationUI guided;
        static CharacterController player;
        static Keyboard keyboard;
        static StringBuilder log;
        static double[,] firstSeen, lastSeen;
        static int stage, assertions, decisions, errors;
        static double began, stageAt;

        static Ruin01MagicJumpF2APlayChecks()
        {
            EditorApplication.update += Tick;
            if (SessionState.GetBool(Key, false)) Application.logMessageReceived += OnLog;
        }

        static void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
        }

        static void Check(bool result, string message)
        {
            assertions++;
            if (!result) throw new InvalidOperationException("Ruin 1 F2-A: " + message);
        }

        static void Next(int next)
        {
            stage = next;
            stageAt = EditorApplication.timeSinceStartup;
        }

        [MenuItem("Tools/FYP/Ruin 1/Jump F2-A/Run Interactive Play Checks")]
        public static void Run()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/MagicWorld_Development.unity" || scene.isDirty)
                throw new InvalidOperationException("Run F2-A checks from a saved development scene in Edit Mode.");
            if (FloatingIslandPrototypeTools.Exact("Ruin01_RelocationRoot/Ruin01JumpRoute/JumpChallenge")
                .GetComponent<Ruin01MagicJumpController>() == null)
                throw new InvalidOperationException("F2-A jump controller is not installed.");
            jump = null; practice = null; guided = null; player = null; keyboard = null;
            stage = 0; assertions = 0; decisions = 0; errors = 0; began = 0;
            log = new StringBuilder("Ruin 1 F2-A actual Unity Play Mode\n");
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
            try
            {
                if (began == 0)
                {
                    began = EditorApplication.timeSinceStartup;
                    log ??= new StringBuilder("Ruin 1 F2-A actual Unity Play Mode\n");
                    return;
                }
                if (EditorApplication.timeSinceStartup - began > 140) throw new TimeoutException("F2-A play check timed out at stage " + stage);
                if (jump == null)
                {
                    if (EditorApplication.timeSinceStartup - began < 1) return;
                    jump = UnityEngine.Object.FindObjectsByType<Ruin01MagicJumpController>(FindObjectsSortMode.None).Single();
                    practice = jump.bridgePractice;
                    guided = UnityEngine.Object.FindObjectsByType<Ruin01GuidedSimulationUI>(FindObjectsSortMode.None).Single();
                    player = jump.characterController;
                    firstSeen = new double[4, 5];
                    lastSeen = new double[4, 5];
                    for (int attempt = 0; attempt < 4; attempt++)
                    for (int step = 0; step < 5; step++) firstSeen[attempt, step] = -1;
                    Check(jump.enabled && jump.destinations.Length == 5 && jump.magicTriggers.Length == 5 &&
                          !jump.JumpChallengeCompleted && !jump.IsOpen,
                        "fresh jump controller, five F1 destinations and five triggers");
                    Check(!jump.CanSharedInteract && !practice.IsComplete && !practice.GuidedLessonCompleted,
                        "guided lesson and bridge completion gate jump interaction");
                    Check(jump.magicTriggers.All(x => x != null && !x.IsVisible &&
                          !x.GetComponent<SphereCollider>().enabled && x.GetComponent<Rigidbody>() == null &&
                          x.PulseCount == 0),
                        "five hidden visual-only platforms cannot drive recursive calls");
                    Check(InteractionHintManager.Instance.interactionTargets.Count(x => x == jump) == 1 &&
                          InteractionHintManager.Instance.modalPanels.Count(x => x == jump.panel) == 1,
                        "one shared E registration and one modal registration");
                    Check(jump.panel.GetComponentsInChildren<Renderer>(true).Length == 0, "no world-space placeholder geometry");
                    player.enabled = false;
                    player.transform.position = jump.jumpStartPoint.position;
                    player.enabled = true;
                    Physics.SyncTransforms();
                    keyboard = InputSystem.AddDevice<Keyboard>("Ruin 1 F2-A QA Keyboard");
                    keyboard.MakeCurrent();
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    Next(1); return;
                }
                if (stageAt > 0 && EditorApplication.timeSinceStartup - stageAt < .2) return;
                switch (stage)
                {
                    case 1:
                        Check(!jump.CanSharedInteract && !jump.IsOpen, "standing on start cannot bypass incomplete bridge");
                        jump.Open();
                        Check(!jump.IsOpen, "input panel stays locked before guided lesson");
                        guided.Open();
                        guided.simulation.JumpToStep(guided.simulation.Playback.Trace.Steps.Count - 1);
                        guided.Finish();
                        Check(practice.GuidedLessonCompleted, "existing guided lesson completed independently");
                        practice.Open();
                        Check(practice.IsOpen, "existing bridge practice opened");
                        Next(2); return;
                    case 2:
                        if (practice.IsComplete) { Next(3); return; }
                        Check(!jump.CanSharedInteract, "jump remains locked during bridge restoration");
                        if (practice.IsBusy || practice.CurrentDecision == null ||
                            practice.CheckpointIndex != practice.CurrentDecision.CheckpointIndex) return;
                        var q = practice.CurrentDecision;
                        Check(practice.SubmitAnswer(q.Id, q.CorrectAnswerId).Accepted, "existing bridge decision accepted");
                        decisions++;
                        return;
                    case 3:
                        Check(decisions == 6 && practice.CheckpointIndex == 21 && practice.RestorationCount == 3,
                            "six questions and 22 checkpoints completed before jumping");
                        practice.Close();
                        Check(!practice.IsOpen && jump.CanSharedInteract, "jump unlocks only after bridge closes");
                        Check(jump.playerMovement.enabled && jump.cameraFollow.enabled && player.enabled,
                            "baseline movement, camera and controller active");
                        keyboard.MakeCurrent();
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        Next(4); return;
                    case 4:
                        Check(InteractionHintManager.Instance.hintText.gameObject.activeInHierarchy &&
                              InteractionHintManager.Instance.hintText.text == jump.SharedHint,
                            "shared E hint selects jump start");
                        keyboard.MakeCurrent();
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(KeyCodeToInputKey.E));
                        Next(5); return;
                    case 5:
                        Check(jump.IsOpen && !jump.IsPlaying && jump.panel.activeInHierarchy,
                            "real E opens the jump input panel");
                        Check(!jump.playerMovement.enabled && !jump.cameraFollow.enabled &&
                              !CursorManager.allowCursorLock && Cursor.visible,
                            "panel suspends movement and camera, then releases cursor");
                        keyboard.MakeCurrent();
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        jump.input.text = "0";
                        jump.castButton.onClick.Invoke();
                        Check(!jump.IsPlaying && jump.AttemptCount == 0, "n=0 rejected");
                        jump.input.text = "1";
                        jump.castButton.onClick.Invoke();
                        Check(jump.IsPlaying && !jump.playerMovement.enabled && !player.enabled,
                            "n=1 begins airborne with normal movement and collision disabled");
                        Check(jump.magicTriggers.All(x => !x.IsVisible), "no platform appears before the first landing");
                        Next(6); return;
                    case 6:
                        if (jump.IsPlaying) { ObservePlatforms(); CheckWaitingAtLastStep(); return; }
                        CheckRetry(1, 1, "Not quite there!");
                        CheckAttemptPlatforms(0, 1);
                        CheckPulses(1, 0, 0, 0, 0);
                        log.AppendLine("n=1: one trigger pulse, insufficient feedback, direct teleport and retry.");
                        StartAttempt(4); Next(7); return;
                    case 7:
                        if (jump.IsPlaying) { ObservePlatforms(); CheckWaitingAtLastStep(); return; }
                        CheckRetry(4, 4, "Not quite there!");
                        CheckAttemptPlatforms(1, 4);
                        CheckPulses(2, 1, 1, 1, 0);
                        log.AppendLine("n=4: four segment pulses, direct teleport and retry.");
                        StartAttempt(6); Next(8); return;
                    case 8:
                        if (jump.IsPlaying)
                        {
                            ObservePlatforms();
                            CheckWaitingAtLastStep();
                            Check(player.transform.position.z <= jump.destinations[4].position.z + .1f,
                                "n=6 never moves beyond safe target");
                            return;
                        }
                        CheckRetry(6, 5, "Too much magic!");
                        CheckAttemptPlatforms(2, 5);
                        CheckPulses(3, 2, 2, 2, 1);
                        log.AppendLine("n=6: sixth call blocked, five pulses, direct teleport and retry.");
                        StartAttempt(5); Next(9); return;
                    case 9:
                        if (jump.IsPlaying) { ObservePlatforms(); return; }
                        Check(jump.JumpChallengeCompleted && jump.CallCount == 5 && jump.PerformedJumps == 5,
                            "n=5 completes exactly five recursive leaps");
                        CheckAttemptPlatforms(3, 5);
                        CheckPulses(4, 3, 3, 3, 2);
                        Check(!jump.IsOpen && !jump.panel.activeInHierarchy && !jump.CanSharedInteract,
                            "completed challenge closes panel and persists for Play session");
                        Check(Vector3.Distance(player.transform.position, jump.destinations[4].position) < .15f && player.enabled,
                            "player lands safely on target with CharacterController enabled");
                        Check(jump.playerMovement.enabled && jump.cameraFollow.enabled && CursorManager.allowCursorLock &&
                              Cursor.lockState == CursorLockMode.Locked,
                            "movement, camera and cursor restored after landing");
                        Check(jump.AttemptCount == 4, "retries and success persisted in one Play session");
                        log.AppendLine("n=5: exact target landing, five pulses, completion retained, movement/camera/cursor restored.");
                        Next(10); return;
                    case 10:
                        if (EditorApplication.timeSinceStartup - stageAt < 1.1) return;
                        Check(jump.magicTriggers.All(x => x.Brightness < .01f),
                            "all five trigger emissions fade to the original colour");
                        Check(Vector3.Distance(player.transform.position, jump.destinations[4].position) < .25f,
                            "gravity remains stable on the landing surface");
                        Finish(true); return;
                }
            }
            catch (Exception ex) { Finish(false, "stage=" + stage + " decisions=" + decisions + " checkpoint=" + (practice == null ? -1 : practice.CheckpointIndex) + "\n" + ex); }
        }

        // Alias avoids Key constant shadowing the Input System Key enum.
        static class KeyCodeToInputKey { public const UnityEngine.InputSystem.Key E = UnityEngine.InputSystem.Key.E; }

        static void StartAttempt(int n)
        {
            jump.input.text = n.ToString();
            jump.castButton.onClick.Invoke();
            Check(jump.IsPlaying && jump.statusText.text.Contains("Calls executed:"), "playback displays call status");
        }

        static void CheckPulses(params int[] expected)
        {
            for (int i = 0; i < 5; i++)
                Check(jump.magicTriggers[i].PulseCount == expected[i],
                    "trigger " + (i + 1) + " reacts once per completed segment");
        }

        static void ObservePlatforms()
        {
            int visible = -1;
            for (int i = 0; i < 5; i++)
            {
                if (!jump.magicTriggers[i].IsVisible) continue;
                Check(visible == -1, "at most one Magic Platform is visible");
                visible = i;
            }
            if (visible < 0) return;
            Check(visible == jump.PerformedJumps - 1, "only the just-completed segment is visible");
            Check(Vector3.Distance(player.transform.position, jump.destinations[visible].position) < .15f,
                "Wizard pauses at the completed segment marker");
            var platform = jump.magicTriggers[visible].transform;
            Check(Mathf.Abs(platform.position.y + platform.lossyScale.y - player.transform.position.y) < .03f &&
                  Vector2.Distance(new Vector2(platform.position.x, platform.position.z),
                      new Vector2(player.transform.position.x, player.transform.position.z)) < .03f,
                "platform top aligns directly beneath the Wizard's feet");
            int attempt = jump.AttemptCount - 1;
            if (firstSeen[attempt, visible] < 0) firstSeen[attempt, visible] = EditorApplication.timeSinceStartup;
            lastSeen[attempt, visible] = EditorApplication.timeSinceStartup;
        }

        static void CheckAttemptPlatforms(int attempt, int count)
        {
            Check(jump.magicTriggers.All(x => !x.IsVisible), "all Magic Platforms hide after the attempt");
            for (int i = 0; i < 5; i++)
            {
                if (i < count)
                    Check(firstSeen[attempt, i] >= 0 && lastSeen[attempt, i] - firstSeen[attempt, i] >= .6,
                        "segment " + (i + 1) + " reveals one platform for a visible standing pause");
                else Check(firstSeen[attempt, i] < 0, "future platform " + (i + 1) + " stayed hidden");
            }
        }

        static void CheckWaitingAtLastStep()
        {
            if (jump.PerformedJumps == 0 || !jump.statusText.text.Contains("Teleporting")) return;
            Check(Vector3.Distance(player.transform.position,
                    jump.destinations[jump.PerformedJumps - 1].position) < .15f,
                "failure feedback holds at the last step; no reverse travel");
        }

        static void CheckRetry(int calls, int leaps, string feedback)
        {
            Check(jump.IsOpen && !jump.JumpChallengeCompleted && jump.CallCount == calls &&
                  jump.PerformedJumps == leaps && jump.statusText.text.Contains(feedback),
                "failed attempt has expected recursion count and feedback");
            Check(Vector3.Distance(player.transform.position, jump.jumpStartPoint.position) < .15f && player.enabled,
                "failed attempt teleports to safe takeoff surface");
            Check(Vector3.Distance(jump.cameraFollow.transform.position, player.transform.position) < 10f,
                "camera teleports with the player before retry");
            Check(!jump.playerMovement.enabled && !jump.cameraFollow.enabled && !CursorManager.allowCursorLock &&
                  jump.input.gameObject.activeInHierarchy && jump.castButton.gameObject.activeInHierarchy,
                "retry input remains usable with modal controls");
            Check(jump.magicTriggers.All(x => !x.IsVisible), "failed attempt leaves no floating platform visible");
        }

        static void Finish(bool success, string reason = null)
        {
            Application.logMessageReceived -= OnLog;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (log == null) log = new StringBuilder("Ruin 1 F2-A actual Unity Play Mode\n");
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
