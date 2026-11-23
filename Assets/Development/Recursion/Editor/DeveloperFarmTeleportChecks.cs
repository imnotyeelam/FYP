using System;
using System.IO;
using System.Linq;
using Fyp.Development.Progress;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Fyp.Development.Recursion.Editor
{
    [InitializeOnLoad]
    public static class DeveloperFarmTeleportChecks
    {
        const string Key="DeveloperFarmTeleport.QA";
        static RuinDeveloperPanel dev;
        static Ruin02HarvestConsole console;
        static Ruin02FragmentReveal reward;
        static Ruin02ReturnGate gate;
        static FinalPortalInteraction portal;
        static Keyboard keyboard;static Mouse mouse;
        static int stage,frame,errors;static double began;
        static Vector2 click;
        static DeveloperFarmTeleportChecks(){EditorApplication.update+=Tick;}
        [MenuItem("Tools/FYP/Developer Farm Teleport/Run Targeted Play Checks %#F10")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Begin in Edit Mode.");
            dev=null;keyboard=null;mouse=null;stage=errors=0;began=0;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        static void Log(string m,string t,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
            try
            {
                if(began==0){began=EditorApplication.timeSinceStartup;Application.logMessageReceived+=Log;return;}
                Check(EditorApplication.timeSinceStartup-began<90,"Timeout stage "+stage);
                if(dev==null)
                {
                    if(EditorApplication.timeSinceStartup-began<1)return;
                    dev=UnityEngine.Object.FindFirstObjectByType<RuinDeveloperPanel>();
                    console=UnityEngine.Object.FindFirstObjectByType<Ruin02HarvestConsole>();
                    reward=UnityEngine.Object.FindFirstObjectByType<Ruin02FragmentReveal>();
                    gate=UnityEngine.Object.FindFirstObjectByType<Ruin02ReturnGate>();
                    portal=UnityEngine.Object.FindFirstObjectByType<FinalPortalInteraction>();
                    Check(dev.farmEntrySpawn==console.transform.parent.Find("FarmEntrySpawn")&&dev.IsEditorOnlyAvailable,"Existing spawn and Editor guard.");
                    keyboard=InputSystem.AddDevice<Keyboard>("Developer Farm QA");mouse=InputSystem.AddDevice<Mouse>("Developer Farm QA Mouse");
                    ProgressLocked();Press(UnityEngine.InputSystem.Key.Tab);Next(1);return;
                }
                if(Time.frameCount<frame)return;
                var sim=console.simulation;
                switch(stage)
                {
                    case 1:
                        Check(dev.IsOpen&&dev.panel.activeInHierarchy&&!dev.playerMovement.enabled&&!dev.cameraFollow.enabled&&
                            Cursor.visible&&!CursorManager.allowCursorLock,"Real Tab opens modal with safe controls/cursor.");
                        var texts=dev.panel.GetComponentsInChildren<TMPro.TMP_Text>(true);
                        Check(dev.panel.GetComponentsInChildren<UnityEngine.UI.Button>(true).Length==6&&
                            texts.Any(t=>t.text=="Teleport to Magical Farm"&&t.font.name.Contains("Chomage"))&&
                            !texts.Any(t=>t.text.IndexOf("Reset",StringComparison.OrdinalIgnoreCase)>=0)&&dev.panel.transform.Find("ResetJumpButton")==null,
                            "Farm button visible, reset control absent, other buttons retained.");
                        Directory.CreateDirectory("Temp/DeveloperFarmTeleport");ScreenCapture.CaptureScreenshot("Temp/DeveloperFarmTeleport/Panel.png");
                        Press();click=RectTransformUtility.WorldToScreenPoint(null,dev.farmButton.transform.position);MouseButton(true);Next(2);break;
                    case 2:MouseButton(false);Next(3);break;
                    case 3:
                        Check(!dev.IsOpen,"UI mouse click invokes Farm teleport and closes panel.");At(dev.farmEntrySpawn);
                        Check(!sim.IsRunning&&!sim.IsComplete&&sim.CompletedRows==0&&sim.PlotsChecked==0&&sim.CropsHarvested==0,
                            "Teleport does not grant farm progress.");ProgressLocked();
                        Check(InteractionHintManager.Instance.hintText.gameObject.activeInHierarchy&&InteractionHintManager.Instance.hintText.text==console.SharedHint,
                            "Shared Farm console hint immediately eligible without staffs.");
                        Press(UnityEngine.InputSystem.Key.E);Next(4);break;
                    case 4:
                        Check(console.IsOpen&&!dev.IsOpen,"Real E opens existing console without staff restoration.");
                        Press(UnityEngine.InputSystem.Key.Tab);Next(5);break;
                    case 5:
                        Check(console.IsOpen&&!dev.IsOpen,"Console modal blocks Developer panel.");Press();console.RequestClose();
                        dev.Open();dev.hubButton.onClick.Invoke();At(dev.mainHubSpawn);
                        dev.Open();dev.ruin01Button.onClick.Invoke();At(dev.ruin01JumpStart);
                        dev.Open();dev.ruin02Button.onClick.Invoke();At(dev.ruin02EntrySpawn);
                        dev.Open();dev.unlockJumpButton.onClick.Invoke();
                        Check(dev.jumpController.DeveloperPrerequisitesUnlocked&&!dev.jumpController.bridgePractice.IsComplete&&
                            !dev.jumpController.jumpGuide.IsCompleted,"Existing Jump testing unlock remains session-only.");
                        dev.closeButton.onClick.Invoke();Check(!dev.IsOpen,"Existing Close still works.");
                        dev.Open();dev.farmButton.onClick.Invoke();At(dev.farmEntrySpawn);ProgressLocked();Next(6);break;
                    case 6:Press(UnityEngine.InputSystem.Key.E);Next(7);break;
                    case 7:
                        Press();Check(console.IsOpen,"Farm console can be reopened after other developer teleports.");
                        console.input.text="n=0";console.submit.onClick.Invoke();Check(!sim.IsRunning&&sim.CompletedRows==0,"Wrong answer still rejected.");
                        console.input.text="n == 0";console.submit.onClick.Invoke();Next(8);break;
                    case 8:
                        ProgressLocked();if(sim.IsRunning)return;
                        Check(sim.CompletedRows==1&&sim.PlotsChecked==10&&sim.CropsHarvested==5,"Original row 1 challenge completed normally.");
                        console.input.text="n - 1";console.submit.onClick.Invoke();Next(9);break;
                    case 9:
                        ProgressLocked();if(sim.IsRunning)return;
                        Check(sim.CompletedRows==2&&sim.PlotsChecked==20&&sim.CropsHarvested==10,"Original row 2 challenge completed normally.");
                        console.input.text="hasCrop(row,n)";console.submit.onClick.Invoke();Next(10);break;
                    case 10:
                        ProgressLocked();if(sim.IsRunning)return;
                        Check(sim.IsComplete&&sim.CompletedRows==3&&sim.PlotsChecked==30&&sim.CropsHarvested==15&&sim.StackDepth==0,
                            "All three unchanged code challenges are playable.");
                        console.RequestClose();Next(11);break;
                    case 11:
                        At(dev.farmEntrySpawn);ProgressLocked();
                        Check(!console.IsOpen&&errors==0,"Controls, camera and cursor restored with no errors.");
                        Finish("PASS Developer Farm: real Tab; six buttons with Farm label and no Reset control; real UI mouse-click safe FarmEntrySpawn teleport; real shared E console; modal blocking; other teleports/Close/Jump test unlock; wrong answer and all three original rows (30/15/empty stack); no staff/fragment/portal progression changes, even after legitimate farm completion. Play errors=0");break;
                }
            }
            catch(Exception e){Finish("FAIL Developer Farm stage "+stage+" "+e);}
        }
        static void ProgressLocked()
        {
            Check(!gate.farmAccess.practice.IsSolved&&gate.farmAccess.restoration.RestoredCount==0&&!gate.farmAccess.StaffCompleted&&
                Enumerable.Range(1,4).All(n=>!gate.farmAccess.restoration.IsStaffRestored(n))&&
                !reward.IsRevealed&&!reward.FragmentCollected&&!reward.fragmentVisual.activeInHierarchy&&
                reward.progress.CollectedCount==0&&!portal.restoration.HasStartedRestoration&&!reward.progress.HasFinalPortalFragments,
                "Developer testing must not alter staffs/reward/normal access/portal progression.");
        }
        static void At(Transform spawn)
        {
            Check(Vector3.ProjectOnPlane(dev.player.position-spawn.position,Vector3.up).magnitude<.3f&&
                Mathf.Abs(Quaternion.Dot(dev.player.rotation,spawn.rotation))>.999f&&dev.characterController.enabled&&dev.playerMovement.enabled&&
                dev.cameraFollow.enabled&&!dev.IsOpen&&CursorManager.allowCursorLock&&Cursor.lockState==CursorLockMode.Locked&&
                Vector3.Distance(dev.cameraFollow.transform.position,dev.player.position)<12,"Safe destination/orientation/controller/camera/cursor restoration.");
        }
        static void Press(UnityEngine.InputSystem.Key? key=null){keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,key.HasValue?new KeyboardState(key.Value):new KeyboardState());}
        static void MouseButton(bool down){mouse.MakeCurrent();InputSystem.QueueStateEvent(mouse,new MouseState{position=click,buttons=(ushort)(down?1:0)});}
        static void Next(int n){stage=n;frame=Time.frameCount+6;}
        static void Check(bool ok,string why){if(!ok)throw new InvalidOperationException(why);}
        static void Finish(string result)
        {
            SessionState.SetBool(Key,false);Application.logMessageReceived-=Log;
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
            Directory.CreateDirectory("Temp/DeveloperFarmTeleport");File.WriteAllText("Temp/DeveloperFarmTeleport/PlayChecks.txt",result);
            if(result.StartsWith("PASS"))Debug.Log(result);else Debug.LogError(result);EditorApplication.isPlaying=false;
        }
    }
}
