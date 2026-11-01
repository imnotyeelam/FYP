using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin02PhaseCTools
    {
        const string ScenePath="Assets/Scenes/MagicWorld_Development.unity";
        const string Backup="LocalBackups/Ruin2PhaseC_20261101";
        static UnityEngine.SceneManagement.Scene Scene()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=ScenePath)
                throw new InvalidOperationException("Open the development scene in Edit Mode.");
            return scene;
        }
        static Transform Root(UnityEngine.SceneManagement.Scene scene,string name)
            =>scene.GetRootGameObjects().Single(g=>g.name==name).transform;
        static Transform Need(Transform parent,string path)
            =>parent.Find(path)??throw new InvalidOperationException("Missing scene path: "+parent.name+"/"+path);
        static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;

        [MenuItem("Tools/FYP/Ruin 2/Phase C/Audit and Backup")]
        public static void Audit()
        {
            var scene=Scene();
            Directory.CreateDirectory(Backup);
            var area=Root(scene,"Ruin02_Area");
            var marker=Need(area,"Interactables/Ruin02ChallengeLandmark/Ruin02ChallengeInteractionPoint");
            var book=Need(area,"Interactables/Ruin02MagicBook");
            var hint=Root(scene,"InteractionHintManager").GetComponent<InteractionHintManager>();
            var ai=Root(scene,"AIFeedbackManager").GetComponent<AIFeedbackManager>();
            var player=Root(scene,"Player").GetComponent<CharacterController>();
            var log=new StringBuilder();
            log.AppendLine("scene="+scene.path+" dirty="+scene.isDirty);
            log.AppendLine("landmark="+PathOf(marker.parent)+" at "+marker.parent.position.ToString("F4"));
            log.AppendLine("interaction="+PathOf(marker)+" at "+marker.position.ToString("F4"));
            log.AppendLine("book="+PathOf(book)+" at "+book.position.ToString("F4"));
            log.AppendLine("player radius="+player.radius+" height="+player.height+" step="+player.stepOffset);
            log.AppendLine("landmark components="+string.Join(",",marker.parent.GetComponents<Component>().Where(c=>c!=null).Select(c=>c.GetType().Name)));
            log.AppendLine("point components="+string.Join(",",marker.GetComponents<Component>().Where(c=>c!=null).Select(c=>c.GetType().Name)));
            log.AppendLine("hint targets="+string.Join(",",hint.interactionTargets.Where(x=>x!=null).Select(x=>PathOf(x.transform))));
            log.AppendLine("modal panels="+string.Join(",",hint.modalPanels.Where(x=>x!=null).Select(x=>PathOf(x.transform))));
            log.AppendLine("backend URL="+ai.backendURL);
            Physics.SyncTransforms();
            foreach(var hit in Physics.RaycastAll(marker.position+Vector3.up*5,Vector3.down,12).OrderBy(x=>x.distance).Take(5))
                log.AppendLine("ground hit="+PathOf(hit.transform)+" y="+hit.point.y.ToString("F3")+" normal="+hit.normal.ToString("F3"));
            foreach(var collider in marker.parent.GetComponentsInChildren<Collider>(true))
                log.AppendLine("landmark collider="+PathOf(collider.transform)+" enabled="+collider.enabled+" bounds="+collider.bounds);
            File.WriteAllText(Backup+"/Audit.txt",log.ToString());
            if(!EditorSceneManager.SaveScene(scene,Backup+"/BeforeLive.unity",true))throw new IOException("Live scene backup failed.");
            Debug.Log("Ruin 2 Phase C audit and live backup saved.\n"+log);
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase C/Test Six-Decision Lesson")]
        public static void TestLesson()
        {
            var session=new Ruin02PracticeSession();
            var trace=session.Lesson.Trace;
            int absorbs=0,completed=0;
            var order=new System.Collections.Generic.List<int>();
            session.AbsorbMagic+=n=>{absorbs++;order.Add(n);};
            session.ChallengeCompleted+=()=>completed++;
            if(trace.Steps.Count!=28 || session.Index!=1 || session.Current.Frames.Count!=1)
                throw new InvalidOperationException("Initial challenge checkpoint failed.");
            if(!Ruin02PracticeLesson.Matches("  SealMagic ( 3 ) ; ","sealMagic(3)") ||
                Ruin02PracticeLesson.Matches("sealMagic(4)","sealMagic(3)") ||
                Ruin02PracticeLesson.Matches("absorbMagic(3);;","absorbMagic(3)") ||
                Ruin02PracticeLesson.Matches("return 0;","return"))
                throw new InvalidOperationException("Input normalization or strict rejection failed.");
            foreach(var q in session.Lesson.Questions)
            {
                int before=session.Index;
                var wrong=session.Submit(q.Id,"absorbMagic(99)");
                if(wrong.Status!=Ruin02AnswerStatus.Incorrect || session.Index!=before || session.Advancing)
                    throw new InvalidOperationException("Wrong answer advanced: "+q.Id);
                var correct=session.Submit(q.Id,"  "+q.Expected+" ; ");
                if(correct.Status!=Ruin02AnswerStatus.Correct || !session.Advancing ||
                    session.Submit(q.Id,q.Expected).Status!=Ruin02AnswerStatus.Busy)
                    throw new InvalidOperationException("Correct or duplicate gate failed: "+q.Id);
                while(session.Advancing)session.TryAdvanceApprovedStep();
                if(session.Index!=q.Through)throw new InvalidOperationException("Range ended at wrong checkpoint: "+q.Id);
                if(q.Id=="ruin02.call.4" && (absorbs!=0 || session.Current.Frames.Count!=5))
                    throw new InvalidOperationException("Call-down skipped frames or performed an absorb.");
            }
            if(!session.Complete || session.Current.Frames.Count!=0 || absorbs!=4 ||
                !order.SequenceEqual(new[]{1,2,3,4}) || completed!=1 ||
                session.Submit("ruin02.absorb.4","absorbMagic(4)").Status!=Ruin02AnswerStatus.Complete)
                throw new InvalidOperationException("Completion, event order, or duplicate prevention failed.");
            var book=new RecursionPlayback(Ruin02ReturnLesson.Create());
            if(book.Index!=0 || book.Current.ActionCount!=0)
                throw new InvalidOperationException("Practice altered guided playback.");
            Debug.Log("PASS Ruin 2 Phase C lesson: six gates, strict text validation, 28 checkpoints, five frames, ordered 1-4 events once, empty-stack completion, independent guided playback.");
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase C/Test Syntax-Aware Feedback")]
        public static void TestSyntaxAwareFeedback()
        {
            string hint;
            if(!Ruin02PracticeLesson.TryGetLocalInputHint("absorb(3)","absorbMagic(3)",out hint) ||
                !hint.Contains("function name",StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Incomplete function-name hint failed.");
            if(!Ruin02PracticeLesson.TryGetLocalInputHint("absorbMagic(4)","absorbMagic(3)",out hint) ||
                !hint.Contains("current n",StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Wrong-n hint failed.");
            if(!Ruin02PracticeLesson.TryGetLocalInputHint("absorbMagic3","absorbMagic(3)",out hint) ||
                !hint.Contains("parentheses",StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Missing-parentheses hint failed.");
            if(Ruin02PracticeLesson.TryGetLocalInputHint("ABSORBMAGIC(3)","absorbMagic(3)",out hint) ||
                !Ruin02PracticeLesson.Matches("ABSORBMAGIC(3)","absorbMagic(3)"))
                throw new InvalidOperationException("Case-only input must remain accepted.");
            Debug.Log("PASS Ruin 2 syntax feedback: function name, current n and parentheses are local; case-only input remains accepted.");
        }

        static RectTransform Rect(string name,Transform parent,float x0,float y0,float x1,float y1)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent,false);Anchor(r,x0,y0,x1,y1);return r;
        }
        static void Anchor(RectTransform r,float x0,float y0,float x1,float y1)
        {r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=r.offsetMax=Vector2.zero;}
        static void Place(Component c,float x0,float y0,float x1,float y1)=>Anchor((RectTransform)c.transform,x0,y0,x1,y1);
        static TMP_Text Label(string name,Transform parent,TMP_FontAsset font,string value,float x0,float y0,float x1,float y1,int size)
        {
            var label=Rect(name,parent,x0,y0,x1,y1).gameObject.AddComponent<TextMeshProUGUI>();
            label.font=font;label.text=value;label.fontSize=size;label.enableAutoSizing=true;
            label.fontSizeMin=16;label.fontSizeMax=size;label.color=new Color(.92f,.95f,1);
            label.raycastTarget=false;return label;
        }
        static Button Button(string name,Transform parent,TMP_FontAsset font,string title,float x0,float y0,float x1,float y1)
        {
            var rect=Rect(name,parent,x0,y0,x1,y1);var image=rect.gameObject.AddComponent<Image>();
            image.color=new Color(.14f,.23f,.31f);
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            Label("Label",rect,font,title,0,0,1,1,20).alignment=TextAlignmentOptions.Center;return button;
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase C/Install Challenge")]
        public static void Install()
        {
            var scene=Scene();
            if(!File.Exists(Backup+"/BeforeLive.unity"))throw new InvalidOperationException("Run Audit and Backup first.");
            TestLesson();
            var area=Root(scene,"Ruin02_Area");var gameplay=Need(area,"Gameplay");
            var interaction=Need(area,"Interactables/Ruin02ChallengeLandmark/Ruin02ChallengeInteractionPoint");
            var book=Need(area,"Gameplay/Ruin02SpellbookManager").GetComponent<Ruin02SpellbookUI>();
            var hint=Root(scene,"InteractionHintManager").GetComponent<InteractionHintManager>();
            var ai=Root(scene,"AIFeedbackManager").GetComponent<AIFeedbackManager>();
            var canvas=Root(scene,"Canvas");
            if(gameplay.Find("Ruin02PracticeManager")!=null || interaction.GetComponent<Ruin02PracticeController>()!=null ||
                canvas.Find("Ruin02PracticePanel")!=null)throw new InvalidOperationException("Phase C is already installed.");
            if(book==null || hint==null || ai==null || !hint.coordinateInteractions)
                throw new InvalidOperationException("Required Phase B/shared references are missing.");
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Ruin 2 Phase C Challenge");
            try
            {
                var root=new GameObject("Ruin02PracticeManager");Undo.RegisterCreatedObjectUndo(root,"Add Ruin 2 practice manager");
                root.transform.SetParent(gameplay,false);
                var controller=Undo.AddComponent<Ruin02PracticeController>(root);
                var client=Undo.AddComponent<Ruin02FeedbackClient>(root);
                controller.player=Root(scene,"Player");controller.interactionPoint=interaction;
                controller.playerMovement=book.playerMovement;controller.cameraFollow=book.cameraFollow;
                controller.feedbackClient=client;client.existingEndpoint=ai;
                // The existing landmark remains the only interaction origin; no E polling is added there.
                var details=RecursionPanelView.Create(canvas,hint.hintText.font);
                Undo.RegisterCreatedObjectUndo(details.gameObject,"Add Ruin 2 challenge UI");
                details.gameObject.name="Ruin02PracticePanel";details.lessonLabel="Ruin II";
                Anchor((RectTransform)details.transform,.53f,0,1,1);
                Place(details.title,.04f,.95f,.96f,.99f);details.title.fontSizeMax=26;
                Place(details.code,.04f,.73f,.96f,.945f);details.code.fontSize=19;details.code.fontSizeMin=18;details.code.fontSizeMax=19;
                Place(details.state,.04f,.68f,.96f,.73f);details.state.fontSizeMax=20;
                Place(details.transform.Find("StackHeading"),.04f,.65f,.96f,.68f);
                var stack=(RectTransform)details.transform.Find("CallStack");Anchor(stack,.04f,.45f,.96f,.65f);
                Place(details.emptyStack,.04f,.45f,.96f,.65f);
                var fifth=UnityEngine.Object.Instantiate(details.frameBackgrounds[0].gameObject,stack);
                fifth.name="Frame_4";details.frameBackgrounds=details.frameBackgrounds.Concat(new[]{fifth.GetComponent<Image>()}).ToArray();
                details.frameLabels=details.frameLabels.Concat(new[]{fifth.transform.Find("Function").GetComponent<TMP_Text>()}).ToArray();
                for(int i=0;i<5;i++)
                {
                    var card=(RectTransform)details.frameBackgrounds[i].transform;float top=1-i*.2f;
                    Anchor(card,0,top-.19f,1,top);
                    details.frameLabels[i].fontSize=16;details.frameLabels[i].fontSizeMin=16;details.frameLabels[i].fontSizeMax=16;
                }
                Place(details.explanation,.04f,.36f,.96f,.44f);details.explanation.fontSizeMax=19;
                Place(details.progress,.04f,.046f,.96f,.075f);details.progress.fontSizeMax=17;
                details.previousButton.gameObject.SetActive(false);details.nextButton.gameObject.SetActive(false);
                details.replayButton.gameObject.SetActive(false);details.checkpointSlider.gameObject.SetActive(false);
                Place(details.closeButton,.66f,.005f,.96f,.045f);
                var panel=Undo.AddComponent<Ruin02PracticePanel>(details.gameObject);panel.details=details;
                panel.question=Label("PracticeQuestion",details.transform,hint.hintText.font,"What recursive call executes next?",.04f,.26f,.96f,.35f,23);
                panel.feedback=Label("PracticeFeedback",details.transform,hint.hintText.font,"Enter the next operation.",.04f,.085f,.96f,.18f,19);
                panel.feedback.richText=false;
                var inputRoot=Rect("AnswerInput",details.transform,.04f,.19f,.96f,.25f);
                var inputImage=inputRoot.gameObject.AddComponent<Image>();inputImage.color=new Color(.12f,.16f,.21f);
                panel.input=inputRoot.gameObject.AddComponent<TMP_InputField>();
                var viewport=Rect("Text Area",inputRoot,.025f,.05f,.975f,.95f);
                var typed=Label("Text",viewport,hint.hintText.font,"",0,0,1,1,23);
                typed.alignment=TextAlignmentOptions.MidlineLeft;typed.raycastTarget=false;
                var placeholder=Label("Placeholder",viewport,hint.hintText.font,"Type a C++ operation, e.g. function(argument)",0,0,1,1,19);
                placeholder.color=new Color(.55f,.63f,.68f);placeholder.alignment=TextAlignmentOptions.MidlineLeft;
                panel.input.textViewport=viewport;panel.input.textComponent=(TextMeshProUGUI)typed;
                panel.input.placeholder=placeholder;panel.input.targetGraphic=inputImage;
                panel.input.lineType=TMP_InputField.LineType.SingleLine;panel.input.characterLimit=100;
                panel.submit=Button("Submit",details.transform,hint.hintText.font,"Submit",.04f,.005f,.62f,.045f);
                controller.panel=panel;
                Undo.RecordObject(hint,"Register Ruin 2 challenge");
                hint.interactionTargets=hint.interactionTargets.Concat(new MonoBehaviour[]{controller}).ToArray();
                hint.modalPanels=hint.modalPanels.Concat(new[]{details.gameObject}).ToArray();
                details.gameObject.SetActive(false);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
                if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Could not save Phase C scene.");
                Debug.Log("Ruin 2 Phase C installed: "+PathOf(root.transform)+"; interaction="+PathOf(interaction)+"; panel="+PathOf(details.transform));
            }
            catch{Undo.RevertAllDownToGroup(group);throw;}
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase C/Verify Scene References")]
        public static void Verify()
        {
            var scene=Scene();var area=Root(scene,"Ruin02_Area");
            var controller=Need(area,"Gameplay/Ruin02PracticeManager").GetComponent<Ruin02PracticeController>();
            var point=Need(area,"Interactables/Ruin02ChallengeLandmark/Ruin02ChallengeInteractionPoint");
            var hint=Root(scene,"InteractionHintManager").GetComponent<InteractionHintManager>();
            var book=Need(area,"Gameplay/Ruin02SpellbookManager").GetComponent<Ruin02SpellbookUI>();
            if(controller==null || controller.interactionPoint!=point || controller.panel==null ||
                controller.feedbackClient==null || controller.feedbackClient.existingEndpoint==null ||
                controller.playerMovement!=book.playerMovement || controller.cameraFollow!=book.cameraFollow ||
                !hint.interactionTargets.Contains(controller) || !hint.modalPanels.Contains(controller.panel.gameObject) ||
                controller.panel.details.frameLabels.Length!=5 || controller.panel.details.frameBackgrounds.Length!=5)
                throw new InvalidOperationException("Phase C scene references failed.");
            Debug.Log("PASS Ruin 2 Phase C scene references: one existing landmark origin, shared hint and modal, five stack rows, independent panel/playback, existing feedback endpoint.");
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase C/Run Play Assertions")]
        public static void RunPlay()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            var controller=UnityEngine.Object.FindObjectsByType<Ruin02PracticeController>(FindObjectsSortMode.None).Single();
            var book=UnityEngine.Object.FindObjectsByType<Ruin02SpellbookUI>(FindObjectsSortMode.None).Single();
            var old=UnityEngine.Object.FindObjectsByType<AncientSpellbookUI>(FindObjectsSortMode.None).Single();
            if(!controller.CanSharedInteract || controller.IsSolved || book.IsBookOpen)
                throw new InvalidOperationException("Challenge did not start eligible and unsolved.");
            bool movement=controller.playerMovement.enabled,camera=controller.cameraFollow.enabled;
            controller.Open();
            if(!controller.IsOpen || controller.playerMovement.enabled || controller.cameraFollow.enabled || CursorManager.allowCursorLock)
                throw new InvalidOperationException("Challenge modal failed.");
            var q=controller.Session.Question;
            int before=controller.Session.Index;
            if(controller.Submit("absorbMagic(4)").Status!=Ruin02AnswerStatus.Incorrect || controller.Session.Index!=before)
                throw new InvalidOperationException("Wrong answer advanced.");
            controller.Close();
            if(controller.IsOpen || controller.playerMovement.enabled!=movement || controller.cameraFollow.enabled!=camera || !CursorManager.allowCursorLock)
                throw new InvalidOperationException("Challenge close did not restore controls.");
            controller.Open();
            if(controller.Session.Question!=q || controller.Session.Index!=before)
                throw new InvalidOperationException("Reopen did not preserve progress.");
            controller.Close();
            book.OpenBook();if(!book.IsBookOpen || controller.CanSharedInteract)throw new InvalidOperationException("Spellbook did not block challenge.");
            book.CloseBook();old.OpenBook();if(!old.IsBookOpen)throw new InvalidOperationException("Ruin 1 book failed.");old.CloseBook();
            Debug.Log("PASS Ruin 2 Phase C Play: initial E eligibility, modal controls, wrong answer, close/reopen preservation, Ruin 1 and Ruin 2 book exclusion.");
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase C/Open Challenge Visual (Play Mode)")]
        public static void OpenVisual()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            var controller=UnityEngine.Object.FindObjectsByType<Ruin02PracticeController>(FindObjectsSortMode.None).Single();
            controller.Open();
            if(!controller.IsOpen)throw new InvalidOperationException("Challenge did not open.");
            Debug.Log("Ruin 2 challenge opened for 1920x1080 visual inspection. Close with Escape or Close button.");
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase C/Position Player by Landmark (Play Only)")]
        public static void PositionPlayerByLandmark()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            var scene=EditorSceneManager.GetActiveScene();
            var area=Root(scene,"Ruin02_Area");
            var point=Need(area,"Interactables/Ruin02ChallengeLandmark/Ruin02ChallengeInteractionPoint");
            var player=Root(scene,"Player").GetComponent<CharacterController>();
            var desired=new Vector3(point.position.x-1.2f,point.position.y+5,point.position.z);
            var hit=Physics.RaycastAll(desired,Vector3.down,12,~0,QueryTriggerInteraction.Ignore)
                .Where(x=>x.transform.IsChildOf(Need(area,"Environment")) && x.transform.name=="Ground")
                .OrderBy(x=>x.distance).FirstOrDefault();
            if(hit.collider==null || Vector3.Angle(hit.normal,Vector3.up)>player.slopeLimit)
                throw new InvalidOperationException("No safe walkable Ruin 2 ground by the landmark.");
            var landing=hit.point+Vector3.up*.05f;
            if(Physics.OverlapCapsule(landing+Vector3.up*.3f,landing+Vector3.up*1.9f,player.radius)
                .Any(c=>c!=player && c.transform!=hit.transform && !c.isTrigger))
                throw new InvalidOperationException("Landmark approach is obstructed; player was not moved.");
            player.enabled=false;player.transform.position=landing;player.enabled=true;
            Debug.Log("Play-only Ruin 2 landmark landing at "+landing.ToString("F3")+". Focus Game View and press E; no scene position is saved.");
        }

        static AIFeedbackManager offlineSavedEndpoint;
        [MenuItem("Tools/FYP/Ruin 2/Phase C/Simulate AI Offline (Play Only)")]
        public static void SimulateOffline()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            var client=UnityEngine.Object.FindObjectsByType<Ruin02FeedbackClient>(FindObjectsSortMode.None).Single();
            if(offlineSavedEndpoint==null)offlineSavedEndpoint=client.existingEndpoint;
            client.existingEndpoint=null;
            Debug.Log("Ruin02 AI link disabled in this Play session only. Portal endpoint remains untouched.");
        }
        [MenuItem("Tools/FYP/Ruin 2/Phase C/Restore AI Link (Play Only)")]
        public static void RestoreOffline()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            var client=UnityEngine.Object.FindObjectsByType<Ruin02FeedbackClient>(FindObjectsSortMode.None).Single();
            if(offlineSavedEndpoint==null)throw new InvalidOperationException("No Play-only AI link to restore.");
            client.existingEndpoint=offlineSavedEndpoint;offlineSavedEndpoint=null;
            Debug.Log("Ruin02 AI link restored in this Play session.");
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase C/Run Full Controller Flow (Play Mode)")]
        public static void RunFullControllerFlow()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            var controller=UnityEngine.Object.FindObjectsByType<Ruin02PracticeController>(FindObjectsSortMode.None).Single();
            var book=UnityEngine.Object.FindObjectsByType<Ruin02SpellbookUI>(FindObjectsSortMode.None).Single();
            if(controller.Session.Index!=1 || controller.Session.Question?.Id!="ruin02.call.4")
                throw new InvalidOperationException("Run Full Controller Flow requires a fresh Play session before answering the challenge.");
            int bookIndex=book.guidedSimulation.CheckpointIndex,actions=0,complete=0;
            var order=new System.Collections.Generic.List<int>();
            controller.AbsorbMagic+=n=>{actions++;order.Add(n);};
            controller.ChallengeCompleted+=()=>complete++;
            controller.Open();
            for(int i=0;i<6;i++)
            {
                var q=controller.Session.Question;
                int before=controller.Session.Index;
                var wrong=controller.Submit("absorbMagic(99)");
                if(wrong.Status!=Ruin02AnswerStatus.Incorrect || controller.Session.Index!=before || actions>i)
                    throw new InvalidOperationException("Controller accepted wrong answer at "+q.Id);
                var correct=controller.Submit(q.Expected);
                if(correct.Status!=Ruin02AnswerStatus.Correct ||
                    controller.Submit(q.Expected).Status!=Ruin02AnswerStatus.Busy)
                    throw new InvalidOperationException("Controller answer or duplicate gate failed at "+q.Id);
                while(controller.Session.Advancing)controller.Session.TryAdvanceApprovedStep();
            }
            if(!controller.IsSolved || actions!=4 || complete!=1 ||
                !order.SequenceEqual(new[]{1,2,3,4}) ||
                controller.Session.Current.Frames.Count!=0 ||
                book.guidedSimulation.CheckpointIndex!=bookIndex)
                throw new InvalidOperationException("Full controller flow failed completion or independence checks.");
            controller.Close();
            Debug.Log("PASS Ruin 2 Phase C full controller flow: six accepted answers, wrong answers held, four ordered Phase D events once, empty-stack completion, Spellbook unchanged.");
        }
    }
}
