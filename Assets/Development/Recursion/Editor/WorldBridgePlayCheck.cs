using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fyp.Development.Recursion.Editor
{
    // Explicitly invoked QA only; never runs automatically on an ordinary Play session.
    [InitializeOnLoad]
    public static class WorldBridgePlayCheck
    {
        private const string Request = "FYP.WorldBridgePlayCheck";
        private static RuinRecursionVisualizer visualizer;
        private static RecursionTrace trace;
        private static Dictionary<Behaviour, bool> originalEnabled;
        private static int index, assertions;
        private static double stepStarted, started;
        private static bool captured;

        static WorldBridgePlayCheck() { EditorApplication.update += Tick; }

        [MenuItem("Tools/FYP/Ruin 1/Run Presentation Play Check")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorSceneManager.GetActiveScene().path != "Assets/Scenes/MagicWorld_Development.unity")
                throw new InvalidOperationException("Run from the development scene in Edit Mode.");
            SessionState.SetBool(Request, true);
            EditorApplication.isPlaying = true;
        }

        private static void Check(bool yes, string message)
        {
            assertions++;
            if (!yes) throw new InvalidOperationException("World bridge Play check: " + message);
        }

        private static void Verify(int checkpoint)
        {
            var step = trace.Steps[checkpoint];
            Check(visualizer.panelView.progress.text.StartsWith("Checkpoint " + (checkpoint + 1) + " / 22"), "UI checkpoint");
            var bridge = visualizer.observationBridge;
            Check(bridge.stones.Length == 3, "three persistent stones");
            for (int i = 0; i < 3; i++)
            {
                var stone = bridge.stones[i];
                bool placed = i < step.ActionCount;
                var pose = placed ? stone.repairedPose : stone.brokenPose;
                Check((stone.transform.position - pose.position).sqrMagnitude < .000001f, "stone pose");
                Check(stone.crossingCollider.enabled == placed, "crossing collider");
            }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Request, false) || !EditorApplication.isPlaying) return;
            try
            {
                if (visualizer == null)
                {
                    visualizer = Object.FindObjectsByType<RuinRecursionVisualizer>(FindObjectsSortMode.None)
                        .FirstOrDefault(v => v.CanSharedInteract && v.observationBridge != null);
                    if (visualizer == null) return; // Wait for runtime Start and CursorManager.
                    assertions = 0; index = 0; trace = BridgeRecursionLesson.Create();
                    originalEnabled = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Where(b => b is PlayerMovement || b is CameraFollow || b is RuinEntrance || b is KnowledgeInteractable || b is PortalInteraction)
                        .ToDictionary(b => (Behaviour)b, b => b.enabled);
                    visualizer.Open();
                    Check(visualizer.observationCamera.enabled && !visualizer.gameplayCamera.enabled, "camera handoff");
                    Check(Cursor.lockState == CursorLockMode.None && Cursor.visible && !CursorManager.allowCursorLock, "modal cursor");
                    Check(!InteractionHintManager.Instance.hintText.gameObject.activeInHierarchy, "shared hint hidden");
                    Check(originalEnabled.Keys.All(b => !b.enabled), "movement, camera controls and interactions paused");
                    started = stepStarted = EditorApplication.timeSinceStartup;
                    captured = false;
                    return;
                }
                double age = EditorApplication.timeSinceStartup - stepStarted;
                if (!captured && age > .55)
                {
                    captured = true;
                    if (index == 0 || index == 10 || index == 17)
                    {
                        Directory.CreateDirectory("Temp/WorldBridgeChecks");
                        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/WorldBridgeChecks/Checkpoint_" + (index + 1) + ".png"));
                    }
                    return;
                }
                if (age < .95) return;
                Verify(index);
                if (index < trace.Steps.Count - 1)
                {
                    index++;
                    visualizer.panelView.nextButton.onClick.Invoke();
                    stepStarted = EditorApplication.timeSinceStartup; captured = false;
                    return;
                }
                for (int i = 20; i >= 0; i--) { visualizer.panelView.previousButton.onClick.Invoke(); Verify(i); }
                foreach (int i in new[] { 21, 2, 10, 13, 0, 17 })
                {
                    visualizer.panelView.checkpointSlider.value = i;
                    Verify(i);
                }
                visualizer.panelView.replayButton.onClick.Invoke(); Verify(0);
                visualizer.panelView.nextButton.onClick.Invoke();
                visualizer.panelView.nextButton.onClick.Invoke();
                Check(!visualizer.observationBridge.stones[0].crossingCollider.enabled, "moving stone is not crossable");
                visualizer.panelView.closeButton.onClick.Invoke(); Verify(2);
                Check(!visualizer.observationCamera.enabled && visualizer.gameplayCamera.enabled, "camera restored");
                Check(!visualizer.panelView.gameObject.activeSelf && CursorManager.allowCursorLock, "modal closed");
                Check(originalEnabled.All(pair => pair.Key.enabled == pair.Value), "original component enabled states restored");
                Debug.Log("PASS: World bridge Play check: " + assertions + " assertions; animated Next, Previous, Replay, slider jumps, camera/cursor, modal interaction lock and Close mid-motion. Screenshots in Temp/WorldBridgeChecks.");
                SessionState.SetBool(Request, false);
                EditorApplication.isPlaying = false;
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (visualizer != null) visualizer.Close();
                SessionState.SetBool(Request, false);
                EditorApplication.isPlaying = false;
            }
        }
    }
}
