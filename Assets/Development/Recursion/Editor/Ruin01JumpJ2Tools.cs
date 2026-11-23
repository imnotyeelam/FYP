using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin01JumpJ2Tools
    {
        [MenuItem("Tools/FYP/Ruin 1/Jump J2/Install Separate Tutorial")]
        public static void Install()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!="Assets/Scenes/MagicWorld_Development.unity")
                throw new InvalidOperationException("Open development scene in Edit Mode.");
            var jump=Ruin01JumpJ1Tools.Jump;
            var canvas=FloatingIslandPrototypeTools.Exact("Canvas");
            var hints=UnityEngine.Object.FindFirstObjectByType<InteractionHintManager>();
            if(jump.jumpGuide!=null || canvas.Find("JumpGuidedSimulationPanel")!=null ||
                UnityEngine.Object.FindFirstObjectByType<Ruin01JumpGuidedSimulation>()!=null)
                throw new InvalidOperationException("Jump tutorial already exists.");
            if(canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>()==null || UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length!=1)
                throw new InvalidOperationException("Existing Canvas/EventSystem missing.");
            var font=hints.hintText.font;
            var codeFont=AssetDatabase.FindAssets("t:TMP_FontAsset").Select(AssetDatabase.GUIDToAssetPath)
                .Where(p=>p.IndexOf("Type",StringComparison.OrdinalIgnoreCase)>=0)
                .Select(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>).First();
            Directory.CreateDirectory("LocalBackups/Ruin01JumpJ2_20261119");
            if(!EditorSceneManager.SaveScene(scene,$"LocalBackups/Ruin01JumpJ2_20261119/Before_{DateTime.Now:HHmmssfff}.unity",true))
                throw new IOException("Backup failed.");
            var owner=new GameObject("JumpGuidedSimulation");owner.transform.SetParent(jump.transform.parent,false);
            Undo.RegisterCreatedObjectUndo(owner,"Add standalone jump lesson");
            var guide=Undo.AddComponent<Ruin01JumpGuidedSimulation>(owner);guide.jump=jump;
            var root=Rect("JumpGuidedSimulationPanel",canvas,.10f,.08f,.90f,.93f);
            Undo.RegisterCreatedObjectUndo(root.gameObject,"Add jump tutorial panel");
            var bg=root.gameObject.AddComponent<UnityEngine.UI.Image>();
            var source=jump.panel.GetComponent<UnityEngine.UI.Image>();
            if(source!=null){bg.sprite=source.sprite;bg.type=source.type;}
            bg.color=new Color(.045f,.035f,.09f,.98f);
            guide.panel=root.gameObject;
            guide.heading=Label(root,"Heading","Magic Jump Guided Simulation",.04f,.85f,.96f,.97f,font,28);
            guide.code=Label(root,"Code","",.05f,.45f,.62f,.81f,codeFont,25);
            Label(root,"StackHeading","CALL STACK  ·  top grows downward",.65f,.76f,.96f,.83f,font,22);
            guide.stackFrames=new TMP_Text[4];
            for(int i=0;i<4;i++)
            {
                var card=Rect($"StackFrame{i}",root,.66f,.66f-i*.075f,.96f,.725f-i*.075f);
                card.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.17f,.1f,.28f,.9f);
                guide.stackFrames[i]=Label(card,"Call","",.03f,.04f,.97f,.96f,codeFont,21);
            }
            guide.state=Label(root,"ExecutionState","",.05f,.37f,.96f,.44f,font,24);
            guide.instruction=Label(root,"Instruction","",.05f,.22f,.96f,.36f,font,24);
            var preview=Rect("IllustrativePreview",root,.07f,.09f,.94f,.21f);
            Label(preview,"Label","ILLUSTRATION ONLY  ·  magicJump(3)",.0f,.76f,.98f,1.1f,font,17);
            guide.previewLandings=new UnityEngine.UI.Image[3];
            for(int i=0;i<3;i++)
            {
                float x=.36f+i*.26f;
                var platform=Rect($"ExampleLanding{i+1}",preview,x-.045f,.1f,x+.045f,.23f);
                guide.previewLandings[i]=platform.gameObject.AddComponent<UnityEngine.UI.Image>();
                guide.previewLandings[i].raycastTarget=false;
                Label(preview,$"LeapLabel{i+1}",$"Leap {i+1}",x-.08f,-.16f,x+.08f,.08f,font,16).alignment=TextAlignmentOptions.Center;
            }
            guide.previewOrb=Rect("SpellOrb",preview,.1f,.37f,.1f,.37f);guide.previewOrb.sizeDelta=new Vector2(16,16);
            guide.previewOrb.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.65f,.55f,1,1);
            guide.previous=Button(root,jump.closeButton,"Previous","Previous",.05f,.015f,.25f,.075f,font);
            guide.next=Button(root,jump.closeButton,"Next","Next",.28f,.015f,.48f,.075f,font);
            guide.finish=Button(root,jump.closeButton,"Finish","Finish",.51f,.015f,.72f,.075f,font);
            guide.close=Button(root,jump.closeButton,"Close","Close [ESC]",.75f,.015f,.95f,.075f,font);
            root.gameObject.SetActive(false);
            Undo.RecordObject(jump,"Connect separate jump lesson");jump.jumpGuide=guide;
            Undo.RecordObject(hints,"Register jump tutorial modal");hints.modalPanels=hints.modalPanels.Append(root.gameObject).ToArray();
            EditorUtility.SetDirty(jump);EditorUtility.SetDirty(hints);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("J2 saved: standalone eight-checkpoint magicJump(3) tutorial; existing rock target and 2.5m range unchanged.");
        }
        static RectTransform Rect(string name,Transform parent,float x0,float y0,float x1,float y1)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=new Vector2(x0,y0);rect.anchorMax=new Vector2(x1,y1);rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
        }
        static TMP_Text Label(Transform parent,string name,string text,float x0,float y0,float x1,float y1,TMP_FontAsset font,int size)
        {
            var label=Rect(name,parent,x0,y0,x1,y1).gameObject.AddComponent<TextMeshProUGUI>();
            label.font=font;label.text=text;label.fontSize=size;label.enableAutoSizing=true;label.fontSizeMin=size-4;
            label.color=new Color(.95f,.91f,1);label.raycastTarget=false;return label;
        }
        static UnityEngine.UI.Button Button(Transform parent,UnityEngine.UI.Button source,string name,string text,float x0,float y0,float x1,float y1,TMP_FontAsset font)
        {
            var button=UnityEngine.Object.Instantiate(source,parent);button.name=name;
            button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();
            var rect=(RectTransform)button.transform;rect.anchorMin=new Vector2(x0,y0);rect.anchorMax=new Vector2(x1,y1);rect.offsetMin=rect.offsetMax=Vector2.zero;
            foreach(var label in button.GetComponentsInChildren<TMP_Text>(true)){label.font=font;label.text=text;label.fontSize=23;label.enableAutoSizing=true;label.fontSizeMin=17;}
            button.gameObject.SetActive(true);return button;
        }
    }
}
