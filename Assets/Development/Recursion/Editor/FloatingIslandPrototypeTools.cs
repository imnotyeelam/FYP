using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    [CustomEditor(typeof(FloatingIslandMotion))]
    public sealed class FloatingIslandMotionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();var motion=(FloatingIslandMotion)target;
            EditorGUILayout.HelpBox("Development motion only: no recursion binding. Edit Mode is repaired. Keep the player on a fixed surface while resetting.",MessageType.Info);
            using(new EditorGUI.DisabledScope(!Application.isPlaying || !motion.isActiveAndEnabled || motion.IsPracticeOwned))
            {
                if(GUILayout.Button("Repair Island"))motion.Repair();
                if(GUILayout.Button("Reset to Broken"))motion.ResetToBroken();
            }
            if(Application.isPlaying)EditorGUILayout.LabelField("State",motion.IsMoving?"Moving (collisions off)":motion.IsRepaired?"Repaired":"Broken / collisions off");
            if(motion.IsPracticeOwned)EditorGUILayout.HelpBox("Practice owns this section. Developer repair/reset controls are locked, including after completion.",MessageType.Info);
        }
    }
    public static class FloatingIslandPrototypeTools
    {
        internal const string Output="Temp/FloatingBridgeB2";
        internal const string RigPath="Ruin01_Area/Gameplay/FloatingBridgeMotionRig";
        internal static readonly string[] ModulePaths={"Bridge (1)","Bridge (2)","Bridge (3)"};
        internal static string P(Transform t)=>SpellbookStageTools.PathOf(t);
        internal static Transform Exact(string path)
        {
            var parts=path.Split('/');var roots=EditorSceneManager.GetActiveScene().GetRootGameObjects().Where(g=>g.name==parts[0]).ToArray();
            if(roots.Length!=1)throw new InvalidOperationException("Expected one exact scene root: "+path);
            var t=roots[0].transform;
            foreach(var name in parts.Skip(1))
            {
                var children=t.Cast<Transform>().Where(c=>c.name==name).ToArray();
                if(children.Length!=1)throw new InvalidOperationException("Expected one exact child "+name+" at "+P(t));
                t=children[0];
            }
            return t;
        }
        internal static FloatingBridgeMotionRig Rig()=>Exact(RigPath).GetComponent<FloatingBridgeMotionRig>() ?? throw new InvalidOperationException("Missing explicit bridge references.");
        [MenuItem("Tools/FYP/Ruin 1/Floating Bridge B2/Audit Live Bridge %#&F7")]
        public static void Audit()
        {
            SpellbookStageTools.RequireScene();Directory.CreateDirectory(Output);Physics.SyncTransforms();var scene=EditorSceneManager.GetActiveScene();
            if(!File.Exists(Output+"/LiveBefore.unity"))EditorSceneManager.SaveScene(scene,Output+"/LiveBefore.unity",true);
            var text=new StringBuilder("B2 LIVE SCENE AUDIT dirty="+scene.isDirty+"\n");
            foreach(var path in new[]{"Bridge","Bridge (1)","Bridge (2)","Bridge (3)","flying_stone_003","Magic Book","Ruin01_Area/SpawnPoints/L1_Spawnpoint","Ruin01_Area/SpawnPoints/L1_ReturnPoint","Ruin01_Area/Interactables/ReturnInteractionPoint"})
            {
                var root=Exact(path);text.AppendLine(P(root)+" P="+root.position.ToString("F6")+" R="+root.eulerAngles.ToString("F5")+" S="+root.lossyScale.ToString("F7"));
                if(!ModulePaths.Contains(path) && path!="Bridge")continue;
                foreach(var child in root.GetComponentsInChildren<Transform>(true))
                {
                    text.AppendLine("  "+P(child)+" P="+child.position.ToString("F5")+" R="+child.eulerAngles.ToString("F5")+" S="+child.lossyScale.ToString("F6"));
                    foreach(var c in child.GetComponents<Collider>())text.AppendLine("    "+c.GetType().Name+" enabled="+c.enabled+" trigger="+c.isTrigger+" bounds="+c.bounds);
                }
            }
            var modules=ModulePaths.Select(Exact).ToArray();
            for(int index=1;index<3;index++)
            {
                var root=modules[index];var pivot=root.TransformPoint(new Vector3(0,14.25f,0));BrokenCandidate(root,index,out var position,out var rotation);
                var delta=Matrix4x4.TRS(position,rotation,Vector3.one)*Matrix4x4.TRS(pivot,root.rotation,Vector3.one).inverse;
                text.AppendLine("CANDIDATE "+P(root)+" repaired="+pivot.ToString("F6")+" broken="+position.ToString("F5")+" R="+rotation.eulerAngles.ToString("F5"));
                foreach(var r in root.GetComponentsInChildren<Renderer>())
                {
                    var moved=BoundsAt(r,delta);text.AppendLine("  "+r.name+" brokenBounds="+moved);
                    foreach(var other in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>()).Where(o=>o.gameObject.activeInHierarchy && !modules.Any(m=>o.transform.IsChildOf(m))))
                        if(moved.Intersects(other.bounds))text.AppendLine("    STATIC AABB OVERLAP "+P(other.transform));
                }
            }
            File.WriteAllText(Output+"/LiveAudit.txt",text.ToString());Debug.Log("Read-only B2 audit: "+Output+"/LiveAudit.txt");
        }
        static void BrokenCandidate(Transform root,int index,out Vector3 position,out Quaternion rotation)
        {
            position=root.TransformPoint(new Vector3(0,14.25f,0))+(index==1?new Vector3(26,28,0):new Vector3(-26,26,0));
            rotation=index==1?Quaternion.Euler(-10,root.eulerAngles.y-25,14):Quaternion.Euler(12,root.eulerAngles.y+18,-14);
        }
        internal static Bounds BoundsAt(Renderer renderer,Matrix4x4 delta)
        {
            var b=renderer.localBounds;var m=delta*renderer.transform.localToWorldMatrix;var result=new Bounds(m.MultiplyPoint3x4(b.min),Vector3.zero);
            for(int i=0;i<8;i++)result.Encapsulate(m.MultiplyPoint3x4(new Vector3((i&1)==0?b.min.x:b.max.x,(i&2)==0?b.min.y:b.max.y,(i&4)==0?b.min.z:b.max.z)));
            return result;
        }
        [MenuItem("Tools/FYP/Ruin 1/Floating Bridge B2/Install Missing Modules and References %#&F8")]
        public static void Install()
        {
            SpellbookStageTools.RequireScene();var roots=ModulePaths.Select(Exact).ToArray();var gameplay=Exact("Ruin01_Area/Gameplay");
            if(gameplay.Cast<Transform>().Any(t=>t.name=="FloatingBridgeMotionRig"))throw new InvalidOperationException("Rig already exists; not duplicated.");
            var first=Exact("Bridge (1)/MotionRoot").GetComponent<FloatingIslandMotion>();if(first==null)throw new InvalidOperationException("Existing B1 module missing.");
            for(int i=1;i<3;i++)
            {
                var root=roots[i];int count=i==2?4:3;
                var expected=i==2?new[]{"flying_stone_006","flying_stone_008","flying_stone_003","PhaseA_EndConnector"}:new[]{"flying_stone_006","flying_stone_008","flying_stone_003"};
                if(root.childCount!=count || expected.Any(n=>root.Cast<Transform>().Count(c=>c.name==n)!=1))throw new InvalidOperationException("Unexpected children at "+P(root));
                if(root.GetComponentsInChildren<FloatingIslandMotion>(true).Length!=0)throw new InvalidOperationException("Already installed: "+P(root));
                var s=root.lossyScale;if(s.x<=0 || Mathf.Abs(s.x-s.y)>.00001f || Mathf.Abs(s.x-s.z)>.00001f)throw new InvalidOperationException("Uniform positive scale required: "+P(root));
                var colliders=root.GetComponentsInChildren<Collider>(true);
                if(colliders.Length!=count || colliders.Any(c=>!(c is MeshCollider mc) || mc.convex || c.isTrigger || !c.enabled))throw new InvalidOperationException("Unexpected colliders at "+P(root));
                if(root.GetComponentsInChildren<Rigidbody>(true).Length!=0)throw new InvalidOperationException("Unexpected Rigidbody at "+P(root));
            }
            Directory.CreateDirectory(Output);if(!File.Exists(Output+"/LiveBefore.unity"))Audit();
            Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Complete floating bridge motion B2");
            var text=new StringBuilder("B2 INSTALL\n");var views=new FloatingIslandMotion[3];views[0]=first;
            try
            {
                for(int i=1;i<3;i++)
                {
                    var root=roots[i];var children=root.Cast<Transform>().ToArray();var before=children.ToDictionary(c=>c,c=>c.localToWorldMatrix);
                    var colliders=root.GetComponentsInChildren<Collider>(true);var motion=Make("MotionRoot",root,new Vector3(0,14.25f,0));var poses=Make("Poses",root,Vector3.zero);
                    var repaired=Make("RepairedPose",poses,new Vector3(0,14.25f,0));var broken=Make("BrokenPose",poses,Vector3.zero);
                    BrokenCandidate(root,i,out var brokenPosition,out var brokenRotation);broken.SetPositionAndRotation(brokenPosition,brokenRotation);
                    foreach(var child in children){Undo.SetTransformParent(child,motion,"Keep repaired world geometry");PrefabUtility.RecordPrefabInstancePropertyModifications(child);}
                    var view=Undo.AddComponent<FloatingIslandMotion>(motion.gameObject);view.brokenPose=broken;view.repairedPose=repaired;view.moveDuration=4;view.startBroken=true;view.walkableColliders=colliders;EditorUtility.SetDirty(view);views[i]=view;
                    float error=0;foreach(var child in children)for(int k=0;k<16;k++)error=Mathf.Max(error,Mathf.Abs(before[child][k]-child.localToWorldMatrix[k]));
                    if(error>.0001f)throw new InvalidOperationException("Reparent error "+error+" at "+P(root));
                    text.AppendLine(P(root)+" maxWorldMatrixError="+error+" colliders="+colliders.Length+"\n repaired="+repaired.position.ToString("F6")+" R="+repaired.eulerAngles.ToString("F5")+"\n broken="+broken.position.ToString("F6")+" R="+broken.eulerAngles.ToString("F5"));
                }
                var rigObject=Make("FloatingBridgeMotionRig",gameplay,Vector3.zero);var rig=Undo.AddComponent<FloatingBridgeMotionRig>(rigObject.gameObject);
                rig.fixedStartingSection=Exact("Bridge");rig.placeStone3=views[0];rig.placeStone2=views[1];rig.placeStone1=views[2];rig.destinationIsland=Exact("flying_stone_003");EditorUtility.SetDirty(rig);
                EditorSceneManager.MarkSceneDirty(gameplay.gameObject.scene);Physics.SyncTransforms();Undo.CollapseUndoOperations(undo);
                File.WriteAllText(Output+"/Install.txt",text.ToString());EditorSceneManager.SaveScene(gameplay.gameObject.scene,Output+"/LiveAfter.unity",true);
                Selection.activeGameObject=rigObject.gameObject;Debug.Log(text.ToString()+"Explicit placeStone(3/2/1) references assigned. No lesson controller connected.");
            }
            catch{Undo.RevertAllDownToGroup(undo);throw;}
        }
        static Transform Make(string name,Transform parent,Vector3 local)
        {
            var go=new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Create bridge marker");go.transform.SetParent(parent,false);go.transform.localPosition=local;return go.transform;
        }
    }
}
