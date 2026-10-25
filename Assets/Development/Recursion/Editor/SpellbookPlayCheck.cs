using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fyp.Development.Recursion.Editor
{
    [InitializeOnLoad]
    public static class SpellbookPlayCheck
    {
        private const string Request="FYP.SpellbookPlayCheck";
        private static AncientSpellbookUI book;
        private static SpellbookRecursionSimulation simulation;
        private static RuinRecursionVisualizer world;
        private static Dictionary<Transform,(Vector3,Quaternion,Vector3)> poses;
        private static Dictionary<Collider,bool> colliders;
        private static double started,last;
        private static int index,checks;
        private static bool captured,initialized;
        static SpellbookPlayCheck(){EditorApplication.update+=Tick;}

        [MenuItem("Tools/FYP/Ruin 1/Stage 1-2/Run Spellbook Play Check")]
        public static void Run()
        {
            SpellbookStageTools.RequireScene();
            if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save or review unsaved edits before this explicit Play check.");
            SessionState.SetBool(Request,true);initialized=false;book=null;checks=0;index=0;
            started=0;EditorApplication.isPlaying=true;
        }
        private static void Check(bool value,string message)
        {checks++;if(!value)throw new InvalidOperationException("Spellbook Play check: "+message);}
        private static void Verify(int i)
        {
            var expected=BridgeRecursionLesson.Create(3).Steps[i];
            Check(simulation.CheckpointIndex==i,"checkpoint "+i);
            Check(simulation.Current.Phase==expected.Phase && simulation.Current.Parameter==expected.Parameter,"phase and parameter");
            Check(simulation.details.progress.text.StartsWith("Checkpoint "+(i+1)+" / 22"),"UI progress");
            Check(simulation.details.state.text.Contains("n = "+(expected.Parameter.HasValue?expected.Parameter.ToString():"--")),"UI n");
            Check(simulation.details.code.text.Contains("<b>>")== (expected.ActiveLine>=0),"code highlight");
            simulation.details.explanation.ForceMeshUpdate();
            Check(!simulation.details.explanation.isTextOverflowing,"explanation fits without truncation");
            Check(simulation.details.emptyStack.gameObject.activeSelf==(expected.Frames.Count==0),"empty stack UI");
            Check(simulation.details.frameBackgrounds.Count(f=>f.gameObject.activeSelf)==expected.Frames.Count,"stack frame count");
            var p=new BridgeSnapshotProjection(expected,3);
            for(int stone=0;stone<3;stone++)
            {
                Check(Mathf.Abs(simulation.diagram.stones[stone].rectTransform.anchoredPosition.y)<.001f == p.IsPlaced(stone),"diagram stone pose");
                Color color=p.ReturningStone==stone?simulation.diagram.returnColor:p.Completed?simulation.diagram.completeColor:
                    p.IsPlaced(stone)?simulation.diagram.placedColor:simulation.diagram.brokenColor;
                Check(simulation.diagram.stones[stone].color==color,"diagram stone tint");
            }
            foreach(var pair in poses)
                Check(pair.Key.position==pair.Value.Item1 && pair.Key.rotation==pair.Value.Item2 && pair.Key.localScale==pair.Value.Item3,"real bridge pose unchanged");
            foreach(var pair in colliders)Check(pair.Key.enabled==pair.Value,"real bridge collider unchanged");
            Check(!world.observationCamera.enabled && !world.panelView.gameObject.activeSelf,"world presentation remains closed");
            Check(!book.playerMovement.enabled && !book.cameraFollow.enabled,"movement/camera paused by book");
            Check(Cursor.visible && Cursor.lockState==CursorLockMode.None && !CursorManager.allowCursorLock,"book owns cursor");
            Check(!InteractionHintManager.Instance.hintText.gameObject.activeInHierarchy,"shared hint hidden");
        }
        private static void Tick()
        {
            if(!SessionState.GetBool(Request,false) || !EditorApplication.isPlaying)return;
            try
            {
                if(!initialized)
                {
                    if(started==0){started=EditorApplication.timeSinceStartup;return;}
                    if(EditorApplication.timeSinceStartup-started<1.5)return;
                    book=Object.FindObjectsByType<AncientSpellbookUI>(FindObjectsSortMode.None).Single();
                    simulation=book.guidedSimulation;Check(simulation!=null,"fourth page assigned");
                    world=Object.FindObjectsByType<RuinRecursionVisualizer>(FindObjectsSortMode.None).Single();
                    poses=world.observationBridge.GetComponentsInChildren<Transform>(true)
                        .ToDictionary(t=>t,t=>(t.position,t.rotation,t.localScale));
                    colliders=world.observationBridge.GetComponentsInChildren<Collider>(true).ToDictionary(c=>c,c=>c.enabled);
                    book.OpenBook();
                    string[] titles={"What Is Recursion?","Recursive Calls","Changing Parameters"};
                    for(int page=0;page<3;page++)
                    {
                        Check(book.titleText.text==titles[page] && book.pageNumberText.text==(page+1)+" / 4","original knowledge page "+page);
                        Check(book.knowledgeText.gameObject.activeSelf && book.codeText.gameObject.activeSelf,"original text visible");
                        book.nextButton.onClick.Invoke();
                    }
                    Check(book.pageNumberText.text=="4 / 4" && simulation.gameObject.activeInHierarchy,"fourth page opens");
                    Check(!book.knowledgeText.gameObject.activeSelf && !book.codeText.gameObject.activeSelf,"legacy body hidden only on page four");
                    initialized=true;index=0;captured=false;last=EditorApplication.timeSinceStartup;
                    return;
                }
                if(EditorApplication.timeSinceStartup-last<.12)return;
                Verify(index);
                if(!captured && (index==0 || index==13 || index==21))
                {
                    Directory.CreateDirectory("Temp/SpellbookChecks");
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/SpellbookChecks/Learning_"+(index+1)+".png"));
                    captured=true;last=EditorApplication.timeSinceStartup;return;
                }
                if(index<21){simulation.details.nextButton.onClick.Invoke();index++;captured=false;last=EditorApplication.timeSinceStartup;return;}
                for(int i=20;i>=0;i--){simulation.details.previousButton.onClick.Invoke();Verify(i);}
                foreach(int i in new[]{21,2,13,0,17}){simulation.details.checkpointSlider.value=i;Verify(i);}
                simulation.details.replayButton.onClick.Invoke();Verify(0);
                book.previousButton.onClick.Invoke();Check(book.titleText.text=="Changing Parameters","back to knowledge");
                book.nextButton.onClick.Invoke();Verify(0);
                book.closeButton.onClick.Invoke();
                Check(!book.IsBookOpen && book.playerMovement.enabled && book.cameraFollow.enabled,"normal close restores controls");
                Check(CursorManager.allowCursorLock && Cursor.lockState==CursorLockMode.Locked,"normal close restores cursor");
                // Preserve a pre-disabled control in the development opt-in path.
                book.playerMovement.enabled=false;book.OpenBook();book.CloseBook();
                Check(!book.playerMovement.enabled,"pre-disabled movement remains disabled");book.playerMovement.enabled=true;
                Debug.Log("PASS: Spellbook Play check: "+checks+" assertions; all 22 checkpoints, Previous/Replay/jumps, original pages, modal and real 3D bridge isolation. Screenshots Temp/SpellbookChecks.");
                SessionState.SetBool(Request,false);EditorApplication.isPlaying=false;
            }
            catch(Exception e)
            {
                Debug.LogException(e);if(book!=null)book.CloseBook();SessionState.SetBool(Request,false);EditorApplication.isPlaying=false;
            }
        }
    }
}
