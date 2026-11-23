using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin02FarmH3Tools
    {
        static Transform Exact(string path) => FloatingIslandPrototypeTools.Exact(path);
        [MenuItem("Tools/FYP/Ruin 2/Magical Farm H3/Install Console")]
        public static void Install()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/MagicWorld_Development.unity")
                throw new InvalidOperationException("Open development scene in Edit Mode.");
            var farm = Exact("Ruin02_MagicalFarm");
            var marker = farm.Find("HarvestConsole");
            if (marker.GetComponent<Ruin02HarvestConsole>() != null) throw new InvalidOperationException("Console already installed.");
            var hints = Exact("InteractionHintManager").GetComponent<InteractionHintManager>();
            var wizard = Exact("Player");
            var inputSource = Exact("Canvas").GetComponentsInChildren<TMP_InputField>(true).First();
            var font = hints.hintText.font;
            var codeFont = AssetDatabase.FindAssets("t:TMP_FontAsset").Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.IndexOf("Type", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>).FirstOrDefault() ?? font;
            Directory.CreateDirectory("LocalBackups/Ruin02FarmH3_20261119");
            if (!EditorSceneManager.SaveScene(scene, $"LocalBackups/Ruin02FarmH3_20261119/Before_{DateTime.Now:HHmmssfff}.unity", true))
                throw new IOException("Scene backup failed.");
            var controller = Undo.AddComponent<Ruin02HarvestConsole>(marker.gameObject);
            var root = Rect("MagicHarvestConsolePanel", Exact("Canvas"), new Vector2(.66f, .08f), new Vector2(.99f, .94f));
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Add harvest panel");
            root.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.035f, .065f, .11f, .95f);
            Label(root, "Title", "Magic Harvest Console", .90f, .98f, font, 28);
            controller.code = Label(root, "Code", "", .55f, .89f, codeFont, 23);
            controller.state = Label(root, "State", "", .35f, .54f, font, 22);
            controller.feedback = Label(root, "Feedback", "", .20f, .34f, font, 21);
            var input = UnityEngine.Object.Instantiate(inputSource.gameObject, root).GetComponent<TMP_InputField>();
            input.name = "AnswerInput";
            var inputRect = (RectTransform)input.transform;
            inputRect.anchorMin = new Vector2(.06f, .13f); inputRect.anchorMax = new Vector2(.94f, .19f);
            inputRect.offsetMin = inputRect.offsetMax = Vector2.zero;
            input.onValueChanged = new TMP_InputField.OnChangeEvent();
            input.onEndEdit = new TMP_InputField.SubmitEvent();
            input.onSubmit = new TMP_InputField.SubmitEvent();
            input.text = "";
            input.characterLimit = 100;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.textComponent.font = codeFont;
            if (input.placeholder is TMP_Text placeholder) { placeholder.text = "Missing expression"; placeholder.font = font; }
            controller.input = input;
            controller.submit = Button(root, "Submit", "Run row", .07f, .125f, font);
            controller.close = Button(root, "Close", "Close [ESC]", -.055f, .025f, font);
            // Keep the close control inside the panel's lower edge.
            var closeRect = (RectTransform)controller.close.transform;
            closeRect.anchorMin = new Vector2(.06f, .005f); closeRect.anchorMax = new Vector2(.94f, .065f);
            controller.panel = root.gameObject;
            controller.simulation = farm.GetComponent<Ruin02FarmSimulation>();
            controller.player = wizard;
            controller.movement = wizard.GetComponent<PlayerMovement>();
            controller.cameraFollow = Exact("Main Camera").GetComponent<CameraFollow>();
            controller.cameraView = farm.Find("FarmCameraView");
            root.gameObject.SetActive(false);
            Undo.RecordObject(hints, "Register farm shared interaction");
            hints.interactionTargets = hints.interactionTargets.Append(controller).ToArray();
            hints.modalPanels = hints.modalPanels.Append(root.gameObject).ToArray();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Farm H3 console installed, registered and saved.");
        }

        static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
        static TMP_Text Label(Transform parent, string name, string text, float min, float max, TMP_FontAsset font, int size)
        {
            var label = Rect(name, parent, new Vector2(.06f, min), new Vector2(.94f, max)).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.text = text; label.fontSize = size;
            label.enableAutoSizing = true; label.fontSizeMin = 16; label.color = Color.white; label.raycastTarget = false;
            return label;
        }
        static UnityEngine.UI.Button Button(Transform parent, string name, string text, float min, float max, TMP_FontAsset font)
        {
            var rect = Rect(name, parent, new Vector2(.06f, min), new Vector2(.94f, max));
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(.16f, .28f, .4f);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            var label = Label(rect, "Label", text, 0, 1, font, 22); label.alignment = TextAlignmentOptions.Center;
            return button;
        }
    }
}
