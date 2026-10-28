using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;

namespace Fyp.Development.Recursion.Editor
{
    public static class SpellbookStageTools
    {
        internal const string ScenePath = "Assets/Scenes/MagicWorld_Development.unity";
        internal static void RequireScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorSceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException("Open the development scene in Edit Mode; no scene will be loaded automatically.");
        }

        internal static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
        [MenuItem("Tools/FYP/Ruin 1/Stage 1-2/Run Lesson Model Tests")]
        public static void TestLesson() => Debug.Log("PASS: recursion lesson unit checks: " + BridgePracticeLessonChecks.Run());
        private static bool Manual(Collider c)
        {
            for(var t=c.transform;t!=null;t=t.parent)
                if(t.name=="Ruin1_Ground" || t.name=="flying_stone_003" || t.name=="Bridge" || t.name.StartsWith("Bridge ("))return true;
            return false;
        }
        private static RaycastHit? Surface(Vector3 point)
        {
            var originY=FloatingIslandPrototypeTools.Exact("Ruin01_Area").position.y+150;
            var hits = Physics.RaycastAll(new Vector3(point.x, originY, point.z), Vector3.down, 200,
                ~0, QueryTriggerInteraction.Ignore).Where(h => Manual(h.collider)).OrderByDescending(h => h.point.y).ToArray();
            return hits.Length == 0 ? (RaycastHit?)null : hits[0];
        }

        [MenuItem("Tools/FYP/Ruin 1/Stage 1-2/Audit Updated Layout")]
        public static void Audit()
        {
            RequireScene(); Physics.SyncTransforms();
            var scene = EditorSceneManager.GetActiveScene();
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var text = new StringBuilder("LIVE SCENE AUDIT (read only)\nDirty=" + scene.isDirty + "\n");
            foreach (var t in all.Where(t => t.name == "L1_Spawnpoint" || t.name == "ReturnInteractionPoint" ||
                t.name == "Magic Book" || t.name == "Bridge" || t.name.StartsWith("Bridge (")))
            {
                text.AppendLine(PathOf(t) + " | active=" + t.gameObject.activeInHierarchy + " | P=" + t.position.ToString("F5") +
                    " R=" + t.eulerAngles.ToString("F5") + " S=" + t.lossyScale.ToString("F5"));
                if (t.name.StartsWith("Bridge"))
                    foreach (var c in t.GetComponentsInChildren<Collider>(true))
                    {
                        var r = c.GetComponent<Renderer>();
                        text.AppendLine("  " + PathOf(c.transform) + " | " + c.GetType().Name + " enabled=" + c.enabled +
                            " trigger=" + c.isTrigger + " colliderBounds=" + c.bounds + (r != null ? " rendererBounds=" + r.bounds : ""));
                    }
                if (t.name == "L1_Spawnpoint" || t.name == "Magic Book")
                {
                    var hit = Surface(t.position);
                    text.AppendLine(hit.HasValue ? "  island surface=" + PathOf(hit.Value.collider.transform) + " hit=" + hit.Value.point.ToString("F5") +
                        " pivot-minus-surface=" + (t.position.y-hit.Value.point.y).ToString("F5") + " slope=" + Vector3.Angle(hit.Value.normal,Vector3.up) : "  NO ISLAND SURFACE BELOW XZ");
                    int supported=0;
                    foreach (float x in new[]{-.3f,0,.3f}) foreach(float z in new[]{-.3f,0,.3f})
                        if (Surface(t.position+new Vector3(x,0,z)).HasValue) supported++;
                    text.AppendLine("  radius .3 footprint samples with island surface=" + supported + "/9");
                }
            }
            text.AppendLine("Bridge (4) exists=" + all.Any(t=>t.name=="Bridge (4)"));
            if(FloatingIslandPrototypeTools.Exact("Ruin01_Area").parent!=null)
            {
                text.AppendLine("Historical fixed-coordinate gap scan omitted after area reparenting; live object transforms and surfaces above were checked.");
                Directory.CreateDirectory("Temp/SpellbookChecks");
                File.WriteAllText("Temp/SpellbookChecks/LiveLayoutAudit.txt",text.ToString());Debug.Log(text.ToString());return;
            }
            text.AppendLine("Candidate gaps along x=-432.1, z=25..235; surface must be y>40. Sample step=.25m (not a capsule path proof):");
            float? start=null; RaycastHit? last=null;
            for(float z=25;z<=235;z+=.25f)
            {
                var hit=Surface(new Vector3(-432.1f,0,z)); bool walk=hit.HasValue && hit.Value.point.y>40;
                if(!walk && last.HasValue && start==null) start=z;
                if(walk && start.HasValue)
                { text.AppendLine("  gap z="+start.Value+".."+z+" length="+(z-start.Value)+" nearHeight="+last.Value.point.y+" farHeight="+hit.Value.point.y);start=null; }
                if(walk)last=hit;
            }
            var spawn=all.Single(t=>t.name=="L1_Spawnpoint");var magic=all.Single(t=>t.name=="Magic Book");
            int routeSupport=0,routeGentle=0;
            for(int i=0;i<=100;i++)
            {
                var h=Surface(Vector3.Lerp(spawn.position,magic.position,i/100f));
                if(h.HasValue){routeSupport++;if(Vector3.Angle(h.Value.normal,Vector3.up)<=45)routeGentle++;}
            }
            text.AppendLine("Entry-to-book straight route: supported="+routeSupport+"/101, slope<=45="+routeGentle+"/101 (not a capsule sweep).");
            text.AppendLine("Off-axis repair-span candidates, first two Bridge groups, width not yet validated:");
            int found=0;
            for(float x=-448;x<=-416 && found<8;x+=1)
            {
                float? gap=null;RaycastHit? near=null;
                for(float z=45;z<=95;z+=.25f)
                {
                    var h=Surface(new Vector3(x,0,z));bool supported=h.HasValue && h.Value.point.y>40;
                    if(!supported && near.HasValue && gap==null)gap=z;
                    if(supported && gap.HasValue)
                    {
                        float length=z-gap.Value;
                        if(length>=3 && length<=12 && near.Value.collider.transform.root.name=="Bridge" &&
                            h.Value.collider.transform.root.name=="Bridge (1)" && Mathf.Abs(near.Value.point.y-h.Value.point.y)<1.2f && found<8)
                        {text.AppendLine("  x="+x+" z="+gap+".."+z+" length="+length+" near="+near.Value.point.ToString("F3")+" far="+h.Value.point.ToString("F3"));found++;}
                        gap=null;
                    }
                    if(supported)near=h;
                }
            }
            Directory.CreateDirectory("Temp/SpellbookChecks");
            File.WriteAllText("Temp/SpellbookChecks/LiveLayoutAudit.txt",text.ToString());
            Debug.Log(text.ToString());
        }

        [MenuItem("Tools/FYP/Ruin 1/Stage 1-2/Install Spellbook Fourth Page")]
        public static void Install()
        {
            RequireScene();
            var scene=EditorSceneManager.GetActiveScene();
            // Refuse a dirty scene rather than saving user edits incidentally.
            if(scene.isDirty)throw new InvalidOperationException("Scene has unsaved changes. No installation performed; preserve and review those changes first.");
            var book=Object.FindObjectsByType<AncientSpellbookUI>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Single(b=>b.gameObject.scene==scene);
            if(book.guidedSimulation!=null || book.spellbookPanel.transform.Find("GuidedSimulationPage")!=null)
                throw new InvalidOperationException("Fourth page already exists; refusing duplicate installation.");
            var oldTransforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
                .ToDictionary(t=>t,t=>EditorJsonUtility.ToJson(t));
            var behaviours=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true))
                .Where(b=>b!=null && b!=book).ToDictionary(b=>b,b=>EditorJsonUtility.ToJson(b));
            Undo.IncrementCurrentGroup(); int undo=Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add independent Spellbook guided simulation");
            try
            {
                var root=Rect("GuidedSimulationPage",book.spellbookPanel.transform,.24f,.20f,.76f,.80f);
                Undo.RegisterCreatedObjectUndo(root.gameObject,"Create guided simulation page");
                root.gameObject.AddComponent<Image>().color=new Color(.035f,.055f,.08f,1);
                var simulation=root.gameObject.AddComponent<SpellbookRecursionSimulation>();
                var details=RecursionPanelView.Create(root,book.codeText.font);
                var dr=(RectTransform)details.transform;
                dr.anchorMin=new Vector2(.44f,0); dr.anchorMax=Vector2.one;
                details.gameObject.name="ExecutionDetails";
                details.closeButton.gameObject.SetActive(false); // The existing book Close remains the only modal owner.
                simulation.details=details;
                details.explanation.textWrappingMode=TextWrappingModes.Normal;
                details.explanation.overflowMode=TextOverflowModes.Ellipsis;
                var diagramRoot=Rect("BridgeDiagram",root,.02f,.05f,.42f,.95f);
                var diagram=diagramRoot.gameObject.AddComponent<BridgeDiagramView>(); simulation.diagram=diagram;
                Label("Heading",diagramRoot,"2D BRIDGE",.02f,.88f,.98f,.99f,28,book.codeText.font);
                Label("LearningOnly",diagramRoot,"Learning only\nThe real bridge stays unchanged.\nNo fragments are awarded.",.02f,.68f,.98f,.86f,22,book.codeText.font);
                Box("NearBank",diagramRoot,.01f,.43f,.16f,.61f,new Color(.33f,.41f,.38f));
                Box("FarBank",diagramRoot,.84f,.43f,.99f,.61f,new Color(.33f,.41f,.38f));
                diagram.stones=new Image[3];
                for(int i=0;i<3;i++)
                {
                    float x=.19f+i*.22f;
                    var image=Box("Stone_"+(i+1),diagramRoot,x,.48f,x+.18f,.57f,new Color(.27f,.3f,.35f));
                    diagram.stones[i]=image;
                    var label=Label("Action",image.transform,"n="+(3-i),.01f,0,.99f,1,20,book.codeText.font);
                    label.alignment=TextAlignmentOptions.Center;
                }
                diagram.status=Label("Status",diagramRoot,"Placed 0 / 3",.02f,.18f,.98f,.32f,23,book.codeText.font);
                Label("Legend",diagramRoot,"Grey: not placed\nTeal: placed\nGold: returning\nGreen: complete",.02f,.01f,.98f,.18f,20,book.codeText.font);
                root.gameObject.SetActive(false);
                Undo.RecordObject(book,"Assign optional fourth page"); book.guidedSimulation=simulation; EditorUtility.SetDirty(book);
                foreach(var pair in oldTransforms)
                {
                    // The book panel legitimately gains one child; every existing TRS and other hierarchy stays unchanged.
                    if(pair.Key==book.spellbookPanel.transform)continue;
                    if(EditorJsonUtility.ToJson(pair.Key)!=pair.Value)throw new InvalidOperationException("Existing transform changed: "+PathOf(pair.Key));
                }
                foreach(var pair in behaviours)
                    if(EditorJsonUtility.ToJson(pair.Key)!=pair.Value)throw new InvalidOperationException("Existing behaviour changed: "+pair.Key.name);
                Undo.CollapseUndoOperations(undo);
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new InvalidOperationException("Scene save failed.");
                Debug.Log("SPELLBOOK INSTALLED: editable fourth page; one optional reference added. Existing transforms/gameplay references preserved. No world/reward dependencies.");
            }
            catch{Undo.RevertAllDownToGroup(undo);throw;}
        }
        private static RectTransform Rect(string name,Transform parent,float x0,float y0,float x1,float y1)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false);rect.anchorMin=new Vector2(x0,y0);rect.anchorMax=new Vector2(x1,y1);
            rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
        }
        private static Image Box(string name,Transform parent,float x0,float y0,float x1,float y1,Color color)
        {var image=Rect(name,parent,x0,y0,x1,y1).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
        private static TMP_Text Label(string name,Transform parent,string value,float x0,float y0,float x1,float y1,float size,TMP_FontAsset font)
        {
            var text=Rect(name,parent,x0,y0,x1,y1).gameObject.AddComponent<TextMeshProUGUI>();
            text.font=font;text.text=value;text.fontSize=size;text.enableAutoSizing=true;text.fontSizeMin=16;text.fontSizeMax=size;
            text.color=new Color(.92f,.95f,1);text.raycastTarget=false;return text;
        }
        [MenuItem("Tools/FYP/Ruin 1/Stage 1-2/Refine Fourth Page Text Wrapping")]
        public static void RefineText()
        {
            RequireScene();var scene=EditorSceneManager.GetActiveScene();
            if(scene.isDirty)throw new InvalidOperationException("Unsaved scene edits detected; no wrapping changes performed.");
            var sim=Object.FindObjectsByType<SpellbookRecursionSimulation>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Single(s=>s.gameObject.scene==scene);
            Undo.RecordObject(sim.details.explanation,"Wrap fourth-page explanation");
            sim.details.explanation.textWrappingMode=TextWrappingModes.Normal;
            sim.details.explanation.overflowMode=TextOverflowModes.Ellipsis;
            EditorUtility.SetDirty(sim.details.explanation);EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene,ScenePath);
            Debug.Log("SPELLBOOK: enabled wrapping only on the new fourth-page explanation.");
        }
    }
}
