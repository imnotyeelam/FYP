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
    /// <summary>Scene-only layout maintenance for Canvas/Ruin02GuidedSimulationPanel.</summary>
    public static class Ruin02GuidedSimulationLayoutTools
    {
        const string ScenePath = "Assets/Scenes/MagicWorld_Development.unity";
        const string BackupFolder = "LocalBackups/Ruin02GuidedSimulationLayout_20261105";

        [MenuItem("Tools/FYP/Ruin 2/Guided Simulation/Audit Layout")]
        public static void Audit()
        {
            var guided = FindGuidedUi();
            var details = Validate(guided);
            LogRect("Panel", guided.panel.GetComponent<RectTransform>());
            LogRect("Staff status", (RectTransform)guided.simulation.diagram.transform);
            LogRect("Execution details", (RectTransform)details.transform);
            LogRect("Title", (RectTransform)details.title.transform);
            LogRect("Code", (RectTransform)details.code.transform);
            LogRect("State", (RectTransform)details.state.transform);
            LogRect("Call stack", (RectTransform)details.frameBackgrounds[0].transform.parent);
            LogRect("Explanation", (RectTransform)details.explanation.transform);
            LogRect("Progress", (RectTransform)details.progress.transform);
            LogRect("Slider", (RectTransform)details.checkpointSlider.transform);
            LogRect("Finish", (RectTransform)guided.finishButton.transform);
            Debug.Log("[Ruin02GuidedLayout] Audit complete. No scene objects were changed.");
        }

        [MenuItem("Tools/FYP/Ruin 2/Guided Simulation/Apply Layout")]
        public static void Apply()
        {
            var scene = RequireCleanDevelopmentScene();
            var guided = FindGuidedUi();
            var details = Validate(guided);

            Directory.CreateDirectory(BackupFolder);
            var backup = BackupFolder + "/BeforeLayout.unity";
            if (!EditorSceneManager.SaveScene(scene, backup, true))
                throw new IOException("Could not create guided-simulation backup: " + backup);

            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Lay out Ruin 2 guided simulation");
            try
            {
                var panel = guided.panel.transform;
                var staff = (RectTransform)guided.simulation.diagram.transform;
                var execution = (RectTransform)details.transform;

                // This is the standalone panel's own title, not a Magic Book page title.
                Undo.RecordObject(details.title.gameObject, "Show guided simulation title");
                details.title.gameObject.SetActive(true);

                // 1920 x 1080 CanvasScaler layout: staff information | orb preview | execution trace.
                Stretch(staff, .02f, .14f, .29f, .94f);
                Stretch(execution, .50f, .12f, .98f, .94f);

                // Right-hand execution zone: the title no longer shares the code rectangle.
                Stretch((RectTransform)details.title.transform, .05f, .92f, .95f, .98f);
                Stretch((RectTransform)details.code.transform, .05f, .66f, .95f, .875f);
                Stretch((RectTransform)details.state.transform, .05f, .595f, .95f, .645f);
                var heading = details.transform.Find("StackHeading").GetComponent<TMP_Text>();
                Stretch((RectTransform)heading.transform, .05f, .545f, .95f, .58f);
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
                Stretch((RectTransform)guided.finishButton.transform, .31f, .025f, .48f, .085f);

                ConfigureText(details.title, 22, 30, TextAlignmentOptions.Center, TextOverflowModes.Ellipsis, 0);
                ConfigureText(details.code, 15, 19, TextAlignmentOptions.TopLeft, TextOverflowModes.Ellipsis, 0);
                ConfigureText(details.state, 16, 20, TextAlignmentOptions.Center, TextOverflowModes.Ellipsis, 0);
                ConfigureText(heading, 14, 17, TextAlignmentOptions.Left, TextOverflowModes.Ellipsis, 0);
                ConfigureText(details.explanation, 15, 19, TextAlignmentOptions.TopLeft, TextOverflowModes.Ellipsis, 2);
                ConfigureText(details.progress, 14, 17, TextAlignmentOptions.Center, TextOverflowModes.Ellipsis, 0);
                ConfigureStaffText(guided.simulation.diagram);
                ConfigureButtonText(details.previousButton);
                ConfigureButtonText(details.nextButton);
                ConfigureButtonText(details.replayButton);
                ConfigureButtonText(details.closeButton);
                ConfigureButtonText(guided.finishButton);
                EnsureSingleOrbPreview(panel, details.code.font);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new IOException("Could not save the guided-simulation layout.");
                Undo.CollapseUndoOperations(group);
                Debug.Log("[Ruin02GuidedLayout] Applied layout only to Canvas/Ruin02GuidedSimulationPanel. Backup: " + backup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        static UnityEngine.SceneManagement.Scene RequireCleanDevelopmentScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before applying the guided-simulation layout.");
            if (scene.path != ScenePath)
                throw new InvalidOperationException("Open " + ScenePath + " before applying the layout.");
            if (scene.isDirty)
                throw new InvalidOperationException("The scene has unsaved changes. Save or back them up first; this tool will not overwrite them.");
            return scene;
        }

        static Ruin02GuidedSimulationUI FindGuidedUi()
        {
            var all = EditorSceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Ruin02GuidedSimulationUI>(true)).ToArray();
            if (all.Length != 1) throw new InvalidOperationException("Expected exactly one Ruin02GuidedSimulationUI, found " + all.Length + ".");
            return all[0];
        }

        static RecursionPanelView Validate(Ruin02GuidedSimulationUI guided)
        {
            if (guided.panel == null || guided.panel.name != "Ruin02GuidedSimulationPanel" || guided.simulation == null ||
                guided.simulation.details == null || guided.simulation.diagram == null || guided.finishButton == null)
                throw new InvalidOperationException("Guided Simulation references are incomplete; no layout change was made.");
            var panel = guided.panel.transform;
            var details = guided.simulation.details;
            if (details.transform.parent != panel || guided.simulation.diagram.transform.parent != panel ||
                guided.finishButton.transform.parent != panel)
                throw new InvalidOperationException("Guided Simulation hierarchy differs from the expected standalone panel; no layout change was made.");
            if (details.frameBackgrounds == null || details.frameBackgrounds.Length != 5 || details.frameBackgrounds[0] == null ||
                details.transform.Find("StackHeading") == null)
                throw new InvalidOperationException("The 28-checkpoint stack presentation is incomplete; no layout change was made.");
            return details;
        }

        static void EnsureSingleOrbPreview(Transform panel, TMP_FontAsset font)
        {
            var existing = panel.Find("MagicOrbPreview");
            if (existing != null)
            {
                if (existing.GetComponent<Image>() == null)
                    throw new InvalidOperationException("MagicOrbPreview exists but is not the expected layout-only Image.");
                return;
            }

            var preview = new GameObject("MagicOrbPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(preview, "Add guided simulation orb preview");
            var root = preview.GetComponent<RectTransform>();
            root.SetParent(panel, false);
            Stretch(root, .31f, .25f, .47f, .83f);
            var backdrop = preview.GetComponent<Image>();
            backdrop.color = new Color(.10f, .08f, .20f, .8f);
            backdrop.raycastTarget = false;

            var orb = new GameObject("Orb", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            orb.transform.SetParent(preview.transform, false);
            var orbRect = orb.GetComponent<RectTransform>();
            Stretch(orbRect, .10f, .34f, .90f, .75f);
            var orbImage = orb.GetComponent<Image>();
            orbImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            orbImage.preserveAspect = true;
            orbImage.color = new Color(.57f, .28f, 1f, .95f);
            orbImage.raycastTarget = false;

            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(preview.transform, false);
            var labelRect = label.GetComponent<RectTransform>();
            Stretch(labelRect, .04f, .08f, .96f, .28f);
            var text = label.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = "MAGIC FLOW\nPREVIEW";
            text.color = new Color(.87f, .82f, 1f, 1f);
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12;
            text.fontSizeMax = 18;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
        }

        static void Stretch(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            Undo.RecordObject(rect, "Adjust Ruin 2 guided simulation layout");
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void ConfigureText(TMP_Text text, float min, float max, TextAlignmentOptions alignment, TextOverflowModes overflow, float lineSpacing)
        {
            Undo.RecordObject(text, "Configure guided simulation text");
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
            text.alignment = alignment;
            text.overflowMode = overflow;
            text.lineSpacing = lineSpacing;
            text.margin = new Vector4(4, 2, 4, 2);
        }

        static void ConfigureStaffText(Ruin02StaffDiagramView diagram)
        {
            foreach (var text in diagram.GetComponentsInChildren<TMP_Text>(true))
                ConfigureText(text, 13, 20, text.name == "Heading" ? TextAlignmentOptions.Center : TextAlignmentOptions.Left,
                              TextOverflowModes.Ellipsis, 1);
        }

        static void ConfigureButtonText(Button button)
        {
            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null) ConfigureText(text, 13, 17, TextAlignmentOptions.Center, TextOverflowModes.Ellipsis, 0);
        }

        static void LogRect(string name, RectTransform rect)
        {
            Debug.Log("[Ruin02GuidedLayout] " + name + " anchors=" + rect.anchorMin + ".." + rect.anchorMax +
                      " offsetMin=" + rect.offsetMin + " offsetMax=" + rect.offsetMax);
        }
    }
}
