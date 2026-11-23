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
    public static class Ruin02FarmH5Checks
    {
        const string Key = "FarmH5.Test";
        static Ruin02FragmentReveal reward;
        static Ruin02FarmAccess entry, exit;
        static Ruin02ReturnGate hub;
        static FinalPortalInteraction portal;
        static Keyboard keyboard;
        static int stage, frame, errors, reveals, collections;
        static double started;
        static Vector3 fragmentPosition;
        static Ruin02FarmH5Checks() { EditorApplication.update += Tick; }

        [MenuItem("Tools/FYP/Ruin 2/Magical Farm H5/Run Targeted Play Checks")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorSceneManager.GetActiveScene().path != "Assets/Scenes/MagicWorld_Development.unity")
                throw new InvalidOperationException("Begin in development scene Edit Mode.");
            reward = null; keyboard = null; stage = errors = reveals = collections = 0; started = 0;
            SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
        }
        static void Log(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
            if (message.StartsWith("Ruin 2 Fragment of Returning Magic revealed after")) reveals++;
        }
        static void Collected(string id) { if (id == FragmentProgressTracker.Ruin02) collections++; }
        static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
            try
            {
                if (started == 0) { started = EditorApplication.timeSinceStartup; Application.logMessageReceived += Log; return; }
                Check(EditorApplication.timeSinceStartup - started < 110, "Timeout at stage " + stage);
                if (reward == null)
                {
                    if (EditorApplication.timeSinceStartup - started < 1) return;
                    reward = UnityEngine.Object.FindObjectsByType<Ruin02FragmentReveal>(FindObjectsSortMode.None).Single();
                    var gates = UnityEngine.Object.FindObjectsByType<Ruin02FarmAccess>(FindObjectsSortMode.None);
                    entry = gates.Single(x => !x.isFarmReturn); exit = gates.Single(x => x.isFarmReturn);
                    hub = UnityEngine.Object.FindFirstObjectByType<Ruin02ReturnGate>();
                    portal = UnityEngine.Object.FindFirstObjectByType<FinalPortalInteraction>();
                    Check(reward.farm == entry.console.simulation && hub.progress == reward.progress &&
                        portal.restoration.fragmentProgress == reward.progress, "Existing farm and shared tracker references.");
                    Check(InteractionHintManager.Instance.interactionTargets.Count(x => x == reward) == 1, "One reward interaction.");
                    fragmentPosition = reward.fragmentVisual.transform.position;
                    reward.progress.FragmentCollected += Collected;
                    keyboard = InputSystem.AddDevice<Keyboard>("Farm H5 QA");
                    Locked(); reward.practice.Open(); Next(1); return;
                }
                if (Time.frameCount < frame) return;
                var console = entry.console;
                var sim = reward.farm;
                switch (stage)
                {
                    case 1:
                        Locked();
                        if (entry.restoration.IsPlaying) { entry.restoration.SkipCurrentSequence(); return; }
                        if (reward.practice.Session.Advancing) { reward.practice.Session.TryAdvanceApprovedStep(); return; }
                        if (!reward.practice.IsSolved)
                        {
                            var q = reward.practice.Session.Question;
                            Check(q != null && reward.practice.Submit(q.Expected).Status == Ruin02AnswerStatus.Correct, "Staff trace answer.");
                            return;
                        }
                        reward.practice.Close(); Place(hub.InteractionOrigin.position); Next(2); break;
                    case 2:
                        Check(entry.StaffCompleted && !sim.IsComplete, "Staff alone is complete."); Locked();
                        Press(true); Next(3); break;
                    case 3:
                        Check(Vector3.Distance(reward.player.position, hub.InteractionOrigin.position) < .5f &&
                            hub.lockedMessage.text == "Collect the magic fragment first!" &&
                            hub.lockedMessage.gameObject.activeInHierarchy, "Hub E locked before collection.");
                        Press(false); Place(entry.transform.position); Next(4); break;
                    case 4: Press(true); Next(5); break;
                    case 5:
                        At(entry.destination); Press(false); Next(6); break;
                    case 6: Press(true); Next(7); break;
                    case 7:
                        Check(console.IsOpen, "Shared E opened farm console."); Press(false);
                        console.input.text = "n == 0"; console.Submit(); console.RequestClose(); Next(8); break;
                    case 8:
                        Locked();
                        if (sim.IsRunning || console.IsOpen) return;
                        Check(sim.CompletedRows == 1 && !sim.IsComplete, "Partial farm remains locked.");
                        Place(exit.transform.position); Next(9); break;
                    case 9: Press(true); Next(10); break;
                    case 10:
                        At(exit.destination); Locked(); Press(false); Next(11); break;
                    case 11: Press(true); Next(12); break;
                    case 12: At(entry.destination); Press(false); Next(13); break;
                    case 13: Press(true); Next(14); break;
                    case 14:
                        Check(console.IsOpen && sim.CompletedRows == 1, "Re-entry retains completed row.");
                        Press(false); console.input.text = "n - 1"; console.Submit(); Next(15); break;
                    case 15:
                        Locked(); if (sim.IsRunning) return;
                        Check(sim.CompletedRows == 2, "Two rows remain insufficient.");
                        console.input.text = "hasCrop(row,n)"; console.Submit(); Next(16); break;
                    case 16:
                        if (sim.IsRunning) { Locked(); return; }
                        Check(sim.IsComplete && sim.CompletedRows == 3 && sim.PlotsChecked == 30 &&
                            sim.CropsHarvested == 15 && sim.StackDepth == 0, "Authoritative farm completion and totals.");
                        Check(reward.IsRevealed && reward.fragmentVisual.activeInHierarchy && reveals == 1 &&
                            reward.fragmentVisual.transform.position == fragmentPosition, "One reveal at original position.");
                        console.RequestClose(); Place(exit.transform.position); Next(17); break;
                    case 17: Press(true); Next(18); break;
                    case 18:
                        At(exit.destination); Press(false); Place(hub.InteractionOrigin.position); Next(19); break;
                    case 19: Press(true); Next(20); break;
                    case 20:
                        Check(Vector3.Distance(reward.player.position, hub.InteractionOrigin.position) < .5f &&
                            !reward.FragmentCollected && hub.lockedMessage.text == "Collect the magic fragment first!",
                            "Full farm still requires collecting before Hub return.");
                        Press(false); Place(reward.interactionPoint.position); Next(21); break;
                    case 21:
                        Check(reward.CanSharedInteract && InteractionHintManager.Instance.hintText.text == reward.SharedHint,
                            "Existing fragment shared E target selected."); Press(true); Next(22); break;
                    case 22:
                        Check(reward.FragmentCollected && !reward.fragmentVisual.activeInHierarchy && collections == 1,
                            "Real E registers Ruin02 once and hides fragment.");
                        Press(false); reward.InteractFromShared();
                        Check(!reward.progress.TryCollect(FragmentProgressTracker.Ruin02) && !reward.CanSharedInteract,
                            "Repeated direct collection cannot duplicate reward."); Next(23); break;
                    case 23: Press(true); Next(24); break;
                    case 24:
                        Check(collections == 1 && reveals == 1 && !reward.fragmentVisual.activeInHierarchy,
                            "Repeated E cannot recollect or reveal.");
                        Press(false); Place(hub.InteractionOrigin.position); Next(25); break;
                    case 25: Press(true); Next(26); break;
                    case 26:
                        At(hub.returnEntrance.destination); Press(false);
                        Check(reward.FragmentCollected && sim.IsComplete && sim.CompletedRows == 3 && collections == 1,
                            "Hub return preserves both session states.");
                        Check(reward.progress.HasFragment("Ruin02") && !reward.progress.HasFinalPortalFragments &&
                            portal.SharedHint.Contains("1/2"), "Portal recognizes actual Ruin02 reward; Ruin01 still required.");
                        // Supply only the other ruin's fragment as a session-local portal fixture.
                        reward.progress.TryCollect(FragmentProgressTracker.Ruin01);
                        Check(reward.progress.HasFinalPortalFragments && portal.SharedHint.Contains("2/2"),
                            "Unchanged two-fragment portal accepts the collected Ruin02.");
                        Place(portal.transform.position - portal.transform.forward * 3f + Vector3.up * .25f); Next(27); break;
                    case 27: Press(true); Next(28); break;
                    case 28:
                        Check(portal.restoration.HasStartedRestoration && collections == 1 && reveals == 1 && errors == 0,
                            "Shared E starts existing portal restoration with no Play errors.");
                        Finish("PASS H5: staff-only/partial farm/unfinished return locked; full 30/15/empty-stack farm reveals original fragment exactly once; real E collects Ruin02 once; Hub E locked before collection and works after; row/fragment session state retained; unchanged Final Portal accepts reward. Play errors=0");
                        break;
                }
            }
            catch (Exception e) { Finish("FAIL H5 stage " + stage + " " + e); }
        }
        static void Locked()
        {
            Check(!reward.IsRevealed && !reward.fragmentVisual.activeInHierarchy && !reward.CanSharedInteract &&
                !reward.FragmentCollected && reveals == 0 && collections == 0, "Incomplete progression keeps reward hidden/non-interactable.");
            reward.InteractFromShared();
            Check(!reward.FragmentCollected, "Direct collection also denied.");
        }
        static void Place(Vector3 point)
        {
            var cc = reward.player.GetComponent<CharacterController>();
            var delta = point - reward.player.position;
            cc.enabled = false; reward.player.position = point; cc.enabled = true;
            reward.player.GetComponent<PlayerMovement>().ResetVerticalVelocity();
            entry.console.cameraFollow.transform.position += delta; Physics.SyncTransforms();
        }
        static void At(Transform point)
        { Check(Vector3.ProjectOnPlane(reward.player.position - point.position, Vector3.up).magnitude < .4f, "Expected teleport destination."); }
        static void Press(bool down)
        {
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, down ? new KeyboardState(UnityEngine.InputSystem.Key.E) : new KeyboardState());
        }
        static void Next(int next) { stage = next; frame = Time.frameCount + 5; }
        static void Check(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
        static void Finish(string result)
        {
            SessionState.SetBool(Key, false); Application.logMessageReceived -= Log;
            if (reward != null) reward.progress.FragmentCollected -= Collected;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            Directory.CreateDirectory("Temp/Ruin02FarmH5"); File.WriteAllText("Temp/Ruin02FarmH5/PlayChecks.txt", result);
            if (result.StartsWith("PASS")) Debug.Log(result); else Debug.LogError(result);
            EditorApplication.isPlaying = false;
        }
    }
}
