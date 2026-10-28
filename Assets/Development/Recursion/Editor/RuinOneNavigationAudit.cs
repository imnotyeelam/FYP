using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fyp.Development.Recursion.Editor
{
    public static class RuinOneNavigationAudit
    {
        public const string Output="Temp/Ruin1Navigation";
        public static readonly Vector3 ProposedRoot=new Vector3(-443,49f,63.75f);
        internal static string P(Transform t)=>SpellbookStageTools.PathOf(t);
        internal static Transform[] All()=>EditorSceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        internal static Transform Named(string name)=>All().Single(t=>t.name==name);
        internal static bool Island(Collider c)
        {
            for(var t=c.transform;t!=null;t=t.parent)
                if(t.name=="Ruin1_Ground" || t.name=="flying_stone_003" || t.name=="Bridge" || t.name.StartsWith("Bridge ("))return true;
            return false;
        }
        internal static RaycastHit? Ground(float x,float z)
        {
            var hits=Physics.RaycastAll(new Vector3(x,Return().transform.position.y+80,z),Vector3.down,160,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>Island(h.collider)).OrderByDescending(h=>h.point.y).ToArray();
            return hits.Length==0?(RaycastHit?)null:hits[0];
        }
        internal static CharacterController PlayerController()=>Object.FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Single(m=>m.gameObject.scene.path==SpellbookStageTools.ScenePath && m.gameObject.name=="Player").GetComponent<CharacterController>();
        internal static RuinEntrance Return()=>Named("Ruin01_Area").Find("Interactables/ReturnInteractionPoint").GetComponent<RuinEntrance>();
        internal static bool Clear(Vector3 feet,CharacterController cc,float extra=.02f)
        {
            var center=feet+cc.center;
            float half=Mathf.Max(0,cc.height*.5f-cc.radius);
            return !Physics.OverlapCapsule(center+Vector3.up*half,center-Vector3.up*half,cc.radius+extra,~0,QueryTriggerInteraction.Ignore)
                .Any(c=>!c.transform.IsChildOf(cc.transform));
        }
        internal static Vector3 SafeReturnPosition()
        {
            var marker=Return().transform;var cc=PlayerController();
            foreach(var offset in new[]{Vector3.zero,new Vector3(2,0,0),new Vector3(-2,0,0),new Vector3(0,0,-2),new Vector3(0,0,2)})
            {
                var xz=marker.position+offset;var hit=Ground(xz.x,xz.z);
                if(!hit.HasValue || Vector3.Angle(hit.Value.normal,Vector3.up)>cc.slopeLimit)continue;
                var position=hit.Value.point+Vector3.up*.45f;
                var stand=Ground(xz.x,xz.z-2);
                if(!stand.HasValue || !Clear(stand.Value.point+Vector3.up*.03f,cc))continue;
                int supported=0;
                foreach(float x in new[]{-.35f,0,.35f})foreach(float z in new[]{-.35f,0,.35f})
                {
                    var h=Ground(xz.x+x,xz.z-2+z);
                    if(h.HasValue && Mathf.Abs(h.Value.point.y-stand.Value.point.y)<.3f && Vector3.Angle(h.Value.normal,Vector3.up)<=cc.slopeLimit)supported++;
                }
                if(supported==9 && Vector3.Distance(stand.Value.point,position)<Return().interactionDistance-.5f)return position;
            }
            throw new InvalidOperationException("No safe return interaction candidate found. Nothing moved.");
        }
        [MenuItem("Tools/FYP/Ruin 1/Navigation/Inspect Return and Repair Span")]
        public static void Inspect()
        {
            SpellbookStageTools.RequireScene();Physics.SyncTransforms();
            var text=new StringBuilder("RUIN 1 NAVIGATION AUDIT — read only\nScene dirty="+EditorSceneManager.GetActiveScene().isDirty+"\n");
            var cc=PlayerController();var ret=Return();
            text.AppendLine("CharacterController height="+cc.height+" radius="+cc.radius+" center="+cc.center+" skin="+cc.skinWidth+
                " step="+cc.stepOffset+" slope="+cc.slopeLimit+" scale="+cc.transform.lossyScale);
            foreach(var entry in Object.FindObjectsByType<RuinEntrance>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Where(e=>e.gameObject.scene.path==SpellbookStageTools.ScenePath))
                text.AppendLine("TELEPORT "+P(entry.transform)+" origin="+entry.transform.position.ToString("F5")+
                    " -> "+P(entry.destination)+" "+entry.destination.position.ToString("F5")+" range="+entry.interactionDistance+" shared="+entry.useSharedInteraction);
            text.AppendLine("Return interaction path: "+P(ret.transform)+" destination: "+P(ret.destination));
            text.AppendLine("ReturnInteractionPoint components: "+string.Join(",",ret.GetComponents<Component>().Select(c=>c.GetType().Name)));
            foreach(var c in ret.GetComponents<Collider>())text.AppendLine("Return collider: "+c.GetType().Name+" enabled="+c.enabled+" bounds="+c.bounds);
            var returnPos=SafeReturnPosition();text.AppendLine("SAFE RETURN INTERACTION CANDIDATE "+returnPos.ToString("F5")+" (sampled near current interaction)");
            var standHit=Ground(returnPos.x,returnPos.z-2).Value;
            text.AppendLine("Return standing point="+(standHit.point+Vector3.up*.03f).ToString("F5")+" slope="+Vector3.Angle(standHit.normal,Vector3.up)+" capsule clear="+Clear(standHit.point+Vector3.up*.03f,cc));
            if(FloatingIslandPrototypeTools.Exact("Ruin01_Area").parent!=null)
            {
                text.AppendLine("Historical fixed-coordinate repair-span proposal omitted after area reparenting; current return wiring and footing above were checked.");
                Directory.CreateDirectory(Output);File.WriteAllText(Output+"/Audit.txt",text.ToString());Debug.Log(text.ToString());return;
            }
            text.AppendLine("GAP EDGES, actual MeshCollider ray hits, .05m resolution, z=52..75:");
            for(float x=-445.5f;x<=-440.5f;x+=.5f)
            {
                RaycastHit? near=null,far=null;
                for(float z=52;z<=75;z+=.05f)
                {
                    var h=Ground(x,z);
                    if(!h.HasValue || h.Value.point.y<40 || Vector3.Angle(h.Value.normal,Vector3.up)>cc.slopeLimit)continue;
                    if(h.Value.collider.transform.root.name=="Bridge")near=h;
                    if(h.Value.collider.transform.root.name=="Bridge (1)" && !far.HasValue)far=h;
                }
                if(near.HasValue && far.HasValue)
                    text.AppendLine("x="+x+" near="+near.Value.point.ToString("F3")+" far="+far.Value.point.ToString("F3")+
                        " gap="+(far.Value.point.z-near.Value.point.z).ToString("F3")+" rise="+(far.Value.point.y-near.Value.point.y).ToString("F3"));
            }
            text.AppendLine("Width strip: current stones span x=-445..-441 (4m); banks x=-445.5..-440.5 (5m)."+
                " Three 3m-long stones occupy z=59.25..68.25. Near bank z=55.25..59.25, far bank z=68.25..72.25; joining banks bridge the larger off-axis void.");
            text.AppendLine("VIRTUAL PROPOSAL — NOT APPLIED. Root="+ProposedRoot.ToString("F5")+" R=(0,0,0) S=(1,1,1)");
            var bridge=Object.FindObjectsByType<BridgeWorldView>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single();
            var delta=ProposedRoot-bridge.transform.position;
            foreach(var t in bridge.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("Bank") || t.name.Contains("Step") || t.name.EndsWith("_Repaired")))
                text.AppendLine("Proposed "+P(t)+" P="+(t.position+delta).ToString("F5")+" R="+t.eulerAngles.ToString("F4")+" S="+t.lossyScale.ToString("F4"));
            var cam=bridge.GetComponentInChildren<Camera>(true);var camPos=cam.transform.position+delta;var look=bridge.ObservationBounds().center+delta;
            var camRot=Quaternion.LookRotation(look-camPos);
            text.AppendLine("Proposed CAMERA P="+camPos.ToString("F5")+" R="+camRot.eulerAngles.ToString("F5")+" bounds center="+look.ToString("F5"));
            Vector3 ext=bridge.ObservationBounds().extents,right=camRot*Vector3.right,up=camRot*Vector3.up;
            float horizontal=Mathf.Abs(right.x)*ext.x+Mathf.Abs(right.y)*ext.y+Mathf.Abs(right.z)*ext.z;
            float vertical=Mathf.Abs(up.x)*ext.x+Mathf.Abs(up.y)*ext.y+Mathf.Abs(up.z)*ext.z;
            text.AppendLine("Proposed orthographic size at 1920x1080, viewport width .65="+(Mathf.Max(vertical,horizontal/(1920f*.65f/1080))*1.12f));
            foreach(var xz in new[]{new Vector2(-443,51.75f),new Vector2(-443,77)})
            {
                var h=Ground(xz.x,xz.y);text.AppendLine("Proposed "+(xz.y<60?"OBSERVATION":"FRAGMENT")+" surface="+(h.HasValue?h.Value.point.ToString("F5"):"MISSING")+
                    " capsuleClear="+(h.HasValue&&Clear(h.Value.point+Vector3.up*.03f,cc)));
            }
            int lines=0,blocked=0,overlapSamples=0;var occluders=new HashSet<string>();var overlaps=new HashSet<string>();
            foreach(var stone in bridge.stones)for(int sample=0;sample<=40;sample++)
            {
                float t=sample/40f;var pos=Vector3.Lerp(stone.brokenPose.position,stone.repairedPose.position,t)+delta;
                var rot=Quaternion.Slerp(stone.brokenPose.rotation,stone.repairedPose.rotation,t);
                var half=Vector3.Scale(stone.crossingCollider.size,stone.transform.lossyScale)*.5f;
                var hits=Physics.OverlapBox(pos+rot*stone.crossingCollider.center,half,rot,~0,QueryTriggerInteraction.Ignore).Where(Island).ToArray();
                if(hits.Length>0){overlapSamples++;foreach(var hit in hits)overlaps.Add(P(hit.transform));}
                foreach(int x in new[]{-1,0,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
                {
                    var target=pos+rot*Vector3.Scale(half,new Vector3(x,y,z));
                    // Orthographic rays parallel to the proposed camera, not perspective fan rays.
                    var forward=camRot*Vector3.forward;float depth=Vector3.Dot(target-camPos,forward);var origin=target-forward*depth;
                    lines++;var rayHits=Physics.RaycastAll(origin,forward,depth-.03f,~0,QueryTriggerInteraction.Ignore).Where(h=>Island(h.collider)).ToArray();
                    if(rayHits.Length>0){blocked++;foreach(var h in rayHits)occluders.Add(P(h.collider.transform));}
                }
            }
            text.AppendLine("Motion-envelope samples intersecting island colliders="+overlapSamples+" / 123; "+string.Join(";",overlaps));
            text.AppendLine("Orthographic sightline samples blocked="+blocked+" / "+lines+"; "+string.Join(";",occluders));
            text.AppendLine("Approach centerline static surfaces and headroom (bank/step heights shown separately):");
            for(float z=50.75f;z<=77.75f;z+=1)
            {
                var h=Ground(-443,z);text.AppendLine("  z="+z+" island="+(h.HasValue?h.Value.point.y.ToString("F3"):"void")+
                    " slope="+(h.HasValue?Vector3.Angle(h.Value.normal,Vector3.up).ToString("F2"):"-")+
                    " headroom="+(h.HasValue&&Clear(h.Value.point+Vector3.up*.03f,cc)));
            }
            Directory.CreateDirectory(Output);File.WriteAllText(Output+"/Audit.txt",text.ToString());Debug.Log(text.ToString());
        }
        [MenuItem("Tools/FYP/Ruin 1/Navigation/Align Return Interaction Only")]
        public static void AlignReturn()
        {
            SpellbookStageTools.RequireScene();var scene=EditorSceneManager.GetActiveScene();
            if(Return().transform.Find("ReturnMagicRock")!=null)
                throw new InvalidOperationException("Return interaction is already aligned and has its landmark. No edit made.");
            if(scene.isDirty)throw new InvalidOperationException("Unsaved scene changes detected. No object was moved or saved.");
            Physics.SyncTransforms();var target=Return();var newPosition=SafeReturnPosition();
            var before=All().ToDictionary(t=>t,t=>EditorJsonUtility.ToJson(t));
            var behaviours=All().SelectMany(t=>t.GetComponents<MonoBehaviour>()).Where(b=>b!=null).ToDictionary(b=>b,b=>EditorJsonUtility.ToJson(b));
            var old=target.transform.position;Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Align Ruin 1 return interaction on far island");
            try
            {
                Undo.RecordObject(target.transform,"Move return interaction only");target.transform.position=newPosition;
                foreach(var pair in before)if(pair.Key!=target.transform && EditorJsonUtility.ToJson(pair.Key)!=pair.Value)
                    throw new InvalidOperationException("Unexpected existing transform change: "+P(pair.Key));
                foreach(var pair in behaviours)if(EditorJsonUtility.ToJson(pair.Key)!=pair.Value)
                    throw new InvalidOperationException("A gameplay reference changed: "+pair.Key.name);
                EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene,SpellbookStageTools.ScenePath))throw new InvalidOperationException("Save failed.");
                Undo.CollapseUndoOperations(undo);
                Debug.Log("RETURN ALIGNED: "+P(target.transform)+" "+old.ToString("F5")+" -> "+newPosition.ToString("F5")+
                    "; destination remains "+P(target.destination)+" "+target.destination.position.ToString("F5")+"; range="+target.interactionDistance+". Bridge untouched.");
            }
            catch{Undo.RevertAllDownToGroup(undo);throw;}
        }
    }
}
