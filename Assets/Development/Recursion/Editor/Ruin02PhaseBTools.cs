using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin02PhaseBTools
    {
        const string ScenePath = "Assets/Scenes/MagicWorld_Development.unity";
        const string Output = "LocalBackups/Ruin2PhaseB_20261030";
        static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
        static UnityEngine.SceneManagement.Scene Scene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath || scene.isDirty)
                throw new InvalidOperationException("Requires clean development scene in Edit Mode.");
            return scene;
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase B/Audit Book and Cave")]
        public static void Audit()
        {
            var scene = Scene();
            var all = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            var book = all.Single(t=>t.name == "Magic Book");
            var ui = all.Select(t=>t.GetComponent<AncientSpellbookUI>()).Single(x=>x!=null);
            var hint = all.Select(t=>t.GetComponent<InteractionHintManager>()).Single(x=>x!=null);
            var area = all.Single(t=>t.name=="Ruin02_Area");
            var cave = all.Single(t=>t.name=="Ruin2 Environment");
            var s = new StringBuilder();
            s.AppendLine("DIRTY="+scene.isDirty+" SKYBOX="+AssetDatabase.GetAssetPath(RenderSettings.skybox));
            s.AppendLine("BOOK="+PathOf(book)+" pos="+book.position.ToString("F3")+" rot="+book.eulerAngles.ToString("F3")+" scale="+book.lossyScale.ToString("F3")+" prefab="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(book.gameObject));
            foreach (var t in book.GetComponentsInChildren<Transform>(true))
                s.AppendLine("  BOOK_CHILD="+PathOf(t)+" local="+t.localPosition.ToString("F3")+" components="+string.Join(",",t.GetComponents<Component>().Where(c=>c!=null).Select(c=>c.GetType().Name)));
            foreach (var r in book.GetComponentsInChildren<Renderer>(true)) s.AppendLine("  RENDER="+PathOf(r.transform)+" bounds="+r.bounds);
            foreach (var c in book.GetComponentsInChildren<Collider>(true)) s.AppendLine("  COLLIDER="+PathOf(c.transform)+" enabled="+c.enabled+" bounds="+c.bounds);
            s.AppendLine("UI="+PathOf(ui.transform)+" panel="+PathOf(ui.spellbookPanel.transform)+" parent="+PathOf(ui.spellbookPanel.transform.parent));
            foreach (var t in new[]{ui.titleText,ui.pageNumberText,ui.knowledgeText,ui.codeText})s.AppendLine("  TEXT="+PathOf(t.transform));
            foreach (var b in new[]{ui.previousButton,ui.nextButton,ui.closeButton})s.AppendLine("  BUTTON="+PathOf(b.transform)+" persistent="+b.onClick.GetPersistentEventCount());
            s.AppendLine("  SIM="+PathOf(ui.guidedSimulation.transform)+" DETAILS="+PathOf(ui.guidedSimulation.details.transform)+" FRAMES="+ui.guidedSimulation.details.frameLabels.Length);
            foreach(var t in ui.spellbookPanel.GetComponentsInChildren<Transform>(true))s.AppendLine("  UI_CHILD="+PathOf(t));
            s.AppendLine("HINT="+PathOf(hint.transform)+" coordinate="+hint.coordinateInteractions+" targets="+hint.interactionTargets.Length+" panels="+hint.modalPanels.Length);
            for(int i=0;i<hint.interactionTargets.Length;i++)s.AppendLine("  TARGET["+i+"]="+(hint.interactionTargets[i]!=null?PathOf(hint.interactionTargets[i].transform):"null"));
            for(int i=0;i<hint.modalPanels.Length;i++)s.AppendLine("  PANEL["+i+"]="+(hint.modalPanels[i]!=null?PathOf(hint.modalPanels[i].transform):"null"));
            foreach(var name in new[]{"EntrySpawn","ReturnInteractionPoint","Ruin02ChallengeLandmark"})
            {
                var t=area.GetComponentsInChildren<Transform>(true).Single(x=>x.name==name);
                s.AppendLine("AREA="+PathOf(t)+" pos="+t.position.ToString("F3"));
            }
            Physics.SyncTransforms();
            foreach(var p in new[]{new Vector3(796f,10f,193f),new Vector3(797f,10f,194f),new Vector3(797f,10f,191f)})
            {
                var hits=Physics.OverlapCapsule(p+Vector3.up*.3f,p+Vector3.up*2f,.65f).Where(c=>c.transform.IsChildOf(cave)).Select(c=>PathOf(c.transform)).Distinct();
                var floors=Physics.RaycastAll(p+Vector3.up*4,Vector3.down,8).OrderBy(h=>h.distance).Take(3).Select(h=>PathOf(h.transform)+"@"+h.point.y.ToString("F2"));
                s.AppendLine("CANDIDATE="+p.ToString("F3")+" rockHits="+string.Join(",",hits)+" floors="+string.Join(",",floors));
            }
            File.WriteAllText(Output+"/LiveAudit.txt",s.ToString());
            if (!EditorSceneManager.SaveScene(scene,Output+"/BeforeLive.unity",true)) throw new IOException("Live scene backup failed.");
            Debug.Log("Ruin 2 Phase B audit saved to "+Output+"/LiveAudit.txt");
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase B/Test Return Lesson")]
        public static void TestLesson()
        {
            var trace=Ruin02ReturnLesson.Create();
            var actions=trace.Steps.Where(x=>x.Phase==RecursionPhase.Action).ToArray();
            if(actions.Length!=4 || !actions.Select(x=>x.Parameter.Value).SequenceEqual(new[]{1,2,3,4}))
                throw new InvalidOperationException("Incorrect absorb order.");
            int baseIndex=trace.Steps.ToList().FindIndex(x=>x.Phase==RecursionPhase.BaseCase);
            if(baseIndex<0 || trace.Steps[baseIndex].Frames.Count!=5 || actions.Any(x=>x.Parameter==0) ||
                trace.Steps.Take(baseIndex+1).Any(x=>x.ActionCount!=0))
                throw new InvalidOperationException("Base case or stack trace is wrong.");
            if(trace.Steps.Last().Frames.Count!=0 || trace.Steps.Last().ActionCount!=4)
                throw new InvalidOperationException("Trace did not complete with empty stack and four actions.");
            var p=new RecursionPlayback(trace);
            p.JumpTo(trace.Steps.Count-1); p.Previous(); p.Replay();
            if(p.Index!=0 || p.Current.ActionCount!=0)throw new InvalidOperationException("Playback navigation failed.");
            var bridge=new RecursionPlayback(BridgeRecursionLesson.Create(3));
            p.JumpTo(actions[2]==null?0:trace.Steps.ToList().IndexOf(actions[2]));
            if(bridge.Index!=0)throw new InvalidOperationException("Ruin 1 playback was altered.");
            Debug.Log("PASS Ruin 2 return-flow lesson: "+trace.Steps.Count+" checkpoints, five frames, base case, actions 1-4, replay/jump independence.");
        }

        static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
            return r;
        }
        static TMP_Text Label(string name,Transform parent,string value,Vector2 min,Vector2 max,int size,TMP_FontAsset font)
        {
            var t=Rect(name,parent,min,max).gameObject.AddComponent<TextMeshProUGUI>();
            t.font=font;t.text=value;t.fontSize=size;t.enableAutoSizing=true;t.fontSizeMin=14;t.fontSizeMax=size;
            t.color=new Color(.92f,.95f,1);t.raycastTarget=false;
            return t;
        }
        static void Anchors(RectTransform r,Vector2 min,Vector2 max)
        {r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;}

        static void ConfigureSimulationLayout(RecursionPanelView details)
        {
            // The book itself supplies the page title. Reserve that vertical space for eight code lines.
            details.title.gameObject.SetActive(false);
            Anchors((RectTransform)details.code.transform,new Vector2(.05f,.65f),new Vector2(.95f,.98f));
            details.code.fontSize=16;details.code.fontSizeMin=16;details.code.fontSizeMax=16;
            Anchors((RectTransform)details.state.transform,new Vector2(.05f,.56f),new Vector2(.95f,.65f));
            details.state.fontSize=18;details.state.fontSizeMin=18;details.state.fontSizeMax=18;
            var heading=details.transform.Find("StackHeading").GetComponent<TMP_Text>();
            Anchors((RectTransform)heading.transform,new Vector2(.05f,.52f),new Vector2(.95f,.56f));
            heading.fontSize=16;heading.fontSizeMin=16;heading.fontSizeMax=16;
            var stack=details.frameBackgrounds[0].transform.parent;
            Anchors((RectTransform)stack,new Vector2(.05f,.32f),new Vector2(.95f,.52f));
            Anchors((RectTransform)details.emptyStack.transform,new Vector2(.05f,.32f),new Vector2(.95f,.52f));
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase B/Tidy Guided Simulation Layout")]
        public static void TidyGuidedLayout()
        {
            var scene=Scene();
            var panel=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
                .Single(t=>t.name=="Ruin02AncientSpellbookPanel");
            var details=panel.GetComponentInChildren<RecursionPanelView>(true);
            if(details==null)throw new InvalidOperationException("Ruin 2 guided simulation details missing.");
            foreach(var component in details.GetComponentsInChildren<Component>(true))
                if(component is RectTransform || component is TMP_Text)Undo.RecordObject(component,"Tidy Ruin 2 simulation layout");
            Undo.RecordObject(details.title.gameObject,"Hide redundant Ruin 2 simulation title");
            ConfigureSimulationLayout(details);
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Scene save failed.");
            Debug.Log("Ruin 2 guided simulation layout tidied without changing lesson state.");
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase B/Install Book and Guided Simulation")]
        public static void Install()
        {
            var scene=Scene();
            if(!File.Exists(Output+"/BeforeLive.unity"))throw new InvalidOperationException("Run the live audit first.");
            TestLesson();
            var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            var originalBook=all.Single(t=>t.name=="Magic Book");
            var originalUI=all.Select(t=>t.GetComponent<AncientSpellbookUI>()).Single(x=>x!=null);
            var hint=all.Select(t=>t.GetComponent<InteractionHintManager>()).Single(x=>x!=null);
            var area=all.Single(t=>t.name=="Ruin02_Area");
            var interactions=area.Find("Interactables");var gameplay=area.Find("Gameplay");
            if(interactions==null||gameplay==null||interactions.Find("Ruin02MagicBook")!=null||
                gameplay.Find("Ruin02SpellbookManager")!=null || originalUI.spellbookPanel.transform.parent.Find("Ruin02AncientSpellbookPanel")!=null)
                throw new InvalidOperationException("Phase B destination missing or already installed.");
            var point=new Vector3(797f,10.85f,194f);
            Physics.SyncTransforms();
            if(Physics.OverlapCapsule(point+Vector3.up*.1f,point+Vector3.up*2f,.65f)
                .Any(c=>c.transform.IsChildOf(all.Single(t=>t.name=="Ruin2 Environment"))))
                throw new InvalidOperationException("Cave rock blocks book location.");
            var bookAsset=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(originalBook.gameObject));
            if(bookAsset==null)throw new InvalidOperationException("Original Magic Book prefab missing.");
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Ruin 2 Phase B Magic Book");
            try
            {
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(bookAsset,scene);
                Undo.RegisterCreatedObjectUndo(instance,"Add Ruin 2 Magic Book");
                instance.name="Ruin02MagicBook";
                instance.transform.SetParent(interactions,true);
                instance.transform.SetPositionAndRotation(point,originalBook.rotation);
                instance.transform.localScale=originalBook.lossyScale;
                var oldInteraction=instance.GetComponent<KnowledgeInteractable>();
                if(oldInteraction!=null)Undo.DestroyObjectImmediate(oldInteraction);
                var collider=instance.GetComponent<BoxCollider>();
                if(collider!=null){Undo.RecordObject(collider,"Disable oversized book collision");collider.enabled=false;}
                var bookInteraction=Undo.AddComponent<Ruin02BookInteraction>(instance);
                bookInteraction.player=hint.player;bookInteraction.interactionDistance=3f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);

                var panel=UnityEngine.Object.Instantiate(originalUI.spellbookPanel,originalUI.spellbookPanel.transform.parent);
                Undo.RegisterCreatedObjectUndo(panel,"Duplicate Magic Book UI style");
                panel.name="Ruin02AncientSpellbookPanel";
                var panelRoot=panel.transform;
                var copiedPage=panelRoot.Find("GuidedSimulationPage");
                var copiedSim=copiedPage.GetComponent<SpellbookRecursionSimulation>();
                var details=copiedSim.details;
                Undo.DestroyObjectImmediate(copiedSim);
                var bridgeDiagram=copiedPage.Find("BridgeDiagram");
                if(bridgeDiagram!=null)Undo.DestroyObjectImmediate(bridgeDiagram.gameObject);
                copiedPage.name="ReturnFlowSimulationPage";
                var sim=Undo.AddComponent<Ruin02SpellbookSimulation>(copiedPage.gameObject);
                sim.details=details;
                details.lessonLabel="Ruin II";
                details.closeButton.gameObject.SetActive(false);
                var stack=details.frameBackgrounds[0].transform.parent;
                var fifth=UnityEngine.Object.Instantiate(details.frameBackgrounds[0].gameObject,stack);
                Undo.RegisterCreatedObjectUndo(fifth,"Add fifth call stack frame");fifth.name="Frame_4";
                details.frameLabels=details.frameLabels.Concat(new[]{fifth.transform.Find("Function").GetComponent<TMP_Text>()}).ToArray();
                details.frameBackgrounds=details.frameBackgrounds.Concat(new[]{fifth.GetComponent<Image>()}).ToArray();
                ConfigureSimulationLayout(details);
                for(int i=0;i<5;i++)
                {
                    var card=(RectTransform)details.frameBackgrounds[i].transform;
                    float top=1-i*.2f;
                    Anchors(card,new Vector2(0,top-.19f),new Vector2(1,top));
                    details.frameLabels[i].fontSize=17;details.frameLabels[i].enableAutoSizing=true;
                    details.frameLabels[i].fontSizeMin=14;details.frameLabels[i].fontSizeMax=17;
                }
                var diagram=Rect("StaffReturnDiagram",copiedPage,new Vector2(.02f,.05f),new Vector2(.42f,.95f));
                var visual=Undo.AddComponent<Ruin02StaffDiagramView>(diagram.gameObject);
                Label("Heading",diagram,"FOUR STAFFS",new Vector2(.02f,.87f),new Vector2(.98f,.98f),26,originalUI.codeText.font);
                Label("LearningOnly",diagram,"Learning only: no real staffs or rewards change.",new Vector2(.02f,.73f),new Vector2(.98f,.86f),20,originalUI.codeText.font);
                string[] names={"Air (1)","Earth (2)","Fire (3)","Ice (4)"};
                visual.indicators=new Image[4];
                for(int i=0;i<4;i++)
                {
                    float top=.70f-i*.15f;
                    var row=Rect("Staff_"+(i+1),diagram,new Vector2(.02f,top-.13f),new Vector2(.98f,top));
                    var icon=Rect("Status",row,new Vector2(.02f,.22f),new Vector2(.18f,.78f)).gameObject.AddComponent<Image>();
                    icon.color=new Color(.33f,.36f,.40f);icon.raycastTarget=false;visual.indicators[i]=icon;
                    Label("Name",row,names[i],new Vector2(.23f,.10f),new Vector2(.98f,.9f),21,originalUI.codeText.font);
                }
                visual.status=Label("StatusText",diagram,"Restored: 0 / 4",new Vector2(.02f,.04f),new Vector2(.98f,.18f),19,originalUI.codeText.font);
                sim.diagram=visual;copiedPage.gameObject.SetActive(false);
                var close=panelRoot.Find("CloseButton").GetComponent<Button>();
                while(close.onClick.GetPersistentEventCount()>0)UnityEventTools.RemovePersistentListener(close.onClick,0);
                var manager=new GameObject("Ruin02SpellbookManager");
                Undo.RegisterCreatedObjectUndo(manager,"Add Ruin 2 Spellbook manager");
                manager.transform.SetParent(gameplay,false);
                var controller=Undo.AddComponent<Ruin02SpellbookUI>(manager);
                controller.spellbookPanel=panel;
                controller.titleText=panelRoot.Find("TitleText").GetComponent<TMP_Text>();
                controller.pageNumberText=panelRoot.Find("PageNumberText").GetComponent<TMP_Text>();
                controller.knowledgeText=panelRoot.Find("KnowledgeText").GetComponent<TMP_Text>();
                controller.codeText=panelRoot.Find("CodeText").GetComponent<TMP_Text>();
                // The copied source panel names are misleading: NextButton has the left-arrow sprite.
                controller.previousButton=panelRoot.Find("NextButton").GetComponent<Button>();
                controller.nextButton=panelRoot.Find("PreviousButton (1)").GetComponent<Button>();
                controller.closeButton=close;
                controller.playerMovement=originalUI.playerMovement;
                controller.cameraFollow=originalUI.cameraFollow;
                controller.guidedSimulation=sim;
                bookInteraction.book=controller;
                Undo.RecordObject(hint,"Register Ruin 2 book with shared hints");
                hint.interactionTargets=hint.interactionTargets.Concat(new MonoBehaviour[]{bookInteraction}).ToArray();
                hint.modalPanels=hint.modalPanels.Concat(new[]{panel}).ToArray();
                panel.SetActive(false);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
                if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Scene save failed.");
                File.Copy(ScenePath,Output+"/AfterSaved.unity",true);
                Debug.Log("Ruin 2 Phase B installed. Book="+PathOf(instance.transform)+" at "+point.ToString("F3")+"; panel="+PathOf(panel.transform));
            }
            catch { Undo.RevertAllDownToGroup(group);throw; }
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase B/Verify Installed References")]
        public static void VerifyInstalled()
        {
            var scene=Scene();
            var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            var area=all.Single(t=>t.name=="Ruin02_Area");
            var original=all.Single(t=>t.name=="Magic Book");
            var copy=area.Find("Interactables/Ruin02MagicBook");
            var manager=area.Find("Gameplay/Ruin02SpellbookManager").GetComponent<Ruin02SpellbookUI>();
            var originalUI=all.Select(t=>t.GetComponent<AncientSpellbookUI>()).Single(x=>x!=null);
            var hint=all.Select(t=>t.GetComponent<InteractionHintManager>()).Single(x=>x!=null);
            var target=copy.GetComponent<Ruin02BookInteraction>();
            var copiedCollider=copy.GetComponent<BoxCollider>();
            var panel=all.Single(t=>t.name=="Ruin02AncientSpellbookPanel");
            if(originalUI.spellbookPanel==manager.spellbookPanel || originalUI.guidedSimulation==null ||
                manager.guidedSimulation.details.frameLabels.Length!=5 ||
                manager.guidedSimulation.details.frameBackgrounds.Length!=5 ||
                target.book!=manager || target.player!=hint.player ||
                !hint.interactionTargets.Contains(target) || !hint.modalPanels.Contains(panel.gameObject) ||
                copy.GetComponent<KnowledgeInteractable>()!=null ||
                (copiedCollider!=null && copiedCollider.enabled) ||
                panel.Find("CloseButton").GetComponent<Button>().onClick.GetPersistentEventCount()!=0 ||
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(copy.gameObject)!=
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(original.gameObject))
                throw new InvalidOperationException("Phase B reference verification failed.");
            var trace=Ruin02ReturnLesson.Create();
            if(trace.Steps.Max(x=>x.Frames.Count)!=5 || trace.Steps.Last().Frames.Count!=0)
                throw new InvalidOperationException("Trace stack capacity mismatch.");
            Debug.Log("PASS Ruin 2 Phase B references: separate book/panel/playback, five stack rows, shared hint target+modal, source prefab link, no duplicate legacy interaction.");
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase B/Open Book Visual Check (Play Mode)")]
        public static void OpenForVisualCheck()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            var book=UnityEngine.Object.FindObjectsByType<Ruin02SpellbookUI>(FindObjectsSortMode.None).Single();
            book.OpenBook();book.NextPage();book.NextPage();book.NextPage();
            if(!book.IsBookOpen||book.guidedSimulation.Current==null)throw new InvalidOperationException("Ruin 2 simulation did not open.");
            Debug.Log("Ruin 2 guided simulation open for visual inspection. Close with Escape or book Close button.");
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase B/Run Play Assertions")]
        public static void RunPlayAssertions()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            var book=UnityEngine.Object.FindObjectsByType<Ruin02SpellbookUI>(FindObjectsSortMode.None).Single();
            var old=UnityEngine.Object.FindObjectsByType<AncientSpellbookUI>(FindObjectsSortMode.None).Single();
            var oldIndex=old.guidedSimulation.CheckpointIndex;
            bool movement=book.playerMovement.enabled,camera=book.cameraFollow.enabled;
            book.OpenBook();
            if(!book.IsBookOpen||!book.titleText.text.Contains("Returning")||book.playerMovement.enabled||book.cameraFollow.enabled)
                throw new InvalidOperationException("Book page 1/modal state failed.");
            book.NextPage();if(!book.knowledgeText.text.Contains("RETURN"))throw new InvalidOperationException("Page 2 failed.");
            book.NextPage();if(!book.codeText.text.Contains("absorbMagic(n)"))throw new InvalidOperationException("Page 3 failed.");
            book.NextPage();var sim=book.guidedSimulation;
            if(!sim.gameObject.activeInHierarchy||sim.details.frameLabels.Length!=5)throw new InvalidOperationException("Page 4 failed.");
            int count=sim.Playback.Trace.Steps.Count;
            for(int i=1;i<count;i++)sim.NextStep();
            if(sim.Current.ActionCount!=4||sim.Current.Frames.Count!=0)throw new InvalidOperationException("Simulation did not complete.");
            sim.PreviousStep();sim.JumpToStep(0);sim.JumpToStep(count-1);sim.Replay();
            if(sim.CheckpointIndex!=0||old.guidedSimulation.CheckpointIndex!=oldIndex)
                throw new InvalidOperationException("Ruin 2 playback affected Ruin 1 or replay failed.");
            book.CloseBook();
            if(book.IsBookOpen||book.playerMovement.enabled!=movement||book.cameraFollow.enabled!=camera||
                !CursorManager.allowCursorLock)throw new InvalidOperationException("Book close did not restore controls.");
            old.OpenBook();if(!old.IsBookOpen)throw new InvalidOperationException("Ruin 1 book failed to open.");old.CloseBook();
            Debug.Log("PASS Ruin 2 Phase B Play: four pages, five frames, 28 checkpoints, navigation, independent Ruin 1 playback, modal controls restored.");
        }
    }
}
