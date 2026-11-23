using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin01JumpInteractionFixTools
    {
        const string ScenePath = "Assets/Scenes/MagicWorld_Development.unity";

        [MenuItem("Tools/FYP/Ruin 1/Jump Interaction Fix/Install Start Surface Reference")]
        public static void Install()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
                throw new InvalidOperationException("Open MagicWorld_Development in Edit Mode.");
            var root = Exact("Ruin01_RelocationRoot");
            var jump = Exact("Ruin01_RelocationRoot/Ruin01JumpRoute/JumpChallenge")
                .GetComponent<Ruin01MagicJumpController>();
            var surface = root.Find("flying_stone_003")?.GetComponent<MeshCollider>();
            if (jump == null || surface == null || !surface.enabled || surface.isTrigger)
                throw new InvalidOperationException("Jump controller or solid second Flying Stone MeshCollider is missing.");
            Undo.RecordObject(jump, "Use second Flying Stone as jump interaction surface");
            jump.jumpStartSurface = surface;
            EditorUtility.SetDirty(jump);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Could not save the jump interaction reference.");
            Verify();
        }

        [MenuItem("Tools/FYP/Ruin 1/Jump Interaction Fix/Verify Installed References")]
        public static void Verify()
        {
            var jump = Exact("Ruin01_RelocationRoot/Ruin01JumpRoute/JumpChallenge")
                .GetComponent<Ruin01MagicJumpController>();
            var hints = UnityEngine.Object.FindFirstObjectByType<InteractionHintManager>();
            if (jump == null || jump.jumpStartSurface == null || jump.jumpStartSurface.isTrigger ||
                !jump.jumpStartSurface.enabled || hints == null || !hints.coordinateInteractions ||
                hints.interactionTargets.Count(x => x == jump) != 1 ||
                hints.modalPanels.Count(x => x == jump.panel) != 1)
                throw new InvalidOperationException("Jump surface, shared interaction registration or modal registration is invalid.");
            Debug.Log($"PASS Ruin 1 jump interaction references: surface={Path(jump.jumpStartSurface.transform)}, " +
                $"selectionRange={jump.SharedInteractionRange:F2}, markerRange={jump.interactionDistance:F2}.");
        }

        static Transform Exact(string path)
        {
            Transform current = null;
            foreach (var part in path.Split('/'))
                current = current == null
                    ? EditorSceneManager.GetActiveScene().GetRootGameObjects().Single(x => x.name == part).transform
                    : current.Cast<Transform>().Single(x => x.name == part);
            return current;
        }

        static string Path(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null) { transform = transform.parent; path = transform.name + "/" + path; }
            return path;
        }
    }
}
