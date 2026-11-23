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
    public static class Ruin01JumpJ2Checks
    {
        const string Key="JumpJ2.QA";
        static Ruin01MagicJumpController jump;
        static Ruin01JumpGuidedSimulation guide;
        static RuinDeveloperPanel dev;
        static Ruin01FragmentReward reward;
        static Keyboard keyboard;
        static int stage,frame,errors;
        static double began,at;
        static Vector3 wizardBefore;
        static Ruin01JumpJ2Checks(){EditorApplication.update+=Tick;}
        [MenuItem("Tools/FYP/Ruin 1/Jump J2/Run Targeted Play Checks")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Begin in Edit Mode.");
            jump=null;keyboard=null;stage=errors=0;began=0;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        static void Log(string m,string t,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
            try
            {
                if(began==0){began=EditorApplication.timeSinceStartup;Application.logMessageReceived+=Log;return;}
                Check(EditorApplication.timeSinceStartup-began<150,"Timeout stage "+stage);
                if(jump==null)
                {
                    if(EditorApplication.timeSinceStartup-began<1)return;
                    jump=Ruin01JumpJ1Tools.Jump;guide=jump.jumpGuide;
                    dev=UnityEngine.Object.FindFirstObjectByType<RuinDeveloperPanel>();
                    reward=UnityEngine.Object.FindFirstObjectByType<Ruin01FragmentReward>();
                    keyboard=InputSystem.AddDevice<Keyboard>("Jump J2 QA");
                    Check(guide!=null && InteractionHintManager.Instance.modalPanels.Count(x=>x==guide.panel)==1 &&
                        InteractionHintManager.Instance.interactionTargets.Count(x=>x==jump)==1,"Separate guide, one rock registration and modal.");
                    Check(guide.heading.font.name.Contains("Chomage") && guide.code.font.name.IndexOf("Type",StringComparison.OrdinalIgnoreCase)>=0,
                        "Chomage prose, Type Machine code.");
                    Place(dev.ruin01JumpStart.position);Press();Next(1);return;
                }
                if(Time.frameCount<frame)return;
                switch(stage)
                {
                    case 1:
                        Check(!jump.CanSharedInteract && !guide.IsCompleted,"Original prerequisites initially locked.");
                        dev.Open();dev.UnlockRuin01JumpForTesting();dev.TeleportToRuin01JumpStart();Next(2);break;
                    case 2:Press(UnityEngine.InputSystem.Key.E);Next(3);break;
                    case 3:
                        Check(jump.IsOpen && !guide.IsOpen && !guide.IsCompleted && !reward.ChallengeSolved,
                            "Developer E bypass is session-only without lesson/reward completion.");
                        Press();jump.Close();jump.ResetForDeveloperTesting();jump.SetDeveloperPrerequisiteOverride(false);
                        var original=UnityEngine.Object.FindFirstObjectByType<Ruin01GuidedSimulationUI>();
                        original.Open();original.simulation.JumpToStep(original.simulation.Playback.Trace.Steps.Count-1);original.Finish();
                        jump.bridgePractice.Open();Next(4);break;
                    case 4:
                        if(jump.bridgePractice.IsComplete){jump.bridgePractice.Close();Next(5);break;}
                        if(jump.bridgePractice.IsBusy || jump.bridgePractice.CurrentDecision==null ||
                            jump.bridgePractice.CheckpointIndex!=jump.bridgePractice.CurrentDecision.CheckpointIndex)return;
                        var q=jump.bridgePractice.CurrentDecision;Check(jump.bridgePractice.SubmitAnswer(q.Id,q.CorrectAnswerId).Accepted,"Bridge decision.");break;
                    case 5:Press(UnityEngine.InputSystem.Key.E);Next(6);break;
                    case 6:
                        Check(guide.IsOpen && !jump.IsOpen && guide.Checkpoint==0 && !guide.IsCompleted,"First normal E opens tutorial only.");
                        Check(!jump.playerMovement.enabled && !jump.cameraFollow.enabled && Cursor.visible && !CursorManager.allowCursorLock,"Tutorial modal state.");
                        wizardBefore=jump.player.position;Press();guide.finish.onClick.Invoke();
                        Check(!guide.IsCompleted && !jump.IsOpen,"Early Finish denied.");
                        guide.next.onClick.Invoke();Check(guide.StackDepth==1,"Initial call stack.");
                        guide.next.onClick.Invoke();Check(guide.Checkpoint==2 && guide.StackDepth==1,"n=3 frame waits.");
                        guide.previous.onClick.Invoke();Check(guide.Checkpoint==1 && guide.StackDepth==1,"Previous rebuilds stack.");
                        guide.next.onClick.Invoke();Press(UnityEngine.InputSystem.Key.Escape);Next(7);break;
                    case 7:
                        Check(!guide.IsOpen && !guide.IsCompleted && !jump.IsOpen && jump.playerMovement.enabled &&
                            jump.cameraFollow.enabled && Cursor.lockState==CursorLockMode.Locked,
                            $"Early ESC restores controls without unlocking: guide={guide.IsOpen}, complete={guide.IsCompleted}, input={jump.IsOpen}, move={jump.playerMovement.enabled}, camera={jump.cameraFollow.enabled}, cursor={Cursor.lockState}.");
                        Press();Next(8);break;
                    case 8:Press(UnityEngine.InputSystem.Key.E);Next(9);break;
                    case 9:
                        Press();Check(guide.IsOpen && guide.Checkpoint==2,"Reopen preserves safe checkpoint.");
                        guide.next.onClick.Invoke();Check(guide.StackDepth==2,"n=2 pushes frame.");
                        guide.next.onClick.Invoke();Check(guide.StackDepth==3,"n=1 pushes frame.");
                        guide.next.onClick.Invoke();Check(guide.StackDepth==4 && guide.Checkpoint==5 && guide.ExampleLeaps==0,"Base case stops descent before any leap.");
                        Directory.CreateDirectory("Temp/Ruin01JumpJ2");
                        ScreenCapture.CaptureScreenshot("Temp/Ruin01JumpJ2/BaseCase.png");
                        Next(10);break;
                    case 10:
                        // Hold the base-case view briefly for visual inspection.
                        if(EditorApplication.timeSinceStartup-at<4)return;
                        guide.next.onClick.Invoke();Check(guide.PreviewRunning && !guide.next.interactable,"Unwinding preview gates Next.");Next(11);break;
                    case 11:
                        Check(jump.player.position==wizardBefore && jump.magicTriggers.All(x=>!x.IsVisible),"Tutorial never moves Wizard/reveals real platforms.");
                        if(guide.PreviewRunning)return;
                        Check(guide.ExampleLeaps==3 && guide.StackDepth==0,"Three illustrative return leaps clear stack.");
                        guide.next.onClick.Invoke();Check(guide.Checkpoint==7 && !guide.IsCompleted && !jump.IsOpen,"Final checkpoint alone cannot complete.");
                        Press(UnityEngine.InputSystem.Key.Escape);Next(12);break;
                    case 12:
                        Check(!guide.IsCompleted && !jump.IsOpen,"Final ESC still not Finish.");Press();Next(13);break;
                    case 13:Press(UnityEngine.InputSystem.Key.E);Next(14);break;
                    case 14:
                        Press();Check(guide.IsOpen && !jump.IsOpen,"Reopen uncompleted final checkpoint.");
                        guide.finish.onClick.Invoke();Check(guide.IsCompleted && !guide.IsOpen && jump.IsOpen,"Explicit Finish opens existing input.");
                        jump.Close();Next(15);break;
                    case 15:Press(UnityEngine.InputSystem.Key.E);Next(16);break;
                    case 16:
                        Check(jump.IsOpen && !guide.IsOpen && guide.IsCompleted,"Subsequent E goes directly to input.");
                        Press();jump.input.text="5";jump.castButton.onClick.Invoke();Next(17);break;
                    case 17:
                        if(jump.IsPlaying)return;
                        Check(jump.JumpChallengeCompleted && jump.AnimationPlayCount==5 && jump.AnimationActivationCount==5 &&
                            jump.magicTriggers.All(x=>!x.IsVisible) && reward.ChallengeSolved && reward.RevealCount==1 && guide.IsCompleted,
                            "Existing jump animation/platforms and real fragment reveal preserved.");
                        Check(jump.playerMovement.enabled && jump.cameraFollow.enabled && jump.characterController.enabled &&
                            Cursor.lockState==CursorLockMode.Locked,"Landing controls/cursor restored.");
                        Place(reward.interactionPoint.position);Next(18);break;
                    case 18:Press(UnityEngine.InputSystem.Key.E);Next(19);break;
                    case 19:
                        Check(reward.FragmentCollected && reward.CollectionCount==1 && reward.targetReturn.enabled && errors==0,"Fragment shared E still works, zero errors.");
                        Finish("PASS J2: first E tutorial; early/final ESC cannot unlock; Previous/Next/code and stack 1/2/3/4 then 0; three UI-only return leaps; explicit Finish input gate; later E direct input; Developer bypass leaves tutorial/reward uncompleted; normal five animated leaps and Fragment E collection. Play errors=0");break;
                }
            }
            catch(Exception e){Finish("FAIL J2 stage "+stage+" "+e);}
        }
        static void Place(Vector3 p){var delta=p-jump.player.position;jump.characterController.enabled=false;jump.player.position=p;jump.characterController.enabled=true;jump.playerMovement.ResetVerticalVelocity();jump.cameraFollow.transform.position+=delta;Physics.SyncTransforms();}
        static void Press(UnityEngine.InputSystem.Key? key=null){keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,key.HasValue?new KeyboardState(key.Value):new KeyboardState());}
        static void Next(int n){stage=n;frame=Time.frameCount+6;at=EditorApplication.timeSinceStartup;}
        static void Check(bool ok,string why){if(!ok)throw new InvalidOperationException(why);}
        static void Finish(string result)
        {
            SessionState.SetBool(Key,false);Application.logMessageReceived-=Log;
            if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);
            Directory.CreateDirectory("Temp/Ruin01JumpJ2");File.WriteAllText("Temp/Ruin01JumpJ2/PlayChecks.txt",result);
            if(result.StartsWith("PASS"))Debug.Log(result);else Debug.LogError(result);EditorApplication.isPlaying=false;
        }
    }
}
