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
    public static class FinalPortalP1Tools
    {
        [MenuItem("Tools/FYP/Final Portal P1/Install Two-Fragment Gate")]
        public static void Install()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/MagicWorld_Development.unity" || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Open MagicWorld_Development in Edit Mode.");

            var portal = Exact("MainHub/BrokenPortal");
            var legacy = portal.GetComponent<PortalInteraction>();
            var restoration = Exact("PortalRestorationManager").GetComponent<PortalRestorationController>();
            var progress = Exact("FragmentProgressTracker").GetComponent<FragmentProgressTracker>();
            var hints = Exact("InteractionHintManager").GetComponent<InteractionHintManager>();
            var toast = Exact("Canvas/FragmentRewardToast").GetComponentInChildren<TMP_Text>(true);
            if (legacy == null || legacy.enabled || restoration == null || progress == null || toast == null ||
                !hints.coordinateInteractions || restoration.brokenPortal != portal.Find("BrokenPortal").gameObject ||
                restoration.completePortal != portal.Find("CompletePortal").gameObject ||
                legacy.challengePanel == null || legacy.challengePanel.activeSelf)
                throw new InvalidOperationException("Existing portal models, disabled quiz or shared UI wiring differs from the audit.");

            const string backupFolder = "LocalBackups/FinalPortalP1_20261119";
            Directory.CreateDirectory(backupFolder);
            string backup = $"{backupFolder}/Before_{DateTime.Now:HHmmssfff}.unity";
            if (!EditorSceneManager.SaveScene(scene, backup, true))
                throw new IOException("Could not back up the live scene.");

            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Install Final Portal two-fragment gate");
            try
            {
                var gate = portal.GetComponent<FinalPortalInteraction>();
                if (gate == null) gate = Undo.AddComponent<FinalPortalInteraction>(portal.gameObject);
                Undo.RecordObjects(new UnityEngine.Object[] { gate, restoration, hints }, "Wire Final Portal");
                gate.restoration = restoration;
                gate.toast = toast;
                gate.interactionDistance = legacy.interactionDistance;
                restoration.requireFinalPortalFragments = true;
                restoration.fragmentProgress = progress;
                restoration.ruinsToActivate = restoration.ruinsToActivate
                    .Where(ruin => ruin != null && ruin.name != "Ruin03_Gateway").ToArray();
                // P1 leaves the legacy quiz and portal VFX disconnected in this scene.
                restoration.challengePanel = null;
                restoration.completePortalEffect = null;
                restoration.ruinEffects = Array.Empty<GameObject>();
                hints.interactionTargets = hints.interactionTargets
                    .Where(x => x != legacy && x != gate).Append(gate).ToArray();

                Undo.CollapseUndoOperations(undo);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Final Portal wiring.");
                Debug.Log("Final Portal P1 installed. Scene backup: " + backup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undo);
                throw;
            }
        }

        static Transform Exact(string path) => FloatingIslandPrototypeTools.Exact(path);
    }
}
