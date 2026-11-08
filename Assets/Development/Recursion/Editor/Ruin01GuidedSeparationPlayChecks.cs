using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using InputKey = UnityEngine.InputSystem.Key;

namespace Fyp.Development.Recursion.Editor
{
    /// <summary>Exercises the real shared-hint E-key dispatch for the Ruin 1 lesson gate.</summary>
    [InitializeOnLoad]
    public static class Ruin01GuidedSeparationPlayChecks
    {
        const string SessionKey = "FYP.Ruin01.GuidedSeparationCheck";
        static BridgePracticeController practice;
        static Ruin01GuidedSimulationUI guided;
        static global::AncientSpellbookUI book;
        static CharacterController controller;
        static Keyboard keyboard;
        static int stage, assertions;
        static double next;

        static Ruin01GuidedSeparationPlayChecks() => EditorApplication.update += Tick;

        [MenuItem("Tools/FYP/Ruin 1/Guided Separation/Run Shared E-Key Play Check")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run this check from Edit Mode.");
            practice = null; guided = null; book = null; controller = null; keyboard = null; stage = 0; assertions = 0; next = 0;
            SessionState.SetBool(SessionKey, true);
            EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(SessionKey, false) || !EditorApplication.isPlaying) return;
            try
            {
                if (next > EditorApplication.timeSinceStartup) return;
                if (practice == null)
                {
                    practice = UnityEngine.Object.FindObjectsByType<BridgePracticeController>(FindObjectsSortMode.None).Single();
                    guided = UnityEngine.Object.FindObjectsByType<Ruin01GuidedSimulationUI>(FindObjectsSortMode.None).Single();
                    book = UnityEngine.Object.FindObjectsByType<global::AncientSpellbookUI>(FindObjectsSortMode.None).Single();
                    controller = UnityEngine.Object.FindObjectsByType<CharacterController>(FindObjectsSortMode.None).Single(x => x.gameObject.name == "Player");
                    Check(!practice.GuidedLessonCompleted && !guided.IsOpen && !practice.IsOpen, "fresh session begins locked");
                    var wasEnabled = controller.enabled;
                    controller.enabled = false;
                    controller.transform.position = practice.interactionPoint.position + Vector3.down * .27f;
                    controller.enabled = wasEnabled;
                    Physics.SyncTransforms();
                    keyboard = InputSystem.AddDevice<Keyboard>("Ruin01 Guided QA Keyboard");
                    keyboard.MakeCurrent();
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    stage = 1; Wait(.5); return;
                }
                switch (stage)
                {
                    case 1:
                        Check(InteractionHintManager.Instance.hintText.gameObject.activeInHierarchy &&
                              InteractionHintManager.Instance.hintText.text == "Press [E] to Study the Guided Simulation", "nearest shared target shows guided-study hint");
                        Press(InputKey.E); stage = 2; Wait(.5); return;
                    case 2:
                        Check(guided.IsOpen && !book.IsBookOpen && !practice.IsOpen, "real shared E opens only standalone guided simulation");
                        Check(!controller.GetComponent<PlayerMovement>().enabled && !CursorManager.allowCursorLock, "guided modal owns movement and cursor");
                        Press(InputKey.Escape); stage = 3; Wait(.5); return;
                    case 3:
                        Check(!guided.IsOpen && !practice.GuidedLessonCompleted, "Escape closes early without unlocking practice");
                        Press(InputKey.E); stage = 4; Wait(.5); return;
                    case 4:
                        Check(guided.IsOpen && !practice.IsOpen, "second real shared E reopens standalone simulation");
                        guided.simulation.JumpToStep(guided.simulation.Playback.Trace.Steps.Count - 1);
                        Check(!guided.GuidedLessonCompleted && guided.finishButton.gameObject.activeInHierarchy, "final checkpoint exposes Finish without unlocking");
                        guided.Finish(); stage = 5; Wait(.5); return;
                    case 5:
                        Check(!guided.IsOpen && practice.GuidedLessonCompleted, "Finish persists lesson completion and closes modal");
                        Press(InputKey.E); stage = 6; Wait(.5); return;
                    case 6:
                        Check(practice.IsOpen && !book.IsBookOpen && !guided.IsOpen, "next real shared E opens only bridge practice");
                        practice.Close();
                        Check(practice.GuidedLessonCompleted, "completion survives practice close in this session");
                        Pass(); return;
                }
            }
            catch (Exception error) { Fail(error); }
        }

        static void Press(InputKey key)
        {
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        }
        static void Wait(double seconds) => next = EditorApplication.timeSinceStartup + seconds;
        static void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException("Ruin 1 guided separation: " + message);
        }
        static void Pass()
        {
            Debug.Log("PASS Ruin 1 standalone guided simulation shared E-key check: " + assertions + " assertions.");
            Cleanup();
        }
        static void Fail(Exception error)
        {
            Debug.LogException(error);
            Cleanup();
        }
        static void Cleanup()
        {
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            SessionState.SetBool(SessionKey, false);
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        }
    }
}
