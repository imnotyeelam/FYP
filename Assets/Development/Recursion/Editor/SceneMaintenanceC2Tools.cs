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
    public static class SceneMaintenanceC2Tools
    {
        public const string Folder="LocalBackups/SceneMaintenanceC2_20261119";
        public static readonly string[] Paths={
            "Ruin01_RelocationRoot/Ruin01_Area/SpawnPoints/L1_Spawnpoint/Cube (2)",
            "Ruin01_RelocationRoot/Ruin01_Area/Interactables/RecursionObservationPoint/Cube",
            "Canvas/RuinInteractionHint",
            "Ruin02_Area/Gameplay/RitualChamber/MagicalFarmGateway/GatewayLabel",
            "Ruin02_Area/Gameplay/RitualChamber/MagicalFarmGateway/FarmGatewayLandmark"
        };
        public static Transform Exact(string path)=>FloatingIslandPrototypeTools.Exact(path);
        [MenuItem("Tools/FYP/Scene Maintenance C2/Archive Five Verified Candidates %#F3")]
        public static void Archive()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/Scenes/MagicWorld_Development.unity")
                throw new InvalidOperationException("Open development scene in Edit Mode.");
            if(scene.GetRootGameObjects().Any(g=>g.name=="_Archived_Objects"))
                throw new InvalidOperationException("Archive already exists; verify it rather than creating duplicates.");
            var candidates=Paths.Select(Exact).ToArray();
            var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            foreach(var t in candidates)
            {
                if(t.gameObject.activeSelf)throw new InvalidOperationException("Candidate is no longer inactive: "+t.name);
                var subtree=new HashSet<Transform>(t.GetComponentsInChildren<Transform>(true));
                foreach(var component in all.Where(x=>!subtree.Contains(x)).SelectMany(x=>x.GetComponents<Component>()).Where(c=>c!=null&&!(c is Transform)))
                {
                    var p=new SerializedObject(component).GetIterator();
                    while(p.Next(true))
                    {
                        if(p.propertyType!=SerializedPropertyType.ObjectReference)continue;
                        var value=p.objectReferenceValue;
                        var target=value is GameObject go?go.transform:value is Component c?c.transform:null;
                        if(target!=null&&subtree.Contains(target))
                            throw new InvalidOperationException($"Live external reference prevents archive: {SceneMaintenanceC1Tools.Path(component.transform)} {component.GetType().Name}.{p.propertyPath} -> {t.name}");
                    }
                }
            }
            // Capture every existing transform and all non-hierarchy serialized references, not just the five roots.
            var matrices=all.ToDictionary(t=>t,t=>t.localToWorldMatrix);
            var parents=all.ToDictionary(t=>t,t=>t.parent);
            var active=all.ToDictionary(t=>t,t=>t.gameObject.activeSelf);
            var corners=candidates.OfType<RectTransform>().ToDictionary(t=>t,WorldCorners);
            var refs=References(all);
            var hints=UnityEngine.Object.FindFirstObjectByType<InteractionHintManager>();
            var registrations=hints.interactionTargets.ToArray();var modals=hints.modalPanels.ToArray();
            Directory.CreateDirectory(Folder);
            if(!EditorSceneManager.SaveScene(scene,$"{Folder}/Before_{DateTime.Now:HHmmssfff}.unity",true))throw new IOException("Scene backup failed.");
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Archive five verified obsolete visuals");
            try
            {
                var archive=new GameObject("_Archived_Objects");
                Undo.RegisterCreatedObjectUndo(archive,"Create archive container");
                archive.SetActive(false);
                foreach(var t in candidates)Undo.SetTransformParent(t,archive.transform,"Archive obsolete visual, preserve world transform");
                float maxError=0;
                foreach(var t in all)
                {
                    float error=MatrixError(matrices[t],t.localToWorldMatrix);maxError=Mathf.Max(maxError,error);
                    if(error>.0002f)throw new InvalidOperationException("World transform changed: "+t.name+" error "+error);
                    if(t.gameObject.activeSelf!=active[t])throw new InvalidOperationException("Active state changed: "+t.name);
                    if(!candidates.Contains(t)&&t.parent!=parents[t])throw new InvalidOperationException("Unapproved parent changed: "+t.name);
                }
                foreach(var pair in corners)
                {
                    var after=WorldCorners(pair.Key);
                    if(after.Where((p,i)=>Vector3.Distance(p,pair.Value[i])>.0002f).Any())
                        throw new InvalidOperationException("Archived UI world corners changed.");
                }
                var afterRefs=References(all);
                if(refs.Count!=afterRefs.Count||refs.Any(p=>!afterRefs.TryGetValue(p.Key,out var v)||v!=p.Value))
                    throw new InvalidOperationException("Serialized component references changed.");
                if(!registrations.SequenceEqual(hints.interactionTargets)||!modals.SequenceEqual(hints.modalPanels))
                    throw new InvalidOperationException("Shared E or modal registration changed.");
                Verify();
                var report=new StringBuilder("C2 archive verification\n");
                report.AppendLine($"Exactly five direct children; all inactive. Existing objects checked: {all.Length}. Max world matrix error: {maxError:F8}. UI world corners preserved. Serialized reference properties checked: {refs.Count}; all unchanged. Shared E/modal arrays unchanged.");
                for(int i=0;i<candidates.Length;i++)report.AppendLine(Paths[i]+" -> "+SceneMaintenanceC1Tools.Path(candidates[i])+" world="+candidates[i].position.ToString("F3"));
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed.");
                File.WriteAllText(Folder+"/ArchiveVerification.txt",report.ToString());
                Undo.CollapseUndoOperations(group);Debug.Log("PASS C2 archive: exactly five inactive objects; all world transforms, serialized references and shared E registrations preserved. Scene saved.");
            }
            catch{Undo.RevertAllDownToGroup(group);throw;}
        }
        public static void Verify()
        {
            var archive=Exact("_Archived_Objects");
            if(archive.parent!=null||archive.childCount!=5||archive.gameObject.activeSelf)
                throw new InvalidOperationException("Archive root/count/state mismatch.");
            foreach(var path in Paths)
            {
                var child=archive.Find(path.Substring(path.LastIndexOf('/')+1));
                if(child==null||child.gameObject.activeSelf||child.gameObject.activeInHierarchy)
                    throw new InvalidOperationException("Archived candidate missing/active: "+path);
            }
            var gate=Exact("Ruin02_Area/Gameplay/RitualChamber/MagicalFarmGateway");
            var exit=Exact("Ruin02_MagicalFarm/FarmReturnPoint").GetComponent<Ruin02FarmAccess>();
            var rock=UnityEngine.Object.FindFirstObjectByType<Ruin02ReturnGate>();
            if(!gate.gameObject.activeInHierarchy||gate.Find("FarmRitualReturnSpawn")!=exit.destination||rock.farmAccess.transform!=gate)
                throw new InvalidOperationException("Protected H4 gateway configuration/return destination changed.");
        }
        static Vector3[] WorldCorners(RectTransform t){var points=new Vector3[4];t.GetWorldCorners(points);return points;}
        static float MatrixError(Matrix4x4 a,Matrix4x4 b){float error=0;for(int i=0;i<16;i++)error=Mathf.Max(error,Mathf.Abs(a[i]-b[i]));return error;}
        static Dictionary<string,int> References(IEnumerable<Transform> all)
        {
            var refs=new Dictionary<string,int>();
            foreach(var component in all.SelectMany(t=>t.GetComponents<Component>()).Where(c=>c!=null&&!(c is Transform)))
            {
                var p=new SerializedObject(component).GetIterator();
                while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference)
                    refs[component.GetInstanceID()+"|"+p.propertyPath]=p.objectReferenceValue==null?0:p.objectReferenceValue.GetInstanceID();
            }
            return refs;
        }
    }
}
