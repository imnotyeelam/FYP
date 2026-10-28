using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fyp.Development.Recursion.Editor
{
    [InitializeOnLoad]
    public static class Ruin01ReturnFixTools
    {
        const string Output = "Temp/Ruin01ReturnFix";
        static Transform Exact(string path) => FloatingIslandPrototypeTools.Exact(path);
        static string Path(Transform t) => FloatingIslandPrototypeTools.P(t);
        static Ruin01ReturnFixTools()
        {
            EditorApplication.delayCall += () =>
            {
                if (File.Exists(Output + "/Audit.txt") || EditorApplication.isPlayingOrWillChangePlaymode ||
                    EditorSceneManager.GetActiveScene().path != SpellbookStageTools.ScenePath) return;
                try { Audit(); } catch (Exception error) { Debug.LogException(error); }
            };
        }
        [MenuItem("Tools/FYP/Ruin 1/Return Fix/Audit Live Navigation %#&F3")]
        public static void Audit()
        {
            SpellbookStageTools.RequireScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Return audit requires Edit Mode.");
            var scene = EditorSceneManager.GetActiveScene();
            Directory.CreateDirectory(Output);
            EditorSceneManager.SaveScene(scene, Output + "/Before.unity", true);
            Physics.SyncTransforms();
            var returnPoint = Exact("Ruin01_Area/SpawnPoints/L1_ReturnPoint");
            var exit = Exact("Ruin01_Area/Interactables/ReturnInteractionPoint");
            var reward = Exact("Ruin01_Area/Interactables/FragmentOfCalling");
            var returnEntrance = exit.GetComponent<RuinEntrance>();
            var entry = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RuinEntrance>(true))
                .Single(e=>e.destination!=null && e.destination.name=="L1_Spawnpoint").transform;
            var destination = returnEntrance.destination;
            var manager = Exact("InteractionHintManager").GetComponent<InteractionHintManager>();
            var player = Exact("Player");
            var cc = player.GetComponent<CharacterController>();
            var s = new StringBuilder();
            s.AppendLine("Ruin 1 return live audit; dirty=" + scene.isDirty);
            foreach (var t in new[]{entry, returnPoint, exit, reward, destination, Exact("flying_stone_003"), Exact("Bridge (3)")})
                s.AppendLine(Path(t) + " P=" + t.position.ToString("F4") + " R=" + t.eulerAngles.ToString("F2") + " active=" + t.gameObject.activeInHierarchy);
            s.AppendLine("L1_ReturnPoint components=" + string.Join(",", returnPoint.GetComponents<Component>().Select(c=>c.GetType().Name)));
            s.AppendLine("Return components=" + string.Join(",", exit.GetComponents<Component>().Select(c=>c.GetType().Name)));
            s.AppendLine("Return enabled=" + returnEntrance.enabled + " range=" + returnEntrance.interactionDistance + " shared=" + returnEntrance.useSharedInteraction + " hint=" + returnEntrance.sharedHint);
            s.AppendLine("Return refs player=" + Path(returnEntrance.player) + " destination=" + Path(returnEntrance.destination) +
                " teleport=" + Path(returnEntrance.teleportManager.transform) + " teleport.player=" + Path(returnEntrance.teleportManager.player));
            s.AppendLine("Manager shared=" + manager.coordinateInteractions + " return registrations=" + manager.interactionTargets.Count(x => x == returnEntrance) +
                " reward registrations=" + manager.interactionTargets.Count(x => x == reward.GetComponent<Ruin01FragmentReward>()) +
                " hint=" + Path(manager.hintText.transform));
            s.AppendLine("Return renderer count=" + exit.GetComponentsInChildren<Renderer>(true).Length + " collider count=" + exit.GetComponentsInChildren<Collider>(true).Length);
            s.AppendLine("CC height=" + cc.height + " radius=" + cc.radius + " step=" + cc.stepOffset + " slope=" + cc.slopeLimit);
            foreach (var root in new[]{Exact("flying_stone_003"),Exact("Ruin1_Ground"),Exact("Bridge (3)")})
                foreach (var col in root.GetComponentsInChildren<Collider>(true))
                    s.AppendLine("COLLIDER " + Path(col.transform) + " enabled=" + col.enabled + " bounds=" + col.bounds);
            for (float z=164; z<=234; z+=2)
            {
                var hits = Physics.RaycastAll(new Vector3(-434.5f, 120, z), Vector3.down, 150, ~0, QueryTriggerInteraction.Ignore)
                    .Where(h => Vector3.Angle(h.normal,Vector3.up)<=cc.slopeLimit)
                    .OrderByDescending(h => h.point.y).Take(3).ToArray();
                s.AppendLine("PATH z=" + z.ToString("F1") + " " + string.Join(" | ", hits.Select(h=>Path(h.collider.transform) + " y=" + h.point.y.ToString("F2"))));
            }
            var island = Exact("flying_stone_003").GetComponent<MeshCollider>();
            for (float x=-444; x<=-430; x+=2)
            for (float z=160; z<=190; z+=2)
            {
                if (!island.Raycast(new Ray(new Vector3(x,120,z),Vector3.down),out var ground,150)) continue;
                if (Vector3.Angle(ground.normal,Vector3.up)>cc.slopeLimit) continue;
                var foot = ground.point+Vector3.up*.08f;
                var center = foot+cc.center;
                var half = Mathf.Max(0,cc.height*.5f-cc.radius);
                var blockers = Physics.OverlapCapsule(center+Vector3.up*half,center-Vector3.up*half,cc.radius+.04f,~0,QueryTriggerInteraction.Ignore)
                    .Where(c=>c!=island && !c.transform.IsChildOf(player)).ToArray();
                if (blockers.Length>0) continue;
                bool support=true;
                foreach(float dx in new[]{-.4f,0,.4f})foreach(float dz in new[]{-.4f,0,.4f})
                    if(!island.Raycast(new Ray(new Vector3(x+dx,120,z+dz),Vector3.down),out var sample,150) ||
                        Mathf.Abs(sample.point.y-ground.point.y)>.3f || Vector3.Angle(sample.normal,Vector3.up)>cc.slopeLimit) support=false;
                if(!support)continue;
                var planarMarker=Vector2.Distance(new Vector2(x,z),new Vector2(returnPoint.position.x,returnPoint.position.z));
                var planarReward=Vector2.Distance(new Vector2(x,z),new Vector2(reward.position.x,reward.position.z));
                if(planarMarker<=10 && planarReward>=6)
                    s.AppendLine("SAFE CANDIDATE x="+x+" z="+z+" groundY="+ground.point.y.ToString("F3")+
                        " slope="+Vector3.Angle(ground.normal,Vector3.up).ToString("F1")+
                        " markerDist="+planarMarker.ToString("F1")+" rewardDist="+planarReward.ToString("F1"));
            }
            File.WriteAllText(Output + "/Audit.txt",s.ToString());
            Debug.Log("Ruin 1 return audit saved to " + Output + "/Audit.txt");
        }

        [MenuItem("Tools/FYP/Ruin 1/Return Fix/Align Existing Exit and Add Landmark %#&F2")]
        public static void Install()
        {
            SpellbookStageTools.RequireScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode before aligning the return interaction.");
            var scene=EditorSceneManager.GetActiveScene();
            if(scene.isDirty)throw new InvalidOperationException("Unsaved scene edits detected; no return object was changed.");
            var exit=Exact("Ruin01_Area/Interactables/ReturnInteractionPoint");
            var entrance=exit.GetComponent<RuinEntrance>();
            var manager=Exact("InteractionHintManager").GetComponent<InteractionHintManager>();
            var island=Exact("flying_stone_003").GetComponent<MeshCollider>();
            var player=Exact("Player");var cc=player.GetComponent<CharacterController>();
            var reward=Exact("Ruin01_Area/Interactables/FragmentOfCalling");
            if(exit.Find("ReturnMagicRock")!=null || exit.Find("ReturnBeaconLight")!=null ||
                entrance==null || !entrance.enabled || !entrance.useSharedInteraction || entrance.interactionDistance!=5 ||
                entrance.destination==null || Path(entrance.destination)!="MainHub/Ruin1/Destination" ||
                entrance.teleportManager==null || entrance.teleportManager.player!=player || entrance.player!=player ||
                !manager.coordinateInteractions || manager.interactionTargets.Count(x=>x==entrance)!=1)
                throw new InvalidOperationException("Return interaction wiring differs from the audited scene; no edit made.");
            const float x=-440,z=166;
            Physics.SyncTransforms();
            if(island==null || !island.enabled || !island.Raycast(new Ray(new Vector3(x,120,z),Vector3.down),out var ground,150) ||
                Vector3.Angle(ground.normal,Vector3.up)>cc.slopeLimit)
                throw new InvalidOperationException("The proposed exit has no safe terminal-island surface.");
            var foot=ground.point+Vector3.up*.08f;var center=foot+cc.center;var half=Mathf.Max(0,cc.height*.5f-cc.radius);
            if(Physics.OverlapCapsule(center+Vector3.up*half,center-Vector3.up*half,cc.radius+.04f,~0,QueryTriggerInteraction.Ignore)
                .Any(c=>c!=island && !c.transform.IsChildOf(player)))
                throw new InvalidOperationException("The exit CharacterController capsule is blocked.");
            foreach(float dx in new[]{-.4f,0,.4f})foreach(float dz in new[]{-.4f,0,.4f})
                if(!island.Raycast(new Ray(new Vector3(x+dx,120,z+dz),Vector3.down),out var sample,150) ||
                    Mathf.Abs(sample.point.y-ground.point.y)>.3f || Vector3.Angle(sample.normal,Vector3.up)>cc.slopeLimit)
                    throw new InvalidOperationException("The exit footing is uneven or unsupported.");
            if(Vector2.Distance(new Vector2(x,z),new Vector2(reward.position.x,reward.position.z))<6)
                throw new InvalidOperationException("Return and reward interaction ranges would overlap excessively.");
            const string prefabPath="Assets/FantasyForest/Prefabs/Landscape/MagicRock_01.prefab";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if(prefab==null || prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials)
                .Any(m=>m==null || m.shader==null || m.shader.name!="Universal Render Pipeline/Lit"))
                throw new InvalidOperationException("Compatible MagicRock_01 prefab is unavailable.");
            Directory.CreateDirectory(Output);EditorSceneManager.SaveScene(scene,Output+"/BeforeInstall.unity",true);
            int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Align existing Ruin 1 return interaction");
            var previous=exit.position;
            try
            {
                Undo.RecordObject(exit,"Move existing return interaction to destination island");
                exit.position=ground.point+Vector3.up*.45f;
                var rock=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                Undo.RegisterCreatedObjectUndo(rock,"Add return landmark scene instance");
                rock.name="ReturnMagicRock";rock.transform.SetParent(exit,true);
                rock.transform.SetPositionAndRotation(ground.point,Quaternion.identity);
                rock.transform.localScale=Vector3.one*1.35f;
                foreach(var col in rock.GetComponentsInChildren<Collider>(true))
                {Undo.RecordObject(col,"Disable decorative return rock collision");col.enabled=false;EditorUtility.SetDirty(col);}
                var beacon=new GameObject("ReturnBeaconLight");Undo.RegisterCreatedObjectUndo(beacon,"Add return beacon");
                beacon.transform.SetParent(exit,false);beacon.transform.position=ground.point+Vector3.up*1.5f;
                var light=beacon.AddComponent<Light>();light.type=LightType.Point;light.range=8;light.intensity=1.8f;
                light.color=new Color(.28f,1f,.72f);light.shadows=LightShadows.None;
                EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(undo);
                EditorSceneManager.SaveScene(scene,Output+"/AfterInstall.unity",true);
                File.WriteAllText(Output+"/Install.txt","Existing return moved "+previous.ToString("F5")+" -> "+exit.position.ToString("F5")+
                    "\nReturn landmark="+Path(rock.transform)+" "+rock.transform.position.ToString("F5")+
                    "\nBeacon="+Path(beacon.transform)+" "+beacon.transform.position.ToString("F5")+
                    "\nDestination="+Path(entrance.destination)+" "+entrance.destination.position.ToString("F5")+
                    "\nRange="+entrance.interactionDistance+" hint="+entrance.sharedHint+
                    "\nGem distance="+Vector2.Distance(new Vector2(x,z),new Vector2(reward.position.x,reward.position.z)).ToString("F2"));
                Selection.activeGameObject=exit.gameObject;
                Debug.Log("Aligned the existing Ruin 1 return interaction on the destination island; source prefab and destination unchanged.");
            }
            catch{Undo.RevertAllDownToGroup(undo);throw;}
        }
    }
}
