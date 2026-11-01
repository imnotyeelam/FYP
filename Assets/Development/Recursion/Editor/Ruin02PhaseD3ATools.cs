using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin02PhaseD3ATools
    {
        const string ScenePath="Assets/Scenes/MagicWorld_Development.unity";
        const string Backup="LocalBackups/Ruin2PhaseD3A_20261102";
        const string GemPath="Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Diamondo.prefab";

        static Transform Need(Transform parent,string path)
        {
            var value=parent.Find(path);
            if(value==null)throw new InvalidOperationException("Missing required object: "+parent.name+"/"+path);
            return value;
        }
        static Transform Area()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(scene.path!=ScenePath)throw new InvalidOperationException("Open MagicWorld_Development in Edit Mode first.");
            return scene.GetRootGameObjects().Single(g=>g.name=="Ruin02_Area").transform;
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase D3-A/Audit and Backup")]
        public static void Audit()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=ScenePath)
                throw new InvalidOperationException("Audit requires MagicWorld_Development in Edit Mode.");
            var area=Area();var gameplay=Need(area,"Gameplay");var chamber=Need(gameplay,"RitualChamber");
            var restoration=Need(gameplay,"AirStaffRestoration").GetComponent<Ruin02AirRestoration>();
            var practice=restoration==null?null:restoration.practice;
            if(practice==null || restoration==null || restoration.restorations.Length!=4)
                throw new InvalidOperationException("Existing Phase C/D2 references are incomplete.");
            if(gameplay.Find("Ruin02FragmentRevealController")!=null || chamber.Find("FragmentOfReturningMagic")!=null)
                throw new InvalidOperationException("A D3-A fragment object already exists. Audit stopped to avoid duplicates.");
            var gem=AssetDatabase.LoadAssetAtPath<GameObject>(GemPath);
            if(gem==null)throw new InvalidOperationException("Diamondo prefab is missing.");
            var orbs=Need(chamber,"ChaoticMagic").GetComponentsInChildren<Transform>(true)
                .Where(t=>t.name.StartsWith("Glowing orb ")).Select(t=>t.position).ToArray();
            if(orbs.Length!=4)throw new InvalidOperationException("Expected four D2 chaotic orbs.");
            var centre=orbs.Aggregate(Vector3.zero,(sum,pos)=>sum+pos)/orbs.Length;
            var report=$"fragmentAnchor=({centre.x:F3}, {centre.y:F3}, {centre.z:F3})\n"+
                "placement=(800.000, 12.450, 204.000)\nparent=Ruin02_Area/Gameplay/RitualChamber\n"+
                "prefab="+GemPath+"\ncolliders=disabled on scene instance (D3-A has no collection yet)";
            Directory.CreateDirectory(Backup);File.WriteAllText(Backup+"/Audit.txt",report);
            if(!EditorSceneManager.SaveScene(scene,Backup+"/BeforeLive.unity",true))throw new IOException("D3-A backup failed.");
            Debug.Log("PASS Ruin 2 D3-A audit and backup:\n"+report);
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase D3-A/Install Fragment Reveal")]
        public static void Install()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=ScenePath)
                throw new InvalidOperationException("Install requires MagicWorld_Development in Edit Mode.");
            if(!File.Exists(Backup+"/BeforeLive.unity"))throw new InvalidOperationException("Run D3-A Audit and Backup first.");
            var area=Area();var gameplay=Need(area,"Gameplay");var chamber=Need(gameplay,"RitualChamber");
            if(gameplay.Find("Ruin02FragmentRevealController")!=null || chamber.Find("FragmentOfReturningMagic")!=null)
                throw new InvalidOperationException("D3-A is already or partially installed. No scene changes were made.");
            var restoration=Need(gameplay,"AirStaffRestoration").GetComponent<Ruin02AirRestoration>();
            var practice=restoration==null?null:restoration.practice;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(GemPath);
            if(practice==null || restoration==null || prefab==null)throw new InvalidOperationException("D3-A references are incomplete.");

            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Install Ruin 2 fragment reveal");
            try
            {
                var fragment=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                Undo.RegisterCreatedObjectUndo(fragment,"Create Fragment of Returning Magic");
                fragment.name="FragmentOfReturningMagic";
                Undo.SetTransformParent(fragment.transform,chamber,"Place Fragment of Returning Magic");
                fragment.transform.SetPositionAndRotation(new Vector3(800f,12.45f,204f),Quaternion.identity);
                fragment.transform.localScale=Vector3.one;
                foreach(var collider in fragment.GetComponentsInChildren<Collider>(true)) collider.enabled=false;

                var controllerObject=new GameObject("Ruin02FragmentRevealController");
                Undo.RegisterCreatedObjectUndo(controllerObject,"Create Ruin 2 fragment reveal controller");
                Undo.SetTransformParent(controllerObject.transform,gameplay,"Parent Ruin 2 fragment reveal controller");
                var reveal=controllerObject.AddComponent<Ruin02FragmentReveal>();
                reveal.practice=practice;reveal.restoration=restoration;reveal.fragmentVisual=fragment;
                EditorUtility.SetDirty(reveal);EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save D3-A scene.");
                Undo.CollapseUndoOperations(group);Verify();
                Debug.Log("Installed D3-A reveal-only FragmentOfReturningMagic. No collection, ProgressTracker, prefab, material, or gameplay lesson was modified.");
            }
            catch { Undo.RevertAllDownToGroup(group);throw; }
        }

        [MenuItem("Tools/FYP/Ruin 2/Phase D3-A/Verify Scene References")]
        public static void Verify()
        {
            var area=Area();var gameplay=Need(area,"Gameplay");var chamber=Need(gameplay,"RitualChamber");
            var reveal=Need(gameplay,"Ruin02FragmentRevealController").GetComponent<Ruin02FragmentReveal>();
            var fragment=Need(chamber,"FragmentOfReturningMagic");
            var restoration=Need(gameplay,"AirStaffRestoration").GetComponent<Ruin02AirRestoration>();
            if(reveal==null || restoration==null || reveal.practice!=restoration.practice ||
                reveal.restoration!=restoration || reveal.fragmentVisual!=fragment.gameObject ||
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(fragment.gameObject)!=GemPath ||
                fragment.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled))
                throw new InvalidOperationException("D3-A scene references are invalid.");
            Debug.Log("PASS Ruin 2 D3-A references: one Diamondo scene instance at the central ritual altar; colliders disabled until a later collection phase.");
        }

        static Ruin02FragmentReveal pendingReveal;
        static Ruin02PracticeController pendingPractice;
        static Ruin02AirRestoration pendingRestoration;
        static float deadline;

        [MenuItem("Tools/FYP/Ruin 2/Phase D3-A/Run Reveal Play Test")]
        public static void RunRevealPlayTest()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first.");
            pendingReveal=UnityEngine.Object.FindObjectsByType<Ruin02FragmentReveal>(FindObjectsSortMode.None).Single();
            pendingPractice=pendingReveal.practice;pendingRestoration=pendingReveal.restoration;
            if(pendingReveal.fragmentVisual.activeSelf || pendingReveal.IsRevealed || pendingPractice.IsSolved || pendingRestoration.RestoredCount!=0)
                throw new InvalidOperationException("Use a fresh Play session; fragment must begin hidden.");
            pendingPractice.Open();
            if(!pendingPractice.IsOpen)throw new InvalidOperationException("Practice did not open from the current test position.");
            deadline=Time.realtimeSinceStartup+20f;EditorApplication.update+=AdvanceRevealTest;
            Debug.Log("Ruin 2 D3-A reveal test started.");
        }

        static void AdvanceRevealTest()
        {
            try
            {
                if(!EditorApplication.isPlaying || pendingReveal==null)throw new InvalidOperationException("D3-A test lost runtime references.");
                if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("D3-A reveal test timed out.");
                if(pendingRestoration.IsPlaying){pendingRestoration.SkipCurrentSequence();return;}
                if(pendingPractice.Session.Advancing){pendingPractice.Session.TryAdvanceApprovedStep();return;}
                if(pendingPractice.Session.Complete)
                {
                    if(!pendingReveal.IsRevealed || !pendingReveal.fragmentVisual.activeSelf || pendingRestoration.RestoredCount!=4 ||
                        pendingPractice.Session.Current.Frames.Count!=0)throw new InvalidOperationException("D3-A fragment reveal conditions failed.");
                    pendingPractice.Close();pendingPractice.Open();
                    if(!pendingReveal.IsRevealed || !pendingReveal.fragmentVisual.activeSelf)throw new InvalidOperationException("Fragment reset after reopening practice.");
                    pendingPractice.Close();EditorApplication.update-=AdvanceRevealTest;
                    Debug.Log("PASS Ruin 2 D3-A Play: fragment hidden before the practice trace, revealed once only after four restorations and an empty stack, then persisted through practice reopen.");
                    pendingReveal=null;pendingPractice=null;pendingRestoration=null;return;
                }
                var question=pendingPractice.Session.Question;
                if(question==null)throw new InvalidOperationException("D3-A practice question was unexpectedly unavailable.");
                if(pendingPractice.Submit(question.Expected).Status!=Ruin02AnswerStatus.Correct)
                    throw new InvalidOperationException("D3-A accepted answer was rejected: "+question.Id);
            }
            catch(Exception e)
            {
                EditorApplication.update-=AdvanceRevealTest;Debug.LogException(e);
                pendingReveal=null;pendingPractice=null;pendingRestoration=null;
            }
        }
    }
}
