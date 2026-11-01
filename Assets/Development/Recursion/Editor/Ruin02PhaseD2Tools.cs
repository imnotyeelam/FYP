using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin02PhaseD2Tools
    {
        const string ScenePath="Assets/Scenes/MagicWorld_Development.unity";
        const string Backup="LocalBackups/Ruin2PhaseD2_20261101";
        const string ProjectileFolder="Assets/GabrielAguiarProductions/UniqueProjectilesVol_1/Prefabs/Projectiles/";

        sealed class Mapping
        {
            public int parameter; public string label, staff, focus, orb, projectile, end;
            public Vector3 endPosition;
            public Mapping(int p,string l,string s,string f,string o,string proj,string e,Vector3 pos)
            {parameter=p;label=l;staff=s;focus=f;orb=o;projectile=proj;end=e;endPosition=pos;}
        }
        static readonly Mapping[] Maps = {
            new Mapping(1,"Air","Air staff","AirStaffFocus","Glowing orb 23","vfx_Projectile_Spell01Violet.prefab","AirFlightCameraEnd",new Vector3(796.5f,14.2f,196.5f)),
            new Mapping(2,"Earth","Earth staff","EarthStaffFocus","Glowing orb 36","vfx_Projectile_Spell02Green.prefab","EarthFlightCameraEnd",new Vector3(808.5f,14.68f,196.5f)),
            new Mapping(3,"Fire","Fire staff","FireStaffFocus","Glowing orb 31","vfx_Projectile_Fireball01Orange.prefab","FireFlightCameraEnd",new Vector3(798.5f,14.38f,202.5f)),
            new Mapping(4,"Ice","Ice staff","IceStaffFocus","Glowing orb 22","vfx_Projectile_Comet_Blue.prefab","IceFlightCameraEnd",new Vector3(806.5f,14.56f,202.5f))
        };

        static Transform Need(Transform parent,string path)
        {
            var value=parent.Find(path);
            if(value==null)throw new InvalidOperationException("Missing required object: "+parent.name+"/"+path);
            return value;
        }
        static Transform Area()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(scene.path!=ScenePath)throw new InvalidOperationException("Open MagicWorld_Development first.");
            return scene.GetRootGameObjects().Single(g=>g.name=="Ruin02_Area").transform;
        }
        static Transform Cave()
        {
            var scene=EditorSceneManager.GetActiveScene();
            return scene.GetRootGameObjects().Single(g=>g.name=="Ruin2 Environment").transform;
        }
        static string ProjectilePath(Mapping map)=>ProjectileFolder+map.projectile;

        static string CameraSafety(Transform cave,Vector3 cameraPosition,Vector3 focus)
        {
            const float probeRadius=.30f;
            var overlaps=Physics.OverlapSphere(cameraPosition,probeRadius,~0,QueryTriggerInteraction.Ignore)
                .Where(c=>c.transform.IsChildOf(cave)).ToArray();
            var delta=focus-cameraPosition;
            var blockers=delta.sqrMagnitude<.0001f ? Array.Empty<RaycastHit>() :
                Physics.RaycastAll(cameraPosition,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore)
                    .Where(hit=>hit.transform.IsChildOf(cave)).ToArray();
            return overlaps.Length==0 && blockers.Length==0 ? "clear" :
                $"blocked (overlaps={overlaps.Length}, sightline={blockers.Length})";
        }

        static void RequireCameraSafety(Transform cave,Mapping map,Transform focus)
        {
            var safety=CameraSafety(cave,map.endPosition,focus.position);
            if(safety!="clear")throw new InvalidOperationException(
                $"{map.label} camera-end candidate is unsafe: {safety}. No scene changes were made.");
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase D2/Audit and Backup")]
        public static void Audit()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=ScenePath)
                throw new InvalidOperationException("Open the development scene in Edit Mode first.");
            var area=Area();var chamber=Need(area,"Gameplay/RitualChamber");var cave=Cave();
            var staffs=Need(chamber,"Staffs");var orbs=Need(chamber,"ChaoticMagic");var markers=Need(chamber,"CinematicMarkers");
            var report=string.Join("\n",Maps.Select(map=> {
                var staff=Need(staffs,map.staff);var focus=Need(markers,map.focus);var orb=Need(orbs,map.orb);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePath(map));
                if(prefab==null)throw new InvalidOperationException("Missing projectile prefab: "+ProjectilePath(map));
                var renderers=staff.GetComponentsInChildren<Renderer>(true).Length;
                var bodies=prefab.GetComponentsInChildren<Rigidbody>(true).Length;
                var colliders=prefab.GetComponentsInChildren<Collider>(true).Length;
                var end=markers.Find(map.end);
                var endPosition=end==null ? map.endPosition : end.position;
                return $"{map.label}: staff={staff.position:F3}, focus={focus.position:F3}, orb={orb.position:F3}, renderers={renderers}, prefab={map.projectile}, bodies={bodies}, colliders={colliders}, camera={CameraSafety(cave,endPosition,focus.position)}";
            }));
            Directory.CreateDirectory(Backup);File.WriteAllText(Backup+"/Audit.txt",report);
            if(!EditorSceneManager.SaveScene(scene,Backup+"/BeforeLive.unity",true))throw new IOException("D2 backup failed.");
            Debug.Log("PASS Ruin 2 D2 audit and backup:\n"+report);
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase D2/Install Four-Staff Mapping")]
        public static void Install()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=ScenePath || scene.isDirty)
                throw new InvalidOperationException("Install requires a clean development scene in Edit Mode.");
            if(!File.Exists(Backup+"/BeforeLive.unity"))throw new InvalidOperationException("Run D2 Audit and Backup first.");
            var area=Area();var gameplay=Need(area,"Gameplay");var chamber=Need(gameplay,"RitualChamber");var cave=Cave();
            var staffs=Need(chamber,"Staffs");var orbs=Need(chamber,"ChaoticMagic");var markers=Need(chamber,"CinematicMarkers");
            var controller=Need(gameplay,"AirStaffRestoration").GetComponent<Ruin02AirRestoration>();
            if(controller==null || controller.cameraRig==null || controller.practice==null)throw new InvalidOperationException("D1 controller missing.");
            if(controller.restorations.Length==4){Verify();Debug.Log("Ruin 2 D2 mapping already installed; no scene changes were made.");return;}
            if(controller.restorations.Length!=0)throw new InvalidOperationException("Unexpected partial D2 mapping. No changes were made.");

            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Install Ruin 2 four-staff restoration");
            try
            {
                var definitions=new Ruin02StaffRestorationEntry[Maps.Length];
                for(int i=0;i<Maps.Length;i++)
                {
                    var map=Maps[i];var end=markers.Find(map.end);var focus=Need(markers,map.focus);
                    if(end==null)RequireCameraSafety(cave,map,focus);
                    if(end==null)
                    {
                        var go=new GameObject(map.end);Undo.RegisterCreatedObjectUndo(go,"Add "+map.end);
                        Undo.SetTransformParent(go.transform,markers,"Parent "+map.end);
                        go.transform.position=map.endPosition;
                        go.transform.rotation=Quaternion.LookRotation(focus.position-go.transform.position);
                        end=go.transform;
                    }
                    definitions[i]=new Ruin02StaffRestorationEntry {
                        absorbMagicParameter=map.parameter,label=map.label,staff=Need(staffs,map.staff),
                        focusMarker=focus,cameraEndView=end,chaoticOrb=Need(orbs,map.orb).gameObject,
                        projectilePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePath(map)),
                        flightSeconds=1.7f,flightArcHeight=.45f
                    };
                }
                // Keep the original D1 fields intact for its diagnostic tool; runtime now uses this array.
                controller.restorations=definitions;EditorUtility.SetDirty(controller);EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save Phase D2 scene.");
                Undo.CollapseUndoOperations(group);
                Verify();Debug.Log("Installed D2 data-driven mapping for Air, Earth, Fire and Ice. No prefab or material asset was modified.");
            }
            catch { Undo.RevertAllDownToGroup(group);throw; }
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase D2/Verify Scene References")]
        public static void Verify()
        {
            var area=Area();var controller=Need(area,"Gameplay/AirStaffRestoration").GetComponent<Ruin02AirRestoration>();
            if(controller==null || controller.restorations==null || controller.restorations.Length!=4)throw new InvalidOperationException("D2 mapping count is invalid.");
            foreach(var map in Maps)
            {
                var entry=controller.restorations.SingleOrDefault(e=>e!=null && e.absorbMagicParameter==map.parameter);
                if(entry==null || entry.label!=map.label || entry.staff.name!=map.staff || entry.focusMarker.name!=map.focus ||
                    entry.chaoticOrb.name!=map.orb || entry.cameraEndView.name!=map.end ||
                    AssetDatabase.GetAssetPath(entry.projectilePrefab)!=ProjectilePath(map))
                    throw new InvalidOperationException("D2 mapping failed for "+map.label+".");
            }
            Debug.Log("PASS Ruin 2 D2 scene references: four staffs, four matching orbs, focus/end markers and runtime-only projectile sources.");
        }

        static Ruin02AirRestoration pending;
        static Ruin02PracticeController practice;
        static int expectedParameter;
        static float deadline;
        static bool submittedWrong;

        [MenuItem("Tools/FYP/Ruin 2/Phase D2/Run Four-Staff Play Test")]
        public static void RunFourStaffPlayTest()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            pending=UnityEngine.Object.FindObjectsByType<Ruin02AirRestoration>(FindObjectsSortMode.None).Single();practice=pending.practice;
            if(practice.Session.Index!=1 || practice.Session.Question?.Id!="ruin02.call.4")throw new InvalidOperationException("Use a fresh Play session.");
            if(pending.RestoredCount!=0 || pending.restorations.Any(e=>!e.chaoticOrb.activeSelf))throw new InvalidOperationException("Initial staff/orb state is invalid.");
            practice.Open();expectedParameter=0;submittedWrong=false;deadline=Time.realtimeSinceStartup+35f;
            EditorApplication.update+=AdvanceTest;Debug.Log("Ruin 2 D2 four-staff test started.");
        }

        static void AdvanceTest()
        {
            try
            {
                if(!EditorApplication.isPlaying || pending==null || practice==null)throw new InvalidOperationException("D2 Play test lost its runtime objects.");
                if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("D2 Play test timed out.");
                if(pending.IsPlaying)return;
                if(expectedParameter>0)
                {
                    if(!pending.IsStaffRestored(expectedParameter) || pending.restorations.Single(e=>e.absorbMagicParameter==expectedParameter).chaoticOrb.activeSelf)
                        throw new InvalidOperationException("Staff "+expectedParameter+" did not restore independently.");
                    expectedParameter=0;submittedWrong=false;
                }
                while(practice.Session.Advancing)practice.Session.TryAdvanceApprovedStep();
                if(practice.Session.Complete)
                {
                    if(pending.RestoredCount!=4 || pending.restorations.Any(e=>e.chaoticOrb.activeSelf) || practice.Session.Current.Frames.Count!=0)
                        throw new InvalidOperationException("D2 completion left a staff, orb, or stack frame unresolved.");
                    practice.Close();practice.Open();
                    if(pending.RestoredCount!=4 || pending.restorations.Any(e=>e.chaoticOrb.activeSelf))throw new InvalidOperationException("D2 restoration reset after reopening.");
                    practice.Close();EditorApplication.update-=AdvanceTest;
                    Debug.Log("PASS Ruin 2 D2 Play: all staffs grey initially; wrong/call/base held; four ordered projectiles restored matching staffs and cleared only matching orbs; modal reopen persisted; trace completed empty.");
                    pending=null;practice=null;return;
                }
                var question=practice.Session.Question;
                if(question==null)return;
                if(!submittedWrong)
                {
                    int before=practice.Session.Index;int restored=pending.RestoredCount;
                    if(practice.Submit("absorbMagic(99)").Status!=Ruin02AnswerStatus.Incorrect || practice.Session.Index!=before || pending.RestoredCount!=restored)
                        throw new InvalidOperationException("Wrong answer changed D2 restoration state.");
                    submittedWrong=true;return;
                }
                var result=practice.Submit(question.Expected);
                if(result.Status!=Ruin02AnswerStatus.Correct)throw new InvalidOperationException("Correct answer rejected: "+question.Id);
                if(question.Expected.StartsWith("absorbMagic("))
                {
                    expectedParameter=int.Parse(question.Expected.Substring("absorbMagic(".Length,1));
                    while(practice.Session.Advancing && !pending.IsPlaying)practice.Session.TryAdvanceApprovedStep();
                    if(!pending.IsPlaying)throw new InvalidOperationException("Successful "+question.Expected+" did not start its cinematic.");
                }
                else submittedWrong=false;
            }
            catch(Exception e)
            {
                EditorApplication.update-=AdvanceTest;Debug.LogException(e);pending=null;practice=null;
            }
        }
    }
}
