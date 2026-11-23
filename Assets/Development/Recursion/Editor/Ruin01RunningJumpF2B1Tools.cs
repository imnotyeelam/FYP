using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    [InitializeOnLoad]
    public static class Ruin01RunningJumpF2B1Tools
    {
        const string ScenePath = "Assets/Scenes/MagicWorld_Development.unity";
        const string ControllerPath = "Assets/Animation/PlayerAnimator.controller";
        const string JumpFbxPath = "Assets/Animation/PolyArtWizardMesh@Running Jump.fbx";
        const string WizardFbxPath = "Assets/WizardPolyArt/Mesh/PolyArtWizardMesh.fbx";
        const string BackupDirectory = "LocalBackups/Ruin01RunningJumpF2B1_20261111";

        static Ruin01RunningJumpF2B1Tools()
        {
            EditorApplication.delayCall += AutoInstallOnce;
        }

        static void AutoInstallOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath) return;
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller != null && controller.layers[0].stateMachine.states.Any(x => x.state.name == "Running Jump")) return;
            Install();
        }

        [MenuItem("Tools/FYP/Ruin 1/Jump F2-B1/Install Running Jump Animation")]
        public static void Install()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
                throw new InvalidOperationException("Open MagicWorld_Development in Edit Mode.");

            Directory.CreateDirectory(BackupDirectory);
            string liveBackup = BackupDirectory + "/BeforeLive.unity";
            if (!File.Exists(liveBackup) && !EditorSceneManager.SaveScene(scene, liveBackup, true))
                throw new IOException("Could not create the live scene backup.");
            CopyOnce(ControllerPath, BackupDirectory + "/PlayerAnimator_BeforeLive.controller");
            CopyOnce(JumpFbxPath + ".meta", BackupDirectory + "/RunningJump_BeforeLive.fbx.meta");
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not preserve the open scene before F2-B1 installation.");

            var sourceAvatar = AssetDatabase.LoadAllAssetsAtPath(WizardFbxPath).OfType<Avatar>().Single();
            if (!sourceAvatar.isValid || !sourceAvatar.isHuman)
                throw new InvalidOperationException("The existing Wizard Avatar is not a valid Humanoid Avatar.");

            var importer = AssetImporter.GetAtPath(JumpFbxPath) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Running Jump FBX importer is unavailable.");
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = sourceAvatar;
            var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
            if (clips.Length != 1) throw new InvalidOperationException("Expected one Mixamo Running Jump take.");
            clips[0].name = "Running Jump";
            clips[0].loopTime = false;
            clips[0].loopPose = false;
            clips[0].lockRootRotation = true;
            clips[0].lockRootHeightY = true;
            clips[0].lockRootPositionXZ = true;
            clips[0].keepOriginalOrientation = false;
            clips[0].keepOriginalPositionY = false;
            clips[0].keepOriginalPositionXZ = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();

            var jumpClip = AssetDatabase.LoadAllAssetsAtPath(JumpFbxPath).OfType<AnimationClip>()
                .Single(x => !x.name.StartsWith("__preview__", StringComparison.Ordinal) && x.name == "Running Jump");
            if (jumpClip.legacy || !jumpClip.isHumanMotion || jumpClip.isLooping || jumpClip.length <= .1f)
                throw new InvalidOperationException("Running Jump did not import as a non-looping Humanoid clip.");

            var animatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (animatorController == null || animatorController.layers.Length != 1)
                throw new InvalidOperationException("PlayerAnimator controller is missing or no longer has one base layer.");
            var machine = animatorController.layers[0].stateMachine;
            var locomotion = machine.states.Select(x => x.state).Single(x => x.name == "Locomotion");
            var blend = locomotion.motion as BlendTree;
            if (blend == null || blend.children.Length != 3 || blend.blendParameter != "Speed")
                throw new InvalidOperationException("The existing Idle/Walk/Run Locomotion blend tree changed unexpectedly.");

            if (!animatorController.parameters.Any(x => x.name == "MagicJumpSpeed"))
                animatorController.AddParameter("MagicJumpSpeed", AnimatorControllerParameterType.Float);
            var runningJump = machine.states.Select(x => x.state).FirstOrDefault(x => x.name == "Running Jump");
            if (runningJump == null) runningJump = machine.AddState("Running Jump", new Vector3(610, 170));
            runningJump.motion = jumpClip;
            runningJump.speed = 1f;
            runningJump.speedParameter = "MagicJumpSpeed";
            runningJump.speedParameterActive = true;
            runningJump.writeDefaultValues = true;
            foreach (var transition in runningJump.transitions.ToArray()) runningJump.RemoveTransition(transition);
            var fallback = runningJump.AddTransition(locomotion);
            fallback.hasExitTime = true;
            fallback.exitTime = 1f;
            fallback.hasFixedDuration = true;
            fallback.duration = .06f;
            EditorUtility.SetDirty(animatorController);
            AssetDatabase.SaveAssets();

            var jump = Exact("Ruin01_RelocationRoot/Ruin01JumpRoute/JumpChallenge")
                .GetComponent<Ruin01MagicJumpController>();
            var player = Exact("Player");
            var animator = player.GetComponentInChildren<Animator>(true);
            if (jump == null || animator == null || animator.runtimeAnimatorController != animatorController ||
                animator.avatar != sourceAvatar)
                throw new InvalidOperationException("Player Animator, controller or Avatar references are incomplete.");
            Undo.RecordObject(jump, "Wire Ruin 1 Running Jump animation");
            Undo.RecordObject(animator, "Disable Root Motion for Running Jump");
            jump.animator = animator;
            jump.runningJumpClip = jumpClip;
            animator.applyRootMotion = false;
            EditorUtility.SetDirty(jump);
            EditorUtility.SetDirty(animator);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save F2-B1 scene references.");
            Debug.Log($"F2-B1 Running Jump installed: clip={jumpClip.name}, length={jumpClip.length:F3}s, " +
                $"human={jumpClip.isHumanMotion}, loop={jumpClip.isLooping}, Avatar={sourceAvatar.name}, RootMotion={animator.applyRootMotion}.");
        }

        [MenuItem("Tools/FYP/Ruin 1/Jump F2-B1/Verify Installed References")]
        public static void VerifyInstalled()
        {
            var jump = Exact("Ruin01_RelocationRoot/Ruin01JumpRoute/JumpChallenge")
                .GetComponent<Ruin01MagicJumpController>();
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            var machine = controller.layers[0].stateMachine;
            var locomotion = machine.states.Select(x => x.state).Single(x => x.name == "Locomotion");
            var running = machine.states.Select(x => x.state).Single(x => x.name == "Running Jump");
            var blend = locomotion.motion as BlendTree;
            if (jump.animator == null || jump.runningJumpClip == null || jump.animator.applyRootMotion ||
                jump.animator.runtimeAnimatorController != controller || jump.animator.avatar == null ||
                !jump.animator.avatar.isValid || !jump.animator.avatar.isHuman ||
                running.motion != jump.runningJumpClip || !running.speedParameterActive ||
                running.speedParameter != "MagicJumpSpeed" || jump.runningJumpClip.isLooping ||
                !jump.runningJumpClip.isHumanMotion || blend == null || blend.children.Length != 3 ||
                blend.blendParameter != "Speed")
                throw new InvalidOperationException("F2-B1 Animator, clip, Avatar, Root Motion or Locomotion validation failed.");
            Debug.Log($"PASS F2-B1 references: Locomotion blend tree preserved, Running Jump length={jump.runningJumpClip.length:F3}s, " +
                "Humanoid retarget valid, non-looping, Root Motion off.");
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

        static void CopyOnce(string source, string destination)
        {
            if (!File.Exists(destination)) File.Copy(source, destination);
        }
    }
}
