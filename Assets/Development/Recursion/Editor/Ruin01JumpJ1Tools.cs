using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin01JumpJ1Tools
    {
        public const string Backup = "LocalBackups/Ruin01JumpJ1_20261119";
        public const string Fbx = "Assets/Animation/PolyArtWizardMesh@Running Jump.fbx";
        public static Ruin01MagicJumpController Jump => UnityEngine.Object.FindFirstObjectByType<Ruin01MagicJumpController>();
        public static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;

        [MenuItem("Tools/FYP/Ruin 1/Jump J1/Audit and Back Up")]
        public static void Audit()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/MagicWorld_Development.unity")
                throw new InvalidOperationException("Open development scene in Edit Mode.");
            Directory.CreateDirectory(Backup);
            if (!EditorSceneManager.SaveScene(scene, Backup + $"/Before_{DateTime.Now:HHmmssfff}.unity", true))
                throw new IOException("Backup failed.");
            foreach (var asset in new[] { Fbx + ".meta", "Assets/Animation/PlayerAnimator.controller", "Assets/Development/Recursion/Ruin01MagicJumpController.cs" })
                File.Copy(asset, Backup + "/" + System.IO.Path.GetFileName(asset) + ".before", true);
            var jump = Jump;
            var log = new StringBuilder();
            log.AppendLine($"START {jump.jumpStartPoint.position:F3}, SURFACE {Path(jump.jumpStartSurface.transform)} bounds {jump.jumpStartSurface.bounds}");
            log.AppendLine($"ANIM {Path(jump.animator.transform)} local {jump.animator.transform.localPosition:F3} scale {jump.animator.transform.lossyScale:F3} RootMotion {jump.animator.applyRootMotion}");
            foreach (var t in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Where(t =>
                Vector3.Distance(t.position, jump.jumpStartPoint.position) < 45 &&
                (t.name.IndexOf("rock", StringComparison.OrdinalIgnoreCase) >= 0 || t.GetComponent<RuinEntrance>() != null)))
            {
                log.AppendLine($"NEAR {Path(t)} pos {t.position:F3}");
                foreach (var b in t.GetComponentsInChildren<MonoBehaviour>(true)) log.AppendLine($"  SCRIPT {Path(b.transform)} {b.GetType().Name} enabled={b.enabled}");
                foreach (var c in t.GetComponentsInChildren<Collider>(true)) log.AppendLine($"  COLLIDER {Path(c.transform)} {c.GetType().Name} enabled={c.enabled} trigger={c.isTrigger} bounds={c.bounds}");
            }
            foreach (var b in InteractionHintManager.Instance != null ? InteractionHintManager.Instance.interactionTargets : UnityEngine.Object.FindFirstObjectByType<InteractionHintManager>().interactionTargets)
                if (b is ISharedInteractionTarget target && target.InteractionOrigin != null && Vector3.Distance(target.InteractionOrigin.position, jump.jumpStartPoint.position) < 60)
                    log.AppendLine($"TARGET {Path(b.transform)} {b.GetType().Name} origin={target.InteractionOrigin.position:F3} range={target.SharedInteractionRange}");
            log.Append(SampleClip());
            Directory.CreateDirectory("Temp/Ruin01JumpJ1");
            File.WriteAllText("Temp/Ruin01JumpJ1/Audit.txt", log.ToString());
            Debug.Log("J1 audit/backup ready: Temp/Ruin01JumpJ1/Audit.txt");
        }

        public static string SampleClip()
        {
            var jump = Jump;
            var clone = UnityEngine.Object.Instantiate(jump.animator.gameObject);
            clone.hideFlags = HideFlags.HideAndDontSave;
            clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var animator = clone.GetComponent<Animator>();
            var log = new StringBuilder("CLIP SAMPLES (world metres, isolated visual clone)\n");
            try
            {
                AnimationMode.StartAnimationMode();
                for (int i = 0; i <= 10; i++)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(clone, jump.runningJumpClip, jump.runningJumpClip.length * i / 10f);
                    AnimationMode.EndSampling();
                    log.AppendLine($"t={i/10f:F1} root={clone.transform.position:F3} hips={animator.GetBoneTransform(HumanBodyBones.Hips).position:F3} feet={animator.GetBoneTransform(HumanBodyBones.LeftFoot).position:F3}/{animator.GetBoneTransform(HumanBodyBones.RightFoot).position:F3}");
                }
            }
            finally { AnimationMode.StopAnimationMode(); UnityEngine.Object.DestroyImmediate(clone); }
            return log.ToString();
        }

        [MenuItem("Tools/FYP/Ruin 1/Jump J1/Install Rock Anchor and In-Place Clip")]
        public static void Install()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/MagicWorld_Development.unity")
                throw new InvalidOperationException("Open development scene in Edit Mode.");
            if (!Directory.Exists(Backup) || !Directory.GetFiles(Backup, "Before_*.unity").Any())
                throw new InvalidOperationException("Run Audit and Back Up first.");
            var jump = Jump;
            var rock = FloatingIslandPrototypeTools.Exact("Ruin01_RelocationRoot/Ruin01_Area/Interactables/ReturnInteractionPoint/ReturnMagicRock");
            var existingReturn = rock.parent.GetComponent<RuinEntrance>();
            var hints = UnityEngine.Object.FindFirstObjectByType<InteractionHintManager>();
            if (existingReturn == null || !hints.interactionTargets.Contains(existingReturn) ||
                rock.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                throw new InvalidOperationException("Rock responsibilities changed; inspect before repurposing.");
            Physics.SyncTransforms();
            Vector3 approach = default;
            bool found = false;
            foreach (var offset in new[] { Vector3.back * 1.6f, Vector3.forward * 1.6f, Vector3.right * 1.6f, Vector3.left * 1.6f })
            {
                var origin = rock.position + offset + Vector3.up * 5;
                if (!jump.jumpStartSurface.Raycast(new Ray(origin, Vector3.down), out var hit, 10)) continue;
                var foot = hit.point + Vector3.up * .12f;
                var cc = jump.characterController;
                var center = foot + cc.center;
                var half = Vector3.up * Mathf.Max(0, cc.height * .5f - cc.radius);
                if (Physics.OverlapCapsule(center-half, center+half, cc.radius, ~0, QueryTriggerInteraction.Ignore)
                    .Any(c => c != cc && !c.transform.IsChildOf(cc.transform))) continue;
                if (Vector3.Distance(foot, rock.position) > 2.5f) continue;
                approach = foot; found = true; break;
            }
            if (!found) throw new InvalidOperationException("No clear approach near existing rock.");
            var marker = rock.parent.Find("JumpRockApproach");
            if (marker == null)
            {
                marker = new GameObject("JumpRockApproach").transform;
                Undo.RegisterCreatedObjectUndo(marker.gameObject, "Add safe jump rock testing spawn");
                marker.SetParent(rock.parent, true);
            }
            Undo.RecordObject(marker, "Set safe rock approach");
            marker.position = approach;
            Undo.RecordObject(jump, "Anchor Jump interaction to existing rock");
            jump.interactionAnchor = rock; jump.interactionDistance = 2.5f;
            var dev = UnityEngine.Object.FindFirstObjectByType<RuinDeveloperPanel>();
            Undo.RecordObject(dev, "Keep testing teleport near jump interaction");
            dev.ruin01JumpStart = marker;
            var importer = (ModelImporter)AssetImporter.GetAtPath(Fbx);
            var clips = importer.clipAnimations;
            // Extract horizontal root travel rather than baking 3.74m into the Humanoid pose.
            // applyRootMotion stays false, so only the existing route controller moves the Wizard.
            foreach (var clip in clips) clip.lockRootPositionXZ = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            EditorUtility.SetDirty(jump); EditorUtility.SetDirty(dev);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Temp/Ruin01JumpJ1/AfterClip.txt", SampleClip());
            Debug.Log($"J1 installed: {Path(rock)} range 2.5m, safe approach {approach:F3}. Existing return and all route markers preserved; horizontal clip travel extracted, Root Motion off.");
        }
    }
}
