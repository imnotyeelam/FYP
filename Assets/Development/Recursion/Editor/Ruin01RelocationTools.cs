using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Fyp.Development.Progress;
using Object = UnityEngine.Object;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin01RelocationTools
    {
        const string Output = "Temp/Ruin01Relocation";
        const string RootName = "Ruin01_RelocationRoot";
        static readonly Vector3 Offset = new Vector3(-1100, 300, 0);
        static readonly string[] Names = { "Ruin01_Area", "Ruin1_Ground", "Bridge", "Bridge (1)",
            "Bridge (2)", "Bridge (3)", "Magic Book", "flying_stone_003" };

        struct Pose
        {
            public Vector3 position, scale;
            public Quaternion rotation;
            public Pose(Transform t) { position=t.position; rotation=t.rotation; scale=t.lossyScale; }
        }

        static void Require(bool condition, string message)
        { if(!condition) throw new InvalidOperationException("Ruin 1 relocation: " + message); }
        static Transform Exact(string path) => FloatingIslandPrototypeTools.Exact(path);
        static Transform[] All() => EditorSceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        static bool Same(Pose pose, Transform t, Vector3 offset) =>
            Vector3.Distance(pose.position+offset,t.position)<.003f &&
            Quaternion.Angle(pose.rotation,t.rotation)<.02f &&
            Vector3.Distance(pose.scale,t.lossyScale)<.003f;

        static Transform[] CheckReady()
        {
            SpellbookStageTools.RequireScene();
            var scene=EditorSceneManager.GetActiveScene();
            Require(!scene.isDirty,"scene has unsaved changes; no edits or backup made");
            Require(!All().Any(t=>t.name==RootName),"relocation root already exists");
            var roots=Names.Select(Exact).ToArray();
            Require(roots.Distinct().Count()==Names.Length && roots.All(t=>t.parent==null),
                "the eight audited objects must still be distinct scene roots");
            var tracker=Exact("Ruin01_Area/Gameplay/FragmentProgressTracker");
            Require(tracker.GetComponent<FragmentProgressTracker>()!=null && tracker.childCount==0,
                "shared progress tracker hierarchy differs from audit");
            var entry=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RuinEntrance>(true))
                .Where(e=>e.destination==Exact("Ruin01_Area/SpawnPoints/L1_Spawnpoint")).ToArray();
            var exit=Exact("Ruin01_Area/Interactables/ReturnInteractionPoint").GetComponent<RuinEntrance>();
            Require(entry.Length==1 && exit!=null && exit.destination==Exact("MainHub/Ruin1/Destination") &&
                exit.teleportManager!=null && entry[0].teleportManager==exit.teleportManager,
                "entry/return teleport references differ from audited gameplay");
            var rig=FloatingIslandPrototypeTools.Rig();
            Require(rig.fixedStartingSection==roots[2] && rig.placeStone3.transform.IsChildOf(roots[3]) &&
                rig.placeStone2.transform.IsChildOf(roots[4]) && rig.placeStone1.transform.IsChildOf(roots[5]) &&
                rig.destinationIsland==roots[7],"floating bridge mapping differs from audit");
            Require(Exact("Bridge (3)/MotionRoot/PhaseA_EndConnector")!=null,
                "third bridge connector is missing");
            var reward=Exact("Ruin01_Area/Interactables/FragmentOfCalling").GetComponent<Ruin01FragmentReward>();
            Require(reward!=null && reward.progress==tracker.GetComponent<FragmentProgressTracker>(),
                "fragment progress reference differs from audit");
            return roots;
        }

        [MenuItem("Tools/FYP/Ruin 1/Relocation/Audit and Backup")]
        public static void AuditAndBackup()
        {
            var roots=CheckReady(); var scene=EditorSceneManager.GetActiveScene();
            Directory.CreateDirectory(Output);
            var backup=Output+"/Before.unity";
            Require(!File.Exists(backup),"pre-relocation backup already exists; do not overwrite it");
            Require(EditorSceneManager.SaveScene(scene,backup,true),"could not save backup scene copy");
            var s=new StringBuilder("Ruin 1 pre-relocation; scene="+scene.path+" dirty="+scene.isDirty+" offset="+Offset+"\n");
            foreach(var root in roots)
            {
                s.AppendLine(FloatingIslandPrototypeTools.P(root)+" P="+root.position.ToString("F5")+
                    " R="+root.eulerAngles.ToString("F4")+" S="+root.lossyScale.ToString("F5"));
                foreach(var collider in root.GetComponentsInChildren<Collider>(true))
                    s.AppendLine("  COLLIDER "+FloatingIslandPrototypeTools.P(collider.transform)+" "+collider.GetType().Name+
                        " enabled="+collider.enabled+" trigger="+collider.isTrigger+" bounds="+collider.bounds);
            }
            var tracker=Exact("Ruin01_Area/Gameplay/FragmentProgressTracker");
            s.AppendLine("TRACKER "+FloatingIslandPrototypeTools.P(tracker)+" P="+tracker.position);
            var entry=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RuinEntrance>(true))
                .Single(e=>e.destination==Exact("Ruin01_Area/SpawnPoints/L1_Spawnpoint"));
            var exit=Exact("Ruin01_Area/Interactables/ReturnInteractionPoint").GetComponent<RuinEntrance>();
            s.AppendLine("ENTRY "+FloatingIslandPrototypeTools.P(entry.transform)+" -> "+FloatingIslandPrototypeTools.P(entry.destination));
            s.AppendLine("RETURN "+FloatingIslandPrototypeTools.P(exit.transform)+" -> "+FloatingIslandPrototypeTools.P(exit.destination));
            File.WriteAllText(Output+"/Before.txt",s.ToString());
            Debug.Log("Ruin 1 pre-relocation backup and audit saved in "+Output+". Scene unchanged.");
        }

        [MenuItem("Tools/FYP/Ruin 1/Relocation/Apply Exact Translation")]
        public static void Apply()
        {
            var roots=CheckReady(); var scene=EditorSceneManager.GetActiveScene();
            var backup=Output+"/Before.unity";
            Require(File.Exists(backup) && File.Exists(Output+"/Before.txt"),"run Audit and Backup first");
            var all=All();
            var tracker=Exact("Ruin01_Area/Gameplay/FragmentProgressTracker");
            var moved=new HashSet<Transform>(roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)));
            moved.Remove(tracker);
            var poses=all.ToDictionary(t=>t,t=>new Pose(t));
            var colliderStates=all.SelectMany(t=>t.GetComponents<Collider>()).ToDictionary(c=>c,c=>
                (c.enabled,c.isTrigger,c is MeshCollider mesh ? mesh.convex : false,c.sharedMaterial));
            var behaviours=all.SelectMany(t=>t.GetComponents<MonoBehaviour>()).Where(b=>b!=null)
                .ToDictionary(b=>b,b=>EditorJsonUtility.ToJson(b));
            var prefabLinks=moved.ToDictionary(t=>t,t=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject));
            int undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Relocate complete Ruin 1 world (-1100,+300,0)");
            try
            {
                Undo.SetTransformParent(tracker,null,"Keep shared fragment progress outside Ruin 1");
                var rootObject=new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(rootObject,"Create Ruin 1 relocation root");
                var relocation=rootObject.transform;
                Require(relocation.position==Vector3.zero && relocation.rotation==Quaternion.identity &&
                    relocation.localScale==Vector3.one,"new root is not identity");
                foreach(var t in roots)Undo.SetTransformParent(t,relocation,"Group Ruin 1 world without moving it");
                Physics.SyncTransforms();
                foreach(var pair in poses)Require(Same(pair.Value,pair.Key,Vector3.zero),
                    "reparent changed world transform: "+FloatingIslandPrototypeTools.P(pair.Key));
                Undo.RecordObject(relocation,"Apply approved Ruin 1 translation");
                relocation.position=Offset;
                Physics.SyncTransforms();
                foreach(var pair in poses)Require(Same(pair.Value,pair.Key,moved.Contains(pair.Key)?Offset:Vector3.zero),
                    "unexpected world transform after translation: "+FloatingIslandPrototypeTools.P(pair.Key));
                Require(relocation.rotation==Quaternion.identity && relocation.localScale==Vector3.one,
                    "relocation root rotated or scaled");
                Require(roots.All(t=>t.parent==relocation) && tracker.parent==null,
                    "final hierarchy does not match approved relocation");
                foreach(var pair in colliderStates)
                {
                    var c=pair.Key; var old=pair.Value;
                    Require(c.enabled==old.enabled && c.isTrigger==old.isTrigger &&
                        (!(c is MeshCollider mesh) || mesh.convex==old.Item3) && c.sharedMaterial==old.sharedMaterial,
                        "collider state changed: "+FloatingIslandPrototypeTools.P(c.transform));
                }
                foreach(var pair in behaviours)Require(EditorJsonUtility.ToJson(pair.Key)==pair.Value,
                    "serialized component changed: "+FloatingIslandPrototypeTools.P(pair.Key.transform)+"/"+pair.Key.GetType().Name);
                foreach(var pair in prefabLinks)Require(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(pair.Key.gameObject)==pair.Value,
                    "prefab source link changed: "+FloatingIslandPrototypeTools.P(pair.Key));
                Require(Exact("Ruin01_Area/Interactables/ReturnInteractionPoint").GetComponent<RuinEntrance>().destination==
                    Exact("MainHub/Ruin1/Destination"),"return destination changed");
                Require(scene.isDirty && EditorSceneManager.SaveScene(scene,SpellbookStageTools.ScenePath),"scene save failed");
                var report=new StringBuilder("Applied "+Offset+"; all "+poses.Count+" original transforms, "+
                    colliderStates.Count+" colliders, "+behaviours.Count+" behaviours and "+prefabLinks.Count+" prefab links verified.\n");
                foreach(var t in roots)report.AppendLine(t.name+" "+poses[t].position.ToString("F5")+" -> "+t.position.ToString("F5"));
                report.AppendLine("Tracker "+FloatingIslandPrototypeTools.P(tracker)+" "+tracker.position);
                File.WriteAllText(Output+"/After.txt",report.ToString());
                Undo.CollapseUndoOperations(undo);Debug.Log(report.ToString());
            }
            catch(Exception error)
            {
                Undo.RevertAllDownToGroup(undo);
                Debug.LogError("Ruin 1 relocation aborted and Undo restored the pre-relocation scene: "+error);
                throw;
            }
        }
    }
}
