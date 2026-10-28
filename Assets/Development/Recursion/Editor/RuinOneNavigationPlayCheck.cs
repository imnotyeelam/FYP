using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace Fyp.Development.Recursion.Editor
{
    [InitializeOnLoad]
    public static class RuinOneNavigationPlayCheck
    {
        private const string Request="FYP.RuinOneNavigationPlayCheck";
        private static int phase,checks,errors;
        private static double changed,started;
        private static CharacterController cc;
        private static PlayerMovement movement;
        private static CameraFollow follow;
        private static bool movementEnabled,followEnabled;
        private static RuinEntrance entrance,exit;
        private static AncientSpellbookUI book;
        private static Keyboard keyboard;
        private static Vector3 stand;
        private static StringBuilder report;
        private static Dictionary<Transform,string> protectedTransforms;
        static RuinOneNavigationPlayCheck(){EditorApplication.update+=Tick;}
        [MenuItem("Tools/FYP/Ruin 1/Navigation/Run Entry Return and Crossing Play Check")]
        public static void Run()
        {
            SpellbookStageTools.RequireScene();
            if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Review unsaved scene edits first; test never saves them.");
            RuinOneNavigationAudit.Inspect();
            phase=0;checks=0;errors=0;started=0;SessionState.SetBool(Request,true);EditorApplication.isPlaying=true;
        }
        private static void Log(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)errors++;}
        private static void Check(bool yes,string message){checks++;if(!yes)throw new InvalidOperationException("Navigation Play check: "+message);}
        private static void Put(Vector3 position)
        {cc.enabled=false;cc.transform.position=position;cc.enabled=true;Physics.SyncTransforms();}
        private static void Key(bool down)
        {keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,down?new KeyboardState(UnityEngine.InputSystem.Key.E):new KeyboardState());changed=EditorApplication.timeSinceStartup;}
        private static void Finish()
        {
            Application.logMessageReceived-=Log;
            if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);
            if(book!=null && book.IsBookOpen)book.CloseBook();
            if(movement!=null)movement.enabled=movementEnabled;
            if(follow!=null)follow.enabled=followEnabled;
            SessionState.SetBool(Request,false);EditorApplication.isPlaying=false;
        }
        private static void Tick()
        {
            if(!SessionState.GetBool(Request,false) || !EditorApplication.isPlaying)return;
            try
            {
                if(started==0){started=EditorApplication.timeSinceStartup;phase=0;return;}
                if(EditorApplication.timeSinceStartup-started>45)throw new TimeoutException("Navigation Play check timed out.");
                if(phase==0)
                {
                    if(EditorApplication.timeSinceStartup-started<1.5)return;
                    report=new StringBuilder("NAVIGATION PLAY CHECK\n");errors=0;checks=0;Application.logMessageReceived+=Log;
                    cc=RuinOneNavigationAudit.PlayerController();movement=cc.GetComponent<PlayerMovement>();
                    follow=Object.FindObjectsByType<CameraFollow>(FindObjectsSortMode.None).Single();
                    movementEnabled=movement.enabled;followEnabled=follow.enabled;movement.enabled=false;follow.enabled=false;
                    var all=Object.FindObjectsByType<RuinEntrance>(FindObjectsSortMode.None);
                    entrance=all.Single(e=>e.destination.name=="L1_Spawnpoint");exit=RuinOneNavigationAudit.Return();
                    book=Object.FindObjectsByType<AncientSpellbookUI>(FindObjectsSortMode.None).Single();
                    var manager=InteractionHintManager.Instance;
                    Check(manager.coordinateInteractions && manager.interactionTargets.Contains(entrance) && manager.interactionTargets.Contains(exit),"both targets registered");
                    Check(exit.destination.name=="Destination" && exit.useSharedInteraction && exit.interactionDistance==5,"existing return wiring");
                    protectedTransforms=RuinOneNavigationAudit.All().Where(t=>t.name.StartsWith("Bridge") ||
                        t.IsChildOf(Object.FindObjectsByType<BridgeWorldView>(FindObjectsSortMode.None).Single().transform) ||
                        t.name=="L1_Spawnpoint" || t==exit.transform || t.name=="Magic Book")
                        .ToDictionary(t=>t,t=>EditorJsonUtility.ToJson(t));
                    keyboard=InputSystem.AddDevice<Keyboard>("FYP Navigation QA Keyboard");
                    CursorManager.Instance.SetUIOpen(false);
                    Put(entrance.transform.position+new Vector3(-1.2f,.5f,0));
                    Key(false);phase=1;return;
                }
                if(EditorApplication.timeSinceStartup-changed<.3)return;
                if(phase==1)
                {
                    Check(InteractionHintManager.Instance.hintText.gameObject.activeInHierarchy,"entry hint visible");
                    Check(InteractionHintManager.Instance.hintText.text==entrance.SharedHint,"entry selected");
                    Key(true);phase=2;return;
                }
                if(phase==2)
                {
                    Check(Vector3.Distance(cc.transform.position,entrance.destination.position)<.005f,"E dispatched entry teleport");
                    Check(cc.enabled && Cursor.lockState==CursorLockMode.Locked && CursorManager.allowCursorLock,"entry controller/cursor preserved");
                    report.AppendLine("Entry E -> "+cc.transform.position.ToString("F5")+"; held E caused no second teleport.");
                    Key(false);phase=3;return;
                }
                if(phase==3)
                {
                    var h=RuinOneNavigationAudit.Ground(exit.transform.position.x,exit.transform.position.z-2).Value;
                    stand=h.point+Vector3.up*.03f;Check(RuinOneNavigationAudit.Clear(stand,cc),"return capsule clearance");
                    Put(stand);book.OpenBook();Key(true);phase=4;return;
                }
                if(phase==4)
                {
                    Check(Vector3.Distance(cc.transform.position,stand)<.005f,"modal blocks return E");
                    Check(!InteractionHintManager.Instance.hintText.gameObject.activeInHierarchy && Cursor.visible && !CursorManager.allowCursorLock,"modal hint/cursor");
                    Key(false);book.CloseBook();phase=5;return;
                }
                if(phase==5)
                {
                    Check(InteractionHintManager.Instance.hintText.gameObject.activeInHierarchy && InteractionHintManager.Instance.hintText.text==exit.SharedHint,"far island return hint selected");
                    Check(Cursor.lockState==CursorLockMode.Locked && CursorManager.allowCursorLock,"close restores cursor");
                    Key(true);phase=6;return;
                }
                if(phase==6)
                {
                    Check(Vector3.Distance(cc.transform.position,exit.destination.position)<.005f,"E dispatched return to unchanged destination");
                    Check(cc.enabled && Cursor.lockState==CursorLockMode.Locked,"return controller/cursor preserved");
                    report.AppendLine("Return E -> "+RuinOneNavigationAudit.P(exit.destination)+" "+cc.transform.position.ToString("F5"));
                    Key(false);phase=7;return;
                }
                if(phase==7)
                {
                    Check(Vector3.Distance(cc.transform.position,exit.destination.position)<.005f,"released E does not bounce back");
                    foreach(var pair in protectedTransforms)Check(EditorJsonUtility.ToJson(pair.Key)==pair.Value,"protected scene transform retained: "+pair.Key.name);
                    Check(errors==0,"no Console errors/exceptions during test");
                    report.AppendLine("PASS: "+checks+" assertions; real queued E input, entry, far return, held/released E, modal block, cursor, CC enabled, protected transforms; errors="+errors+". Bridge traversal is covered by the Phase C and D play checks.");
                    Directory.CreateDirectory(RuinOneNavigationAudit.Output);File.WriteAllText(RuinOneNavigationAudit.Output+"/PlayCheck.txt",report.ToString());
                    Debug.Log(report.ToString());Finish();
                }
            }
            catch(Exception error)
            {
                Directory.CreateDirectory(RuinOneNavigationAudit.Output);
                File.WriteAllText(RuinOneNavigationAudit.Output+"/FailedPlayCheck.txt",(report?.ToString()??"")+"\nFAIL: "+error);
                Debug.LogException(error);Finish();
            }
        }
    }
}
