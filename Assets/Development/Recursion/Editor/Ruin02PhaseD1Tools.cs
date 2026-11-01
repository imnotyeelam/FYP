using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin02PhaseD1Tools
    {
        const string ScenePath = "Assets/Scenes/MagicWorld_Development.unity";
        const string ProjectilePath = "Assets/GabrielAguiarProductions/UniqueProjectilesVol_1/Prefabs/Projectiles/vfx_Projectile_Spell01Violet.prefab";
        const string Backup = "LocalBackups/Ruin2PhaseD1_20261101";

        static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
        static string V(Vector3 p) => p.ToString("F3");

        [MenuItem("Tools/FYP/Ruin 2/Phase D1/Audit and Backup")]
        public static void Audit()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
                throw new InvalidOperationException("Open MagicWorld_Development in Edit Mode first.");
            var roots = scene.GetRootGameObjects();
            var area = roots.Single(g => g.name == "Ruin02_Area").transform;
            var chamber = area.Find("Gameplay/RitualChamber");
            if(chamber == null) throw new InvalidOperationException("RitualChamber missing.");
            Directory.CreateDirectory(Backup);
            var s = new StringBuilder();
            s.AppendLine("scene=" + scene.path + " dirty=" + scene.isDirty);
            s.AppendLine("ritual=" + PathOf(chamber) + " pos=" + V(chamber.position));
            foreach(var t in chamber.GetComponentsInChildren<Transform>(true))
            {
                s.AppendLine("OBJECT " + PathOf(t) + " active=" + t.gameObject.activeSelf +
                    " pos=" + V(t.position) + " rot=" + V(t.eulerAngles) + " scale=" + V(t.lossyScale) +
                    " prefab=" + PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) +
                    " components=" + string.Join(",", t.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name)));
                foreach(var r in t.GetComponents<Renderer>())
                {
                    s.AppendLine("  RENDER " + r.GetType().Name + " enabled=" + r.enabled + " bounds=" + r.bounds);
                    foreach(var m in r.sharedMaterials.Where(m => m != null))
                    {
                        s.AppendLine("  MAT " + AssetDatabase.GetAssetPath(m) + " shader=" + m.shader.name +
                            " _BaseColor=" + (m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : "none") +
                            " _Color=" + (m.HasProperty("_Color") ? m.GetColor("_Color").ToString() : "none") +
                            " _EmissionColor=" + (m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor").ToString() : "none") +
                            " _BaseMap=" + (m.HasProperty("_BaseMap") ? AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")) : "none"));
                    }
                }
                foreach(var c in t.GetComponents<Collider>())s.AppendLine("  COLLIDER " + c.GetType().Name + " enabled=" + c.enabled + " bounds=" + c.bounds);
            }
            var controller = area.Find("Gameplay/Ruin02PracticeManager")?.GetComponent<Ruin02PracticeController>();
            if(controller != null)s.AppendLine("PRACTICE cameraFollow=" + PathOf(controller.cameraFollow.transform) +
                " panel=" + PathOf(controller.panel.transform) + " movement=" + PathOf(controller.playerMovement.transform));
            foreach(var camera in roots.SelectMany(g => g.GetComponentsInChildren<Camera>(true)))
                s.AppendLine("CAMERA " + PathOf(camera.transform) + " pos=" + V(camera.transform.position) +
                    " rot=" + V(camera.transform.eulerAngles) + " enabled=" + camera.enabled +
                    " fov=" + camera.fieldOfView + " clip=" + camera.nearClipPlane + " / " + camera.farClipPlane);
            Physics.SyncTransforms();
            var cave = roots.Single(g => g.name == "Ruin2 Environment").transform;
            var origin = chamber.Find("CinematicMarkers/ProjectileOrigin").position;
            var target = chamber.Find("CinematicMarkers/AirStaffFocus").position;
            foreach(var pos in new[] {
                chamber.Find("CinematicMarkers/ChallengeViewpoint").position,
                new Vector3(798.5f,14.2f,195.5f),
                new Vector3(796.5f,14.2f,196.5f),
                new Vector3(795.5f,14.5f,198f)})
            {
                var overlaps = Physics.OverlapSphere(pos,.3f,~0,QueryTriggerInteraction.Ignore)
                    .Where(c => c.transform.IsChildOf(cave)).Select(c => PathOf(c.transform)).Distinct();
                var focus = Vector3.Lerp(origin,target,Mathf.InverseLerp(800,794,pos.x));
                var blocks = Physics.RaycastAll(focus,(pos-focus).normalized,Vector3.Distance(pos,focus),~0,QueryTriggerInteraction.Ignore)
                    .Where(h => h.transform.IsChildOf(cave)).OrderBy(h => h.distance).Take(3)
                    .Select(h => PathOf(h.transform) + "@" + h.distance.ToString("F2"));
                s.AppendLine("CAMERA_CANDIDATE pos=" + V(pos) + " caveOverlaps=" + string.Join(",",overlaps) +
                    " caveSightBlocks=" + string.Join(",",blocks));
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePath);
            if(prefab == null)throw new InvalidOperationException("Violet projectile prefab missing.");
            foreach(var t in prefab.GetComponentsInChildren<Transform>(true))
                s.AppendLine("PREFAB " + PathOf(t) + " components=" +
                    string.Join(",", t.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name)));
            File.WriteAllText(Backup + "/Audit.txt", s.ToString());
            if(!EditorSceneManager.SaveScene(scene, Backup + "/BeforeLive.unity", true))
                throw new IOException("Live scene backup failed.");
            Debug.Log("Ruin 2 Phase D1 audit and backup saved. Scene dirty=" + scene.isDirty);
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase D1/Install Air Prototype")]
        public static void Install()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=ScenePath || scene.isDirty)
                throw new InvalidOperationException("Install requires a clean development scene in Edit Mode. Save or protect any live changes first.");
            if(!File.Exists(Backup+"/BeforeLive.unity"))
                throw new InvalidOperationException("Run Audit and Backup before installing D1.");
            var roots=scene.GetRootGameObjects();
            var area=roots.Single(g=>g.name=="Ruin02_Area").transform;
            var gameplay=area.Find("Gameplay");
            var chamber=gameplay.Find("RitualChamber");
            var markers=chamber.Find("CinematicMarkers");
            var existingRoot=gameplay.Find("AirStaffRestoration");
            var existingEnd=markers.Find("AirFlightCameraEnd");
            if(existingRoot!=null || existingEnd!=null)
            {
                if(existingRoot==null || existingEnd==null ||
                    existingRoot.GetComponent<Ruin02AirRestoration>()==null ||
                    existingRoot.GetComponent<Ruin02CinematicCamera>()==null)
                    throw new InvalidOperationException("A partial D1 installation exists. No duplicate was created; inspect the existing object before repairing it.");
                Verify();
                Debug.Log("Ruin 2 D1 Air prototype is already installed and valid; no scene changes were made.");
                return;
            }
            var practice=gameplay.Find("Ruin02PracticeManager").GetComponent<Ruin02PracticeController>();
            var staff=chamber.Find("Staffs/Air staff");
            var orb=chamber.Find("ChaoticMagic/Glowing orb 23");
            var origin=markers.Find("ProjectileOrigin");
            var focus=markers.Find("AirStaffFocus");
            var start=markers.Find("ChallengeViewpoint");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePath);
            var camera=practice.cameraFollow.GetComponent<Camera>();
            if(staff==null || orb==null || origin==null || focus==null || start==null || prefab==null || camera==null)
                throw new InvalidOperationException("Air prototype dependencies are incomplete.");
            Undo.IncrementCurrentGroup();
            int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Install Ruin 2 Air restoration prototype");
            try
            {
                var end=new GameObject("AirFlightCameraEnd");
                Undo.RegisterCreatedObjectUndo(end,"Add Air camera end pose");
                Undo.SetTransformParent(end.transform,markers,"Parent Air camera pose");
                end.transform.position=new Vector3(796.5f,14.2f,196.5f);
                end.transform.rotation=Quaternion.LookRotation(focus.position-end.transform.position);
                var root=new GameObject("AirStaffRestoration");
                Undo.RegisterCreatedObjectUndo(root,"Add Air restoration manager");
                Undo.SetTransformParent(root.transform,gameplay,"Parent Air restoration manager");
                root.transform.localPosition=Vector3.zero;
                var rig=Undo.AddComponent<Ruin02CinematicCamera>(root);
                rig.gameplayCamera=camera;rig.flightStartView=start;rig.flightEndView=end.transform;
                var restoration=Undo.AddComponent<Ruin02AirRestoration>(root);
                restoration.practice=practice;restoration.cameraRig=rig;
                restoration.airStaff=staff;restoration.projectileOrigin=origin;
                restoration.airStaffFocus=focus;restoration.purpleOrb=orb.gameObject;
                restoration.violetProjectilePrefab=prefab;
                EditorUtility.SetDirty(rig);EditorUtility.SetDirty(restoration);
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Failed to save development scene after D1 install.");
                Undo.CollapseUndoOperations(group);
                Debug.Log("Installed D1 Air prototype: one manager and one camera end marker. Source prefab and materials remain unchanged.");
            }
            catch{Undo.RevertAllDownToGroup(group);throw;}
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase D1/Verify Scene References")]
        public static void Verify()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(scene.path!=ScenePath)throw new InvalidOperationException("Open development scene first.");
            var area=scene.GetRootGameObjects().Single(g=>g.name=="Ruin02_Area").transform;
            var restoration=area.Find("Gameplay/AirStaffRestoration")?.GetComponent<Ruin02AirRestoration>();
            if(restoration==null || restoration.practice==null || restoration.cameraRig==null ||
                restoration.airStaff.name!="Air staff" || restoration.purpleOrb.name!="Glowing orb 23" ||
                AssetDatabase.GetAssetPath(restoration.violetProjectilePrefab)!=ProjectilePath ||
                restoration.cameraRig.flightEndView.name!="AirFlightCameraEnd")
                throw new InvalidOperationException("D1 Air references failed validation.");
            Debug.Log("PASS Ruin 2 D1 scene references: original Air staff, purple orb, existing Phase C event, Main Camera, and violet source prefab.");
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase D1/Skip Air Cinematic (Play Only)")]
        public static void Skip()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required.");
            var restoration=UnityEngine.Object.FindObjectsByType<Ruin02AirRestoration>(FindObjectsSortMode.None).Single();
            restoration.SkipCurrentSequence();
            Debug.Log("D1 Air cinematic skipped in this Play session; camera and modal state restored.");
        }

        static Ruin02AirRestoration pendingAir;
        static Ruin02PracticeController pendingPractice;
        static float midCheckAfter;
        static float checkAfter;
        static Vector3 cameraBefore;
        static Quaternion rotationBefore;
        static bool midFlightPassed;

        [MenuItem("Tools/FYP/Ruin 2/Phase D1/Run Air Sequence Test (Play Mode)")]
        public static void RunAirSequenceTest()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            var air=UnityEngine.Object.FindObjectsByType<Ruin02AirRestoration>(FindObjectsSortMode.None).Single();
            var practice=air.practice;
            if(practice.Session.Index!=1 || practice.Session.Question?.Id!="ruin02.call.4")
                throw new InvalidOperationException("Run the D1 sequence test in a fresh Play session.");
            if(air.IsRestored || !air.purpleOrb.activeSelf || air.IsPlaying)
                throw new InvalidOperationException("Air initial state is incorrect.");
            var renderer=air.airStaff.GetComponentInChildren<MeshRenderer>();
            var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block,0);
            int baseColor=Shader.PropertyToID("_BaseColor");
            if(!block.HasColor(baseColor) || block.GetColor(baseColor).r>.5f)
                throw new InvalidOperationException("Air staff did not start grey.");
            practice.Open();
            if(!practice.IsOpen)throw new InvalidOperationException("Practice modal failed to open.");
            if(practice.Submit("absorbMagic(4)").Status!=Ruin02AnswerStatus.Incorrect || air.IsPlaying)
                throw new InvalidOperationException("Wrong Call answer triggered Air.");
            if(practice.Submit("sealMagic(3)").Status!=Ruin02AnswerStatus.Correct)
                throw new InvalidOperationException("Call answer failed.");
            while(practice.Session.Advancing)practice.Session.TryAdvanceApprovedStep();
            if(air.IsPlaying || practice.Submit("return").Status!=Ruin02AnswerStatus.Correct)
                throw new InvalidOperationException("Base Case triggered Air or failed.");
            while(practice.Session.Advancing)practice.Session.TryAdvanceApprovedStep();
            if(air.IsPlaying || practice.Submit("absorbMagic(2)").Status!=Ruin02AnswerStatus.Incorrect)
                throw new InvalidOperationException("Wrong Return answer triggered Air.");
            cameraBefore=air.cameraRig.gameplayCamera.transform.position;
            rotationBefore=air.cameraRig.gameplayCamera.transform.rotation;
            if(practice.Submit("absorbMagic(1)").Status!=Ruin02AnswerStatus.Correct)
                throw new InvalidOperationException("absorbMagic(1) was rejected.");
            while(practice.Session.Advancing && !air.IsPlaying)practice.Session.TryAdvanceApprovedStep();
            if(!air.IsPlaying || !practice.IsCinematicActive || practice.panel.gameObject.activeSelf ||
                practice.playerMovement.enabled || practice.cameraFollow.enabled || !air.purpleOrb.activeSelf)
                throw new InvalidOperationException("Air cinematic did not begin with modal ownership intact.");
            // Escape invokes Ruin02PracticeController.Close(). During the cinematic it must queue
            // the close, not release movement/cursor ownership or interrupt the camera return.
            practice.Close();
            if(!practice.IsOpen || !practice.IsCinematicActive || practice.panel.gameObject.activeSelf ||
                practice.playerMovement.enabled || practice.cameraFollow.enabled)
                throw new InvalidOperationException("Closing during the Air cinematic released modal state early.");
            pendingAir=air;pendingPractice=practice;midFlightPassed=false;
            midCheckAfter=Time.realtimeSinceStartup+1.1f;
            checkAfter=Time.realtimeSinceStartup+5.5f;
            EditorApplication.update+=CheckAirSequence;
            Debug.Log("Ruin 2 D1 Air test running; checking restored state after cinematic.");
        }

        static void CheckAirSequence()
        {
            if(!midFlightPassed && Time.realtimeSinceStartup>=midCheckAfter)
            {
                try
                {
                    var host=pendingAir!=null?pendingAir.transform.Find("AirProjectileRuntime"):null;
                    var projectile=host!=null && host.childCount>0?host.GetChild(0):null;
                    float projectileDistance=projectile!=null?
                        Vector3.Distance(projectile.position,pendingAir.projectileOrigin.position):-1f;
                    float cameraDistance=pendingAir!=null?
                        Vector3.Distance(pendingAir.cameraRig.gameplayCamera.transform.position,cameraBefore):-1f;
                    if(!EditorApplication.isPlaying || pendingAir==null || pendingPractice==null ||
                        !pendingAir.IsPlaying || host==null || !host.gameObject.activeSelf || projectile==null ||
                        projectileDistance<.05f || cameraDistance<1f ||
                        pendingPractice.panel.gameObject.activeSelf || pendingPractice.playerMovement.enabled ||
                        pendingPractice.cameraFollow.enabled)
                        throw new InvalidOperationException(
                            $"Air mid-flight state incorrect: playing={pendingAir?.IsPlaying}, host={host!=null}, " +
                            $"hostActive={host!=null&&host.gameObject.activeSelf}, projectile={projectile!=null}, " +
                            $"projectileDistance={projectileDistance:F3}, cameraDistance={cameraDistance:F3}, " +
                            $"panel={pendingPractice?.panel.gameObject.activeSelf}, " +
                            $"movement={pendingPractice?.playerMovement.enabled}, cameraFollow={pendingPractice?.cameraFollow.enabled}.");
                    midFlightPassed=true;
                    Debug.Log("PASS Ruin 2 D1 mid-flight: isolated violet projectile moved from ProjectileOrigin and the gameplay camera tracked it with modal controls held.");
                }
                catch(Exception e)
                {
                    EditorApplication.update-=CheckAirSequence;
                    Debug.LogException(e);pendingAir=null;pendingPractice=null;return;
                }
            }
            if(Time.realtimeSinceStartup<checkAfter)return;
            EditorApplication.update-=CheckAirSequence;
            try
            {
                float finalCameraDistance=pendingAir!=null?
                    Vector3.Distance(pendingAir.cameraRig.gameplayCamera.transform.position,cameraBefore):-1f;
                float finalCameraAngle=pendingAir!=null?
                    Quaternion.Angle(pendingAir.cameraRig.gameplayCamera.transform.rotation,rotationBefore):-1f;
                if(!EditorApplication.isPlaying || pendingAir==null || pendingPractice==null ||
                    !midFlightPassed || !pendingAir.IsRestored || pendingAir.IsPlaying || pendingAir.purpleOrb.activeSelf ||
                    pendingPractice.IsCinematicActive || pendingPractice.IsOpen ||
                    pendingPractice.panel.gameObject.activeSelf || !pendingPractice.playerMovement.enabled ||
                    !pendingPractice.cameraFollow.enabled ||
                    pendingPractice.Session.Question?.Id!="ruin02.absorb.2" ||
                    finalCameraDistance>.05f || finalCameraAngle>.5f)
                    throw new InvalidOperationException(
                        $"Air final state incorrect: mid={midFlightPassed}, restored={pendingAir?.IsRestored}, " +
                        $"playing={pendingAir?.IsPlaying}, orb={pendingAir?.purpleOrb.activeSelf}, " +
                        $"cinematic={pendingPractice?.IsCinematicActive}, open={pendingPractice?.IsOpen}, " +
                        $"panel={pendingPractice?.panel.gameObject.activeSelf}, movement={pendingPractice?.playerMovement.enabled}, " +
                        $"cameraFollow={pendingPractice?.cameraFollow.enabled}, question={pendingPractice?.Session.Question?.Id}, " +
                        $"cameraDistance={finalCameraDistance:F3}, cameraAngle={finalCameraAngle:F3}.");
                var renderer=pendingAir.airStaff.GetComponentInChildren<MeshRenderer>();
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block,0);
                int baseColor=Shader.PropertyToID("_BaseColor");
                Color expected=renderer.sharedMaterials[0].GetColor(baseColor);
                if(block.HasColor(baseColor) &&
                    Vector4.Distance(block.GetColor(baseColor),expected)>.01f)
                    throw new InvalidOperationException("Air original material appearance was not restored.");
                pendingPractice.Close();pendingPractice.Open();
                if(!pendingPractice.IsOpen || !pendingAir.IsRestored || pendingAir.purpleOrb.activeSelf ||
                    pendingPractice.Session.Question?.Id!="ruin02.absorb.2")
                    throw new InvalidOperationException("Air restoration did not persist across challenge reopen.");
                pendingPractice.Close();
                Debug.Log("PASS Ruin 2 D1 Air: wrong inputs held, one absorbMagic(1) cinematic, grey-to-original staff, orb off, ESC/Close queued safely, camera/UI/controls restored, next question absorbMagic(2), reopen persistent.");
            }
            catch(Exception e){Debug.LogException(e);}
            finally{pendingAir=null;pendingPractice=null;midFlightPassed=false;}
        }
    }
}
