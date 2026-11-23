using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    public static class FarmVisualPolishTools
    {
        public const string Folder="LocalBackups/FarmVisualPolish_20261119";
        public static Ruin02HarvestConsole Console=>UnityEngine.Object.FindFirstObjectByType<Ruin02HarvestConsole>();
        [MenuItem("Tools/FYP/Farm Visual Polish/Audit and Back Up %#F11")]
        public static void Audit()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/Scenes/MagicWorld_Development.unity")
                throw new InvalidOperationException("Open development scene in Edit Mode.");
            Directory.CreateDirectory(Folder);
            if(!EditorSceneManager.SaveScene(scene,$"{Folder}/Before_{DateTime.Now:HHmmssfff}.unity",true))throw new IOException("Backup failed.");
            var c=Console;var text=c.simulation.debugDisplay;var log=new StringBuilder();
            foreach(var rect in c.panel.GetComponentsInChildren<RectTransform>(true))
            {
                log.AppendLine($"UI {SceneMaintenanceC1Tools.Path(rect)} anchors={rect.anchorMin}/{rect.anchorMax} rect={rect.rect} offsets={rect.offsetMin}/{rect.offsetMax}");
                var label=rect.GetComponent<TMP_Text>();if(label!=null)log.AppendLine($"  FONT {label.font.name} size={label.fontSize} color={label.color}");
                var image=rect.GetComponent<UnityEngine.UI.Image>();if(image!=null)log.AppendLine($"  IMAGE color={image.color} sprite={(image.sprite!=null?image.sprite.name:"none")}");
            }
            log.AppendLine($"LEFT STATUS {SceneMaintenanceC1Tools.Path(text.transform)} position={text.transform.position:F3} rotation={text.transform.rotation.eulerAngles} scale={text.transform.lossyScale} characterSize={text.characterSize} fontSize={text.fontSize} anchor={text.anchor} alignment={text.alignment} text={text.text}");
            log.AppendLine($"Status mesh bounds={text.GetComponent<MeshRenderer>().bounds}; camera marker {c.cameraView.position:F3} rotation {c.cameraView.rotation.eulerAngles}");
            File.WriteAllText(Folder+"/UIAudit.txt",log.ToString());Debug.Log("Farm visual UI inspected; live scene backed up.");
        }
        [MenuItem("Tools/FYP/Farm Visual Polish/Apply Visual Backgrounds %#&9")]
        public static void Install()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/Scenes/MagicWorld_Development.unity")
                throw new InvalidOperationException("Open development scene in Edit Mode.");
            if(!Directory.Exists(Folder)||Directory.GetFiles(Folder,"Before_*.unity").Length==0)
                throw new InvalidOperationException("Back up the live scene first.");
            var c=Console;var root=(RectTransform)c.panel.transform;var status=c.simulation.debugDisplay;
            if(root.Find("SkyBackdrop")!=null||status.transform.Find("StatusBackdrop")!=null)
                throw new InvalidOperationException("Visual backgrounds already exist; inspect rather than duplicate.");
            var camera=c.cameraFollow.GetComponent<Camera>();var cameraPosition=c.cameraView.localToWorldMatrix;
            Undo.RecordObject(root,"Improve console margins");root.anchorMin=new Vector2(.67f,.055f);root.anchorMax=new Vector2(.985f,.945f);
            var oldImage=root.GetComponent<UnityEngine.UI.Image>();Undo.RecordObject(oldImage,"Soften farm panel background");oldImage.color=Color.clear;
            // Children share the existing panel's visibility; no runtime activation or gameplay code is added.
            var wash=Background("SkyBackdrop",root,ScreenAnchors(root,Vector2.zero),ScreenAnchors(root,Vector2.one),new Color(.47f,.74f,.87f,.14f));
            wash.transform.SetSiblingIndex(0);
            var surround=Background("SkySurround",root,ScreenAnchors(root,new Vector2(.65f,0)),ScreenAnchors(root,Vector2.one),new Color(.40f,.65f,.78f,.72f));
            surround.transform.SetSiblingIndex(1);
            var surface=Background("PanelSurface",root,Vector2.zero,Vector2.one,new Color(.055f,.135f,.20f,.94f));surface.transform.SetSiblingIndex(2);
            var edge=Background("PanelAccent",root,Vector2.zero,new Vector2(0,1),new Color(.61f,.84f,.92f,.75f));
            ((RectTransform)edge.transform).sizeDelta=new Vector2(2,0);edge.transform.SetSiblingIndex(3);
            SetText(root.Find("Title").GetComponent<TMP_Text>(),25,TextAlignmentOptions.MidlineLeft);
            SetText(c.code,20,TextAlignmentOptions.TopLeft);
            SetText(c.state,20,TextAlignmentOptions.TopLeft);
            SetText(c.feedback,18,TextAlignmentOptions.TopLeft);
            foreach(var label in c.submit.GetComponentsInChildren<TMP_Text>(true).Concat(c.close.GetComponentsInChildren<TMP_Text>(true)))
                SetText(label,21,TextAlignmentOptions.Center);
            Undo.RecordObject(c.submit.targetGraphic,"Tint harvest action");c.submit.targetGraphic.color=new Color(.22f,.42f,.54f,1);
            Undo.RecordObject(c.close.targetGraphic,"Tint close action");c.close.targetGraphic.color=new Color(.14f,.29f,.39f,1);

            var world=new GameObject("StatusBackdrop",typeof(RectTransform),typeof(Canvas));
            Undo.RegisterCreatedObjectUndo(world,"Add status contrast backing");world.layer=5;
            var rect=(RectTransform)world.transform;rect.SetParent(status.transform,false);rect.localPosition=new Vector3(0,0,.04f);
            rect.sizeDelta=new Vector2(22f,3.4f);
            var canvas=world.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            canvas.overrideSorting=true;canvas.sortingOrder=-1;
            Background("StatusSurface",rect,Vector2.zero,Vector2.one,new Color(.055f,.16f,.23f,.70f));
            if(cameraPosition!=c.cameraView.localToWorldMatrix)throw new InvalidOperationException("Camera marker changed.");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("Farm visual polish saved: soft sky wash/surround, right console surface and restrained text sizing, world-space status backing. Camera and gameplay references unchanged.");
        }
        static Vector2 ScreenAnchors(RectTransform root,Vector2 point)
        {var size=root.anchorMax-root.anchorMin;return new Vector2((point.x-root.anchorMin.x)/size.x,(point.y-root.anchorMin.y)/size.y);}
        static UnityEngine.UI.Image Background(string name,Transform parent,Vector2 min,Vector2 max,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));
            Undo.RegisterCreatedObjectUndo(go,"Add farm UI background");go.layer=5;
            var rect=(RectTransform)go.transform;rect.SetParent(parent,false);rect.anchorMin=min;rect.anchorMax=max;
            rect.offsetMin=rect.offsetMax=Vector2.zero;
            var image=go.GetComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return image;
        }
        static void SetText(TMP_Text text,float maximum,TextAlignmentOptions alignment)
        {
            Undo.RecordObject(text,"Improve farm text readability");text.enableAutoSizing=true;text.fontSize=maximum;
            text.fontSizeMax=maximum;text.fontSizeMin=maximum-3;text.alignment=alignment;
            text.color=new Color(.94f,.98f,1,1);
        }
        [MenuItem("Tools/FYP/Farm Visual Polish/Refine Readability %#&8")]
        public static void Refine()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode only.");
            var c=Console;var root=c.panel.transform;
            SetText(root.Find("Title").GetComponent<TMP_Text>(),32,TextAlignmentOptions.MidlineLeft);
            SetText(c.code,26,TextAlignmentOptions.TopLeft);SetText(c.state,22,TextAlignmentOptions.TopLeft);SetText(c.feedback,22,TextAlignmentOptions.TopLeft);
            foreach(var label in c.submit.GetComponentsInChildren<TMP_Text>(true).Concat(c.close.GetComponentsInChildren<TMP_Text>(true)))
                SetText(label,23,TextAlignmentOptions.Center);
            var wash=root.Find("SkyBackdrop").GetComponent<UnityEngine.UI.Image>();Undo.RecordObject(wash,"Keep field colours clear");
            wash.color=new Color(.47f,.74f,.87f,.08f);
            var status=c.simulation.debugDisplay;Undo.RecordObject(status,"Improve status readability");status.characterSize=.065f;
            var card=(RectTransform)status.transform.Find("StatusBackdrop");Undo.RecordObject(card,"Tighten status backdrop padding");card.sizeDelta=new Vector2(16.5f,2.4f);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("Farm UI visual spacing and font caps refined for 1920x1080; gameplay untouched.");
        }
    }
}
