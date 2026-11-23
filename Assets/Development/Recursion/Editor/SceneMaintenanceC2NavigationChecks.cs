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
    public static class SceneMaintenanceC2NavigationChecks
    {
        const string Key="SceneMaintenanceC2.Navigation.QA";
        static Ruin02ReturnGate gate;
        static Ruin02FarmAccess oldEntry,exit;
        static Ruin02FragmentReveal reward;
        static Keyboard keyboard;
        static int stage,frame,errors,teleports,collections;
        static double began;
        static Vector3 approach,rockPosition;
        static SceneMaintenanceC2NavigationChecks(){EditorApplication.update+=Tick;}
        [MenuItem("Tools/FYP/Scene Maintenance C2/Run Navigation Checks")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Begin in Edit Mode.");
            gate=null;keyboard=null;stage=errors=teleports=collections=0;began=0;
            SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        static void Log(string m,string t,LogType type)
        {
            if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;
            if(m.StartsWith("Teleported to: "))teleports++;
        }
        static void Collected(string id){if(id=="Ruin02")collections++;}
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
            try
            {
                if(began==0){began=EditorApplication.timeSinceStartup;Application.logMessageReceived+=Log;return;}
                Check(EditorApplication.timeSinceStartup-began<110,"Timeout stage "+stage);
                if(gate==null)
                {
                    if(EditorApplication.timeSinceStartup-began<1)return;
                    gate=Ruin02RockNavigationTools.Gate;oldEntry=gate.farmAccess;
                    exit=UnityEngine.Object.FindObjectsByType<Ruin02FarmAccess>(FindObjectsSortMode.None).Single(x=>x.isFarmReturn);
                    reward=UnityEngine.Object.FindFirstObjectByType<Ruin02FragmentReveal>();
                    var hints=InteractionHintManager.Instance;
                    Check(!oldEntry.enabled&&!gate.returnEntrance.enabled&&hints.interactionTargets.Count(x=>x==gate)==1&&
                        !hints.interactionTargets.Contains(oldEntry)&&!hints.interactionTargets.Contains(gate.returnEntrance)&&
                        hints.interactionTargets.Count(x=>x==exit)==1,"One original rock E target, no competing old entrance.");
                    Check(SceneMaintenanceC2Tools.Exact("_Archived_Objects/FarmGatewayLandmark").gameObject.activeSelf==false&&
                        SceneMaintenanceC2Tools.Exact("_Archived_Objects/GatewayLabel").gameObject.activeSelf==false&&exit.destination.gameObject.activeInHierarchy,
                        "Only redundant H4 visuals retired; farm return landing preserved.");
                    var rock=gate.returnEntrance.transform.Find("ReturnMagicRock");
                    Check(rock.GetComponent<BoxCollider>().enabled&&!rock.GetComponent<BoxCollider>().isTrigger,"Existing reachable solid rock collider.");
                    rockPosition=rock.position;approach=Ruin02RockNavigationTools.Approach(gate);
                    reward.progress.FragmentCollected+=Collected;
                    keyboard=InputSystem.AddDevice<Keyboard>("Ruin 2 rock QA");Place(approach);Press(false);Next(1);return;
                }
                if(Time.frameCount<frame)return;
                var console=oldEntry.console;var sim=console.simulation;
                switch(stage)
                {
                    case 1:
                        Check(!oldEntry.StaffCompleted&&gate.CanSharedInteract&&!reward.IsRevealed,"Original rock available before restoration.");
                        Hint(gate.SharedHint);Press(true);Next(2);break;
                    case 2:
                        Check(teleports==0&&Vector3.Distance(gate.returnEntrance.player.position,approach)<.4f&&
                            gate.lockedMessage.gameObject.activeInHierarchy&&gate.lockedMessage.text.Contains("four staffs")&&
                            gate.lockedMessage.font.name.Contains("Chomage"),"Staff-locked E shows Chomage toast without travel.");
                        Press(false);oldEntry.practice.Open();Next(3);break;
                    case 3:
                        if(oldEntry.restoration.IsPlaying){oldEntry.restoration.SkipCurrentSequence();return;}
                        if(oldEntry.practice.Session.Advancing){oldEntry.practice.Session.TryAdvanceApprovedStep();return;}
                        if(!oldEntry.practice.IsSolved)
                        {
                            var q=oldEntry.practice.Session.Question;
                            Check(q!=null&&oldEntry.practice.Submit(q.Expected).Status==Ruin02AnswerStatus.Correct,"Staff answer.");return;
                        }
                        oldEntry.practice.Close();Next(4);break;
                    case 4:
                        Check(oldEntry.StaffCompleted&&!sim.IsComplete&&!reward.IsRevealed,"Real four-staff completion, farm still unfinished.");
                        Hint("Press [E] to Enter Magical Farm");Press(true);Next(5);break;
                    case 5:
                        At(oldEntry.destination);Check(teleports==1,"Single E causes exactly one farm teleport.");Press(false);Next(6);break;
                    case 6:Hint(console.SharedHint);Press(true);Next(7);break;
                    case 7:
                        Check(console.IsOpen&&!gate.CanSharedInteract&&!exit.CanSharedInteract,"Farm console modal blocks navigation.");
                        Press(false);console.input.text="n == 0";console.Submit();console.RequestClose();Next(8);break;
                    case 8:
                        if(sim.IsRunning||console.IsOpen)return;
                        Check(sim.CompletedRows==1&&!sim.IsComplete&&!reward.IsRevealed,"Partial row preserved, reward locked.");
                        Place(exit.transform.position);Next(9);break;
                    case 9:Hint(exit.SharedHint);Press(true);Next(10);break;
                    case 10:
                        At(exit.destination);Check(teleports==2&&sim.CompletedRows==1,"Farm return works before completion.");
                        Press(false);Place(approach);Next(11);break;
                    case 11:Hint("Press [E] to Enter Magical Farm");Press(true);Next(12);break;
                    case 12:At(oldEntry.destination);Check(teleports==3,"Same original rock re-enters unfinished farm once.");Press(false);Next(13);break;
                    case 13:Press(true);Next(14);break;
                    case 14:
                        Check(console.IsOpen&&sim.CompletedRows==1,"Console resumes next row.");Press(false);
                        console.input.text="n - 1";console.Submit();Next(15);break;
                    case 15:
                        if(sim.IsRunning)return;
                        Check(sim.CompletedRows==2&&!reward.IsRevealed,"Two rows insufficient for H5 reward.");
                        console.input.text="hasCrop(row,n)";console.Submit();Next(16);break;
                    case 16:
                        if(sim.IsRunning)return;
                        Check(sim.IsComplete&&sim.PlotsChecked==30&&sim.CropsHarvested==15&&sim.StackDepth==0&&reward.IsRevealed&&!reward.FragmentCollected,
                            "Unchanged H5 reveals reward only after complete farm.");
                        console.RequestClose();Place(exit.transform.position);Next(17);break;
                    case 17:Press(true);Next(18);break;
                    case 18:
                        At(exit.destination);Check(teleports==4,"Farm return after completion.");Press(false);Place(approach);Next(19);break;
                    case 19:Hint(gate.SharedHint);Press(true);Next(20);break;
                    case 20:
                        Check(teleports==4&&Vector3.Distance(gate.returnEntrance.player.position,approach)<.4f&&
                            gate.lockedMessage.text=="Collect the magic fragment first!"&&gate.lockedMessage.gameObject.activeInHierarchy,
                            "Completed farm without fragment blocks both Farm and Hub with exact toast.");
                        Press(false);Place(reward.interactionPoint.position);Next(21);break;
                    case 21:Hint(reward.SharedHint);Press(true);Next(22);break;
                    case 22:
                        Check(reward.FragmentCollected&&collections==1&&!reward.CanSharedInteract,"Original fragment E collects Ruin02 once.");
                        Press(false);Place(approach);Next(23);break;
                    case 23:Hint("Press [E] to Return to Hub");Press(true);Next(24);break;
                    case 24:
                        At(gate.returnEntrance.destination);
                        Check(teleports==5&&collections==1&&sim.IsComplete&&sim.CompletedRows==3&&reward.FragmentCollected&&errors==0&&
                            gate.returnEntrance.transform.Find("ReturnMagicRock").position==rockPosition,
                            "Single original Hub teleport, latched progression, unchanged rock, zero errors.");
                        Finish("PASS Ruin 2 single rock: Staff lock/Chomage toast; real restoration unlocks Farm E; console and partial-return/re-entry retain rows; complete farm blocks travel until fragment E; collected Ruin02 enables original Hub return. Five expected teleports, no duplicate targets/actions. Play errors=0");break;
                }
            }
            catch(Exception e){Finish("FAIL navigation stage "+stage+" "+e);}
        }
        static void Hint(string text){Check(InteractionHintManager.Instance.hintText.gameObject.activeInHierarchy&&InteractionHintManager.Instance.hintText.text==text,"Expected winning shared hint: "+text+" actual="+InteractionHintManager.Instance.hintText.text);}
        static void Place(Vector3 point)
        {
            var p=gate.returnEntrance.player;var delta=point-p.position;var cc=p.GetComponent<CharacterController>();
            cc.enabled=false;p.position=point;cc.enabled=true;p.GetComponent<PlayerMovement>().ResetVerticalVelocity();
            oldEntry.console.cameraFollow.transform.position+=delta;Physics.SyncTransforms();
        }
        static void At(Transform destination)
        {
            var p=gate.returnEntrance.player;
            Check(Vector3.ProjectOnPlane(p.position-destination.position,Vector3.up).magnitude<.3f&&p.GetComponent<CharacterController>().enabled&&
                oldEntry.console.movement.enabled&&oldEntry.console.cameraFollow.enabled&&CursorManager.allowCursorLock&&Cursor.lockState==CursorLockMode.Locked&&
                Vector3.Distance(oldEntry.console.cameraFollow.transform.position,p.position)<10,"Safe teleport/player/camera/cursor state.");
        }
        static void Press(bool down){keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,down?new KeyboardState(UnityEngine.InputSystem.Key.E):new KeyboardState());}
        static void Next(int n){stage=n;frame=Time.frameCount+6;}
        static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        static void Finish(string result)
        {
            SessionState.SetBool(Key,false);Application.logMessageReceived-=Log;
            if(reward!=null)reward.progress.FragmentCollected-=Collected;
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
            Directory.CreateDirectory("Temp/SceneMaintenanceC2Navigation");File.WriteAllText("Temp/SceneMaintenanceC2Navigation/PlayChecks.txt",result);
            if(result.StartsWith("PASS"))Debug.Log(result);else Debug.LogError(result);EditorApplication.isPlaying=false;
        }
    }
}
