using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin01FragmentF3Tools
    {
        const string ScenePath = "Assets/Scenes/MagicWorld_Development.unity";

        [MenuItem("Tools/FYP/Ruin 1/Fragment F3/Install Reward Flow")]
        public static void Install()
        {
            var scene = Application.isBatchMode
                ? EditorSceneManager.OpenScene(ScenePath)
                : EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Open MagicWorld_Development in Edit Mode.");

            var reward = Exact("Ruin01_Area/Interactables/FragmentOfCalling").GetComponent<Ruin01FragmentReward>();
            var originalExit = Exact("Ruin01_Area/Interactables/ReturnInteractionPoint").GetComponent<RuinEntrance>();
            var jump = Exact("Ruin01_RelocationRoot/Ruin01JumpRoute/JumpChallenge").GetComponent<Ruin01MagicJumpController>();
            var surface = Exact("Ruin01_RelocationRoot/Ruin01_JumpTargetStone").GetComponent<MeshCollider>();
            var manager = Exact("InteractionHintManager").GetComponent<InteractionHintManager>();
            var altar = reward.transform.Find("FragmentAltar").gameObject;
            if (reward.practice != jump.bridgePractice || reward.player != jump.player || reward.progress == null ||
                reward.interactionPoint == null || reward.gemVisual == null || reward.confirmationText == null ||
                surface == null || !surface.enabled || surface.isTrigger || originalExit.destination == null ||
                originalExit.teleportManager == null || originalExit.teleportManager.player != jump.player ||
                !manager.coordinateInteractions || manager.interactionTargets.Count(x => x == reward) != 1)
                throw new InvalidOperationException("Existing jump, reward, return or shared interaction wiring is incomplete.");

            Physics.SyncTransforms();
            var landing = jump.destinations.Last().position;
            Vector3 ground = FindRewardPosition(surface, jump.characterController, landing);
            const string backupDirectory = "LocalBackups/Ruin01FragmentF3_20261117";
            Directory.CreateDirectory(backupDirectory);
            string backup = backupDirectory + "/Before_" + DateTime.Now.ToString("HHmmssfff") + ".unity";
            if (!EditorSceneManager.SaveScene(scene, backup, true))
                throw new IOException("Could not back up the current scene snapshot.");

            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Install Ruin 1 fragment reward flow");
            try
            {
                Undo.RecordObjects(new UnityEngine.Object[] { reward, reward.transform, reward.gemVisual, altar, manager },
                    "Wire Ruin 1 reward");
                reward.transform.position = ground;
                reward.jump = jump;
                reward.altarVisual = altar;
                reward.gemVisual.SetActive(false);
                altar.SetActive(false);

                // The altar's collection point becomes a return prompt after collection.
                var exit = reward.interactionPoint.GetComponent<RuinEntrance>();
                if (exit == null) exit = Undo.AddComponent<RuinEntrance>(reward.interactionPoint.gameObject);
                Undo.RecordObject(exit, "Wire target stone return");
                exit.enabled = false;
                exit.useSharedInteraction = true;
                exit.player = originalExit.player;
                exit.destination = originalExit.destination;
                exit.teleportManager = originalExit.teleportManager;
                exit.interactionDistance = reward.interactionDistance;
                exit.sharedHint = originalExit.sharedHint;
                reward.targetReturn = exit;
                if (!manager.interactionTargets.Contains(exit))
                    manager.interactionTargets = manager.interactionTargets.Append(exit).ToArray();

                PrefabUtility.RecordPrefabInstancePropertyModifications(altar);
                PrefabUtility.RecordPrefabInstancePropertyModifications(reward.gemVisual);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the reward scene.");
                Undo.CollapseUndoOperations(undo);
                Debug.Log($"Ruin 1 F3 installed. Altar {ground:F3}, collection/return {reward.interactionPoint.position:F3}. Backup {backup}");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undo);
                throw;
            }
        }

        static Vector3 FindRewardPosition(MeshCollider surface, CharacterController player, Vector3 landing)
        {
            var forward = Vector3.ProjectOnPlane(surface.bounds.center - landing, Vector3.up).normalized;
            var right = Vector3.Cross(Vector3.up, forward);
            foreach (float sideways in new[] { 0f, -1.5f, 1.5f })
            {
                var goal = landing + forward * 5f + right * sideways;
                Vector3 ground = default;
                bool safe = true;
                for (int i = 0; i <= 10 && safe; i++)
                {
                    var sample = Vector3.Lerp(landing, goal, i / 10f);
                    safe = TryGround(surface, player, sample, out ground);
                }
                if (safe) return ground;
            }
            throw new InvalidOperationException("Could not find a clear, supported path from landing to the altar.");
        }

        static bool TryGround(MeshCollider surface, CharacterController player, Vector3 sample, out Vector3 ground)
        {
            ground = default;
            sample.y = surface.bounds.max.y + 3f;
            float rayLength = surface.bounds.size.y + 6f;
            if (!surface.Raycast(new Ray(sample, Vector3.down), out var hit, rayLength) ||
                Vector3.Angle(hit.normal, Vector3.up) > player.slopeLimit) return false;
            float span = player.radius + .15f;
            foreach (float x in new[] { -span, 0f, span })
            foreach (float z in new[] { -span, 0f, span })
            {
                if (!surface.Raycast(new Ray(sample + new Vector3(x, 0, z), Vector3.down), out var edge, rayLength) ||
                    Mathf.Abs(edge.point.y - hit.point.y) > .25f ||
                    Vector3.Angle(edge.normal, Vector3.up) > player.slopeLimit) return false;
            }
            var center = hit.point + Vector3.up * .08f + player.center;
            var half = Vector3.up * Mathf.Max(0f, player.height * .5f - player.radius);
            if (Physics.OverlapCapsule(center - half, center + half, player.radius + .05f, ~0,
                    QueryTriggerInteraction.Ignore).Any(c => c != surface && c != player &&
                    !c.transform.IsChildOf(player.transform))) return false;
            ground = hit.point;
            return true;
        }

        static Transform Exact(string path) => FloatingIslandPrototypeTools.Exact(path);
    }
}
