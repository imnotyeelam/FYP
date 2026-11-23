using System;
using System.IO;
using System.Linq;
using Fyp.Development.Progress;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Fyp.Development.Recursion.Editor
{
    [InitializeOnLoad]
    public static class FinalPortalP1PlayChecks
    {
        const string Pending = "FYP.FinalPortalP1.PlayChecks";
        const string R1 = FragmentProgressTracker.Ruin01;
        const string R2 = FragmentProgressTracker.Ruin02;
        const string R3 = FragmentProgressTracker.Ruin03;
        static readonly string[][] Cases = { Array.Empty<string>(), new[] { R1 }, new[] { R2 },
            new[] { R1, R3 }, new[] { R2, R3 }, new[] { R2, R1 } };

        static FinalPortalInteraction gate;
        static FragmentProgressTracker sceneProgress, testProgress;
        static PortalInteraction legacy;
        static Keyboard keyboard;
        static int testIndex, stage, waitFrame, errors, swaps;
        static double started, deadline;

        static FinalPortalP1PlayChecks()
        {
            EditorApplication.update += Tick;
            if (SessionState.GetBool(Pending, false)) Application.logMessageReceived += OnLog;
        }

        [MenuItem("Tools/FYP/Final Portal P1/Run Targeted Play Checks")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorSceneManager.GetActiveScene().path != "Assets/Scenes/MagicWorld_Development.unity")
                throw new InvalidOperationException("Open the development scene in Edit Mode.");
            gate = null;
            testIndex = stage = errors = swaps = 0;
            started = 0;
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }

        static void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
            if (message == "Magic restored!") swaps++;
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying || !SessionState.GetBool(Pending, false)) return;
            try
            {
                if (started == 0)
                {
                    started = EditorApplication.timeSinceStartup;
                    Application.logMessageReceived -= OnLog;
                    Application.logMessageReceived += OnLog;
                }
                if (EditorApplication.timeSinceStartup - started > 45)
                    throw new TimeoutException($"Portal check timed out at case {testIndex}, stage {stage}.");
                if (gate == null)
                {
                    if (EditorApplication.timeSinceStartup - started < 1) return;
                    Initialize();
                }
                if (Time.frameCount < waitFrame) return;

                switch (stage)
                {
                    case 0: SetUpCase(); break;
                    case 1:
                        var hint = InteractionHintManager.Instance.hintText;
                        Check(hint.gameObject.activeInHierarchy && hint.text.Contains(gate.restoration.fragmentProgress.FinalPortalProgress),
                            "the shared E prompt shows the required fragment count");
                        KeyPress(true);
                        Next(2);
                        break;
                    case 2:
                        Check(!legacy.challengePanel.activeSelf, "the quiz remains closed");
                        if (testIndex < Cases.Length - 1)
                        {
                            Check(!gate.restoration.HasStartedRestoration && gate.restoration.brokenPortal.activeSelf &&
                                !gate.restoration.completePortal.activeSelf, "E keeps the arch locked");
                            Check(gate.toast.gameObject.activeInHierarchy &&
                                gate.toast.text.Contains("Fragment of Calling") == !Cases[testIndex].Contains(R1) &&
                                gate.toast.text.Contains("Fragment of Returning Magic") == !Cases[testIndex].Contains(R2),
                                "the existing toast names only the missing required fragments");
                            KeyPress(false);
                            testIndex++;
                            Next(0);
                            break;
                        }
                        Check(gate.restoration.HasStartedRestoration && !gate.CanSharedInteract,
                            "shared E starts restoration and retires its interaction");
                        deadline = EditorApplication.timeSinceStartup + 12;
                        KeyPress(false);
                        Next(3);
                        break;
                    case 3:
                        if (!gate.restoration.completePortal.activeSelf)
                        {
                            Check(EditorApplication.timeSinceStartup < deadline, "restoration completed");
                            break;
                        }
                        gate.restoration.RestoreMagic();
                        KeyPress(true);
                        deadline = EditorApplication.timeSinceStartup + 3;
                        Next(4);
                        break;
                    case 4:
                        if (EditorApplication.timeSinceStartup < deadline) break;
                        Check(!gate.restoration.brokenPortal.activeSelf && gate.restoration.completePortal.activeSelf &&
                            swaps == 1 && !gate.CanSharedInteract && !legacy.challengePanel.activeSelf && errors == 0,
                            "repeat E and direct restoration preserve a single completed swap without Play Mode errors");
                        KeyPress(false);
                        CheckToastReplacement("Fragment of Calling collected  •  1/2");
                        Next(5);
                        break;
                    case 5:
                        if (Time.unscaledTime < deadline) break;
                        Check(gate.toast.gameObject.activeInHierarchy && gate.toast.text == "Fragment of Calling collected  •  1/2",
                            "the previous timeout cannot hide the Ruin 1 reward toast");
                        CheckToastReplacement("Fragment of Returning Magic collected  •  2/2");
                        Next(6);
                        break;
                    case 6:
                        if (Time.unscaledTime < deadline) break;
                        Check(gate.toast.gameObject.activeInHierarchy && gate.toast.text == "Fragment of Returning Magic collected  •  2/2",
                            "the previous timeout cannot hide the Ruin 2 reward toast");
                        deadline = Time.unscaledTime + 2;
                        Next(7);
                        break;
                    case 7:
                        if (gate.toast.gameObject.activeInHierarchy)
                        {
                            Check(Time.unscaledTime < deadline, "reward toast dismisses on its own timeout");
                            break;
                        }
                        Check(errors == 0, "no Play Mode Console errors");
                        Finish("PASS: all six fragment combinations, shared E, missing-fragment toast, direct entry guard, one arch swap and both reward toast timeouts. Play errors=0");
                        break;
                }
            }
            catch (Exception ex) { Finish($"FAIL: case {testIndex}, stage {stage}, Play errors={errors}\n{ex}"); }
        }

        static void Initialize()
        {
            gate = UnityEngine.Object.FindObjectsByType<FinalPortalInteraction>(FindObjectsSortMode.None).Single();
            sceneProgress = gate.restoration.fragmentProgress;
            legacy = gate.GetComponent<PortalInteraction>();
            var hints = InteractionHintManager.Instance;
            var ruin1 = UnityEngine.Object.FindObjectsByType<Ruin01FragmentReward>(FindObjectsSortMode.None).Single();
            var ruin2 = UnityEngine.Object.FindObjectsByType<Ruin02FragmentReveal>(FindObjectsSortMode.None).Single();
            Check(sceneProgress == ruin1.progress && sceneProgress == ruin2.progress &&
                gate.toast == ruin1.confirmationText && gate.toast == ruin2.confirmationText &&
                gate.toast.font != null && gate.toast.font.name.IndexOf("Chomage", StringComparison.OrdinalIgnoreCase) >= 0,
                "portal and both rewards share the scene tracker and existing Chomage toast");
            Check(!legacy.enabled && !legacy.challengePanel.activeSelf &&
                hints.interactionTargets.Count(x => x == gate) == 1 && !hints.interactionTargets.Contains(legacy),
                "the new gate is the only shared portal interaction");
            var player = hints.player;
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.position = gate.transform.position - gate.transform.forward * 3f + Vector3.up * .25f;
            controller.enabled = true;
            Physics.SyncTransforms();
            keyboard = InputSystem.AddDevice<Keyboard>("Final Portal P1 QA Keyboard");
            KeyPress(false);
            Next(0);
        }

        static void SetUpCase()
        {
            if (testProgress != null && testProgress != sceneProgress)
                UnityEngine.Object.Destroy(testProgress.gameObject);
            testProgress = testIndex == Cases.Length - 1 ? sceneProgress :
                new GameObject("P1 QA fragment state").AddComponent<FragmentProgressTracker>();
            foreach (string id in Cases[testIndex]) Check(testProgress.TryCollect(id), "test ID was unique");
            gate.restoration.fragmentProgress = testProgress;
            int requiredCount = Cases[testIndex].Count(id => id == R1 || id == R2);
            Check(testProgress.FinalPortalProgress == requiredCount + "/2", "Ruin03 never counts toward portal progress");
            if (requiredCount < 2)
            {
                gate.restoration.RestoreMagic();
                Check(!gate.restoration.HasStartedRestoration, "direct restoration is guarded");
            }
            Next(1);
        }

        static void KeyPress(bool down)
        {
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, down ? new KeyboardState(Key.E) : new KeyboardState());
        }

        static void CheckToastReplacement(string rewardMessage)
        {
            FragmentRewardToast.Show(gate.toast, "Missing fragments", .1f);
            FragmentRewardToast.Show(gate.toast, rewardMessage, 1.4f);
            deadline = Time.unscaledTime + .35f;
        }

        static void Next(int next)
        {
            stage = next;
            waitFrame = Time.frameCount + 3;
        }

        static void Check(bool ok, string reason)
        {
            if (!ok) throw new InvalidOperationException(reason);
        }

        static void Finish(string result)
        {
            SessionState.SetBool(Pending, false);
            Application.logMessageReceived -= OnLog;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            Directory.CreateDirectory("Temp/FinalPortalP1");
            File.WriteAllText("Temp/FinalPortalP1/PlayChecks.txt", result);
            if (result.StartsWith("PASS")) Debug.Log(result); else Debug.LogError(result);
            EditorApplication.isPlaying = false;
        }
    }
}
