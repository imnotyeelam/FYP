using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    public static class SceneMaintenanceC1Tools
    {
        public const string Folder="LocalBackups/SceneMaintenanceC1_20261119";
        [Serializable] public class Node { public string path;public bool activeSelf,activeHierarchy;public string position;public string[] components;public string[] references; }
        [Serializable] public class Snapshot { public Node[] nodes; }
        [MenuItem("Tools/FYP/Scene Maintenance C1/Back Up and Read-Only Audit %#F7")]
        public static void Audit()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/Scenes/MagicWorld_Development.unity")
                throw new InvalidOperationException("Audit the development scene in Edit Mode.");
            Directory.CreateDirectory(Folder);
            if(!EditorSceneManager.SaveScene(scene,$"{Folder}/Before_{DateTime.Now:HHmmssfff}.unity",true))throw new IOException("Backup failed.");
            var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            var refs=new Dictionary<Transform,List<string>>();
            foreach(var component in all.SelectMany(t=>t.GetComponents<Component>()).Where(c=>c!=null&&!(c is Transform)))
            {
                var serialized=new SerializedObject(component);var p=serialized.GetIterator();
                while(p.Next(true))
                {
                    if(p.propertyType!=SerializedPropertyType.ObjectReference)continue;
                    var obj=p.objectReferenceValue;
                    Transform target=obj is GameObject go?go.transform:obj is Component c?c.transform:null;
                    if(target==null||target.gameObject.scene!=scene)continue;
                    if(!refs.TryGetValue(target,out var list))refs[target]=list=new List<string>();
                    list.Add(Path(component.transform)+" | "+component.GetType().Name+"."+p.propertyPath);
                }
            }
            var nodes=all.Select(t=>new Node {path=Path(t),activeSelf=t.gameObject.activeSelf,activeHierarchy=t.gameObject.activeInHierarchy,
                position=t.position.ToString("F3"),components=t.GetComponents<Component>().Select(c=>c==null?"MISSING SCRIPT":c.GetType().Name+
                    (c is Behaviour b?" enabled="+b.enabled:c is Collider col?" enabled="+col.enabled+" trigger="+col.isTrigger:"")).ToArray(),
                references=refs.TryGetValue(t,out var incoming)?incoming.Distinct().ToArray():Array.Empty<string>()}).ToArray();
            File.WriteAllText(Folder+"/HierarchySnapshot.json",JsonUtility.ToJson(new Snapshot{nodes=nodes},true));
            var jump=UnityEngine.Object.FindFirstObjectByType<Ruin01MagicJumpController>(FindObjectsInactive.Include);
            var hints=UnityEngine.Object.FindFirstObjectByType<InteractionHintManager>(FindObjectsInactive.Include);
            var dev=UnityEngine.Object.FindFirstObjectByType<RuinDeveloperPanel>(FindObjectsInactive.Include);
            var log=new StringBuilder();
            log.AppendLine($"JUMP {Path(jump.transform)} active={jump.gameObject.activeInHierarchy} enabled={jump.enabled} range={jump.interactionDistance} priority={jump.SharedInteractionPriority}");
            log.AppendLine($"ANCHOR {Path(jump.interactionAnchor)} active={jump.interactionAnchor.gameObject.activeInHierarchy} world={jump.interactionAnchor.position:F3}");
            log.AppendLine($"START {Path(jump.jumpStartPoint)} {jump.jumpStartPoint.position:F3}");
            log.AppendLine($"SURFACE {Path(jump.jumpStartSurface.transform)} active={jump.jumpStartSurface.gameObject.activeInHierarchy} enabled={jump.jumpStartSurface.enabled} trigger={jump.jumpStartSurface.isTrigger} bounds={jump.jumpStartSurface.bounds}");
            log.AppendLine($"PLAYER {jump.player.position:F3} distanceToRock={Vector3.Distance(jump.player.position,jump.interactionAnchor.position):F3}");
            log.AppendLine($"DEV APPROACH {Path(dev.ruin01JumpStart)} {dev.ruin01JumpStart.position:F3} distance={Vector3.Distance(dev.ruin01JumpStart.position,jump.interactionAnchor.position):F3}");
            log.AppendLine($"PREREQUISITES {Path(jump.bridgePractice.transform)} enabled={jump.bridgePractice.enabled} active={jump.bridgePractice.gameObject.activeInHierarchy}");
            log.AppendLine($"GUIDE {Path(jump.jumpGuide.transform)} enabled={jump.jumpGuide.enabled} active={jump.jumpGuide.gameObject.activeInHierarchy} modalRegistration={hints.modalPanels.Count(p=>p==jump.jumpGuide.panel)}");
            log.AppendLine($"HINT MANAGER enabled={hints.enabled} active={hints.gameObject.activeInHierarchy} coordinated={hints.coordinateInteractions} registration={hints.interactionTargets.Count(t=>t==jump)} hintActive={hints.hintText.gameObject.activeInHierarchy}");
            Physics.SyncTransforms();
            foreach(var point in new[]{dev.ruin01JumpStart.position,jump.interactionAnchor.position+Vector3.back*1.6f,jump.jumpStartPoint.position})
            {
                bool surface=jump.jumpStartSurface.Raycast(new Ray(point+Vector3.up*2,Vector3.down),out var hit,5);
                log.AppendLine($"PROBE {point:F3} explicitSurfaceHit={surface} {(surface?hit.point.ToString("F3"):"")}");
                foreach(var ground in Physics.RaycastAll(point+Vector3.up*3,Vector3.down,8,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).Take(3))
                    log.AppendLine($"  PHYSICS {Path(ground.collider.transform)} y={ground.point.y:F3} normal={ground.normal}");
            }
            foreach(var target in hints.interactionTargets)
                if(target is ISharedInteractionTarget interaction && interaction.InteractionOrigin!=null&&Vector3.Distance(interaction.InteractionOrigin.position,jump.interactionAnchor.position)<15)
                    log.AppendLine($"TARGET {Path(target.transform)} {target.GetType().Name} enabled={target.enabled} range={interaction.SharedInteractionRange} priority={(target is ISharedInteractionPriority priority?priority.SharedInteractionPriority:0)}");
            foreach(var collider in jump.interactionAnchor.GetComponentsInChildren<Collider>(true))
                log.AppendLine($"ROCK COLLIDER {Path(collider.transform)} enabled={collider.enabled} active={collider.gameObject.activeInHierarchy} {collider.bounds}");
            File.WriteAllText(Folder+"/JumpAudit.txt",log.ToString());
            Debug.Log("C1 backed up live scene; read-only hierarchy/reference and Jump audit written to "+Folder);
        }
        public static string Path(Transform t)=>t==null?"NULL":t.parent==null?t.name:Path(t.parent)+"/"+t.name;
    }
}
