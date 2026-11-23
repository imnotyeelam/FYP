using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Fyp.Development.Recursion.Editor
{
    [InitializeOnLoad]
    public static class Ruin02FarmH3Checks
    {
        const string Key = "FarmH3.QA";
        static Ruin02HarvestConsole console;
        static Keyboard keyboard;
        static double started;
        static int stage, frame, errors;
        static Rect originalViewport;
        static float originalFov;
        static Ruin02FarmH3Checks() { EditorApplication.update += Tick; }
        [MenuItem("Tools/FYP/Ruin 2/Magical Farm H3/Run Targeted Play Checks")]
        public static void Run()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Begin in Edit Mode.");
            Check(Ruin02HarvestConsole.Matches(1, " ((n) == (0)) ") &&
                Ruin02HarvestConsole.Matches(1, "(0==n)") && Ruin02HarvestConsole.Matches(2, "((n) - (1))") &&
                Ruin02HarvestConsole.Matches(3, "(hasCrop( (row), (n) ))") &&
                !Ruin02HarvestConsole.Matches(1, "n=0") && !Ruin02HarvestConsole.Matches(2, "n+1") &&
                !Ruin02HarvestConsole.Matches(3, "collectCrop(row,n)"), "Expression validation.");
            console = null; keyboard = null; stage = errors = 0; started = 0;
            SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
        }
        static void Log(string s, string t, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++; }
        static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
            try
            {
                if (started == 0) { started = EditorApplication.timeSinceStartup; Application.logMessageReceived += Log; return; }
                Check(EditorApplication.timeSinceStartup - started < 65, "Timed out at stage " + stage);
                if (console == null)
                {
                    if (EditorApplication.timeSinceStartup - started < 1) return;
                    console = UnityEngine.Object.FindObjectsByType<Ruin02HarvestConsole>(FindObjectsSortMode.None).Single();
                    var cc = console.player.GetComponent<CharacterController>();
                    var camera = console.cameraFollow.GetComponent<Camera>();
                    originalViewport = camera.rect;
                    originalFov = camera.fieldOfView;
                    cc.enabled = false;
                    console.player.position = console.transform.position + Vector3.back * .6f + Vector3.up * .12f;
                    cc.enabled = true; Physics.SyncTransforms();
                    keyboard = InputSystem.AddDevice<Keyboard>("Farm H3 QA"); Press(false); Next(1); return;
                }
                if (Time.frameCount < frame) return;
                var sim = console.simulation;
                switch (stage)
                {
                    case 1: Press(true); Next(2); break;
                    case 2:
                        Check(console.IsOpen && !console.movement.enabled && !console.cameraFollow.enabled &&
                            !CursorManager.allowCursorLock && Cursor.visible, "E opens safe modal.");
                        Check(console.cameraFollow.GetComponent<Camera>().rect.width == .65f &&
                            console.cameraFollow.GetComponent<Camera>().fieldOfView == 66f, "Farm camera reserves room beside panel.");
                        Press(false);
                        console.input.text = "wrong"; console.Submit();
                        Check(!sim.IsRunning && sim.PlotsChecked == 0, "Wrong answer cannot harvest.");
                        console.input.text = "((n)==(0))"; console.Submit(); console.Submit();
                        Check(sim.IsRunning, "Correct row one starts.");
                        keyboard.MakeCurrent();
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(UnityEngine.InputSystem.Key.Escape));
                        Next(3); break;
                    case 3:
                        if (sim.IsRunning) return;
                        if (console.IsOpen) return;
                        Check(sim.CompletedRows == 1 && sim.PlotsChecked == 10 && !sim.IsComplete &&
                            console.movement.enabled && console.cameraFollow.enabled && CursorManager.allowCursorLock,
                            "Queued ESC keeps finished row and restores controls.");
                        Check(console.cameraFollow.GetComponent<Camera>().rect == originalViewport &&
                            console.cameraFollow.GetComponent<Camera>().fieldOfView == originalFov, "Camera viewport and FOV restored.");
                        Press(true); Next(4); break;
                    case 4:
                        Check(console.IsOpen, "E reopens console.");
                        Press(false); console.input.text = "n==0"; console.Submit();
                        Check(!sim.IsRunning && sim.PlotsChecked == 10, "Old row answer cannot repeat a completed row.");
                        console.input.text = "n - (1)"; console.Submit(); Next(5); break;
                    case 5:
                        if (sim.IsRunning) return;
                        Check(sim.CompletedRows == 2 && sim.PlotsChecked == 20 && sim.StackDepth == 0, "Row two finishes.");
                        console.RequestClose(); Press(true); Next(6); break;
                    case 6:
                        Check(console.IsOpen, "E opens final challenge.");
                        Press(false); console.input.text = "hasCrop(row, n)"; console.Submit(); Next(7); break;
                    case 7:
                        if (sim.IsRunning) return;
                        Next(8); break;
                    case 8:
                        Check(sim.IsComplete && sim.CompletedRows == 3 && sim.PlotsChecked == 30 &&
                            sim.CropsHarvested == 15 && sim.StackDepth == 0 && console.state.text.Contains("Magical Farm Completed!"),
                            "Three decisions finish all harvesting.");
                        console.Submit(); console.RequestClose();
                        Check(!console.IsOpen && sim.IsComplete && sim.History.Count == 66 && errors == 0, "Completion stays latched.");
                        Finish("PASS H3: E, wrong/equivalent answers, one row per decision, queued close/reopen, 30 plots, 15 crops, empty stack, completion latch, controls/cursor. Play errors=0");
                        break;
                }
            }
            catch (Exception e) { Finish("FAIL " + e); }
        }
        static void Press(bool down) { keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, down ? new KeyboardState(UnityEngine.InputSystem.Key.E) : new KeyboardState()); }
        static void Next(int next) { stage = next; frame = Time.frameCount + 4; }
        static void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException(reason); }
        static void Finish(string result)
        {
            SessionState.SetBool(Key, false); Application.logMessageReceived -= Log;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            Directory.CreateDirectory("Temp/Ruin02FarmH3"); File.WriteAllText("Temp/Ruin02FarmH3/PlayChecks.txt", result);
            if (result.StartsWith("PASS")) Debug.Log(result); else Debug.LogError(result);
            EditorApplication.isPlaying = false;
        }
    }
}
