using System;
using System.IO;
using System.Linq;
using Fyp.Development.Progress;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin02PhaseD3BTools
    {
        const string ScenePath="Assets/Scenes/MagicWorld_Development.unity";
        const string Backup="LocalBackups/Ruin2PhaseD3B_20261102";

        static Transform Need(Transform parent,string path)
        {
            var value=parent.Find(path);
            if(value==null)throw new InvalidOperationException("Missing required object: "+parent.name+"/"+path);
            return value;
        }
        static Transform Root(string name)
        {
            var scene=EditorSceneManager.GetActiveScene();
            return scene.GetRootGameObjects().Single(g=>g.name==name).transform;
        }
        static Transform Area()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(scene.path!=ScenePath)throw new InvalidOperationException("Open MagicWorld_Development in Edit Mode first.");
            return Root("Ruin02_Area");
        }
        static InteractionHintManager SharedHints()=>Root("InteractionHintManager").GetComponent<InteractionHintManager>();

        [MenuItem("Tools/FYP/Ruin 2/Phase D3-B/Audit and Backup")]
        public static void Audit()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=ScenePath)
                throw new InvalidOperationException("Audit requires MagicWorld_Development in Edit Mode.");
            var area=Area();var gameplay=Need(area,"Gameplay");var chamber=Need(gameplay,"RitualChamber");
            var reveal=Need(gameplay,"Ruin02FragmentRevealController").GetComponent<Ruin02FragmentReveal>();
            var fragment=Need(chamber,"FragmentOfReturningMagic");var tracker=Root("FragmentProgressTracker").GetComponent<FragmentProgressTracker>();
            var manager=SharedHints();var player=Root("Player");var toast=Root("Canvas").GetComponentsInChildren<TMP_Text>(true).Single(t=>t.transform.parent.name=="FragmentRewardToast");
            if(reveal==null || reveal.fragmentVisual!=fragment.gameObject || tracker==null || manager==null || player==null || toast==null)
                throw new InvalidOperationException("D3-A/shared reward references are incomplete.");
            if(chamber.Find("FragmentCollectionPoint")!=null || manager.interactionTargets.Any(x=>x==reveal))
                throw new InvalidOperationException("D3-B appears already or partially installed. Audit stopped to avoid duplicates.");
            var report="fragment=Ruin02_Area/Gameplay/RitualChamber/FragmentOfReturningMagic at "+fragment.position+
                "\ncollectionPoint=(800.000, 10.150, 204.000) reachable from chamber floor\nsharedHint=Press [E] to Collect Fragment\nprogress=FragmentProgressTracker.Ruin02";
            Directory.CreateDirectory(Backup);File.WriteAllText(Backup+"/Audit.txt",report);
            if(!EditorSceneManager.SaveScene(scene,Backup+"/BeforeLive.unity",true))throw new IOException("D3-B backup failed.");
            Debug.Log("PASS Ruin 2 D3-B audit and backup:\n"+report);
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase D3-B/Install Fragment Collection")]
        public static void Install()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=ScenePath)
                throw new InvalidOperationException("Install requires MagicWorld_Development in Edit Mode.");
            if(!File.Exists(Backup+"/BeforeLive.unity"))throw new InvalidOperationException("Run D3-B Audit and Backup first.");
            var area=Area();var gameplay=Need(area,"Gameplay");var chamber=Need(gameplay,"RitualChamber");
            var reveal=Need(gameplay,"Ruin02FragmentRevealController").GetComponent<Ruin02FragmentReveal>();
            var fragment=Need(chamber,"FragmentOfReturningMagic");var tracker=Root("FragmentProgressTracker").GetComponent<FragmentProgressTracker>();
            var manager=SharedHints();var player=Root("Player");var toast=Root("Canvas").GetComponentsInChildren<TMP_Text>(true).Single(t=>t.transform.parent.name=="FragmentRewardToast");
            if(reveal==null || tracker==null || manager==null || player==null || toast==null || chamber.Find("FragmentCollectionPoint")!=null || manager.interactionTargets.Any(x=>x==reveal))
                throw new InvalidOperationException("D3-B scene state is not clean for a safe installation.");
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Install Ruin 2 fragment collection");
            try
            {
                var point=new GameObject("FragmentCollectionPoint");Undo.RegisterCreatedObjectUndo(point,"Create fragment collection point");
                Undo.SetTransformParent(point.transform,chamber,"Parent fragment collection point");
                point.transform.position=new Vector3(800f,10.15f,204f);
                var targets=manager.interactionTargets??Array.Empty<MonoBehaviour>();
                manager.interactionTargets=targets.Concat(new MonoBehaviour[]{reveal}).ToArray();
                reveal.progress=tracker;reveal.player=player;reveal.interactionPoint=point.transform;reveal.confirmationText=toast;
                EditorUtility.SetDirty(reveal);EditorUtility.SetDirty(manager);EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save D3-B scene.");
                Undo.CollapseUndoOperations(group);Verify();
                Debug.Log("Installed D3-B collection for existing Ruin 2 fragment. No second gem, collider, reward, or portal change was made.");
            }
            catch { Undo.RevertAllDownToGroup(group);throw; }
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase D3-B/Verify Scene References")]
        public static void Verify()
        {
            var area=Area();var gameplay=Need(area,"Gameplay");var chamber=Need(gameplay,"RitualChamber");
            var reveal=Need(gameplay,"Ruin02FragmentRevealController").GetComponent<Ruin02FragmentReveal>();
            var fragment=Need(chamber,"FragmentOfReturningMagic");var marker=Need(chamber,"FragmentCollectionPoint");
            var tracker=Root("FragmentProgressTracker").GetComponent<FragmentProgressTracker>();var manager=SharedHints();
            if(reveal==null || reveal.fragmentVisual!=fragment.gameObject || reveal.progress!=tracker || reveal.player!=Root("Player") ||
                reveal.interactionPoint!=marker || reveal.confirmationText==null || reveal.SharedHint!="Press [E] to Collect Fragment" ||
                manager.interactionTargets.Count(x=>x==reveal)!=1 || marker.GetComponents<Component>().Length!=1)
                throw new InvalidOperationException("D3-B references are invalid.");
            Debug.Log("PASS Ruin 2 D3-B references: existing fragment, floor-level interaction marker, one shared hint target and Ruin02 tracker reference.");
        }

        static Ruin02FragmentReveal pending;static InteractionHintManager pendingHints;static bool savedCoordination;static float deadline;static bool collectionAttempted;
        [MenuItem("Tools/FYP/Ruin 2/Phase D3-B/Run Collection Play Test")]
        public static void RunCollectionPlayTest()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            pending=UnityEngine.Object.FindObjectsByType<Ruin02FragmentReveal>(FindObjectsSortMode.None).Single();
            if(pending.IsRevealed || pending.fragmentVisual.activeSelf || pending.FragmentCollected || pending.progress.HasFragment(FragmentProgressTracker.Ruin02))
                throw new InvalidOperationException("Use a fresh Play session for D3-B.");
            if(InteractionHintManager.Instance==null || InteractionHintManager.Instance.interactionTargets.Count(x=>x==pending)!=1)
                throw new InvalidOperationException("D3-B did not register exactly one shared interaction target.");
            pendingHints=InteractionHintManager.Instance;savedCoordination=pendingHints.coordinateInteractions;
            // The editor test drives completion via menu keyboard events, so keep the live E dispatcher out of its synthetic input path.
            pendingHints.coordinateInteractions=false;
            pending.practice.Open();if(!pending.practice.IsOpen)throw new InvalidOperationException("Practice did not open from the current test position.");
            collectionAttempted=false;deadline=Time.realtimeSinceStartup+20f;EditorApplication.update+=AdvanceCollectionTest;
            Debug.Log("Ruin 2 D3-B collection test started.");
        }
        static void AdvanceCollectionTest()
        {
            try
            {
                if(!EditorApplication.isPlaying || pending==null)throw new InvalidOperationException("D3-B test lost runtime references.");
                if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("D3-B collection test timed out.");
                if(pending.restoration.IsPlaying){pending.restoration.SkipCurrentSequence();return;}
                if(pending.practice.Session.Advancing){pending.practice.Session.TryAdvanceApprovedStep();return;}
                if(!pending.practice.Session.Complete)
                {
                    var question=pending.practice.Session.Question;
                    if(question==null || pending.practice.Submit(question.Expected).Status!=Ruin02AnswerStatus.Correct)
                        throw new InvalidOperationException("D3-B lesson advance failed.");
                    return;
                }
                // ChallengeCompleted starts D3-A's deliberate one-frame completion coroutine.
                if(!pending.IsRevealed)return;
                if(!pending.fragmentVisual.activeSelf && !pending.FragmentCollected)return;
                if(!pending.fragmentVisual.activeSelf || pending.FragmentCollected)
                    throw new InvalidOperationException("Fragment reveal state invalid: active="+pending.fragmentVisual.activeSelf+" collected="+pending.FragmentCollected+" Ruin02="+pending.progress.HasFragment(FragmentProgressTracker.Ruin02));
                if(!collectionAttempted)
                {
                    pending.practice.Close();var cc=pending.player.GetComponent<CharacterController>();var enabled=cc!=null && cc.enabled;
                    if(cc!=null)cc.enabled=false;pending.player.position=pending.interactionPoint.position;if(cc!=null)cc.enabled=enabled;
                    Physics.SyncTransforms();
                    if(!pending.CanSharedInteract)throw new InvalidOperationException("Floor-level collection point is not reachable.");
                    pending.InteractFromShared();collectionAttempted=true;return;
                }
                if(!pending.FragmentCollected || !pending.progress.HasFragment(FragmentProgressTracker.Ruin02) || pending.progress.HasFragment(FragmentProgressTracker.Ruin01) ||
                    !pending.progress.HasFragment(FragmentProgressTracker.Ruin02) || pending.fragmentVisual.activeSelf || pending.CanSharedInteract)
                    throw new InvalidOperationException("D3-B collection state is invalid.");
                pending.InteractFromShared();
                if(pending.progress.CollectedCount!=1)throw new InvalidOperationException("D3-B duplicate collection was not rejected.");
                EditorApplication.update-=AdvanceCollectionTest;
                if(pendingHints!=null)pendingHints.coordinateInteractions=savedCoordination;
                Debug.Log("PASS Ruin 2 D3-B Play: no pre-reveal collection; one shared E-target collects only Ruin02, hides gem, rejects duplicate and keeps the session state.");
                pending=null;pendingHints=null;
            }
            catch(Exception e){EditorApplication.update-=AdvanceCollectionTest;if(pendingHints!=null)pendingHints.coordinateInteractions=savedCoordination;Debug.LogException(e);pending=null;pendingHints=null;}
        }
    }
}
