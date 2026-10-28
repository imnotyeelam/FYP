using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    public static class BridgePracticeLandmarkTools
    {
        const string Output="Temp/BridgePracticeLandmark";
        const string MagicRock="Assets/FantasyForest/Prefabs/Landscape/MagicRock_01.prefab";
        static Transform Exact(string path)=>FloatingIslandPrototypeTools.Exact(path);
        static RaycastHit? Ground(float x,float z)
        {
            var point=BridgePracticeTools.Controller().interactionPoint.position;
            var hits=Physics.RaycastAll(new Vector3(x,point.y+80,z),Vector3.down,150,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>h.point.y>point.y-12 && Vector3.Angle(h.normal,Vector3.up)<45)
                .OrderByDescending(h=>h.point.y).ToArray();
            return hits.Length>0?hits[0]:(RaycastHit?)null;
        }
        [MenuItem("Tools/FYP/Ruin 1/Phase C/Audit Practice Landmark")]
        public static void Audit()
        {
            SpellbookStageTools.RequireScene();Directory.CreateDirectory(Output);Physics.SyncTransforms();
            var scene=EditorSceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene,Output+"/SceneBeforeLandmark.unity",true);
            var point=Exact("Ruin01_Area/Interactables/BridgePracticeInteractionPoint");
            var controller=BridgePracticeTools.Controller();var manager=Exact("InteractionHintManager").GetComponent<InteractionHintManager>();
            var spawn=Exact("Ruin01_Area/SpawnPoints/L1_Spawnpoint");var book=Exact("Magic Book");
            var fixedBridge=Exact("Bridge");var sb=new StringBuilder();
            sb.AppendLine("sceneDirty="+scene.isDirty+" EditMode="+!EditorApplication.isPlaying+" branch checked outside Unity");
            sb.AppendLine("Controller="+FloatingIslandPrototypeTools.P(controller.transform)+" enabled="+controller.enabled+" active="+controller.gameObject.activeInHierarchy);
            sb.AppendLine("Point="+FloatingIslandPrototypeTools.P(point)+" world="+point.position.ToString("F5")+" components="+string.Join(",",point.GetComponents<Component>().Select(c=>c.GetType().Name)));
            sb.AppendLine("Prompt="+controller.SharedHint+" range="+controller.interactionDistance+" managerRegistered="+manager.interactionTargets.Contains(controller)+" coordinated="+manager.coordinateInteractions);
            sb.AppendLine("Spawn="+spawn.position.ToString("F5")+" distance="+Vector3.Distance(spawn.position,point.position).ToString("F2"));
            sb.AppendLine("Book="+book.position.ToString("F5")+" distance="+Vector3.Distance(book.position,point.position).ToString("F2"));
            sb.AppendLine("fixedBridge="+fixedBridge.position.ToString("F5"));
            foreach(var dx in new[]{-3f,-2f,-1f,0f,1f,2f,3f})
                foreach(var dz in new[]{-3f,-2f,0f,2f,3f})
                {
                    var hit=Ground(point.position.x+dx,point.position.z+dz);
                    if(hit.HasValue)sb.AppendLine("Support dx="+dx+" dz="+dz+" y="+hit.Value.point.y.ToString("F3")+" collider="+FloatingIslandPrototypeTools.P(hit.Value.collider.transform)+" fixed="+hit.Value.collider.transform.IsChildOf(fixedBridge));
                }
            foreach(var offset in new[]{-44.638f,-38.638f,-33.638f,-28.638f,-23.638f,-18.638f,-13.638f,-8.638f,-3.638f,0f})
            {
                var z=point.position.z+offset;var h=Ground(point.position.x,z);
                sb.AppendLine("Approach x="+point.position.x.ToString("F2")+" z="+z+" "+(h.HasValue?"y="+h.Value.point.y.ToString("F2")+" "+FloatingIslandPrototypeTools.P(h.Value.collider.transform):"NO_SUPPORT"));
            }
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(MagicRock);
            if(prefab==null)throw new InvalidOperationException("Missing existing MagicRock prefab");
            sb.AppendLine("Candidate="+MagicRock+" rootScale="+prefab.transform.localScale+" colliders="+prefab.GetComponentsInChildren<Collider>(true).Length);
            foreach(var mf in prefab.GetComponentsInChildren<MeshFilter>(true))
                sb.AppendLine("Mesh="+FloatingIslandPrototypeTools.P(mf.transform)+" bounds="+mf.sharedMesh.bounds+" scale="+mf.transform.localScale);
            foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                foreach(var mat in renderer.sharedMaterials)
                    sb.AppendLine("Material="+AssetDatabase.GetAssetPath(mat)+" shader="+(mat!=null && mat.shader!=null?mat.shader.name:"MISSING"));
            File.WriteAllText(Output+"/Audit.txt",sb.ToString());Debug.Log("Bridge practice landmark audit: "+Output+"/Audit.txt");
        }
        [MenuItem("Tools/FYP/Ruin 1/Phase C/Install Practice Landmark %#&F10")]
        public static void Install()
        {
            SpellbookStageTools.RequireScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Install only in Edit Mode.");
            var scene=EditorSceneManager.GetActiveScene();var point=Exact("Ruin01_Area/Interactables/BridgePracticeInteractionPoint");
            var controller=BridgePracticeTools.Controller();var fixedBridge=Exact("Bridge");
            if(controller.interactionPoint!=point || point.childCount!=0 || !controller.enabled)
                throw new InvalidOperationException("Expected the unchanged active practice point with no existing landmark children.");
            if(!File.Exists(Output+"/Audit.txt"))Audit();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(MagicRock);
            if(prefab==null)throw new InvalidOperationException("Expected existing compatible MagicRock_01 prefab.");
            var materialShaders=prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials)
                .Where(m=>m!=null).Select(m=>m.shader!=null?m.shader.name:"").Distinct().ToArray();
            if(materialShaders.Length==0 || materialShaders.Any(s=>s!="Universal Render Pipeline/Lit"))
                throw new InvalidOperationException("MagicRock material is no longer uniformly URP/Lit; no scene edit made.");
            Physics.SyncTransforms();var p=point.position+Vector3.right*2f;
            var ground=Physics.RaycastAll(new Vector3(p.x,110,p.z),Vector3.down,150,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>h.collider.transform.IsChildOf(fixedBridge) && Vector3.Angle(h.normal,Vector3.up)<45)
                .OrderByDescending(h=>h.point.y).FirstOrDefault();
            if(ground.collider==null || Mathf.Abs(ground.point.y-(point.position.y-.3f))>.55f)
                throw new InvalidOperationException("The proposed right-side landmark has no safe fixed-bridge surface.");
            Directory.CreateDirectory(Output);EditorSceneManager.SaveScene(scene,Output+"/BeforeInstall.unity",true);
            int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Add visual bridge practice landmark");
            try
            {
                var rock=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                Undo.RegisterCreatedObjectUndo(rock,"Create practice MagicRock scene instance");
                rock.name="PracticeMagicRock";rock.transform.SetParent(point,true);
                rock.transform.SetPositionAndRotation(ground.point,Quaternion.identity);rock.transform.localScale=Vector3.one*1.35f;
                foreach(var collider in rock.GetComponentsInChildren<Collider>(true))
                {Undo.RecordObject(collider,"Keep practice landmark visually decorative");collider.enabled=false;EditorUtility.SetDirty(collider);}
                var beacon=new GameObject("PracticeBeaconLight");Undo.RegisterCreatedObjectUndo(beacon,"Create practice beacon light");
                beacon.transform.SetParent(point,false);beacon.transform.position=ground.point+Vector3.up*1.5f;
                var light=beacon.AddComponent<Light>();light.type=LightType.Point;light.range=7f;light.intensity=1.6f;
                light.color=new Color(.45f,.82f,1f);light.shadows=LightShadows.None;
                EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(group);
                Selection.activeGameObject=rock;SceneView.lastActiveSceneView?.FrameSelected();
                EditorSceneManager.SaveScene(scene,Output+"/AfterInstall.unity",true);
                File.WriteAllText(Output+"/Install.txt","Point="+FloatingIslandPrototypeTools.P(point)+" "+point.position.ToString("F5")+
                    "\nVisual="+FloatingIslandPrototypeTools.P(rock.transform)+" "+rock.transform.position.ToString("F5")+
                    " scale="+rock.transform.localScale+" colliders="+rock.GetComponentsInChildren<Collider>(true).Length+" (all disabled scene instance only)"+
                    "\nLight="+FloatingIslandPrototypeTools.P(beacon.transform)+" "+beacon.transform.position.ToString("F5")+
                    "\nSource prefab="+MagicRock+"\nPrompt="+controller.SharedHint);
                Debug.Log("Installed decorative practice landmark without adding a second interaction. Review and save the live scene.");
            }
            catch{Undo.RevertAllDownToGroup(group);throw;}
        }
    }
}
