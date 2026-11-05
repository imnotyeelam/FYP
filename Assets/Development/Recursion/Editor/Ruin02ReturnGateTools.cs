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
    public static class Ruin02ReturnGateTools
    {
        const string ScenePath = "Assets/Scenes/MagicWorld_Development.unity";
        const string BackupFolder = "LocalBackups/Ruin02ReturnGate_20261105";

        [MenuItem("Tools/FYP/Ruin 2/Return Gate/Audit")]
        public static void Audit()
        {
            var area = FindArea();
            var entrance = FindReturnEntrance(area);
            var cube = FindVerifiedPlaceholder(area, entrance.transform, false);
            Debug.Log("[Ruin02ReturnGate] return=" + Path(entrance.transform) + " destination=" + Path(entrance.destination) +
                      " range=" + entrance.interactionDistance + " placeholder=" + (cube == null ? "NOT FOUND" : Path(cube.transform)));
        }

        [MenuItem("Tools/FYP/Ruin 2/Return Gate/Install Lock and Monument")]
        public static void Install()
        {
            var scene = RequireCleanScene();
            var area = FindArea();
            var entrance = FindReturnEntrance(area);
            // A missing placeholder is valid: the actual Ruin 2 environment may already be dressed.
            // Never widen this search beyond Ruin02_Area and never touch the known Ruin 1 Cube (2).
            var cube = FindVerifiedPlaceholder(area, entrance.transform, false);
            var ruin01 = FindExactlyOneTransform("Ruin01_Area");
            var sourceMonument = ruin01.GetComponentsInChildren<Transform>(true).SingleOrDefault(t => t.name == "ReturnMagicRock");
            if (sourceMonument == null)
                throw new InvalidOperationException("Ruin 1 ReturnMagicRock was not found; no source prefab was changed.");
            var sourcePrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sourceMonument.gameObject);
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefabPath);
            if (sourcePrefab == null)
                throw new InvalidOperationException("Ruin 1 monument has no source prefab asset; no replacement was made.");
            var tracker = UnityEngine.Object.FindObjectsByType<FragmentProgressTracker>(FindObjectsInactive.Include, FindObjectsSortMode.None).SingleOrDefault();
            if (tracker == null) throw new InvalidOperationException("FragmentProgressTracker is missing.");

            Directory.CreateDirectory(BackupFolder);
            var backup = BackupFolder + "/BeforeInstall.unity";
            if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Could not create scene backup: " + backup);

            Undo.IncrementCurrentGroup(); var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Lock Ruin 2 return and replace monument");
            try
            {
                var existingMonuments = entrance.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name == "ReturnMagicRock").ToArray();
                if (existingMonuments.Length > 1)
                    throw new InvalidOperationException("More than one Ruin 2 ReturnMagicRock exists; refusing to create a duplicate.");
                var monument = existingMonuments.Length == 1 ? existingMonuments[0].gameObject :
                    (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab, scene);
                if (existingMonuments.Length == 0)
                {
                    Undo.RegisterCreatedObjectUndo(monument, "Add Ruin 2 return monument");
                    monument.name = "ReturnMagicRock";
                    monument.transform.SetParent(entrance.transform, false);
                    monument.transform.localPosition = sourceMonument.localPosition;
                    monument.transform.localRotation = sourceMonument.localRotation;
                    monument.transform.localScale = sourceMonument.localScale;
                    foreach (var collider in monument.GetComponentsInChildren<Collider>(true))
                    {
                        Undo.RecordObject(collider, "Keep return monument non-blocking");
                        collider.enabled = false;
                    }
                }

                var message = FindSharedToastText();
                var gate = entrance.GetComponent<Ruin02ReturnGate>() ?? Undo.AddComponent<Ruin02ReturnGate>(entrance.gameObject);
                gate.returnEntrance = entrance;
                gate.progress = tracker;
                gate.lockedMessage = message;
                Undo.RecordObject(entrance, "Route Ruin 2 return through fragment gate");
                entrance.enabled = false; // Gate retains the same origin, range, hint, destination, and TeleportManager.

                var hints = UnityEngine.Object.FindObjectsByType<InteractionHintManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
                Undo.RecordObject(hints, "Register Ruin 2 gated return");
                var targets = hints.interactionTargets ?? Array.Empty<MonoBehaviour>();
                var index = Array.IndexOf(targets, entrance);
                if (index >= 0) targets[index] = gate;
                else if (!targets.Contains(gate)) targets = targets.Concat(new MonoBehaviour[] { gate }).ToArray();
                hints.interactionTargets = targets;

                EnsureStaticMonumentCollider(monument);

                if (cube != null) Undo.DestroyObjectImmediate(cube);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save Ruin 2 return-gate changes.");
                Undo.CollapseUndoOperations(group);
                VerifyInstalledState(entrance, gate);
                Debug.Log("[Ruin02ReturnGate] Installed or upgraded. The return destination, range, and interaction point are unchanged. Backup: " + backup);
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        static UnityEngine.SceneManagement.Scene RequireCleanScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath || scene.isDirty)
                throw new InvalidOperationException("Requires the clean development scene in Edit Mode; no unsaved work will be overwritten.");
            return scene;
        }

        static Transform FindArea() => FindExactlyOneTransform("Ruin02_Area");
        static Transform FindExactlyOneTransform(string name)
        {
            var result = EditorSceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Where(t => t.name == name).ToArray();
            if (result.Length != 1) throw new InvalidOperationException("Expected exactly one " + name + ", found " + result.Length + ".");
            return result[0];
        }

        static RuinEntrance FindReturnEntrance(Transform area)
        {
            var entries = area.GetComponentsInChildren<RuinEntrance>(true)
                .Where(e => e.useSharedInteraction && e.sharedHint.IndexOf("return", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            if (entries.Length != 1) throw new InvalidOperationException("Expected exactly one Ruin 2 shared return interaction, found " + entries.Length + ".");
            return entries[0];
        }

        static GameObject FindVerifiedPlaceholder(Transform area, Transform returnPoint, bool required)
        {
            var candidates = area.GetComponentsInChildren<MeshFilter>(true)
                .Where(f => f.sharedMesh != null && f.sharedMesh.name == "Cube")
                .Select(f => f.gameObject)
                .Where(go => Vector3.Distance(go.transform.position, returnPoint.position) <= 12f)
                .Where(go => go.GetComponents<Component>().All(c => c is Transform || c is MeshFilter || c is MeshRenderer || c is BoxCollider))
                .ToArray();
            if (candidates.Length == 1) return candidates[0];
            if (!required) return null;
            throw new InvalidOperationException("Expected exactly one script-free Cube placeholder within 12m of the Ruin 2 return point, found " + candidates.Length + ". No object was removed.");
        }

        static void VerifyInstalledState(RuinEntrance entrance, Ruin02ReturnGate gate)
        {
            var monument = entrance.GetComponentsInChildren<Transform>(true).SingleOrDefault(t => t.name == "ReturnMagicRock");
            if (entrance.enabled || gate.returnEntrance != entrance || gate.progress == null || gate.lockedMessage != FindSharedToastText() ||
                monument == null || !monument.TryGetComponent<BoxCollider>(out var collider) || !collider.enabled || collider.isTrigger)
                throw new InvalidOperationException("A partial Ruin 2 return-gate installation was found; refusing to duplicate objects.");
            var hints = UnityEngine.Object.FindObjectsByType<InteractionHintManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
            if (hints.interactionTargets.Count(target => target == gate) != 1 || hints.interactionTargets.Any(target => target == entrance))
                throw new InvalidOperationException("Ruin 2 return interaction registration is not idempotent; no duplicate target was created.");
        }

        static TMP_Text FindSharedToastText()
        {
            var canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .SingleOrDefault(c => c.name == "Canvas");
            if (canvas == null) throw new InvalidOperationException("Shared Canvas is missing.");
            var toasts = canvas.GetComponentsInChildren<TMP_Text>(true)
                .Where(t => t.transform.parent != null && t.transform.parent.name == "FragmentRewardToast").ToArray();
            if (toasts.Length != 1 || toasts[0].font == null || !toasts[0].font.name.Contains("Chomage"))
                throw new InvalidOperationException("Expected the one existing Chomage FragmentRewardToast; no custom return-gate UI was created.");
            return toasts[0];
        }

        static void EnsureStaticMonumentCollider(GameObject monument)
        {
            var renderers = monument.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("ReturnMagicRock has no visible renderer to size its collider.");
            var localBounds = new Bounds(); var initialized = false;
            foreach (var renderer in renderers)
            {
                var b = renderer.bounds;
                foreach (var point in new[]
                {
                    new Vector3(b.min.x,b.min.y,b.min.z), new Vector3(b.min.x,b.min.y,b.max.z),
                    new Vector3(b.min.x,b.max.y,b.min.z), new Vector3(b.min.x,b.max.y,b.max.z),
                    new Vector3(b.max.x,b.min.y,b.min.z), new Vector3(b.max.x,b.min.y,b.max.z),
                    new Vector3(b.max.x,b.max.y,b.min.z), new Vector3(b.max.x,b.max.y,b.max.z)
                })
                {
                    var local = monument.transform.InverseTransformPoint(point);
                    if (initialized) localBounds.Encapsulate(local); else { localBounds = new Bounds(local, Vector3.zero); initialized = true; }
                }
            }
            var collider = monument.GetComponent<BoxCollider>();
            if (collider == null)
            {
                // Undo.AddComponent does not reliably return the newly-created component on all
                // Unity 6 editor paths. Reacquire from this scene instance before recording it.
                Undo.AddComponent<BoxCollider>(monument);
                collider = monument.GetComponent<BoxCollider>();
            }
            if (collider == null)
                throw new InvalidOperationException("ReturnMagicRock could not receive a BoxCollider; the scene was not saved.");
            Undo.RecordObject(collider, "Size Ruin 2 return monument collider");
            collider.center = localBounds.center; collider.size = localBounds.size; collider.isTrigger = false; collider.enabled = true;
            foreach (var childCollider in monument.GetComponentsInChildren<Collider>(true).Where(c => c != collider))
            {
                Undo.RecordObject(childCollider, "Keep one Ruin 2 return monument collider");
                childCollider.enabled = false;
            }
        }

        static string Path(Transform t) => t == null ? "null" : t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    }
}
