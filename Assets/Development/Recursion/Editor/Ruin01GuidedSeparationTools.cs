using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Fyp.Development.Recursion.Editor
{
    /// <summary>
    /// Moves the already-authored Ruin 1 Page 4 simulation out of the book and
    /// wires it as a standalone modal.  It never creates a trace or duplicates
    /// the existing simulation content.
    /// </summary>
    public static class Ruin01GuidedSeparationTools
    {
        const string ScenePath = "Assets/Scenes/MagicWorld_Development.unity";
        const string BackupFolder = "LocalBackups/Ruin01GuidedSeparation_20261107";

        [MenuItem("Tools/FYP/Ruin 1/Guided Separation/Install Standalone Simulation _F9")]
        public static void Install()
        {
            var scene = RequireDevelopmentScene();

            // Saving first preserves all user changes; the copy is a recoverable
            // snapshot rather than an overwrite of the source scene.
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save the current development scene before backup.");
            Directory.CreateDirectory(BackupFolder);
            var backup = BackupFolder + "/BeforeInstall.unity";
            if (!EditorSceneManager.SaveScene(scene, backup, true))
                throw new IOException("Could not create scene backup: " + backup);

            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var book = ExactlyOne(all.Select(t => t.GetComponent<global::AncientSpellbookUI>()).Where(x => x != null), "AncientSpellbookUI");
            var practice = ExactlyOne(all.Select(t => t.GetComponent<BridgePracticeController>()).Where(x => x != null), "BridgePracticeController");
            var hint = ExactlyOne(all.Select(t => t.GetComponent<InteractionHintManager>()).Where(x => x != null), "InteractionHintManager");
            var simulation = book.guidedSimulation ?? ExactlyOne(all.Select(t => t.GetComponent<SpellbookRecursionSimulation>()).Where(x => x != null), "SpellbookRecursionSimulation");
            ValidateSimulation(simulation);
            var canvas = book.spellbookPanel != null ? book.spellbookPanel.transform.parent : null;
            if (canvas == null) throw new InvalidOperationException("Ruin 1 Magic Book Canvas parent is missing.");

            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Install Ruin 1 standalone guided simulation");
            try
            {
                var panel = simulation.transform;
                if (panel.parent != canvas) Undo.SetTransformParent(panel, canvas, "Separate Ruin 1 guided simulation from Magic Book");
                Undo.RecordObject(panel, "Lay out Ruin 1 standalone simulation");
                panel.name = "Ruin01GuidedSimulationPanel";
                Stretch((RectTransform)panel, .05f, .06f, .95f, .94f);
                var background = panel.GetComponent<Image>() ?? Undo.AddComponent<Image>(panel.gameObject);
                Undo.RecordObject(background, "Style Ruin 1 guided simulation");
                background.color = new Color(.025f, .045f, .07f, .98f);

                var details = simulation.details;
                var diagram = simulation.diagram;
                Stretch((RectTransform)details.transform, .46f, .12f, .98f, .94f);
                Stretch((RectTransform)diagram.transform, .03f, .16f, .42f, .92f);
                LayoutDetails(details);
                LayoutDiagram(diagram);

                var finish = panel.Find("FinishButton")?.GetComponent<Button>() ??
                    CreateButton(panel, "FinishButton", "Finish / Complete", new Vector2(.31f, .025f), new Vector2(.48f, .085f), details.title.font);

                var area = ExactlyOne(all.Where(t => t.name == "Ruin01_Area"), "Ruin01_Area");
                var gameplay = area.Find("Gameplay") ?? throw new InvalidOperationException("Ruin01_Area/Gameplay is missing.");
                var manager = gameplay.Find("Ruin01GuidedSimulationManager")?.GetComponent<Ruin01GuidedSimulationUI>();
                if (manager == null)
                {
                    var go = new GameObject("Ruin01GuidedSimulationManager");
                    Undo.RegisterCreatedObjectUndo(go, "Create Ruin 1 guided simulation manager");
                    go.transform.SetParent(gameplay, false);
                    manager = Undo.AddComponent<Ruin01GuidedSimulationUI>(go);
                }

                Undo.RecordObject(manager, "Wire Ruin 1 guided simulation");
                manager.panel = panel.gameObject;
                manager.simulation = simulation;
                manager.finishButton = finish;
                manager.playerMovement = book.playerMovement;
                manager.cameraFollow = book.cameraFollow;
                Undo.RecordObject(book, "Keep Magic Book to three pages");
                book.guidedSimulation = simulation;
                book.standaloneGuidedSimulation = manager;
                Undo.RecordObject(practice, "Gate bridge practice behind guided simulation");
                practice.guidedSimulationUi = manager;
                Undo.RecordObject(hint, "Register standalone guided modal");
                hint.modalPanels = (hint.modalPanels ?? Array.Empty<GameObject>()).Concat(new[] { panel.gameObject }).Where(x => x != null).Distinct().ToArray();

                panel.gameObject.SetActive(false);
                EditorUtility.SetDirty(manager);
                EditorUtility.SetDirty(book);
                EditorUtility.SetDirty(practice);
                EditorUtility.SetDirty(hint);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new IOException("Could not save the standalone guided simulation setup.");
                Undo.CollapseUndoOperations(group);
                Debug.Log("[Ruin01Guided] Installed standalone 22-checkpoint simulation. Backup: " + backup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        static UnityEngine.SceneManagement.Scene RequireDevelopmentScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before installing Ruin 1 guided simulation.");
            if (scene.path != ScenePath) throw new InvalidOperationException("Open " + ScenePath + " before installing.");
            return scene;
        }

        static T ExactlyOne<T>(System.Collections.Generic.IEnumerable<T> values, string label)
        {
            var array = values.Where(x => x != null).ToArray();
            if (array.Length != 1) throw new InvalidOperationException("Expected exactly one " + label + ", found " + array.Length + ".");
            return array[0];
        }

        static void ValidateSimulation(SpellbookRecursionSimulation simulation)
        {
            if (simulation == null || simulation.details == null || simulation.diagram == null || simulation.details.frameBackgrounds == null ||
                simulation.details.frameBackgrounds.Length != 4 || simulation.details.frameBackgrounds.Any(x => x == null))
                throw new InvalidOperationException("The existing 22-checkpoint Ruin 1 simulation is incomplete; no scene changes were made.");
        }

        static void LayoutDetails(RecursionPanelView details)
        {
            Stretch((RectTransform)details.title.transform, .05f, .92f, .95f, .98f);
            Stretch((RectTransform)details.code.transform, .05f, .66f, .95f, .875f);
            Stretch((RectTransform)details.state.transform, .05f, .595f, .95f, .645f);
            var heading = details.transform.Find("StackHeading")?.GetComponent<TMP_Text>();
            if (heading != null) Stretch((RectTransform)heading.transform, .05f, .545f, .95f, .58f);
            var stack = (RectTransform)details.frameBackgrounds[0].transform.parent;
            Stretch(stack, .05f, .315f, .95f, .535f);
            Stretch((RectTransform)details.emptyStack.transform, .05f, .315f, .95f, .535f);
            Stretch((RectTransform)details.explanation.transform, .05f, .205f, .95f, .295f);
            Stretch((RectTransform)details.progress.transform, .05f, .165f, .95f, .195f);
            Stretch((RectTransform)details.checkpointSlider.transform, .05f, .115f, .95f, .145f);
            Stretch((RectTransform)details.previousButton.transform, .05f, .035f, .265f, .085f);
            Stretch((RectTransform)details.nextButton.transform, .285f, .035f, .50f, .085f);
            Stretch((RectTransform)details.replayButton.transform, .52f, .035f, .735f, .085f);
            Stretch((RectTransform)details.closeButton.transform, .755f, .035f, .95f, .085f);
            Configure(details.title, 22, 30, TextAlignmentOptions.Center);
            Configure(details.code, 15, 19, TextAlignmentOptions.TopLeft);
            Configure(details.state, 16, 20, TextAlignmentOptions.Center);
            Configure(details.explanation, 15, 19, TextAlignmentOptions.TopLeft);
            Configure(details.progress, 14, 17, TextAlignmentOptions.Center);
            if (heading != null) Configure(heading, 14, 17, TextAlignmentOptions.Left);
            foreach (var button in new[] { details.previousButton, details.nextButton, details.replayButton, details.closeButton })
            {
                var label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
                if (label != null) Configure(label, 13, 17, TextAlignmentOptions.Center);
            }
        }

        static void LayoutDiagram(BridgeDiagramView diagram)
        {
            foreach (var text in diagram.GetComponentsInChildren<TMP_Text>(true)) Configure(text, 14, 20, TextAlignmentOptions.Center);
        }

        static void Stretch(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            Undo.RecordObject(rect, "Lay out Ruin 1 guided simulation");
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Configure(TMP_Text text, float min, float max, TextAlignmentOptions alignment)
        {
            Undo.RecordObject(text, "Configure Ruin 1 guided simulation text");
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
            text.alignment = alignment;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.lineSpacing = 0;
            text.margin = new Vector4(4, 2, 4, 2);
        }

        static Button CreateButton(Transform parent, string name, string title, Vector2 min, Vector2 max, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, "Create Ruin 1 Finish button");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = new Color(.17f, .42f, .36f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(rect, false);
            Stretch((RectTransform)label.transform, 0, 0, 1, 1);
            label.font = font;
            label.text = title;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            Configure(label, 13, 17, TextAlignmentOptions.Center);
            return button;
        }
    }
}
