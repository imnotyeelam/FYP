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
    public static class Ruin02FarmH4Checks
    {
        const string Key = "FarmH4.Test";
        static Ruin02FarmAccess entry, exit;
        static Keyboard keyboard;
        static int stage, frame, errors;
        static double started;
        static Ruin02FarmH4Checks() { EditorApplication.update += Tick; }
        [MenuItem("Tools/FYP/Ruin 2/Magical Farm H4/Run Targeted Play Checks")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Begin in Edit Mode.");
            entry = exit = null; keyboard = null; stage = errors = 0; started = 0;
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
                Check(EditorApplication.timeSinceStartup - started < 100, "Timeout at stage " + stage);
                if (entry == null)
                {
                    if (EditorApplication.timeSinceStartup - started < 1) return;
                    var gates = UnityEngine.Object.FindObjectsByType<Ruin02FarmAccess>(FindObjectsSortMode.None);
                    entry = gates.Single(g => !g.isFarmReturn); exit = gates.Single(g => g.isFarmReturn);
                    Check(InteractionHintManager.Instance.interactionTargets.Count(x => x == entry) == 1 &&
                        InteractionHintManager.Instance.interactionTargets.Count(x => x == exit) == 1, "Unique registrations.");
                    keyboard = InputSystem.AddDevice<Keyboard>("Farm H4 QA");
                    Place(entry.transform.position); Press(false); Next(1); return;
                }
                if (Time.frameCount < frame) return;
                var console = entry.console;
                var sim = console.simulation;
                switch (stage)
                {
                    case 1:
                        Check(!entry.StaffCompleted && entry.CanSharedInteract, "Fresh entrance is available but locked.");
                        Press(true); Next(2); break;
                    case 2:
                        Check(Vector3.Distance(entry.teleport.player.position, entry.transform.position) < .5f &&
                            entry.toast.gameObject.activeInHierarchy && entry.toast.text.Contains("four Staff"),
                            "Locked E shows toast without teleport.");
                        Press(false);
                        entry.practice.Open(); Next(3); break;
                    case 3:
                        if (!entry.practice.IsSolved || entry.restoration.IsPlaying || entry.practice.Session.Current.Frames.Count != 0)
                            Check(!entry.StaffCompleted, "Access stays locked while staff trace is running.");
                        if (entry.restoration.IsPlaying) { entry.restoration.SkipCurrentSequence(); return; }
                        if (entry.practice.Session.Advancing) { entry.practice.Session.TryAdvanceApprovedStep(); return; }
                        if (!entry.practice.IsSolved)
                        {
                            var q = entry.practice.Session.Question;
                            Check(q != null && entry.practice.Submit(q.Expected).Status == Ruin02AnswerStatus.Correct, "Staff answer accepted.");
                            return;
                        }
                        entry.practice.Close(); Next(4); break;
                    case 4:
                        Check(entry.StaffCompleted && entry.restoration.RestoredCount == 4 &&
                            entry.practice.Session.Current.Frames.Count == 0, "All exact staff completion flags unlock entrance.");
                        Press(true); Next(5); break;
                    case 5:
                        At(entry.destination); Press(false);
                        Check(InteractionHintManager.Instance.hintText.text == console.SharedHint, "Entry selects console, not return.");
                        Press(true); Next(6); break;
                    case 6:
                        Check(console.IsOpen && !exit.CanSharedInteract, "Console modal blocks teleport.");
                        Press(false); console.input.text = "n == 0"; console.Submit(); console.RequestClose(); Next(7); break;
                    case 7:
                        if (sim.IsRunning || console.IsOpen) return;
                        Check(sim.CompletedRows == 1, "One row retained.");
                        Place(exit.transform.position); Next(8); break;
                    case 8:
                        Check(InteractionHintManager.Instance.hintText.text == exit.SharedHint, "Farm return wins only at its marker.");
                        Press(true); Next(9); break;
                    case 9:
                        At(exit.destination); Press(false); Check(sim.CompletedRows == 1, "Return before completion preserves row.");
                        Press(true); Next(10); break;
                    case 10:
                        At(entry.destination); Press(false); Press(true); Next(11); break;
                    case 11:
                        Check(console.IsOpen && sim.CompletedRows == 1, "Re-entry resumes second row.");
                        Press(false); console.input.text = "n-1"; console.Submit(); Next(12); break;
                    case 12:
                        if (sim.IsRunning) return;
                        console.input.text = "hasCrop(row,n)"; console.Submit(); Next(13); break;
                    case 13:
                        if (sim.IsRunning) return;
                        Check(sim.IsComplete && sim.CompletedRows == 3 && sim.PlotsChecked == 30 &&
                            sim.CropsHarvested == 15 && sim.StackDepth == 0, "Farm completed normally.");
                        console.RequestClose(); Place(exit.transform.position); Next(14); break;
                    case 14:
                        Press(true); Next(15); break;
                    case 15:
                        At(exit.destination); Press(false); Press(true); Next(16); break;
                    case 16:
                        At(entry.destination);
                        Check(sim.IsComplete && sim.History.Count == 66 && errors == 0, "Completed farm survives return/re-entry with zero errors.");
                        Finish("PASS H4: locked E toast; real staff progression and four flags; shared E entrance/return; console separation/modal safety; row preservation; completed return/re-entry; controls/camera/cursor. Play errors=0");
                        break;
                }
            }
            catch (Exception e) { Finish("FAIL stage " + stage + " " + e); }
        }
        static void At(Transform destination)
        {
            var p = entry.teleport.player;
            Check(Vector3.ProjectOnPlane(p.position - destination.position, Vector3.up).magnitude < .25f &&
                p.GetComponent<CharacterController>().enabled && entry.console.movement.enabled &&
                entry.console.cameraFollow.enabled && CursorManager.allowCursorLock &&
                Cursor.lockState == CursorLockMode.Locked &&
                Vector3.Distance(entry.console.cameraFollow.transform.position, p.position) < 10f, "Safe teleport/restored state.");
        }
        static void Place(Vector3 point)
        {
            var p = entry.teleport.player; var cc = p.GetComponent<CharacterController>();
            cc.enabled = false; p.position = point; cc.enabled = true;
            p.GetComponent<PlayerMovement>().ResetVerticalVelocity(); Physics.SyncTransforms();
        }
        static void Press(bool down)
        {
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, down ? new KeyboardState(UnityEngine.InputSystem.Key.E) : new KeyboardState());
        }
        static void Next(int next) { stage = next; frame = Time.frameCount + 5; }
        static void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException(reason); }
        static void Finish(string result)
        {
            SessionState.SetBool(Key, false); Application.logMessageReceived -= Log;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            Directory.CreateDirectory("Temp/Ruin02FarmH4"); File.WriteAllText("Temp/Ruin02FarmH4/PlayChecks.txt", result);
            if (result.StartsWith("PASS")) Debug.Log(result); else Debug.LogError(result);
            EditorApplication.isPlaying = false;
        }
    }
}
