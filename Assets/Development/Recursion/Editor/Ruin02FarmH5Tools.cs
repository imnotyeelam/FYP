using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin02FarmH5Tools
    {
        [MenuItem("Tools/FYP/Ruin 2/Magical Farm H5/Install Reward Prerequisite")]
        public static void Install()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/MagicWorld_Development.unity" || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Open development scene in Edit Mode.");
            var reward = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Ruin02FragmentReveal>(true)).Single();
            var farm = FloatingIslandPrototypeTools.Exact("Ruin02_MagicalFarm").GetComponent<Ruin02FarmSimulation>();
            if (farm == null || reward.fragmentVisual == null || reward.practice == null || reward.restoration == null)
                throw new InvalidOperationException("Existing reward/farm references are incomplete.");
            Directory.CreateDirectory("LocalBackups/Ruin02FarmH5_20261119");
            if (!EditorSceneManager.SaveScene(scene, $"LocalBackups/Ruin02FarmH5_20261119/Before_{DateTime.Now:HHmmssfff}.unity", true))
                throw new IOException("Scene backup failed.");
            Undo.RecordObject(reward, "Require Magical Farm for Ruin 2 reward");
            reward.farm = farm;
            EditorUtility.SetDirty(reward);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"H5 saved: existing {reward.fragmentVisual.name} remains at {reward.fragmentVisual.transform.position:F3}; farm prerequisite wired, no reward duplicated.");
        }
    }
}
