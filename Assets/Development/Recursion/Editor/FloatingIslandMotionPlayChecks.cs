using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Fyp.Development.Recursion.Editor
{
    [InitializeOnLoad]
    public static class FloatingIslandMotionPlayChecks
    {
        const string Request="FYP.B2.PlayCheck",Output="Temp/FloatingBridgeB2";
        static FloatingBridgeMotionRig rig;
        static FloatingIslandMotion[] modules;
        static Dictionary<Transform,Matrix4x4> localMatrices;
        static Dictionary<MeshFilter,Mesh> meshes;
        static Dictionary<Renderer,Material[]> materials;
        static Matrix4x4[] idleMatrices;
        static Collider[][] colliders;
        static StringBuilder report;
        static double started,changed;
        static int stage,index,assertions,errors,samples;
        static bool repeated;
        static FloatingIslandMotionPlayChecks()
        {
            EditorApplication.update+=Tick;
            if(SessionState.GetBool(Request,false))Application.logMessageReceived+=Log;
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool("FYP.B2.SnapshotAfter",false))
                {
                    SessionState.SetBool("FYP.B2.SnapshotAfter",false);
                    EditorApplication.delayCall+=()=>
                    {
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Output+"/AfterPlay.unity",true);
                        if(File.Exists(Output+"/PrefabSourcesBefore.txt"))
                        {
                            string after=PrefabLinks();File.WriteAllText(Output+"/PrefabSourcesAfter.txt",after);
                            bool same=after==File.ReadAllText(Output+"/PrefabSourcesBefore.txt");
                            File.WriteAllText(Output+"/PrefabVerification.txt",same?"PASS: all ten source prefab links unchanged in Edit Mode.":"FAIL: source prefab links changed.");
                            if(!same)Debug.LogError("B2 prefab source verification failed.");
                        }
                    };
                }
            };
        }
        [MenuItem("Tools/FYP/Ruin 1/Floating Bridge B2/Run All Module Play Checks %#&F9")]
        public static void Run()
        {
            SpellbookStageTools.RequireScene();var r=FloatingIslandPrototypeTools.Rig();
            if(EditorSceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BridgePracticeController>(true)).Any(c=>c.enabled && c.gameObject.activeInHierarchy))
                throw new InvalidOperationException("Phase C owns the bridge. Run Phase C practice checks; standalone B2 reset tests require the practice controller to be disabled first.");
            if(r.placeStone3==null || r.placeStone2==null || r.placeStone1==null)throw new InvalidOperationException("Assign all three explicit module references.");
            Directory.CreateDirectory(Output);EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Output+"/BeforePlay.unity",true);
            File.WriteAllText(Output+"/PrefabSourcesBefore.txt",PrefabLinks());
            rig=null;modules=null;started=0;stage=0;index=0;assertions=0;errors=0;samples=0;repeated=false;
            Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;
            SessionState.SetBool(Request,true);EditorApplication.isPlaying=true;
        }
        static void Log(string m,string s,LogType t){if(t==LogType.Error || t==LogType.Exception || t==LogType.Assert)errors++;}
        static string PrefabLinks()
        {
            var r=FloatingIslandPrototypeTools.Rig();var text=new StringBuilder();
            foreach(var module in new[]{r.placeStone3,r.placeStone2,r.placeStone1})foreach(var mesh in module.GetComponentsInChildren<MeshFilter>(true))
            {
                string path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(mesh.gameObject);
                if(string.IsNullOrEmpty(path))throw new InvalidOperationException("Missing Edit Mode prefab source: "+FloatingIslandPrototypeTools.P(mesh.transform));
                text.AppendLine(FloatingIslandPrototypeTools.P(mesh.transform)+" -> "+path+" mesh="+AssetDatabase.GetAssetPath(mesh.sharedMesh));
            }
            return text.ToString();
        }
        static void Check(bool condition,string label){assertions++;if(!condition)throw new InvalidOperationException("B2: "+label);}
        static float MatrixError(Matrix4x4 a,Matrix4x4 b){float result=0;for(int i=0;i<16;i++)result=Mathf.Max(result,Mathf.Abs(a[i]-b[i]));return result;}
        static Matrix4x4 Local(Transform t)=>Matrix4x4.TRS(t.localPosition,t.localRotation,t.localScale);
        static void CollisionState(int i,bool enabledState)
        {Check(colliders[i].All(c=>c.enabled==enabledState),"all owned colliders in module "+i+" enabled="+enabledState);}
        static void Pose(int i,bool repaired)
        {
            var m=modules[i];var p=repaired?m.repairedPose:m.brokenPose;
            Check(!m.IsMoving && m.IsRepaired==repaired,"settled state "+i);
            Check(Vector3.Distance(m.transform.position,p.position)<.0001f && Quaternion.Angle(m.transform.rotation,p.rotation)<.01f,"exact pose "+i);CollisionState(i,repaired);
        }
        static void Begin(int nextStage,int active)
        {stage=nextStage;index=active;changed=EditorApplication.timeSinceStartup;idleMatrices=modules.Select(m=>m.transform.localToWorldMatrix).ToArray();repeated=false;}
        static void Finish(bool success,string failure=null)
        {
            Application.logMessageReceived-=Log;Directory.CreateDirectory(Output);if(report==null)report=new StringBuilder();
            report.AppendLine((success?"PASS":"FAIL")+" assertions="+assertions+" samples="+samples+" Console errors="+errors+(failure==null?"":"\n"+failure));
            File.WriteAllText(Output+"/PlayChecks.txt",report.ToString());
            if(success)Debug.Log(report.ToString());else Debug.LogError(report.ToString());
            SessionState.SetBool(Request,false);SessionState.SetBool("FYP.B2.SnapshotAfter",true);EditorApplication.isPlaying=false;
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Request,false) || !EditorApplication.isPlaying)return;
            try
            {
                if(started==0){started=EditorApplication.timeSinceStartup;return;}
                if(EditorApplication.timeSinceStartup-started>150)throw new TimeoutException("B2 verification exceeded 150 seconds.");
                if(rig==null)
                {
                    if(EditorApplication.timeSinceStartup-started<1)return;
                    report=new StringBuilder("ONE FLOATING BRIDGE - B2 actual Unity Play Mode checks\n");
                    rig=FloatingIslandPrototypeTools.Rig();modules=new[]{rig.placeStone3,rig.placeStone2,rig.placeStone1};
                    Check(modules.Distinct().Count()==3,"distinct explicit module references");
                    for(int i=0;i<3;i++)Check(modules[i].transform==FloatingIslandPrototypeTools.Exact(FloatingIslandPrototypeTools.ModulePaths[i]+"/MotionRoot"),"reference matches exact scene path "+i);
                    Check(rig.fixedStartingSection==FloatingIslandPrototypeTools.Exact("Bridge"),"fixed first section reference");
                    for(int n=3;n>=1;n--){Check(rig.TryGetSection(n,out var section) && section==modules[3-n],"placeStone mapping "+n);}
                    Check(!rig.TryGetSection(0,out _) && !rig.TryGetSection(4,out _),"base case/unmapped values produce no module");
                    colliders=modules.Select(m=>m.GetComponentsInChildren<Collider>(true)).ToArray();
                    Check(colliders[0].Length==3 && colliders[1].Length==3 && colliders[2].Length==4,"3 + 3 + 4 original colliders");
                    Check(colliders.SelectMany(c=>c).Distinct().Count()==10,"ten unique collider instances");
                    Check(modules.All(m=>m.walkableColliders.OrderBy(c=>c.GetInstanceID()).SequenceEqual(m.GetComponentsInChildren<Collider>(true).OrderBy(c=>c.GetInstanceID()))),"component includes every owned collider");
                    Check(colliders.SelectMany(c=>c).All(c=>c is MeshCollider mc && !mc.convex && !c.isTrigger),"original non-convex MeshCollider configurations");
                    Check(modules.All(m=>m.GetComponentsInChildren<Rigidbody>(true).Length==0),"no Rigidbody added");
                    Check(FloatingIslandPrototypeTools.Exact("Bridge (3)/MotionRoot/PhaseA_EndConnector").IsChildOf(modules[2].transform),"connector belongs to third module");
                    var allTransforms=modules.SelectMany(m=>m.GetComponentsInChildren<Transform>(true)).Distinct().ToArray();localMatrices=allTransforms.ToDictionary(t=>t,Local);
                    meshes=modules.SelectMany(m=>m.GetComponentsInChildren<MeshFilter>(true)).ToDictionary(m=>m,m=>m.sharedMesh);
                    materials=modules.SelectMany(m=>m.GetComponentsInChildren<Renderer>(true)).ToDictionary(r=>r,r=>r.sharedMaterials);
                    Check(meshes.All(p=>!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(p.Value))),"original mesh assets remain referenced at runtime");
                    var others=EditorSceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>()).Where(r=>!modules.Any(m=>r.transform.IsChildOf(m.transform))).ToArray();
                    for(int i=0;i<3;i++)
                    {
                        Pose(i,false);Check(!modules[i].GetComponentsInChildren<Renderer>().Any(r=>others.Any(o=>r.bounds.Intersects(o.bounds))),"broken endpoint clear of static renderers "+i);
                        for(int j=i+1;j<3;j++)Check(!modules[i].GetComponentsInChildren<Renderer>().Any(a=>modules[j].GetComponentsInChildren<Renderer>().Any(b=>a.bounds.Intersects(b.bounds))),"broken module bounds separated");
                    }
                    report.AppendLine("Startup: all three broken, ten original colliders off, connector moved with third module; endpoint bounds clear.");
                    Begin(1,0);modules[0].Repair();return;
                }
                samples++;
                foreach(var p in localMatrices.Where(p=>!modules.Any(m=>m.transform==p.Key)))Check(MatrixError(Local(p.Key),p.Value)<.00001f,"rigid child transform "+p.Key.name);
                Check(meshes.All(p=>p.Key.sharedMesh==p.Value),"mesh references unchanged");Check(materials.All(p=>p.Key.sharedMaterials.SequenceEqual(p.Value)),"material references unchanged");
                for(int i=0;i<3;i++)
                {
                    Check((modules[i].transform.localScale-Vector3.one).sqrMagnitude<.00000001f,"motion scale unchanged");
                    CollisionState(i,modules[i].IsRepaired);
                    if((stage==1 || stage==2 || stage==4 || stage==5) && i!=index)Check(MatrixError(modules[i].transform.localToWorldMatrix,idleMatrices[i])<.00001f,"non-target module independent "+i);
                }
                if(stage==1)
                {
                    if(!repeated && EditorApplication.timeSinceStartup-changed>1){modules[index].Repair();repeated=true;}
                    if(modules[index].IsMoving)return;Pose(index,true);
                    Check(EditorApplication.timeSinceStartup-changed<modules[index].moveDuration+.7f,"idempotent repeated Repair");
                    report.AppendLine("Independent repair PASS "+FloatingIslandPrototypeTools.ModulePaths[index]);Begin(2,index);modules[index].ResetToBroken();return;
                }
                if(stage==2)
                {
                    if(modules[index].IsMoving)return;Pose(index,false);report.AppendLine("Independent reset PASS "+FloatingIslandPrototypeTools.ModulePaths[index]);
                    if(index<2){Begin(1,index+1);modules[index].Repair();return;}
                    Begin(3,0);foreach(var m in modules)m.Repair();return;
                }
                if(stage==3)
                {
                    if(modules.Any(m=>m.IsMoving))return;for(int i=0;i<3;i++)Pose(i,true);
                    RepairedTraversal();Begin(4,0);modules[0].ResetToBroken();return;
                }
                if(stage==4)
                {
                    if(modules[index].IsMoving)return;Pose(index,false);MissingModuleTraversal(index);
                    Begin(5,index);modules[index].Repair();return;
                }
                if(stage==5)
                {
                    if(modules[index].IsMoving)return;Pose(index,true);
                    if(index<2){Begin(4,index+1);modules[index].ResetToBroken();return;}
                    report.AppendLine("Sequential missing-module gating passed; full route restored. No recursion/lesson code invoked.");
                    Check(errors==0,"no Console errors");Finish(true);
                }
            }
            catch(Exception ex){Finish(false,ex.ToString());}
        }
        static bool Allowed(Collider c)=>c.transform.IsChildOf(rig.fixedStartingSection) || c.transform.IsChildOf(rig.destinationIsland) || modules.Any(m=>c.transform.IsChildOf(m.transform));
        static RaycastHit? Ground(float x,float z)
        {
            var hits=Physics.RaycastAll(new Vector3(x,110,z),Vector3.down,130,~0,QueryTriggerInteraction.Ignore).Where(h=>Allowed(h.collider)).OrderByDescending(h=>h.point.y).ToArray();
            return hits.Length==0?(RaycastHit?)null:hits[0];
        }
        static bool Walk(CharacterController cc,float x,float from,float to,float speed)
        {
            var h=Ground(x,from);if(!h.HasValue || Vector3.Angle(h.Value.normal,Vector3.up)>cc.slopeLimit)return false;
            cc.enabled=false;cc.transform.position=h.Value.point+Vector3.up*.2f;cc.enabled=true;Physics.SyncTransforms();
            for(int i=0;i<30;i++)cc.Move(Vector3.down*.05f);
            float vertical=-2,sign=Mathf.Sign(to-from),floor=h.Value.point.y-8;
            for(int frame=0;frame<3000;frame++)
            {
                var p=cc.transform.position;if(sign>0?p.z>=to-.1f:p.z<=to+.1f)return p.y>floor;
                if(p.y<floor)return false;
                if(cc.isGrounded && vertical<0)vertical=-2;vertical+=cc.GetComponent<PlayerMovement>().gravity/60f;
                cc.Move(new Vector3(0,vertical/60f,sign*speed/60f));
            }
            return false;
        }
        static void WithPlayer(Action<CharacterController,PlayerMovement> test)
        {
            var cc=FloatingIslandPrototypeTools.Exact("Player").GetComponent<CharacterController>();var movement=cc.GetComponent<PlayerMovement>();
            var p=cc.transform.position;var r=cc.transform.rotation;bool wasEnabled=movement.enabled;movement.enabled=false;
            try{test(cc,movement);}finally{cc.enabled=false;cc.transform.SetPositionAndRotation(p,r);cc.enabled=true;movement.enabled=wasEnabled;Physics.SyncTransforms();}
        }
        static void RepairedTraversal()
        {
            Physics.SyncTransforms();WithPlayer((cc,movement)=>
            {
                foreach(float lane in new[]{-.6f,0,.6f})foreach(float speed in new[]{movement.moveSpeed,movement.runSpeed})foreach(bool reverse in new[]{false,true})
                {
                    float x=rig.fixedStartingSection.position.x+lane,near=rig.fixedStartingSection.position.z,far=rig.destinationIsland.position.z;
                    bool pass=Walk(cc,x,reverse?far:near,reverse?near:far,speed);
                    report.AppendLine("REPAIRED ROUTE lane="+lane+" speed="+speed+" reverse="+reverse+" "+(pass?"PASS":"FAIL")+" end="+cc.transform.position.ToString("F3"));Check(pass,"actual Player complete repaired route");
                }
            });
        }
        static void MissingModuleTraversal(int missing)
        {
            Physics.SyncTransforms();CollisionState(missing,false);
            float near=missing==0?rig.fixedStartingSection.position.z:modules[missing-1].repairedPose.position.z;
            float far=missing==2?rig.destinationIsland.position.z:modules[missing+1].repairedPose.position.z;
            int tested=0,crossed=0;
            WithPlayer((cc,movement)=>
            {
                foreach(float speed in new[]{movement.moveSpeed,movement.runSpeed})foreach(bool reverse in new[]{false,true})
                for(float dx=-18;dx<=18;dx+=.75f)
                {
                    float x=rig.fixedStartingSection.position.x+dx,start=reverse?far:near,end=reverse?near:far;
                    var h=Ground(x,start);if(!h.HasValue || Vector3.Angle(h.Value.normal,Vector3.up)>cc.slopeLimit)continue;
                    tested++;if(Walk(cc,x,start,end,speed)){crossed++;report.AppendLine("UNEXPECTED CROSSING missing="+missing+" x="+x+" speed="+speed+" reverse="+reverse);}
                }
            });
            report.AppendLine("MISSING "+FloatingIslandPrototypeTools.ModulePaths[missing]+" straight traversal sweeps="+tested+" crossings="+crossed+"; all owned colliders disabled including connector when applicable.");
            Check(tested>0 && crossed==0,"no sampled shortcut past missing module "+missing);
        }
    }
}
