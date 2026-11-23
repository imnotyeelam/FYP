using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin02RockNavigationTools
    {
        public const string Backup="LocalBackups/Ruin02RockNavigation_20261119";
        public static Ruin02ReturnGate Gate=>UnityEngine.Object.FindFirstObjectByType<Ruin02ReturnGate>();
        public static string Path(Transform t)=>t.parent==null?t.name:Path(t.parent)+"/"+t.name;
        [MenuItem("Tools/FYP/Ruin 2/Rock Navigation/Audit and Back Up")]
        public static void Audit()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/Scenes/MagicWorld_Development.unity")
                throw new InvalidOperationException("Open development scene in Edit Mode.");
            var gate=Gate;
            var hints=UnityEngine.Object.FindFirstObjectByType<InteractionHintManager>();
            var rock=gate.returnEntrance.transform.Find("ReturnMagicRock");
            var solid=rock.GetComponent<BoxCollider>();
            if(solid==null||!solid.enabled||solid.isTrigger||gate.returnEntrance.enabled||
                hints.interactionTargets.Count(x=>x==gate)!=1||hints.interactionTargets.Contains(gate.returnEntrance))
                throw new InvalidOperationException("Inspect original rock/collider/shared registration before proceeding.");
            Directory.CreateDirectory(Backup);
            if(!EditorSceneManager.SaveScene(scene,$"{Backup}/Before_{DateTime.Now:HHmmssfff}.unity",true))throw new IOException("Backup failed.");
            var log=new StringBuilder();
            log.AppendLine($"RETURN {Path(gate.transform)} origin={gate.InteractionOrigin.position:F3} range={gate.SharedInteractionRange}");
            log.AppendLine($"ROCK {Path(rock)} pos={rock.position:F3} bounds={solid.bounds} enabled={solid.enabled} trigger={solid.isTrigger}");
            log.AppendLine($"HUB {Path(gate.returnEntrance.destination)} {gate.returnEntrance.destination.position:F3}");
            foreach(var script in rock.GetComponentsInChildren<MonoBehaviour>(true))log.AppendLine($"ROCK SCRIPT {script.GetType().Name}");
            foreach(var b in hints.interactionTargets)
                if(b is ISharedInteractionTarget t && t.InteractionOrigin!=null && Vector3.Distance(t.InteractionOrigin.position,rock.position)<20)
                    log.AppendLine($"TARGET {Path(b.transform)} {b.GetType().Name} enabled={b.enabled} origin={t.InteractionOrigin.position:F3} range={t.SharedInteractionRange}");
            log.AppendLine("SAFE APPROACH "+Approach(gate).ToString("F3"));
            Directory.CreateDirectory("Temp/Ruin02RockNavigation");File.WriteAllText("Temp/Ruin02RockNavigation/Audit.txt",log.ToString());
            Debug.Log("Ruin 2 rock audit and live backup ready: Temp/Ruin02RockNavigation/Audit.txt");
        }
        public static Vector3 Approach(Ruin02ReturnGate gate)
        {
            Physics.SyncTransforms();
            var cc=gate.returnEntrance.player.GetComponent<CharacterController>();
            var ground=FloatingIslandPrototypeTools.Exact("Ruin02_Area/Environment/Ground").GetComponent<BoxCollider>();
            foreach(var offset in new[]{Vector3.back*2f,Vector3.forward*2f,Vector3.left*2f,Vector3.right*2f,Vector3.back*2.7f})
            {
                var origin=gate.InteractionOrigin.position+offset;origin.y=30;
                if(!ground.Raycast(new Ray(origin,Vector3.down),out var hit,40))continue;
                var foot=hit.point+Vector3.up*.12f;var center=foot+cc.center;
                var half=Vector3.up*Mathf.Max(0,cc.height*.5f-cc.radius);
                if(Vector3.Distance(foot,gate.InteractionOrigin.position)>gate.SharedInteractionRange)continue;
                if(Physics.OverlapCapsule(center-half,center+half,cc.radius,~0,QueryTriggerInteraction.Ignore)
                    .All(c=>c==cc||c.transform.IsChildOf(cc.transform)))return foot;
            }
            throw new InvalidOperationException("No reachable collision-clear approach within existing return range.");
        }

        [MenuItem("Tools/FYP/Ruin 2/Rock Navigation/Install Single Progression Gateway")]
        public static void Install()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/Scenes/MagicWorld_Development.unity")
                throw new InvalidOperationException("Open development scene in Edit Mode.");
            if(!Directory.Exists(Backup)||!Directory.GetFiles(Backup,"Before_*.unity").Any())
                throw new InvalidOperationException("Audit and back up the live scene first.");
            var gate=Gate;
            var oldEntry=FloatingIslandPrototypeTools.Exact("Ruin02_Area/Gameplay/RitualChamber/MagicalFarmGateway").GetComponent<Ruin02FarmAccess>();
            var exit=FloatingIslandPrototypeTools.Exact("Ruin02_MagicalFarm/FarmReturnPoint").GetComponent<Ruin02FarmAccess>();
            var hints=UnityEngine.Object.FindFirstObjectByType<InteractionHintManager>();
            if(oldEntry==null||oldEntry.isFarmReturn||exit==null||!exit.isFarmReturn||exit.destination==null||
                oldEntry.teleport!=gate.returnEntrance.teleportManager||oldEntry.console==null||
                hints.interactionTargets.Count(x=>x==gate)!=1)
                throw new InvalidOperationException("Existing H4/return references do not match.");
            var approach=Approach(gate);
            Undo.RecordObject(gate,"Reuse return rock for progression");gate.farmAccess=oldEntry;
            Undo.RecordObject(oldEntry,"Retire duplicate farm entrance");oldEntry.enabled=false;
            foreach(var name in new[]{"FarmGatewayLandmark","GatewayLabel"})
            {
                var visual=oldEntry.transform.Find(name);
                if(visual==null)continue;
                Undo.RecordObject(visual.gameObject,"Hide redundant farm gateway dressing");visual.gameObject.SetActive(false);
            }
            Undo.RecordObject(hints,"Retain one progression E target");
            hints.interactionTargets=hints.interactionTargets.Where(x=>x!=oldEntry&&x!=gate.returnEntrance).ToArray();
            EditorUtility.SetDirty(gate);EditorUtility.SetDirty(oldEntry);EditorUtility.SetDirty(hints);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log($"Ruin 2 single rock navigation saved. Original rock/collider/Hub destination unchanged; clear approach {approach:F3}; H4 entrance disabled/unregistered, FarmReturnPoint and ritual landing retained.");
        }
    }
}
