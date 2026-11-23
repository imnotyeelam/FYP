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
    public static class Ruin01JumpJ1Checks
    {
        const string Key = "Ruin01.J1.Checks";
        static Ruin01MagicJumpController jump;
        static RuinDeveloperPanel dev;
        static Ruin01FragmentReward reward;
        static Ruin01GuidedSimulationUI guided;
        static Keyboard keyboard;
        static int stage, frame, errors;
        static double began;
        static J1MotionProbe probe;
        static Ruin01JumpJ1Checks() { EditorApplication.update += Tick; }
        [MenuItem("Tools/FYP/Ruin 1/Jump J1/Run Targeted Play Checks")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Begin in Edit Mode.");
            jump = null; keyboard = null; began = 0; stage = errors = 0;
            SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
        }
        static void Log(string m, string t, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++; }
        static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
            try
            {
                if (began == 0) { began = EditorApplication.timeSinceStartup; Application.logMessageReceived += Log; return; }
                Check(EditorApplication.timeSinceStartup - began < 180, "Timeout stage " + stage);
                if (jump == null)
                {
                    if (EditorApplication.timeSinceStartup - began < 1) return;
                    jump = Ruin01JumpJ1Tools.Jump;
                    dev = UnityEngine.Object.FindFirstObjectByType<RuinDeveloperPanel>();
                    reward = UnityEngine.Object.FindFirstObjectByType<Ruin01FragmentReward>();
                    guided = UnityEngine.Object.FindFirstObjectByType<Ruin01GuidedSimulationUI>();
                    keyboard = InputSystem.AddDevice<Keyboard>("J1 QA");
                    probe = jump.player.gameObject.AddComponent<J1MotionProbe>(); probe.jump = jump;
                    Check(jump.interactionAnchor.name == "ReturnMagicRock" && jump.SharedInteractionRange == 2.5f &&
                        Mathf.Approximately(jump.secondsPerLeap,.8f) && Mathf.Approximately(jump.platformPauseSeconds,.9f), "Anchor/range/timings.");
                    Place(dev.ruin01JumpStart.position); Press(false); Next(1); return;
                }
                if (Time.frameCount < frame) return;
                Check(probe.failure == null, probe.failure);
                switch(stage)
                {
                    case 1:
                        Check(!jump.CanSharedInteract && !jump.bridgePractice.IsComplete, "Normal prerequisites locked at rock.");
                        dev.Open(); dev.UnlockRuin01JumpForTesting(); dev.TeleportToRuin01JumpStart(); dev.Close(); Next(2); break;
                    case 2:
                        Check(jump.CanSharedInteract && !reward.ChallengeSolved && !jump.bridgePractice.IsComplete,
                            "Developer teleport/unlock enables rock without rewards.");
                        Press(true); Next(3); break;
                    case 3:
                        Check(jump.IsOpen, "Actual E opens jump at rock."); Press(false); jump.Close();
                        Place(jump.jumpStartPoint.position); Next(4); break;
                    case 4:
                        Check(!jump.CanSharedInteract, "Elsewhere on Flying Stone not eligible."); Press(true); Next(5); break;
                    case 5:
                        Check(!jump.IsOpen, "E elsewhere cannot open jump."); Press(false);
                        Place(dev.ruin01JumpStart.position); Next(6); break;
                    case 6: Press(true); Next(7); break;
                    case 7:
                        Check(jump.IsOpen, "Rock retry E."); Press(false); StartAttempt(1); Next(8); break;
                    case 8:
                        if(jump.IsPlaying) return;
                        Check(jump.IsOpen && Vector3.Distance(jump.player.position,jump.jumpStartPoint.position)<.1f &&
                            jump.magicTriggers.All(x=>!x.IsVisible) && !jump.JumpChallengeCompleted && !reward.ChallengeSolved,
                            "Insufficient spell teleports safely, hides platforms and grants no reward.");
                        StartAttempt(6); Next(9); break;
                    case 9:
                        if(jump.IsPlaying)return;
                        Check(jump.IsOpen && Vector3.Distance(jump.player.position,jump.jumpStartPoint.position)<.1f &&
                            jump.magicTriggers.All(x=>!x.IsVisible), "Overshoot resets directly and safely.");
                        jump.Close(); jump.ResetForDeveloperTesting(); jump.SetDeveloperPrerequisiteOverride(false);
                        Place(dev.ruin01JumpStart.position);
                        Check(!jump.CanSharedInteract, "Removing test override restores prerequisites.");
                        guided.Open(); guided.simulation.JumpToStep(guided.simulation.Playback.Trace.Steps.Count-1); guided.Finish();
                        jump.bridgePractice.Open(); Next(10); break;
                    case 10:
                        if(jump.bridgePractice.IsComplete) { jump.bridgePractice.Close(); Next(11); break; }
                        if(jump.bridgePractice.IsBusy || jump.bridgePractice.CurrentDecision==null ||
                            jump.bridgePractice.CheckpointIndex!=jump.bridgePractice.CurrentDecision.CheckpointIndex)return;
                        var q=jump.bridgePractice.CurrentDecision;
                        Check(jump.bridgePractice.SubmitAnswer(q.Id,q.CorrectAnswerId).Accepted,"Bridge decision."); break;
                    case 11:
                        Check(jump.bridgePractice.GuidedLessonCompleted && jump.bridgePractice.IsComplete && jump.CanSharedInteract,
                            "Normal guided/bridge route enables rock."); Press(true); Next(12); break;
                    case 12:
                        Check(jump.IsOpen,"Normal real E interaction."); Press(false); StartAttempt(5); Next(13); break;
                    case 13:
                        if(jump.IsPlaying)return;
                        Check(jump.JumpChallengeCompleted && !jump.IsOpen && jump.PerformedJumps==5 &&
                            jump.AnimationPlayCount==5 && jump.AnimationActivationCount==5 &&
                            Vector3.Distance(jump.player.position,jump.destinations[4].position)<.3f,
                            "Five animations and smooth controller landing.");
                        Check(jump.playerMovement.enabled && jump.characterController.enabled && jump.cameraFollow.enabled &&
                            Cursor.lockState==CursorLockMode.Locked && !jump.animator.applyRootMotion &&
                            jump.magicTriggers.All(x=>!x.IsVisible) && !jump.IsJumpAnimationActive,
                            "Idle, controls, platforms and cursor restored.");
                        Check(reward.ChallengeSolved && reward.RevealCount==1,"Original fragment reward reveals normally.");
                        Place(reward.interactionPoint.position); Next(14); break;
                    case 14: Press(true); Next(15); break;
                    case 15:
                        Check(reward.FragmentCollected && reward.CollectionCount==1 && reward.targetReturn.enabled,"Fragment E collected once.");
                        Press(false); Place(reward.targetReturn.transform.position); Next(16); break;
                    case 16: Press(true); Next(17); break;
                    case 17:
                        Check(Vector3.ProjectOnPlane(jump.player.position-reward.targetReturn.destination.position,Vector3.up).magnitude<.4f,
                            "Existing target-stone Hub return works.");
                        Check(probe.segments==11 && probe.maxPoseForward<.15f && probe.maxBackward<.03f && errors==0,
                            "All 11 segments bounded/in place and zero Console errors.");
                        Finish($"PASS J1: actual E near rock only; normal and Developer prerequisites; n=1/n=6 direct retries; five-leap success, pauses/trail/animation/Idle restoration; fragment E and Hub return. Segments={probe.segments}; max hips forward offset={probe.maxPoseForward:F4}m; max backward root step={probe.maxBackward:F4}m; landing error={probe.maxLandingError:F5}m; Play errors={errors}."); break;
                }
            }
            catch(Exception e){Finish("FAIL J1 stage "+stage+" "+e);}
        }
        static void StartAttempt(int n){jump.input.text=n.ToString();jump.castButton.onClick.Invoke();}
        static void Place(Vector3 p)
        {
            var delta=p-jump.player.position; jump.characterController.enabled=false; jump.player.position=p;
            jump.characterController.enabled=true; jump.playerMovement.ResetVerticalVelocity();
            jump.cameraFollow.transform.position+=delta; Physics.SyncTransforms();
        }
        static void Press(bool down){keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,down?new KeyboardState(UnityEngine.InputSystem.Key.E):new KeyboardState());}
        static void Next(int n){stage=n;frame=Time.frameCount+6;}
        static void Check(bool ok,string why){if(!ok)throw new InvalidOperationException(why);}
        static void Finish(string result)
        {
            SessionState.SetBool(Key,false);Application.logMessageReceived-=Log;
            if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);
            Directory.CreateDirectory("Temp/Ruin01JumpJ1");File.WriteAllText("Temp/Ruin01JumpJ1/PlayChecks.txt",result);
            if(result.StartsWith("PASS"))Debug.Log(result);else Debug.LogError(result);
            EditorApplication.isPlaying=false;
        }
    }

    [DefaultExecutionOrder(10000)]
    public sealed class J1MotionProbe : MonoBehaviour
    {
        public Ruin01MagicJumpController jump;
        public string failure;
        public int segments;
        public float maxPoseForward,maxBackward,maxLandingError;
        bool emittingBefore;
        Vector3 previous;
        TrailRenderer trail;
        void LateUpdate()
        {
            if(jump==null)return;
            trail ??= jump.animator.GetComponentInChildren<TrailRenderer>(true);
            bool emitting=trail!=null && trail.emitting;
            if(emitting)
            {
                if(!emittingBefore)segments++;
                else maxBackward=Mathf.Max(maxBackward,-Vector3.Dot(jump.player.position-previous,jump.player.forward));
                float pose=Vector3.Dot(jump.animator.GetBoneTransform(HumanBodyBones.Hips).position-jump.player.position,jump.player.forward);
                maxPoseForward=Mathf.Max(maxPoseForward,Mathf.Abs(pose));
                if(jump.characterController.enabled || jump.playerMovement.enabled || jump.animator.applyRootMotion)
                    failure="Movement controller/Root Motion active during flight.";
                if(jump.magicTriggers.Any(x=>x.IsVisible))failure="Platform visible during flight.";
            }
            if(jump.magicTriggers.Any(x=>x.IsVisible))
            {
                if(jump.magicTriggers.Count(x=>x.IsVisible)!=1 || emitting)failure="Pause platform/trail synchronization.";
                int i=Array.FindIndex(jump.magicTriggers,x=>x.IsVisible);
                maxLandingError=Mathf.Max(maxLandingError,Vector3.Distance(jump.player.position,jump.destinations[i].position));
                if(jump.characterController.enabled)failure="Gravity not suspended during platform pause.";
            }
            if(!jump.JumpChallengeCompleted && jump.statusText.text.Contains("of 5"))failure="Answer spoiler.";
            previous=jump.player.position;emittingBefore=emitting;
        }
    }
}
