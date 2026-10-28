using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    // Captures the existing gameplay camera at a fixed 1920x1080 render size.
    // The inspection runs only in Play Mode, so sampled camera poses never touch the saved scene.
    [InitializeOnLoad]
    public static class Ruin01VisualAuditTools
    {
        const string Key = "FYP.Ruin01.VisualAudit";
        const string Output = "Temp/Ruin01Relocation/Visual";
        static double started;

        static Ruin01VisualAuditTools() { EditorApplication.update += Tick; }

        [MenuItem("Tools/FYP/Ruin 1/Relocation/Capture Gameplay Camera Views")]
        public static void Run()
        {
            SpellbookStageTools.RequireScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorSceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Visual audit requires a saved development scene in Edit Mode.");
            Directory.CreateDirectory(Output);
            started = 0;
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
            if (started == 0) { started = EditorApplication.timeSinceStartup; return; }
            if (EditorApplication.timeSinceStartup - started < 1.5) return;
            SessionState.SetBool(Key, false);
            try { CaptureAll(); Debug.Log("Ruin 1 gameplay camera views saved under " + Output); }
            catch (Exception ex) { Debug.LogError("Ruin 1 visual audit failed: " + ex); }
            finally { EditorApplication.isPlaying = false; }
        }

        static Transform Exact(string path) => FloatingIslandPrototypeTools.Exact(path);

        static Vector3 WalkPosition(Transform root)
        {
            var colliders = root.GetComponentsInChildren<MeshCollider>(true);
            float top = root.position.y;
            foreach (var collider in colliders)
                if (collider.enabled && collider.bounds.max.y > top) top = collider.bounds.max.y;
            return new Vector3(root.position.x, top + .15f, root.position.z);
        }

        static void CaptureAll()
        {
            var camera = Exact("Main Camera").GetComponent<Camera>();
            var follow = camera.GetComponent<CameraFollow>();
            var originalPosition = camera.transform.position;
            var originalRotation = camera.transform.rotation;
            var originalTarget = camera.targetTexture;
            var originalActive = RenderTexture.active;
            bool originalFollow = follow.enabled;
            var texture = new RenderTexture(1920, 1080, 24);
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            var bridge = Exact("Bridge");
            var bridge1 = Exact("Bridge (1)");
            var bridge2 = Exact("Bridge (2)");
            var bridge3 = Exact("Bridge (3)");
            var destination = Exact("flying_stone_003");
            var samples = new (string name, Vector3 feet, Vector3 focus)[]
            {
                ("01_Spawn", Exact("Ruin01_Area/SpawnPoints/L1_Spawnpoint").position, Exact("Magic Book").position),
                ("02_MagicBook", Exact("Magic Book").position, WalkPosition(bridge)),
                ("03_PracticePoint", Exact("Ruin01_Area/Interactables/BridgePracticeInteractionPoint").position, WalkPosition(bridge1)),
                ("04_BridgeFixed", WalkPosition(bridge), WalkPosition(bridge1)),
                ("05_Bridge1", WalkPosition(bridge1), WalkPosition(bridge2)),
                ("06_Bridge2", WalkPosition(bridge2), WalkPosition(bridge3)),
                ("07_Bridge3", WalkPosition(bridge3), WalkPosition(destination)),
                ("08_FragmentAltar", Exact("Ruin01_Area/Interactables/FragmentOfCalling").position, WalkPosition(bridge3)),
                ("09_Return", Exact("Ruin01_Area/Interactables/ReturnInteractionPoint").position, Exact("Magic Book").position)
            };
            try
            {
                follow.enabled = false;
                camera.targetTexture = texture;
                using (var log = new StreamWriter(Output + "/Views.txt", false))
                {
                    log.WriteLine("Existing Main Camera; 1920x1080; Play Mode; no saved-scene changes.");
                    log.WriteLine("Far clip=" + camera.farClipPlane + " follow distance=" + follow.distance + " height=" + follow.height);
                    log.WriteLine("Samples 04-09 use a Play Mode-only repaired-pose visual override; motion and collision behavior are verified by separate play checks.");
                    for (int i = 0; i < samples.Length; i++)
                    {
                        if (i == 3)
                        {
                            var rig = FloatingIslandPrototypeTools.Rig();
                            foreach (var module in new[] { rig.placeStone3, rig.placeStone2, rig.placeStone1 })
                            {
                                module.enabled = false;
                                module.transform.SetPositionAndRotation(module.repairedPose.position, module.repairedPose.rotation);
                                foreach (var collider in module.walkableColliders) collider.enabled = true;
                            }
                            Physics.SyncTransforms();
                        }
                        var sample = samples[i];
                        Vector3 feet = sample.feet, focus = sample.focus;
                        if (i == 3) { feet = WalkPosition(bridge); focus = WalkPosition(bridge1); }
                        if (i == 4) { feet = WalkPosition(bridge1); focus = WalkPosition(bridge2); }
                        if (i == 5) { feet = WalkPosition(bridge2); focus = WalkPosition(bridge3); }
                        if (i == 6) { feet = WalkPosition(bridge3); focus = WalkPosition(destination); }
                        if (i == 7) focus = WalkPosition(bridge3);
                        Vector3 look = feet + Vector3.up * follow.height;
                        Vector3 direction = focus - look;
                        direction.y = Mathf.Clamp(direction.y, -8f, 8f);
                        if (direction.sqrMagnitude < .01f) direction = Vector3.forward;
                        var rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                        camera.transform.SetPositionAndRotation(look + rotation * new Vector3(0, 0, -follow.distance), rotation);
                        camera.Render();
                        RenderTexture.active = texture;
                        image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                        image.Apply();
                        File.WriteAllBytes(Output + "/" + sample.name + ".png", image.EncodeToPNG());
                        log.WriteLine(sample.name + " feet=" + feet.ToString("F3") + " camera=" + camera.transform.position.ToString("F3") + " focus=" + focus.ToString("F3"));
                    }
                }
            }
            finally
            {
                camera.targetTexture = originalTarget;
                RenderTexture.active = originalActive;
                camera.transform.SetPositionAndRotation(originalPosition, originalRotation);
                follow.enabled = originalFollow;
                UnityEngine.Object.Destroy(texture);
                UnityEngine.Object.Destroy(image);
            }
        }
    }
}
