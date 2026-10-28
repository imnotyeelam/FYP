using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using Benjathemaker;
using Fyp.Development.Progress;
using Object=UnityEngine.Object;

namespace Fyp.Development.Recursion.Editor
{
    [InitializeOnLoad]
    public static class Ruin01FragmentTools
    {
        internal const string Output="Temp/Ruin01PhaseD";
        internal const string GemPath="Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Diamondo.prefab";
        static Transform Exact(string path)=>FloatingIslandPrototypeTools.Exact(path);
        static Ruin01FragmentTools()
        {
            EditorApplication.delayCall+=()=>
            {
                if(File.Exists(Output+"/Audit.txt") || EditorApplication.isPlayingOrWillChangePlaymode ||
                    EditorSceneManager.GetActiveScene().path!=SpellbookStageTools.ScenePath)return;
                try{Audit();}catch(Exception ex){Debug.LogError("Phase D read-only audit failed: "+ex);}
            };
        }

        [MenuItem("Tools/FYP/Ruin 1/Phase D/Audit Fragment Destination")]
        public static void Audit()
        {
            SpellbookStageTools.RequireScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Phase D audit requires Edit Mode.");
            Directory.CreateDirectory(Output);Physics.SyncTransforms();
            var scene=EditorSceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene,Output+"/Before.unity",true);
            var rig=FloatingIslandPrototypeTools.Rig();var destination=rig.destinationIsland;
            var bridge3=rig.placeStone1;var cc=Exact("Player").GetComponent<CharacterController>();
            var book=Exact("Magic Book");var spawn=Exact("Ruin01_Area/SpawnPoints/L1_Spawnpoint");
            var exit=Exact("Ruin01_Area/Interactables/ReturnInteractionPoint");
            var gem=AssetDatabase.LoadAssetAtPath<GameObject>(GemPath);
            if(gem==null)throw new InvalidOperationException("Diamondo prefab is not imported at the requested path.");
            var s=new StringBuilder();s.AppendLine("PHASE D EDIT MODE AUDIT dirty="+scene.isDirty);
            foreach(var t in new[]{destination,bridge3.transform,bridge3.repairedPose,book,spawn,exit})
                s.AppendLine(FloatingIslandPrototypeTools.P(t)+" position="+t.position.ToString("F5")+" rotation="+t.rotation.eulerAngles.ToString("F3")+" scale="+t.lossyScale.ToString("F4"));
            foreach(var c in destination.GetComponentsInChildren<Collider>(true))s.AppendLine("Destination collider="+FloatingIslandPrototypeTools.P(c.transform)+" "+c.GetType().Name+" enabled="+c.enabled+" bounds="+c.bounds);
            foreach(var r in destination.GetComponentsInChildren<Renderer>(true))s.AppendLine("Destination renderer="+FloatingIslandPrototypeTools.P(r.transform)+" bounds="+r.bounds);
            s.AppendLine("CC height="+cc.height+" radius="+cc.radius+" center="+cc.center+" step="+cc.stepOffset+" slope="+cc.slopeLimit);
            foreach(float x in new[]{destination.position.x-4,destination.position.x-2,destination.position.x,destination.position.x+2,destination.position.x+4})
            foreach(float z in new[]{destination.position.z-4,destination.position.z-2,destination.position.z,destination.position.z+2,destination.position.z+4,destination.position.z+6,destination.position.z+8})
            {
                var hits=Physics.RaycastAll(new Vector3(x,110,z),Vector3.down,150,~0,QueryTriggerInteraction.Ignore)
                    .Where(h=>h.collider.transform.IsChildOf(destination) && Vector3.Angle(h.normal,Vector3.up)<=cc.slopeLimit)
                    .OrderByDescending(h=>h.point.y).ToArray();
                if(hits.Length==0){s.AppendLine("GRID x="+x.ToString("F2")+" z="+z.ToString("F2")+" NO DESTINATION SURFACE");continue;}
                var h=hits[0];var foot=h.point+Vector3.up*.08f;
                var low=foot+cc.center+Vector3.up*(-cc.height*.5f+cc.radius);
                var high=foot+cc.center+Vector3.up*(cc.height*.5f-cc.radius);
                var blockers=Physics.OverlapCapsule(low,high,cc.radius,~0,QueryTriggerInteraction.Ignore)
                    .Where(c=>!c.transform.IsChildOf(destination) && !c.transform.IsChildOf(Exact("Player"))).ToArray();
                s.AppendLine("GRID x="+x.ToString("F2")+" z="+z.ToString("F2")+" y="+h.point.y.ToString("F3")+
                    " slope="+Vector3.Angle(h.normal,Vector3.up).ToString("F1")+" blockers="+string.Join(",",blockers.Select(c=>FloatingIslandPrototypeTools.P(c.transform))));
            }
            s.AppendLine("GEM="+GemPath+" prefabRoot="+gem.name+" scripts="+string.Join(",",gem.GetComponentsInChildren<MonoBehaviour>(true).Select(m=>m.GetType().FullName)));
            foreach(var mf in gem.GetComponentsInChildren<MeshFilter>(true))s.AppendLine("Gem mesh="+FloatingIslandPrototypeTools.P(mf.transform)+" bounds="+mf.sharedMesh.bounds+" scale="+mf.transform.localScale);
            foreach(var r in gem.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials)
                s.AppendLine("Gem material="+AssetDatabase.GetAssetPath(m)+" shader="+(m?.shader?.name??"MISSING"));
            File.WriteAllText(Output+"/Audit.txt",s.ToString());Debug.Log("Ruin 1 Phase D audit saved to "+Output+"/Audit.txt");
        }

        [MenuItem("Tools/FYP/Ruin 1/Phase D/Install Fragment of Calling %#&F4")]
        public static void Install()
        {
            SpellbookStageTools.RequireScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Phase D installation requires Edit Mode.");
            var scene=EditorSceneManager.GetActiveScene();var interactables=Exact("Ruin01_Area/Interactables");
            var gameplay=Exact("Ruin01_Area/Gameplay");var rig=FloatingIslandPrototypeTools.Rig();
            var manager=Exact("InteractionHintManager").GetComponent<InteractionHintManager>();
            var practice=BridgePracticeTools.Controller();var player=Exact("Player");
            if(interactables.Find("FragmentOfCalling")!=null || gameplay.Find("FragmentProgressTracker")!=null ||
                scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Ruin01FragmentReward>(true)).Any())
                throw new InvalidOperationException("Fragment reward already exists. No duplicate installed.");
            if(!manager.coordinateInteractions || !manager.interactionTargets.Contains(practice))
                throw new InvalidOperationException("Shared interaction setup has changed; inspect before installing.");
            var gemPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(GemPath);
            const string altarPath="Assets/FantasyForest/Prefabs/Landscape/MagicRock_01.prefab";
            var altarPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(altarPath);
            if(gemPrefab==null || altarPrefab==null || gemPrefab.GetComponent<SimpleGemsAnim>()==null)
                throw new InvalidOperationException("Expected imported Diamondo and MagicRock prefabs and gem animation component.");
            var shaders=gemPrefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials)
                .Concat(altarPrefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials))
                .Where(m=>m!=null).Select(m=>m.shader?.name??"").Distinct().ToArray();
            if(shaders.Length!=1 || shaders[0]!="Universal Render Pipeline/Lit")
                throw new InvalidOperationException("The chosen gem or altar material is not URP/Lit.");
            var destination=rig.destinationIsland;
            var x=destination.position.x+3f;var z=destination.position.z-2f;
            var groundCollider=destination.GetComponent<MeshCollider>();
            if(groundCollider==null || !groundCollider.enabled || !groundCollider.Raycast(new Ray(new Vector3(x,110,z),Vector3.down),out var hit,150))
                throw new InvalidOperationException("No destination-island ground under proposed fragment.");
            var cc=player.GetComponent<CharacterController>();
            if(Vector3.Angle(hit.normal,Vector3.up)>cc.slopeLimit || z<rig.placeStone1.repairedPose.position.z+30)
                throw new InvalidOperationException("Proposed fragment surface is too steep or too near the moving section.");
            foreach(float dx in new[]{-.5f,0,.5f})foreach(float dz in new[]{-.5f,0,.5f})
            {
                if(!groundCollider.Raycast(new Ray(new Vector3(x+dx,110,z+dz),Vector3.down),out var foot,150) ||
                    Mathf.Abs(foot.point.y-hit.point.y)>.28f || Vector3.Angle(foot.normal,Vector3.up)>cc.slopeLimit)
                    throw new InvalidOperationException("Fragment pedestal footprint is not fully walkable.");
            }
            Physics.SyncTransforms();var stand=hit.point+Vector3.up*.08f;
            var low=stand+cc.center+Vector3.up*(-cc.height*.5f+cc.radius);
            var high=stand+cc.center+Vector3.up*(cc.height*.5f-cc.radius);
            var blockers=Physics.OverlapCapsule(low,high,cc.radius,~0,QueryTriggerInteraction.Ignore)
                .Where(c=>!c.transform.IsChildOf(destination) && !c.transform.IsChildOf(player)).ToArray();
            if(blockers.Length>0)throw new InvalidOperationException("Fragment location blocks CharacterController: "+string.Join(",",blockers.Select(c=>c.name)));
            Directory.CreateDirectory(Output);EditorSceneManager.SaveScene(scene,Output+"/BeforeInstall.unity",true);
            Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Install Ruin 1 Fragment of Calling");
            try
            {
                var trackerObject=Empty("FragmentProgressTracker",gameplay);
                var tracker=trackerObject.AddComponent<FragmentProgressTracker>();
                var root=Empty("FragmentOfCalling",interactables);root.transform.position=hit.point;
                var reward=root.AddComponent<Ruin01FragmentReward>();
                var marker=Empty("CollectionPoint",root.transform);marker.transform.localPosition=Vector3.up*1.25f;
                var altar=(GameObject)PrefabUtility.InstantiatePrefab(altarPrefab,scene);
                Undo.RegisterCreatedObjectUndo(altar,"Add fragment altar scene instance");
                altar.name="FragmentAltar";altar.transform.SetParent(root.transform,false);
                altar.transform.localPosition=Vector3.zero;altar.transform.localRotation=Quaternion.identity;altar.transform.localScale=Vector3.one*.65f;
                foreach(var collider in altar.GetComponentsInChildren<Collider>(true))
                {Undo.RecordObject(collider,"Decorative altar collision off");collider.enabled=false;EditorUtility.SetDirty(collider);}
                var gem=(GameObject)PrefabUtility.InstantiatePrefab(gemPrefab,scene);
                Undo.RegisterCreatedObjectUndo(gem,"Add Diamondo scene instance");
                gem.name="Diamondo_FragmentOfCalling";gem.transform.SetParent(root.transform,false);
                gem.transform.localPosition=Vector3.up*1.7f;gem.transform.localRotation=Quaternion.identity;gem.transform.localScale=Vector3.one*.4f;
                var animation=gem.GetComponent<SimpleGemsAnim>();Undo.RecordObject(animation,"Gentle fragment hover");
                animation.isRotating=true;animation.rotateY=true;animation.isFloating=true;
                animation.floatHeight=.22f;animation.floatSpeed=.55f;animation.isScaling=false;EditorUtility.SetDirty(animation);
                foreach(var collider in gem.GetComponentsInChildren<Collider>(true))
                {Undo.RecordObject(collider,"Fragment uses shared E interaction");collider.enabled=false;EditorUtility.SetDirty(collider);}
                gem.SetActive(false);
                var toast=EmptyUI("FragmentRewardToast",Exact("Canvas"));
                var rect=(RectTransform)toast.transform;rect.anchorMin=new Vector2(.25f,.86f);rect.anchorMax=new Vector2(.75f,.94f);
                rect.offsetMin=Vector2.zero;rect.offsetMax=Vector2.zero;
                var background=toast.AddComponent<UnityEngine.UI.Image>();background.color=new Color(.04f,.1f,.16f,.86f);background.raycastTarget=false;
                var textObject=EmptyUI("Message",toast.transform);var textRect=(RectTransform)textObject.transform;
                textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;textRect.offsetMin=Vector2.zero;textRect.offsetMax=Vector2.zero;
                var label=textObject.AddComponent<TextMeshProUGUI>();label.font=manager.hintText.font;label.fontSize=30;
                label.enableAutoSizing=true;label.fontSizeMin=20;label.fontSizeMax=32;
                label.color=new Color(1f,.88f,.52f);label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
                label.text="Fragment of Calling collected";toast.SetActive(false);
                reward.practice=practice;reward.progress=tracker;reward.player=player;reward.interactionPoint=marker.transform;
                reward.gemVisual=gem;reward.confirmationText=label;
                Undo.RecordObject(manager,"Register Fragment of Calling shared interaction");
                manager.interactionTargets=manager.interactionTargets.Concat(new MonoBehaviour[]{reward}).ToArray();EditorUtility.SetDirty(manager);
                EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(undo);
                EditorSceneManager.SaveScene(scene,Output+"/AfterInstall.unity",true);
                File.WriteAllText(Output+"/Install.txt","Fragment root="+FloatingIslandPrototypeTools.P(root.transform)+" "+root.transform.position.ToString("F5")+
                    "\nGem="+FloatingIslandPrototypeTools.P(gem.transform)+" world="+gem.transform.position.ToString("F5")+
                    "\nCollection marker="+FloatingIslandPrototypeTools.P(marker.transform)+" world="+marker.transform.position.ToString("F5")+
                    "\nAltar="+FloatingIslandPrototypeTools.P(altar.transform)+" world="+altar.transform.position.ToString("F5")+
                    "\nTracker="+FloatingIslandPrototypeTools.P(tracker.transform)+"\nToast="+FloatingIslandPrototypeTools.P(toast.transform)+
                    "\nSurface="+FloatingIslandPrototypeTools.P(hit.collider.transform)+" slope="+Vector3.Angle(hit.normal,Vector3.up).ToString("F2")+
                    "\nGem source="+GemPath+"\nAltar source="+altarPath);
                Selection.activeGameObject=root;Debug.Log("Installed Ruin 1 fragment in the live scene; no source prefabs, other ruins or portal changed.");
            }
            catch{Undo.RevertAllDownToGroup(undo);throw;}
        }
        static GameObject Empty(string name,Transform parent)
        {var go=new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Create Phase D scene object");go.transform.SetParent(parent,false);return go;}
        static GameObject EmptyUI(string name,Transform parent)
        {var go=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(go,"Create Phase D UI object");go.transform.SetParent(parent,false);return go;}
    }
}
