using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin02SpellbookLayoutTools
    {
        const string ScenePath = "Assets/Scenes/MagicWorld_Development.unity";
        const string Output = "LocalBackups/Ruin2SpellbookFix_20261030";

        static UnityEngine.SceneManagement.Scene Scene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
                throw new InvalidOperationException("Open MagicWorld_Development in Edit Mode first.");
            return scene;
        }

        static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;

        [MenuItem("Tools/FYP/Ruin 2/Spellbook UI/Audit and Back Up Live Scene")]
        public static void AuditAndBackup()
        {
            var scene = Scene();
            Directory.CreateDirectory(Output);
            File.Copy(ScenePath, Output + "/BeforeSaved.unity", true);
            if (!EditorSceneManager.SaveScene(scene, Output + "/BeforeLive.unity", true))
                throw new IOException("Live scene backup failed.");
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var panel = all.Single(t => t.name == "Ruin02AncientSpellbookPanel");
            var controller = all.Select(t => t.GetComponent<Ruin02SpellbookUI>()).Single(c => c != null);
            var report = new StringBuilder();
            report.AppendLine("sceneDirty=" + scene.isDirty + " panelActive=" + panel.gameObject.activeSelf);
            foreach (var t in panel.GetComponentsInChildren<RectTransform>(true))
            {
                var text = t.GetComponent<TMP_Text>();
                var image = t.GetComponent<UnityEngine.UI.Image>();
                var button = t.GetComponent<UnityEngine.UI.Button>();
                report.AppendLine(PathOf(t) + " active=" + t.gameObject.activeSelf +
                    " anchors=" + t.anchorMin + "/" + t.anchorMax +
                    " offsets=" + t.offsetMin + "/" + t.offsetMax +
                    " size=" + t.rect.size + " worldRect=" + WorldRect(t) +
                    (text != null ? " font=" + text.fontSize + " auto=" + text.enableAutoSizing + " text=" + text.text.Replace("\n", "\\n") : "") +
                    (image != null ? " sprite=" + (image.sprite == null ? "null" : image.sprite.name) : "") +
                    (button != null ? " persistentClicks=" + button.onClick.GetPersistentEventCount() : ""));
            }
            report.AppendLine("title=" + PathOf(controller.titleText.transform));
            report.AppendLine("knowledge=" + PathOf(controller.knowledgeText.transform));
            report.AppendLine("code=" + PathOf(controller.codeText.transform));
            report.AppendLine("page=" + PathOf(controller.pageNumberText.transform));
            report.AppendLine("previous=" + PathOf(controller.previousButton.transform));
            report.AppendLine("next=" + PathOf(controller.nextButton.transform));
            report.AppendLine("simulation=" + PathOf(controller.guidedSimulation.transform));
            File.WriteAllText(Output + "/LiveAudit.txt", report.ToString());
            Debug.Log("Ruin 2 spellbook UI live audit and backup saved to " + Output);
        }

        static string WorldRect(RectTransform t)
        {
            var corners = new Vector3[4];
            t.GetWorldCorners(corners);
            return corners[0] + ".." + corners[2];
        }

        static void Place(RectTransform rect, Vector2 anchors, Vector2 lower, Vector2 upper)
        {
            Undo.RecordObject(rect, "Space Ruin 2 spellbook page");
            rect.anchorMin = anchors;
            rect.anchorMax = anchors;
            rect.offsetMin = lower;
            rect.offsetMax = upper;
        }

        [MenuItem("Tools/FYP/Ruin 2/Spellbook UI/Fix Page Layout and Arrows")]
        public static void FixPageLayoutAndArrows()
        {
            var scene = Scene();
            if (!File.Exists(Output + "/BeforeLive.unity"))
                throw new InvalidOperationException("Back up the live scene before fixing the UI.");
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var panel = all.Single(t => t.name == "Ruin02AncientSpellbookPanel");
            var controller = all.Select(t => t.GetComponent<Ruin02SpellbookUI>()).Single(c => c != null);
            var left = panel.Find("NextButton").GetComponent<UnityEngine.UI.Button>();
            var right = panel.Find("PreviousButton (1)").GetComponent<UnityEngine.UI.Button>();
            if (left.GetComponent<UnityEngine.UI.Image>().sprite.name != "Button_left_B" ||
                right.GetComponent<UnityEngine.UI.Image>().sprite.name != "Button_right_B" ||
                left.onClick.GetPersistentEventCount() != 0 || right.onClick.GetPersistentEventCount() != 0)
                throw new InvalidOperationException("Arrow visual or callbacks differ from the audited state.");
            if (controller.previousButton != right || controller.nextButton != left ||
                controller.spellbookPanel != panel.gameObject)
                throw new InvalidOperationException("Ruin 2 button references changed since audit.");

            Undo.RecordObject(controller, "Correct Ruin 2 arrow references");
            controller.previousButton = left;
            controller.nextButton = right;
            Place((RectTransform)controller.knowledgeText.transform, new Vector2(.5f, 1f),
                new Vector2(-500f, -450f), new Vector2(500f, -230f));
            Place((RectTransform)controller.codeText.transform, new Vector2(.5f, 0f),
                new Vector2(-475f, 160f), new Vector2(475f, 430f));
            Place((RectTransform)controller.pageNumberText.transform, new Vector2(.5f, 0f),
                new Vector2(-150f, 50f), new Vector2(150f, 110f));
            var simulation = (RectTransform)controller.guidedSimulation.transform;
            Undo.RecordObject(simulation, "Pad Ruin 2 simulation page");
            simulation.anchorMin = new Vector2(.24f, .15f);
            simulation.anchorMax = new Vector2(.76f, .73f);
            simulation.offsetMin = Vector2.zero;
            simulation.offsetMax = Vector2.zero;
            Undo.RecordObject(controller.knowledgeText, "Fit Ruin 2 lesson text");
            controller.knowledgeText.enableAutoSizing = true;
            controller.knowledgeText.fontSizeMin = 28;
            controller.knowledgeText.fontSizeMax = 35;
            Undo.RecordObject(controller.codeText, "Fit Ruin 2 code text");
            controller.codeText.enableAutoSizing = true;
            controller.codeText.fontSizeMin = 23;
            controller.codeText.fontSizeMax = 36;
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Ruin 2 spellbook layout and arrow references updated in the live scene. Save after visual verification.");
        }

        [MenuItem("Tools/FYP/Ruin 2/Spellbook UI/Run Play Navigation Checks")]
        public static void RunPlayNavigationChecks()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            var scene = EditorSceneManager.GetActiveScene();
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var controller = all.Select(t => t.GetComponent<Ruin02SpellbookUI>()).Single(c => c != null);
            var interaction = all.Select(t => t.GetComponent<Ruin02BookInteraction>()).Single(c => c != null);
            if (controller.previousButton.transform.position.x >= controller.nextButton.transform.position.x)
                throw new InvalidOperationException("Previous button is not left of Next button.");
            interaction.InteractFromShared();
            if (!controller.IsBookOpen || controller.pageNumberText.text != "1 / 4" || controller.previousButton.interactable)
                throw new InvalidOperationException("Ruin 2 book did not open on page 1 with disabled Previous.");
            for (int page = 2; page <= 4; page++)
            {
                controller.nextButton.onClick.Invoke();
                if (controller.pageNumberText.text != page + " / 4")
                    throw new InvalidOperationException("Right arrow did not advance to page " + page);
            }
            if (controller.nextButton.interactable) throw new InvalidOperationException("Next remains active on page 4.");
            for (int page = 3; page >= 1; page--)
            {
                controller.previousButton.onClick.Invoke();
                if (controller.pageNumberText.text != page + " / 4")
                    throw new InvalidOperationException("Left arrow did not return to page " + page);
            }
            controller.closeButton.onClick.Invoke();
            if (controller.IsBookOpen) throw new InvalidOperationException("Close button did not close book.");
            interaction.InteractFromShared();
            if (!controller.IsBookOpen || controller.pageNumberText.text != "1 / 4")
                throw new InvalidOperationException("Reopening Ruin 2 book failed.");
            controller.CloseBook();
            Debug.Log("PASS Ruin 2 spellbook navigation: left Previous, right Next, page limits, close and reopen.");
        }

        [MenuItem("Tools/FYP/Ruin 2/Spellbook UI/Darken Code Text")]
        public static void DarkenCodeText()
        {
            var scene = Scene();
            var controller = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Select(t => t.GetComponent<Ruin02SpellbookUI>()).Single(c => c != null);
            Undo.RecordObject(controller.codeText, "Improve Ruin 2 code contrast");
            controller.codeText.color = new Color(.19f, .13f, .09f, 1f);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Ruin 2 spellbook code contrast improved in the live scene.");
        }

        [MenuItem("Tools/FYP/Ruin 2/Spellbook UI/Position Player by Book (Play Only)")]
        public static void PositionPlayerByBook()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            var scene = EditorSceneManager.GetActiveScene();
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var book = all.Select(t => t.GetComponent<Ruin02BookInteraction>()).Single(c => c != null);
            var teleport = all.Select(t => t.GetComponent<TeleportManager>()).Single(c => c != null);
            var player = teleport.player;
            var controller = player.GetComponent<CharacterController>();
            var landing = new Vector3(797.6f, 10.25f, 192.7f);
            var facing = book.transform.position - landing;
            facing.y = 0;
            if (controller != null) controller.enabled = false;
            player.SetPositionAndRotation(landing, Quaternion.LookRotation(facing));
            if (controller != null) controller.enabled = true;
            Physics.SyncTransforms();
            Debug.Log("Play-only player positioning near the existing Ruin 2 book; press E in Game View.");
        }
    }
}
