using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace Fyp.Development.Recursion.Editor
{
    public static class BridgePracticeTools
    {
        internal const string Output="Temp/BridgePracticeC";
        internal const string ControllerPath="Ruin01_Area/Gameplay/BridgePractice";
        internal static Transform Exact(string p)=>FloatingIslandPrototypeTools.Exact(p);
        internal static BridgePracticeController Controller()=>Exact(ControllerPath).GetComponent<BridgePracticeController>();
        static Vector3 SafePoint(FloatingBridgeMotionRig rig)
        {
            var p=rig.fixedStartingSection.position+Vector3.back*6;var cc=Exact("Player").GetComponent<CharacterController>();
            var hits=Physics.RaycastAll(new Vector3(p.x,110,p.z),Vector3.down,140,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>h.collider.transform.IsChildOf(rig.fixedStartingSection) && Vector3.Angle(h.normal,Vector3.up)<=cc.slopeLimit).OrderByDescending(h=>h.point.y).ToArray();
            if(hits.Length==0)throw new InvalidOperationException("No safe fixed-bank interaction surface.");
            var ground=hits[0].point;
            foreach(float dx in new[]{-.4f,0,.4f})foreach(float dz in new[]{-.4f,0,.4f})
            {
                var support=Physics.RaycastAll(new Vector3(ground.x+dx,110,ground.z+dz),Vector3.down,140,~0,QueryTriggerInteraction.Ignore)
                    .Any(h=>h.collider.transform.IsChildOf(rig.fixedStartingSection) && Mathf.Abs(h.point.y-ground.y)<.3f && Vector3.Angle(h.normal,Vector3.up)<=cc.slopeLimit);
                if(!support)throw new InvalidOperationException("Interaction footprint is not fully supported.");
            }
            return ground+Vector3.up*.3f;
        }
        [MenuItem("Tools/FYP/Ruin 1/Phase C/Audit Practice Dependencies %#&F11")]
        public static void Audit()
        {
            SpellbookStageTools.RequireScene();Directory.CreateDirectory(Output);Physics.SyncTransforms();
            var scene=EditorSceneManager.GetActiveScene();if(!File.Exists(Output+"/LiveBefore.unity"))EditorSceneManager.SaveScene(scene,Output+"/LiveBefore.unity",true);
            var rig=FloatingIslandPrototypeTools.Rig();var legacy=Exact("Ruin01_Area/Gameplay/RuinRecursionVisualizer").GetComponent<RuinRecursionVisualizer>();
            var manager=Exact("InteractionHintManager").GetComponent<InteractionHintManager>();
            var s=new StringBuilder("PHASE C READ-ONLY LIVE AUDIT dirty="+scene.isDirty+"\n");
            s.AppendLine("Fallback enabled="+legacy.enabled+" panel="+FloatingIslandPrototypeTools.P(legacy.panelView.transform)+" interaction="+FloatingIslandPrototypeTools.P(legacy.interactionPoint)+" "+legacy.interactionPoint.position);
            s.AppendLine("Canvas="+FloatingIslandPrototypeTools.P(legacy.existingCanvas.transform)+" mode="+legacy.existingCanvas.renderMode+" scaler="+legacy.existingCanvas.GetComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode);
            s.AppendLine("Practice candidate="+SafePoint(rig).ToString("F6"));
            foreach(var path in new[]{"Ruin01_Area/SpawnPoints/L1_Spawnpoint","Ruin01_Area/SpawnPoints/L1_ReturnPoint","Ruin01_Area/Interactables/ReturnInteractionPoint","Magic Book"})
            {var t=Exact(path);s.AppendLine(path+" "+t.position.ToString("F5")+" components="+string.Join(",",t.GetComponents<Component>().Select(c=>c.GetType().Name)));}
            var ret=Exact("Ruin01_Area/Interactables/ReturnInteractionPoint").GetComponent<RuinEntrance>();
            s.AppendLine("Return interaction destination="+FloatingIslandPrototypeTools.P(ret.destination)+" "+ret.destination.position+" range="+ret.interactionDistance);
            s.AppendLine("Existing shared targets="+string.Join(",",manager.interactionTargets.Select(t=>t.GetType().Name+"@"+FloatingIslandPrototypeTools.P(t.transform))));
            File.WriteAllText(Output+"/LiveAudit.txt",s.ToString());Debug.Log("Phase C audit: "+Output+"/LiveAudit.txt");
        }
        [MenuItem("Tools/FYP/Ruin 1/Phase C/Install Interactive Practice %#&F12")]
        public static void Install()
        {
            SpellbookStageTools.RequireScene();Physics.SyncTransforms();
            var gameplay=Exact("Ruin01_Area/Gameplay");if(gameplay.Cast<Transform>().Any(c=>c.name=="BridgePractice"))throw new InvalidOperationException("Practice already installed; no duplicate created.");
            var legacy=Exact("Ruin01_Area/Gameplay/RuinRecursionVisualizer").GetComponent<RuinRecursionVisualizer>();
            var rig=FloatingIslandPrototypeTools.Rig();var manager=Exact("InteractionHintManager").GetComponent<InteractionHintManager>();
            if(!manager.coordinateInteractions || legacy.panelView==null)throw new InvalidOperationException("Expected existing shared interaction and serialized right panel.");
            if(new[]{rig.placeStone3,rig.placeStone2,rig.placeStone1}.Any(m=>!m.startBroken || !m.enabled))throw new InvalidOperationException("All three modules must start broken with their motion components enabled.");
            if(!File.Exists(Output+"/LiveBefore.unity"))Audit();
            Vector3 point=SafePoint(rig);Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Install Phase C bridge practice");
            try
            {
                var root=Empty("BridgePractice",gameplay);var controller=root.AddComponent<BridgePracticeController>();
                controller.rig=rig;controller.player=Exact("Player");controller.interactionPoint=Empty("BridgePracticeInteractionPoint",Exact("Ruin01_Area/Interactables")).transform;
                controller.interactionPoint.position=point;
                var panelObject=Object.Instantiate(legacy.panelView.gameObject,legacy.existingCanvas.transform,false);panelObject.name="RecursionPracticePanel";Undo.RegisterCreatedObjectUndo(panelObject,"Adapt editable practice panel from existing template");
                var details=panelObject.GetComponent<RecursionPanelView>();var view=panelObject.AddComponent<BridgePracticePanel>();view.details=details;controller.panel=view;
                ConfigurePanel(view,manager.hintText.font);panelObject.SetActive(false);
                var cameraObject=Empty("BridgePracticeCamera",root.transform);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=35;camera.nearClipPlane=.1f;camera.farClipPlane=500;
                cameraObject.transform.position=rig.placeStone3.repairedPose.position+new Vector3(-50,45,-35);cameraObject.transform.LookAt(rig.placeStone3.repairedPose.position);
                var data=cameraObject.AddComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=false;data.renderShadows=true;
                var presentation=cameraObject.AddComponent<BridgePracticeCamera>();presentation.practiceCamera=camera;presentation.gameplayCamera=Exact("Main Camera").GetComponent<Camera>();presentation.rig=rig;presentation.panel=(RectTransform)panelObject.transform;controller.presentation=presentation;
                controller.otherModalPanels=manager.modalPanels.Where(p=>p!=null && p!=panelObject).ToArray();
                Undo.RecordObject(legacy,"Keep Cube visualizer as disabled fallback");legacy.enabled=false;EditorUtility.SetDirty(legacy);
                Undo.RecordObject(manager,"Register the single practice interaction and modal");
                manager.interactionTargets=manager.interactionTargets.Select(t=>t==legacy?(MonoBehaviour)controller:t).ToArray();
                if(!manager.interactionTargets.Contains(controller))manager.interactionTargets=manager.interactionTargets.Concat(new MonoBehaviour[]{controller}).ToArray();
                manager.modalPanels=manager.modalPanels.Concat(new[]{panelObject}).Distinct().ToArray();EditorUtility.SetDirty(manager);
                EditorSceneManager.MarkSceneDirty(root.scene);Undo.CollapseUndoOperations(undo);Selection.activeGameObject=root;
                EditorSceneManager.SaveScene(root.scene,Output+"/LiveAfter.unity",true);
                File.WriteAllText(Output+"/Install.txt","Controller="+ControllerPath+"\nInteraction="+FloatingIslandPrototypeTools.P(controller.interactionPoint)+" P="+point.ToString("F6")+"\nPanel="+FloatingIslandPrototypeTools.P(panelObject.transform)+"\nFallback panel and Cube objects preserved; only legacy interaction component disabled/unregistered.\nCamera="+FloatingIslandPrototypeTools.P(cameraObject.transform));
                Debug.Log("Phase C installed in the live development scene; review before saving. No model, geometry, teleport, shared asset, or Spellbook modifications.");
            }
            catch{Undo.RevertAllDownToGroup(undo);throw;}
        }
        static GameObject Empty(string name,Transform parent)
        {var go=new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Create Phase C object");go.transform.SetParent(parent,false);return go;}
        static void Anchor(RectTransform r,float x0,float y0,float x1,float y1)
        {r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;}
        static void Place(Component c,float x0,float y0,float x1,float y1)=>Anchor((RectTransform)c.transform,x0,y0,x1,y1);
        static TMP_Text Label(string name,Transform parent,TMP_FontAsset font,float y0,float y1,float size)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var label=go.AddComponent<TextMeshProUGUI>();
            label.font=font;label.fontSize=size;label.enableAutoSizing=true;label.fontSizeMin=16;label.fontSizeMax=size;label.color=new Color(.92f,.95f,1);label.raycastTarget=false;
            label.alignment=TextAlignmentOptions.TopLeft;Place(label,.05f,y0,.95f,y1);return label;
        }
        static UnityEngine.UI.Button Button(string name,Transform parent,TMP_FontAsset font,float x0,float y0,float x1,float y1,out TMP_Text label)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);Anchor((RectTransform)go.transform,x0,y0,x1,y1);
            var image=go.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.14f,.23f,.31f);
            var button=go.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
            label=Label("Label",go.transform,font,.05f,.95f,21);label.alignment=TextAlignmentOptions.Center;return button;
        }
        static void ConfigurePanel(BridgePracticePanel view,TMP_FontAsset font)
        {
            var d=view.details;Anchor((RectTransform)view.transform,.62f,0,1,1);
            Place(d.title,.05f,.95f,.95f,.99f);d.title.fontSizeMax=26;
            Place(d.code,.05f,.80f,.95f,.945f);d.code.fontSizeMax=22;
            Place(d.state,.05f,.745f,.95f,.80f);d.state.fontSizeMax=22;
            var heading=view.transform.Find("StackHeading");if(heading!=null)Anchor((RectTransform)heading,.05f,.72f,.95f,.745f);
            Anchor((RectTransform)view.transform.Find("CallStack"),.05f,.56f,.95f,.72f);
            Place(d.emptyStack,.05f,.56f,.95f,.72f);Place(d.explanation,.05f,.48f,.95f,.555f);d.explanation.fontSizeMax=20;
            Place(d.progress,.05f,.045f,.95f,.075f);d.progress.fontSizeMax=17;
            d.previousButton.gameObject.SetActive(false);d.nextButton.gameObject.SetActive(false);d.replayButton.gameObject.SetActive(false);d.checkpointSlider.gameObject.SetActive(false);
            Place(d.closeButton,.66f,.005f,.95f,.045f);
            view.question=Label("PracticeQuestion",view.transform,font,.39f,.475f,23);
            view.feedback=Label("PracticeFeedback",view.transform,font,.075f,.165f,20);
            view.answers=new UnityEngine.UI.Button[3];view.answerLabels=new TMP_Text[3];
            for(int i=0;i<3;i++){float top=.382f-i*.071f;view.answers[i]=Button("Answer_"+i,view.transform,font,.05f,top-.064f,.95f,top,out view.answerLabels[i]);}
            view.submit=Button("CastSpell",view.transform,font,.05f,.005f,.62f,.045f,out var text);text.text="Cast Spell";
        }
    }
}
