using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin02GuidedSeparationTools
    {
        const string ScenePath="Assets/Scenes/MagicWorld_Development.unity";
        [MenuItem("Tools/FYP/Ruin 2/Guided Separation/Install Standalone Simulation")]
        public static void Install()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=ScenePath || scene.isDirty)throw new InvalidOperationException("Open the saved development scene in Edit Mode first.");
            var all=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
            var book=all.Select(x=>x.GetComponent<Ruin02SpellbookUI>()).Single(x=>x!=null);
            var practice=all.Select(x=>x.GetComponent<Ruin02PracticeController>()).Single(x=>x!=null);
            var hint=all.Select(x=>x.GetComponent<InteractionHintManager>()).Single(x=>x!=null);
            var canvas=book.spellbookPanel.transform.parent;
            var page=book.guidedSimulation!=null?book.guidedSimulation.transform:all.Single(x=>x.name=="ReturnFlowSimulationPage");
            var simulation=page.GetComponent<Ruin02SpellbookSimulation>();
            if(simulation==null)throw new InvalidOperationException("Existing 28-checkpoint simulation is missing.");
            var area=all.Single(x=>x.name=="Ruin02_Area");var gameplay=area.Find("Gameplay");
            var manager=gameplay.Find("Ruin02GuidedSimulationManager")?.GetComponent<Ruin02GuidedSimulationUI>();
            if(manager==null){var go=new GameObject("Ruin02GuidedSimulationManager");Undo.RegisterCreatedObjectUndo(go,"Create standalone guided manager");go.transform.SetParent(gameplay,false);manager=Undo.AddComponent<Ruin02GuidedSimulationUI>(go);}
            Undo.SetTransformParent(page,canvas,"Separate guided simulation from magic book");page.name="Ruin02GuidedSimulationPanel";
            var panel=(RectTransform)page;panel.anchorMin=new Vector2(.06f,.06f);panel.anchorMax=new Vector2(.94f,.94f);panel.offsetMin=panel.offsetMax=Vector2.zero;
            var background=page.GetComponent<Image>()??Undo.AddComponent<Image>(page.gameObject);background.color=new Color(.025f,.045f,.07f,.98f);
            var details=simulation.details;var detailRect=(RectTransform)details.transform;detailRect.anchorMin=new Vector2(.44f,.10f);detailRect.anchorMax=new Vector2(.98f,.94f);detailRect.offsetMin=detailRect.offsetMax=Vector2.zero;details.title.gameObject.SetActive(true);
            var diagram=simulation.diagram!=null?(RectTransform)simulation.diagram.transform:null;if(diagram!=null){diagram.anchorMin=new Vector2(.02f,.10f);diagram.anchorMax=new Vector2(.42f,.94f);diagram.offsetMin=diagram.offsetMax=Vector2.zero;}
            var finish=page.Find("FinishButton")?.GetComponent<Button>()??CreateButton(page,"FinishButton","Finish / Complete",new Vector2(.74f,.02f),new Vector2(.98f,.075f),book.titleText.font);
            manager.panel=page.gameObject;manager.simulation=simulation;manager.finishButton=finish;manager.playerMovement=book.playerMovement;manager.cameraFollow=book.cameraFollow;practice.guidedSimulationUi=manager;
            hint.modalPanels=hint.modalPanels.Concat(new[]{page.gameObject}).Distinct().ToArray();
            LayoutBook(book);page.gameObject.SetActive(false);EditorUtility.SetDirty(manager);EditorUtility.SetDirty(practice);EditorUtility.SetDirty(hint);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Scene save failed.");
            Debug.Log("Ruin 2 standalone guided simulation installed. Magic Book is now pages 1-3 only.");
        }
        static void LayoutBook(Ruin02SpellbookUI book)
        { Set((RectTransform)book.titleText.transform,.08f,.78f,.92f,.95f);Set((RectTransform)book.knowledgeText.transform,.08f,.53f,.92f,.74f);Set((RectTransform)book.codeText.transform,.08f,.20f,.92f,.50f);Set((RectTransform)book.pageNumberText.transform,.38f,.10f,.62f,.16f); }
        static void Set(RectTransform r,float x0,float y0,float x1,float y1){r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=r.offsetMax=Vector2.zero;}
        static Button CreateButton(Transform parent,string name,string text,Vector2 min,Vector2 max,TMP_FontAsset font)
        {var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));Undo.RegisterCreatedObjectUndo(go,"Create Finish button");var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;var image=go.GetComponent<Image>();image.color=new Color(.17f,.42f,.36f);var button=go.GetComponent<Button>();button.targetGraphic=image;var label=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();label.transform.SetParent(r,false);var lr=(RectTransform)label.transform;lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=lr.offsetMax=Vector2.zero;label.font=font;label.text=text;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return button;}
    }
}
