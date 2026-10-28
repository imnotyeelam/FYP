using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Fyp.Development.Recursion.Editor
{
    [InitializeOnLoad]
    public static class BridgePracticePlayChecks
    {
        const string Key="FYP.C.PracticeChecks", Output="Temp/BridgePracticeC";
        static BridgePracticeController controller;
        static FloatingIslandMotion[] modules;
        static StringBuilder log;
        static int stage, decision, assertions, errors, lastIndex, previousActions, closedIndex;
        static double began, stageBegan;
        static bool motionCloseTested, returnCloseTested, spellbookTested;
        static Keyboard keyboard;

        static BridgePracticePlayChecks()
        {
            EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state!=PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("FYP.C.After",false))return;
                SessionState.SetBool("FYP.C.After",false);
                EditorApplication.delayCall+=()=>EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Output+"/AfterPlay.unity",true);
            };
            if(SessionState.GetBool(Key,false))Application.logMessageReceived+=OnLog;
        }
        static void OnLog(string message,string stack,LogType type)
        {if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)errors++;}
        static void Check(bool value,string message)
        {assertions++;if(!value)throw new InvalidOperationException("Phase C: "+message);}
        static void Next(int target){stage=target;stageBegan=EditorApplication.timeSinceStartup;}

        [MenuItem("Tools/FYP/Ruin 1/Phase C/Run Interactive Practice Play Checks %#&F6")]
        public static void Run()
        {
            SpellbookStageTools.RequireScene();
            Check(BridgePracticeLessonChecks.Run()>0,"six-decision pure lesson checks");
            var c=BridgePracticeTools.Controller();
            Check(c.enabled && c.rig!=null && c.panel!=null && c.presentation!=null,"installed practice references");
            Check(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(BridgePracticeTools.Exact("Ruin01_Area/Interactables/BridgePracticeInteractionPoint/PracticeMagicRock").gameObject)==
                "Assets/FantasyForest/Prefabs/Landscape/MagicRock_01.prefab","existing URP MagicRock prefab link in Edit Mode");
            Directory.CreateDirectory(Output);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Output+"/BeforePlay.unity",true);
            controller=null;modules=null;log=new StringBuilder("PHASE C ACTUAL UNITY PLAY MODE\n");
            stage=0;decision=0;assertions=0;errors=0;lastIndex=-1;previousActions=0;
            motionCloseTested=false;returnCloseTested=false;spellbookTested=false;began=0;
            Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;
            SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        static void Finish(bool success,string reason=null)
        {
            Application.logMessageReceived-=OnLog;
            if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);
            if(log==null)log=new StringBuilder("PHASE C ACTUAL UNITY PLAY MODE\n");
            log.AppendLine((success?"PASS":"FAIL")+" assertions="+assertions+" Console errors="+errors);
            if(reason!=null)log.AppendLine(reason);
            Directory.CreateDirectory(Output);File.WriteAllText(Output+"/PlayChecks.txt",log.ToString());
            if(success)Debug.Log(log.ToString());else Debug.LogError(log.ToString());
            SessionState.SetBool(Key,false);SessionState.SetBool("FYP.C.After",true);
            EditorApplication.isPlaying=false;
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying)return;
            if(log==null)log=new StringBuilder("PHASE C ACTUAL UNITY PLAY MODE\n");
            try
            {
                if(began==0){began=EditorApplication.timeSinceStartup;return;}
                if(EditorApplication.timeSinceStartup-began>165)throw new TimeoutException("Interactive practice test exceeded 165 seconds at stage "+stage+" checkpoint "+controller?.CheckpointIndex);
                if(controller==null)
                {
                    if(EditorApplication.timeSinceStartup-began<1)return;
                    controller=BridgePracticeTools.Controller();modules=new[]{controller.rig.placeStone3,controller.rig.placeStone2,controller.rig.placeStone1};
                    Check(controller.CheckpointIndex==0 && controller.RestorationCount==0,"initial source checkpoint with no placements");
                    Check(modules.All(m=>m.IsPracticeOwned && !m.IsRepaired && m.walkableColliders.All(x=>!x.enabled)),"all broken colliders off and owned");
                    Check(controller.rig.fixedStartingSection==BridgePracticeTools.Exact("Bridge"),"fixed starting section retained");
                    Check(BridgePracticeTools.Exact("Ruin01_Area/Gameplay/RuinRecursionVisualizer").GetComponent<RuinRecursionVisualizer>().enabled==false,"Cube fallback interaction inactive");
                    Check(BridgePracticeTools.Exact("Magic Book").GetComponent<KnowledgeInteractable>().enabled,"Spellbook enabled before modal");
                    Check(Vector3.Distance(BridgePracticeTools.Exact("Ruin01_Area/Interactables/ReturnInteractionPoint").position,
                        new Vector3(-440f,46.08545f,166f))<.001f,"aligned return interaction position retained");
                    ApproachTraversal();
                    var landmark=BridgePracticeTools.Exact("Ruin01_Area/Interactables/BridgePracticeInteractionPoint/PracticeMagicRock");
                    Check(landmark.GetComponentsInChildren<Collider>(true).All(x=>!x.enabled),"landmark scene colliders cannot obstruct walking");
                    Check(landmark.GetComponentsInChildren<Renderer>(true).Any(x=>x.enabled),"landmark mesh visible");
                    Check(BridgePracticeTools.Exact("Ruin01_Area/Interactables/BridgePracticeInteractionPoint/PracticeBeaconLight").GetComponent<Light>().enabled,"landmark beacon enabled");
                    Check(!controller.interactionPoint.GetComponentsInChildren<MonoBehaviour>(true).Any(x=>x is ISharedInteractionTarget),"visual children add no second interaction");
                    keyboard=InputSystem.AddDevice<Keyboard>("FYP Phase C QA Keyboard");keyboard.MakeCurrent();
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                    var cc=BridgePracticeTools.Exact("Player").GetComponent<CharacterController>();
                    cc.enabled=false;cc.transform.position=controller.interactionPoint.position+Vector3.down*.27f;cc.enabled=true;Physics.SyncTransforms();
                    Next(-1);return;
                }
                if(stage==-1)
                {
                    if(EditorApplication.timeSinceStartup-stageBegan<.35)return;
                    Check(controller.SharedHint=="Press [E] to Restore the Bridge","new precise E prompt");
                    Check(InteractionHintManager.Instance.hintText.gameObject.activeInHierarchy && InteractionHintManager.Instance.hintText.text==controller.SharedHint,"single shared hint selects practice point");
                    ScreenCapture.CaptureScreenshot(Output+"/PracticeLandmarkPrompt.png");
                    keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.E));
                    Next(-2);return;
                }
                if(stage==-2)
                {
                    if(EditorApplication.timeSinceStartup-stageBegan<.35)return;
                    Check(controller.IsOpen && controller.OpenCount==1,"one queued E opens one practice interaction");
                    Check(!BridgePracticeTools.Exact("Ruin01_Area/Interactables/ReturnInteractionPoint").GetComponent<RuinEntrance>().enabled,"teleport interactions suspended during practice");
                    Check(BridgePracticeTools.Exact("Player").GetComponent<CharacterController>().enabled,"player collider remains enabled");
                    if(EditorApplication.timeSinceStartup-stageBegan<.7)return;
                    Check(controller.OpenCount==1,"held E does not reopen or duplicate interaction");
                    keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                    Check(!BridgePracticeTools.Exact("Player").GetComponent<PlayerMovement>().enabled,"movement paused");
                    Check(!BridgePracticeTools.Exact("Main Camera").GetComponent<CameraFollow>().enabled,"gameplay camera follow paused");
                    Check(!CursorManager.allowCursorLock && Cursor.visible,"practice unlocks UI cursor");
                    Check(!InteractionHintManager.Instance.hintText.gameObject.activeInHierarchy,"shared hint hidden during practice modal");
                    Check(controller.panel.gameObject.activeInHierarchy && controller.presentation.practiceCamera.enabled,"practice panel and camera active");
                    Check(!controller.presentation.gameplayCamera.enabled && controller.presentation.practiceCamera.rect.width<.7f,"world camera uses space left of compact UI");
                    Check(modules.All(m=>m.walkableColliders.All(x=>!x.enabled)),"broken colliders remain off during modal");
                    controller.Open();Check(controller.OpenCount==1,"duplicate open ignored");
                    Next(1);return;
                }
                if(stage==1)
                {
                    if(controller.CheckpointIndex!=1)return;
                    Check(controller.CurrentDecision?.CheckpointIndex==1,"first deterministic gate");
                    Check(controller.RestorationCount==0,"pre-roll has no placement");
                    ScreenCapture.CaptureScreenshot(Output+"/PracticeFirstQuestion.png");
                    Next(2);return;
                }
                if(stage==2)
                {
                    var q=controller.CurrentDecision;
                    if(q==null || controller.CheckpointIndex!=q.CheckpointIndex || controller.IsBusy)return;
                    Check(decision<6 && controller.Current==controller.Trace.Steps[q.CheckpointIndex],"current decision uses source trace");
                    int at=controller.CheckpointIndex,count=controller.RestorationCount;
                    var wrong=q.Answers.First(a=>a.Id!=q.CorrectAnswerId);
                    var miss=controller.SubmitAnswer(q.Id,wrong.Id);
                    Check(miss.Status==PracticeSubmissionStatus.Incorrect && controller.LastAnswerIncorrect && !string.IsNullOrEmpty(controller.Feedback),"wrong answer feedback");
                    Check(controller.CheckpointIndex==at && controller.RestorationCount==count,"wrong answer leaves trace and bridge unchanged");
                    Check(controller.SubmitAnswer("stale",q.CorrectAnswerId).Status==PracticeSubmissionStatus.StaleQuestion,"stale ID rejected");
                    Check(controller.SubmitAnswer(q.Id,"unknown").Status==PracticeSubmissionStatus.UnknownAnswer,"unknown answer rejected");
                    controller.SelectAnswer(q.Id,q.CorrectAnswerId);
                    Check(controller.SelectedAnswerId==q.CorrectAnswerId,"UI answer selection");
                    controller.panel.submit.onClick.Invoke();
                    Check(controller.SubmitAnswer(q.Id,q.CorrectAnswerId).Status==PracticeSubmissionStatus.Advancing,"double submit rejected");
                    log.AppendLine("Decision "+(decision+1)+" "+q.Id+": wrong, stale, unknown and duplicate rejected; correct accepted.");
                    decision++;Next(3);return;
                }
                if(stage==3)
                {
                    int now=controller.CheckpointIndex;
                    if(now!=lastIndex)
                    {
                        if(lastIndex>=0)Check(now==lastIndex+1,"one trace snapshot per advance");
                        Check(controller.Current==controller.Trace.Steps[now],"current source snapshot identity");
                        int actions=controller.Current.ActionCount;
                        Check(controller.RestorationCount==actions,"world placements equal source action count");
                        if(actions!=previousActions)
                        {
                            Check(controller.Current.Phase==RecursionPhase.Action,"repair only on actual Action phase");
                            Check(actions==previousActions+1 && controller.RestorationParameters[actions-1]==controller.Current.Parameter,"placeStone mapping and order");
                            previousActions=actions;
                        }
                        Check(modules.Take(actions).All(m=>m.IsPracticeOwned),"restored modules remain claimed");
                        Check(modules.Skip(actions).All(m=>!m.IsRepaired && m.walkableColliders.All(x=>!x.enabled)),"future modules unwalkable");
                        lastIndex=now;
                    }
                    if(now==2 && !motionCloseTested)
                    {
                        motionCloseTested=true;Check(controller.IsBusy && !modules[0].IsRepaired,"first placement moving without collision");
                        controller.panel.details.closeButton.onClick.Invoke();
                        Check(controller.ClosePending && controller.IsOpen,"close deferred while moving");
                        Next(4);return;
                    }
                    if(now==16 && !returnCloseTested)
                    {
                        returnCloseTested=true;closedIndex=now;controller.Close();Check(!controller.IsOpen,"close during return snapshot");
                        Next(5);return;
                    }
                    if(controller.IsComplete)
                    {
                        Check(controller.CheckpointIndex==21 && controller.Current.Frames.Count==0,"complete only at checkpoint 22 and empty stack");
                        Check(controller.RestorationCount==3 && controller.RestorationParameters.SequenceEqual(new[]{3,2,1}),"exactly 3,2,1 placements");
                        Check(controller.VisitedCheckpoints.SequenceEqual(Enumerable.Range(0,22)),"all 22 source snapshots displayed in order");
                        Check(controller.CompletionCount==1,"single completion event");
                        Check(modules.All(m=>m.IsRepaired && m.walkableColliders.All(x=>x.enabled)),"all bridge collisions on after settle");
                        var connector=BridgePracticeTools.Exact("Bridge (3)/MotionRoot/PhaseA_EndConnector").GetComponent<Collider>();
                        Check(connector!=null && connector.enabled,"end connector owned and walkable");
                        foreach(var m in modules)m.ResetToBroken();
                        Check(modules.All(m=>m.IsRepaired),"developer reset cannot remove bridge under player");
                        Check(!modules[0].TryClaimForPractice(new object()),"another controller cannot claim motion");
                        Check(controller.SubmitAnswer("bridge.unwind","return.callers").Status==PracticeSubmissionStatus.Complete,"rewardless completed practice rejects duplicate answer");
                        controller.Close();Check(!controller.IsOpen && modules.All(m=>m.IsRepaired),"close preserves completed geometry");
                        Check(BridgePracticeTools.Exact("Player").GetComponent<PlayerMovement>().enabled,"movement restored");
                        Check(BridgePracticeTools.Exact("Main Camera").GetComponent<CameraFollow>().enabled,"camera controls restored");
                        Check(CursorManager.allowCursorLock,"normal cursor mode restored");
                        Check(controller.presentation.gameplayCamera.enabled && !controller.presentation.practiceCamera.enabled,"camera enable states restored");
                        RepairedTraversal();
                        Check(controller.CompletionCount==1 && errors==0,"one completion and no Console errors");
                        log.AppendLine("All 22 checkpoints, three repairs, base case, returns, modal close/reopen, and completion passed.");
                        Finish(true);return;
                    }
                    if(!controller.IsBusy && controller.CurrentDecision!=null && controller.CheckpointIndex==controller.CurrentDecision.CheckpointIndex)
                        Next(2);
                    return;
                }
                if(stage==4)
                {
                    Check(controller.CheckpointIndex==2,"deferred close did not consume more checkpoints");
                    if(controller.IsOpen)
                    {
                        Check(modules[0].walkableColliders.All(x=>x.enabled==modules[0].IsRepaired),"collision state follows settled pose during deferred close");
                        return;
                    }
                    Check(modules[0].IsRepaired && modules[0].walkableColliders.All(x=>x.enabled),"motion settled before release");
                    Check(controller.RestorationCount==1,"progress retained on close");
                    Check(BridgePracticeTools.Exact("Player").GetComponent<PlayerMovement>().enabled,"movement restored after motion");
                    if(!spellbookTested)
                    {
                        spellbookTested=true;
                        var book=BridgePracticeTools.Exact("Magic Book").GetComponent<KnowledgeInteractable>().spellbookUI;
                        int saved=controller.CheckpointIndex;var pose=modules[0].transform.localToWorldMatrix;
                        book.OpenBook();Check(book.IsBookOpen,"Spellbook opens independently");
                        for(int i=0;i<3;i++)book.nextButton.onClick.Invoke();
                        var sim=book.guidedSimulation;Check(sim.gameObject.activeInHierarchy,"fourth page 2D simulation visible");
                        sim.JumpToStep(21);Check(sim.CheckpointIndex==21,"independent learning reaches last checkpoint");
                        sim.PreviousStep();Check(sim.CheckpointIndex==20,"independent Previous works");
                        sim.Replay();Check(sim.CheckpointIndex==0,"independent Replay works");
                        controller.Open();Check(!controller.IsOpen,"practice blocked during Spellbook modal");
                        Check(controller.CheckpointIndex==saved && controller.RestorationCount==1 && modules[0].transform.localToWorldMatrix==pose,"Spellbook cannot alter real bridge or practice checkpoint");
                        book.CloseBook();Check(!book.IsBookOpen,"Spellbook closes normally");
                        log.AppendLine("Spellbook fourth-page Jump/Previous/Replay left real bridge and practice progress unchanged.");
                    }
                    controller.Open();Check(controller.IsOpen && controller.OpenCount==2 && controller.CheckpointIndex==2,"reopen resumes exact checkpoint");
                    Next(3);return;
                }
                if(stage==5)
                {
                    if(EditorApplication.timeSinceStartup-stageBegan<.6)
                    {Check(controller.CheckpointIndex==closedIndex,"closed trace stays paused");return;}
                    controller.Open();Check(controller.IsOpen && controller.CheckpointIndex==closedIndex,"return sequence resumes checkpoint");
                    Next(3);return;
                }
            }
            catch(Exception ex){Finish(false,ex.ToString());}
        }
        static bool Allowed(Collider c)=>c.transform.IsChildOf(controller.rig.fixedStartingSection) || c.transform.IsChildOf(controller.rig.destinationIsland) || modules.Any(m=>c.transform.IsChildOf(m.transform));
        static bool ApproachSegment(CharacterController cc,PlayerMovement movement,Vector2 target,float speed)
        {
            float vertical=-2;
            for(int frame=0;frame<1500;frame++)
            {
                var p=cc.transform.position;var delta=new Vector2(target.x-p.x,target.y-p.z);
                if(delta.magnitude<.5f)return p.y>38;
                if(p.y<38)return false;
                if(cc.isGrounded && vertical<0)vertical=-2;
                vertical+=movement.gravity/60f;
                var move=delta.normalized*speed/60f;
                cc.Move(new Vector3(move.x,vertical/60f,move.y));
            }
            return false;
        }
        static void ApproachTraversal()
        {
            var cc=BridgePracticeTools.Exact("Player").GetComponent<CharacterController>();var movement=cc.GetComponent<PlayerMovement>();
            var before=cc.transform.position;var rotation=cc.transform.rotation;bool wasEnabled=movement.enabled;movement.enabled=false;
            try
            {
                foreach(var start in new[]{BridgePracticeTools.Exact("Ruin01_Area/SpawnPoints/L1_Spawnpoint"),BridgePracticeTools.Exact("Magic Book")})
                {
                    var p=start.position;
                    var hits=Physics.RaycastAll(new Vector3(p.x,110,p.z),Vector3.down,140,~0,QueryTriggerInteraction.Ignore)
                        .Where(h=>h.point.y>38 && Vector3.Angle(h.normal,Vector3.up)<=cc.slopeLimit)
                        .OrderByDescending(h=>h.point.y).ToArray();
                    Check(hits.Length>0,"walkable start surface near "+start.name);
                    cc.enabled=false;cc.transform.position=hits[0].point+Vector3.up*.2f;cc.enabled=true;Physics.SyncTransforms();
                    for(int i=0;i<25;i++)cc.Move(Vector3.down*.05f);
                    foreach(var waypoint in new[]{new Vector2(-435.33f,p.z),new Vector2(-435.33f,15f),new Vector2(-435.33f,53.638f)})
                        Check(ApproachSegment(cc,movement,waypoint,movement.moveSpeed),"actual CharacterController reaches "+waypoint+" from "+start.name);
                    Check(Vector3.Distance(cc.transform.position,controller.interactionPoint.position)<controller.interactionDistance,"walked into practice interaction range from "+start.name);
                    log.AppendLine("Actual Player CharacterController walked from "+start.name+" to practice point on existing surfaces.");
                }
            }
            finally{cc.enabled=false;cc.transform.SetPositionAndRotation(before,rotation);cc.enabled=true;movement.enabled=wasEnabled;Physics.SyncTransforms();}
        }
        static RaycastHit? Ground(float x,float z)
        {
            var hits=Physics.RaycastAll(new Vector3(x,110,z),Vector3.down,130,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>Allowed(h.collider)).OrderByDescending(h=>h.point.y).ToArray();
            return hits.Length>0?hits[0]:(RaycastHit?)null;
        }
        static bool Walk(CharacterController cc,PlayerMovement movement,float x,float from,float to,float speed)
        {
            var h=Ground(x,from);if(!h.HasValue || Vector3.Angle(h.Value.normal,Vector3.up)>cc.slopeLimit)return false;
            cc.enabled=false;cc.transform.position=h.Value.point+Vector3.up*.2f;cc.enabled=true;Physics.SyncTransforms();
            for(int i=0;i<30;i++)cc.Move(Vector3.down*.05f);
            float vertical=-2,sign=Mathf.Sign(to-from),floor=h.Value.point.y-8;
            for(int frame=0;frame<3000;frame++)
            {
                var p=cc.transform.position;if(sign>0?p.z>=to-.1f:p.z<=to+.1f)return p.y>floor;
                if(p.y<floor)return false;
                if(cc.isGrounded && vertical<0)vertical=-2;vertical+=movement.gravity/60f;
                cc.Move(new Vector3(0,vertical/60f,sign*speed/60f));
            }
            return false;
        }
        static void RepairedTraversal()
        {
            var cc=BridgePracticeTools.Exact("Player").GetComponent<CharacterController>();var movement=cc.GetComponent<PlayerMovement>();
            var original=cc.transform.position;var rotation=cc.transform.rotation;bool enabled=movement.enabled;movement.enabled=false;
            try
            {
                Physics.SyncTransforms();
                foreach(float lane in new[]{-.6f,0,.6f})foreach(float speed in new[]{movement.moveSpeed,movement.runSpeed})foreach(bool reverse in new[]{false,true})
                {
                    float x=controller.rig.fixedStartingSection.position.x+lane,near=controller.rig.fixedStartingSection.position.z,far=controller.rig.destinationIsland.position.z;
                    Check(Walk(cc,movement,x,reverse?far:near,reverse?near:far,speed),"actual CharacterController repaired crossing lane="+lane+" speed="+speed+" reverse="+reverse);
                }
                log.AppendLine("Player CharacterController: 12 walking/running, forward/reverse, three-lane repaired crossings passed.");
            }
            finally{cc.enabled=false;cc.transform.SetPositionAndRotation(original,rotation);cc.enabled=true;movement.enabled=enabled;Physics.SyncTransforms();}
        }
    }
}
