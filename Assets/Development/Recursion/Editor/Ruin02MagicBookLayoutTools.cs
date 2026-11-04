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
    /// Explicit, scene-only layout maintenance for the Ruin 2 three-page knowledge book.
    /// It deliberately does not inspect or change the standalone guided-simulation panel.
    /// </summary>
    public static class Ruin02MagicBookLayoutTools
    {
        const string ScenePath = "Assets/Scenes/MagicWorld_Development.unity";
        const string BackupFolder = "LocalBackups/Ruin02MagicBookLayout_20261105";

        [MenuItem("Tools/FYP/Ruin 2/Magic Book/Audit 3-Page Layout")]
        public static void Audit()
        {
            var controller = FindController();
            ValidateBook(controller);

            LogRect("Panel", controller.spellbookPanel.GetComponent<RectTransform>());
            LogRect("Title", (RectTransform)controller.titleText.transform);
            LogRect("Knowledge", (RectTransform)controller.knowledgeText.transform);
            LogRect("Code", (RectTransform)controller.codeText.transform);
            LogRect("Page number", (RectTransform)controller.pageNumberText.transform);
            LogRect("Previous / left arrow", (RectTransform)controller.previousButton.transform);
            LogRect("Next / right arrow", (RectTransform)controller.nextButton.transform);
            Debug.Log("[Ruin02MagicBookLayout] Audit complete. The controller retains the existing button mapping: " +
                      "left arrow = Previous, right arrow = Next.");
        }

        [MenuItem("Tools/FYP/Ruin 2/Magic Book/Apply 3-Page Layout")]
        public static void Apply()
        {
            var scene = RequireCleanDevelopmentScene();
            var controller = FindController();
            ValidateBook(controller);

            Directory.CreateDirectory(BackupFolder);
            var backup = BackupFolder + "/BeforeLayout.unity";
            if (!EditorSceneManager.SaveScene(scene, backup, true))
                throw new IOException("Could not create Ruin 2 Magic Book scene backup: " + backup);

            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Lay out Ruin 2 Magic Book pages 1-3");
            try
            {
                // The parchment is 1250 x 1000 and centred in the full-screen panel.
                // These fixed regions stay inside it at the CanvasScaler reference size (1920 x 1080).
                SetCentered((RectTransform)controller.titleText.transform, new Vector2(0f, 330f), new Vector2(900f, 88f));
                SetCentered((RectTransform)controller.knowledgeText.transform, new Vector2(0f, 135f), new Vector2(900f, 230f));
                SetCentered((RectTransform)controller.codeText.transform, new Vector2(0f, -145f), new Vector2(900f, 235f));
                SetCentered((RectTransform)controller.pageNumberText.transform, new Vector2(0f, -408f), new Vector2(210f, 36f));

                // Existing sprite assignments already identify these correctly:
                // previousButton has Button_left_B; nextButton has Button_right_B.
                SetCentered((RectTransform)controller.previousButton.transform, new Vector2(-540f, -15f), new Vector2(72f, 72f));
                SetCentered((RectTransform)controller.nextButton.transform, new Vector2(540f, -15f), new Vector2(72f, 72f));

                ConfigureTitle(controller.titleText);
                ConfigureBody(controller.knowledgeText);
                ConfigureCode(controller.codeText);
                ConfigurePageNumber(controller.pageNumberText);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new IOException("Could not save the Ruin 2 Magic Book layout.");
                Undo.CollapseUndoOperations(group);
                Debug.Log("[Ruin02MagicBookLayout] Applied only the Ruin 2 Magic Book pages 1-3 layout. " +
                          "Backup: " + backup);
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
                throw new InvalidOperationException("Exit Play Mode before changing the Ruin 2 Magic Book layout.");
            if (scene.path != ScenePath)
                throw new InvalidOperationException("Open " + ScenePath + " before applying this layout.");
            if (scene.isDirty)
                throw new InvalidOperationException("The scene has unsaved changes. Save or back up those changes first; this tool will not overwrite them.");
            return scene;
        }

        static Ruin02SpellbookUI FindController()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var controllers = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Ruin02SpellbookUI>(true))
                .ToArray();
            if (controllers.Length != 1)
                throw new InvalidOperationException("Expected exactly one Ruin02SpellbookUI, found " + controllers.Length + ".");
            return controllers[0];
        }

        static void ValidateBook(Ruin02SpellbookUI controller)
        {
            if (controller.spellbookPanel == null || controller.spellbookPanel.name != "Ruin02AncientSpellbookPanel")
                throw new InvalidOperationException("The Ruin 2 book panel reference is missing or does not target Ruin02AncientSpellbookPanel.");
            if (controller.titleText == null || controller.knowledgeText == null || controller.codeText == null ||
                controller.pageNumberText == null || controller.previousButton == null || controller.nextButton == null)
                throw new InvalidOperationException("A required Ruin 2 Magic Book text or navigation reference is missing.");

            var panel = controller.spellbookPanel.transform;
            if (controller.titleText.transform.parent != panel || controller.knowledgeText.transform.parent != panel ||
                controller.codeText.transform.parent != panel || controller.pageNumberText.transform.parent != panel ||
                controller.previousButton.transform.parent != panel || controller.nextButton.transform.parent != panel)
                throw new InvalidOperationException("The Ruin 2 Magic Book references no longer belong directly to its panel; refusing to affect another UI.");

            var previousImage = controller.previousButton.GetComponent<Image>();
            var nextImage = controller.nextButton.GetComponent<Image>();
            if (previousImage == null || nextImage == null || previousImage.sprite == null || nextImage.sprite == null ||
                previousImage.sprite.name.IndexOf("left", StringComparison.OrdinalIgnoreCase) < 0 ||
                nextImage.sprite.name.IndexOf("right", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException("Navigation sprite mapping is not left=Previous and right=Next; no layout change was made.");
        }

        static void SetCentered(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            Undo.RecordObject(rect, "Adjust Ruin 2 Magic Book layout");
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        static void ConfigureTitle(TMP_Text text)
        {
            Undo.RecordObject(text, "Configure Ruin 2 title text");
            text.enableAutoSizing = true;
            text.fontSizeMin = 26;
            text.fontSizeMax = 42;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.margin = Vector4.zero;
        }

        static void ConfigureBody(TMP_Text text)
        {
            Undo.RecordObject(text, "Configure Ruin 2 body text");
            text.enableAutoSizing = true;
            text.fontSizeMin = 18;
            text.fontSizeMax = 26;
            text.fontStyle = FontStyles.Normal;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.lineSpacing = 3;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.margin = new Vector4(8, 4, 8, 4);
        }

        static void ConfigureCode(TMP_Text text)
        {
            Undo.RecordObject(text, "Configure Ruin 2 code text");
            text.enableAutoSizing = true;
            text.fontSizeMin = 16;
            text.fontSizeMax = 22;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.lineSpacing = 0;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.margin = new Vector4(10, 6, 10, 6);
        }

        static void ConfigurePageNumber(TMP_Text text)
        {
            Undo.RecordObject(text, "Configure Ruin 2 page number");
            text.enableAutoSizing = false;
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Center;
            text.overflowMode = TextOverflowModes.Overflow;
            text.margin = Vector4.zero;
        }

        static void LogRect(string label, RectTransform rect)
        {
            Debug.Log("[Ruin02MagicBookLayout] " + label + " anchors=" + rect.anchorMin + ".." + rect.anchorMax +
                      " position=" + rect.anchoredPosition + " size=" + rect.sizeDelta);
        }
    }
}
