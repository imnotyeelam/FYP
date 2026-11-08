using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin01MagicJumpF2ATools
    {
        const string ScenePath = "Assets/Scenes/MagicWorld_Development.unity";
        const string Backup = "LocalBackups/Ruin01JumpF2A_20261108/BeforeLive.unity";
        const string RefineBackup = "LocalBackups/Ruin01JumpRefine_20261108/BeforeLive.unity";
        const string TriggerMaterialPath = "Assets/Development/Recursion/Materials/Ruin01MagicTrigger.mat";
        const string PlatformBackup = "LocalBackups/Ruin01MagicPlatform_20261108/BeforeLive.unity";

        [MenuItem("Tools/FYP/Ruin 1/Jump F2-A/Convert Triggers to Hidden Platforms")]
        public static void ConvertTriggersToPlatforms()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
                throw new InvalidOperationException("Open the Ruin 1 development scene in Edit Mode.");
            var root = scene.GetRootGameObjects().Single(g => g.name == "Ruin01_RelocationRoot").transform;
            var route = root.Find("Ruin01JumpRoute");
            var challenge = route == null ? null : route.Find("JumpChallenge");
            var controller = challenge == null ? null : challenge.GetComponent<Ruin01MagicJumpController>();
            if (controller == null || controller.destinations == null || controller.destinations.Length != 5 ||
                controller.magicTriggers == null || controller.magicTriggers.Length != 5 || challenge.childCount != 5)
                throw new InvalidOperationException("Expected exactly five existing route destinations and Magic Triggers.");
            for (int i = 0; i < 5; i++)
                if (controller.magicTriggers[i] == null || controller.magicTriggers[i].transform.parent != challenge ||
                    controller.magicTriggers[i].name != "MagicTrigger0" + (i + 1) || controller.destinations[i] == null)
                    throw new InvalidOperationException("Existing Magic Trigger references are incomplete or reordered.");

            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var cylinderMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(primitive);
            if (controller.magicTriggers.All(t => t.GetComponent<MeshFilter>().sharedMesh == cylinderMesh &&
                !t.GetComponent<MeshRenderer>().enabled))
            {
                Debug.Log("Ruin 1 Magic Triggers are already hidden platforms.");
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(PlatformBackup));
            if (File.Exists(PlatformBackup)) throw new IOException("Live platform backup already exists; refusing to overwrite it.");
            if (!EditorSceneManager.SaveScene(scene, PlatformBackup, true) || !EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not back up and save current live scene edits.");

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Convert Ruin 1 Magic Triggers to hidden platforms");
            for (int i = 0; i < 5; i++)
            {
                var trigger = controller.magicTriggers[i];
                var meshFilter = trigger.GetComponent<MeshFilter>();
                var renderer = trigger.GetComponent<MeshRenderer>();
                var collider = trigger.GetComponent<SphereCollider>();
                if (meshFilter == null || renderer == null || collider == null)
                    throw new InvalidOperationException("Magic Trigger " + (i + 1) + " is missing its original components.");
                Undo.RecordObjects(new UnityEngine.Object[] { trigger.transform, meshFilter, renderer, collider },
                    "Reshape existing Magic Trigger");
                meshFilter.sharedMesh = cylinderMesh;
                trigger.transform.localScale = new Vector3(3.2f, .06f, 3.2f);
                trigger.transform.position = controller.destinations[i].position - Vector3.up * .06f;
                collider.enabled = false;
                renderer.enabled = false;
            }
            Undo.RecordObject(controller, "Set Magic Platform pause");
            controller.platformPauseSeconds = .9f;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save Magic Platforms.");
            Undo.CollapseUndoOperations(group);
            Debug.Log("Ruin 1 five Magic Triggers converted to hidden, visual-only platforms. Backup: " + PlatformBackup);
        }

        [MenuItem("Tools/FYP/Ruin 1/Jump F2-A/Tune Trigger Material")]
        public static void TuneTriggerMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(TriggerMaterialPath);
            if (material == null) throw new InvalidOperationException("Magic Trigger material is missing.");
            material.SetColor("_BaseColor", new Color(.72f, .42f, 1f, .38f));
            material.SetColor("_EmissionColor", new Color(.3f, .1f, .5f));
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            Debug.Log("Ruin 1 Magic Trigger material tuned; emission keyword=" + material.IsKeywordEnabled("_EMISSION"));
        }

        [MenuItem("Tools/FYP/Ruin 1/Jump F2-A/Install Trigger Refinement")]
        public static void Refine()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
                throw new InvalidOperationException("Open the saved Ruin 1 development scene in Edit Mode.");
            var root = scene.GetRootGameObjects().Single(g => g.name == "Ruin01_RelocationRoot").transform;
            var route = root.Find("Ruin01JumpRoute");
            var challenge = route == null ? null : route.Find("JumpChallenge");
            var controller = challenge == null ? null : challenge.GetComponent<Ruin01MagicJumpController>();
            if (controller == null || controller.destinations == null || controller.destinations.Length != 5 ||
                controller.destinations.Any(x => x == null) || route.Find("JumpStartPoint") != controller.jumpStartPoint)
                throw new InvalidOperationException("Existing F1 route or F2-A controller references are incomplete.");
            if (challenge.childCount != 0 || (controller.magicTriggers != null && controller.magicTriggers.Length != 0))
                throw new InvalidOperationException("Trigger children or references already exist; refusing duplicates.");
            Directory.CreateDirectory(Path.GetDirectoryName(RefineBackup));
            if (File.Exists(RefineBackup)) throw new IOException("Live refinement backup already exists; refusing to overwrite it.");
            if (!EditorSceneManager.SaveScene(scene, RefineBackup, true) || !EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not preserve the live scene before refinement.");

            var material = AssetDatabase.LoadAssetAtPath<Material>(TriggerMaterialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit shader is unavailable.");
                if (!AssetDatabase.IsValidFolder("Assets/Development/Recursion/Materials"))
                    AssetDatabase.CreateFolder("Assets/Development/Recursion", "Materials");
                material = new Material(shader) { name = "Ruin01MagicTrigger" };
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.SetFloat("_Smoothness", .72f);
                material.SetColor("_BaseColor", new Color(.72f, .42f, 1f, .38f));
                material.SetColor("_EmissionColor", new Color(.3f, .1f, .5f));
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.EnableKeyword("_EMISSION");
                material.renderQueue = (int)RenderQueue.Transparent;
                AssetDatabase.CreateAsset(material, TriggerMaterialPath);
            }
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Refine Ruin 1 recursive jump triggers");
            var triggers = new Ruin01MagicJumpTrigger[5];
            for (int i = 0; i < triggers.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Undo.RegisterCreatedObjectUndo(go, "Create Magic Trigger");
                go.name = "MagicTrigger0" + (i + 1);
                go.layer = 2; // Ignore Raycast; the scripted segment completion owns the reaction.
                go.transform.SetParent(challenge, false);
                go.transform.position = controller.destinations[i].position + Vector3.up * 1.05f;
                go.transform.localScale = Vector3.one * 2.4f;
                go.GetComponent<SphereCollider>().isTrigger = true;
                go.GetComponent<MeshRenderer>().sharedMaterial = material;
                triggers[i] = Undo.AddComponent<Ruin01MagicJumpTrigger>(go);
            }
            Undo.RecordObject(controller, "Assign Magic Triggers");
            controller.magicTriggers = triggers;
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save refined jump scene.");
            Undo.CollapseUndoOperations(group);
            Debug.Log("Ruin 1 F2-A trigger refinement saved. Backup: " + RefineBackup);
        }

        [MenuItem("Tools/FYP/Ruin 1/Jump F2-A/Install Functional Prototype")]
        public static void Install()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
                throw new InvalidOperationException("Open the Ruin 1 development scene in Edit Mode.");
            var root = scene.GetRootGameObjects().Single(g => g.name == "Ruin01_RelocationRoot").transform;
            var route = root.Find("Ruin01JumpRoute");
            if (route == null || route.Find("JumpChallenge") != null ||
                scene.GetRootGameObjects().Any(g => g.name == "Ruin01JumpPanel"))
                throw new InvalidOperationException("F1 route missing or F2-A prototype already installed.");
            var start = route.Find("JumpStartPoint");
            var destinations = new[] { "MagicStep01", "MagicStep02", "MagicStep03", "MagicStep04", "JumpLandingPoint" }
                .Select(route.Find).ToArray();
            if (start == null || destinations.Any(x => x == null) || route.childCount != 6)
                throw new InvalidOperationException("Expected the six original F1 route markers.");
            var practice = root.Find("Ruin01_Area/Gameplay/BridgePractice")?.GetComponent<BridgePracticeController>();
            var player = scene.GetRootGameObjects().Single(g => g.name == "Player").transform;
            var camera = scene.GetRootGameObjects().Single(g => g.name == "Main Camera").GetComponent<CameraFollow>();
            var hint = scene.GetRootGameObjects().Single(g => g.name == "InteractionHintManager")
                .GetComponent<InteractionHintManager>();
            var canvas = scene.GetRootGameObjects().Single(g => g.name == "Canvas").transform;
            if (practice == null || !hint.coordinateInteractions || player.GetComponent<PlayerMovement>() == null ||
                player.GetComponent<CharacterController>() == null || camera == null || hint.hintText == null)
                throw new InvalidOperationException("Existing bridge, player, camera or interaction references are incomplete.");

            Directory.CreateDirectory(Path.GetDirectoryName(Backup));
            if (File.Exists(Backup)) throw new IOException("F2-A live backup already exists; refusing to overwrite it.");
            if (!EditorSceneManager.SaveScene(scene, Backup, true) || !EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not back up and save all live scene edits.");

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Install Ruin 1 F2-A jump prototype");
            try
            {
                var panel = CreatePanel(canvas, hint.hintText.font, out var input, out var cast, out var close, out var status);
                var go = new GameObject("JumpChallenge");
                Undo.RegisterCreatedObjectUndo(go, "Create F2-A jump controller");
                go.transform.SetParent(route, false);
                var controller = Undo.AddComponent<Ruin01MagicJumpController>(go);
                controller.bridgePractice = practice;
                controller.player = player;
                controller.playerMovement = player.GetComponent<PlayerMovement>();
                controller.cameraFollow = camera;
                controller.characterController = player.GetComponent<CharacterController>();
                controller.jumpStartPoint = start;
                controller.destinations = destinations;
                controller.panel = panel;
                controller.input = input;
                controller.castButton = cast;
                controller.closeButton = close;
                controller.statusText = status;
                Undo.RecordObject(hint, "Register F2-A shared jump interaction");
                hint.interactionTargets = (hint.interactionTargets ?? Array.Empty<MonoBehaviour>())
                    .Concat(new MonoBehaviour[] { controller }).Where(x => x != null).Distinct().ToArray();
                hint.modalPanels = (hint.modalPanels ?? Array.Empty<GameObject>())
                    .Concat(new[] { panel }).Where(x => x != null).Distinct().ToArray();
                panel.SetActive(false);
                EditorUtility.SetDirty(controller);
                EditorUtility.SetDirty(hint);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save F2-A scene setup.");
                Undo.CollapseUndoOperations(group);
                Debug.Log("Ruin 1 F2-A installed. Backup: " + Backup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        static GameObject CreatePanel(Transform canvas, TMP_FontAsset font, out TMP_InputField input,
            out Button cast, out Button close, out TMP_Text status)
        {
            var panel = NewUI("Ruin01JumpPanel", canvas);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.sizeDelta = new Vector2(440, 258);
            rect.anchoredPosition = new Vector2(-20, -20);
            var background = Undo.AddComponent<Image>(panel);
            background.color = new Color(.025f, .045f, .075f, .91f);
            Text(panel.transform, "Title", "RECURSIVE MAGIC JUMP", font, 22, .04f, .79f, .96f, .96f);
            Text(panel.transform, "Prompt", "Choose recursion depth n (1–6)", font, 17, .05f, .65f, .95f, .78f);
            var field = NewUI("NInput", panel.transform);
            Place((RectTransform)field.transform, .05f, .46f, .29f, .63f);
            var fieldImage = Undo.AddComponent<Image>(field);
            fieldImage.color = new Color(.92f, .95f, 1f, 1f);
            input = Undo.AddComponent<TMP_InputField>(field);
            var viewport = NewUI("Text Area", field.transform);
            Place((RectTransform)viewport.transform, .08f, .08f, .92f, .92f);
            var inputText = Text(viewport.transform, "Text", "", font, 20, 0, 0, 1, 1);
            inputText.color = new Color(.04f, .07f, .12f, 1f);
            inputText.alignment = TextAlignmentOptions.Center;
            var placeholder = Text(viewport.transform, "Placeholder", "n", font, 20, 0, 0, 1, 1);
            placeholder.color = new Color(.4f, .44f, .5f, .7f);
            placeholder.alignment = TextAlignmentOptions.Center;
            input.textViewport = (RectTransform)viewport.transform;
            input.textComponent = inputText;
            input.placeholder = placeholder;
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 1;
            cast = Button(panel.transform, "CastButton", "Cast", font, .32f, .46f, .66f, .63f);
            close = Button(panel.transform, "CloseButton", "Close", font, .69f, .46f, .95f, .63f);
            status = Text(panel.transform, "ExecutionStatus", "", font, 16, .05f, .07f, .95f, .41f);
            status.alignment = TextAlignmentOptions.TopLeft;
            return panel;
        }

        static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            Undo.RegisterCreatedObjectUndo(go, "Create F2-A jump UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        static void Place(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static TextMeshProUGUI Text(Transform parent, string name, string value, TMP_FontAsset font,
            float size, float minX, float minY, float maxX, float maxY)
        {
            var go = NewUI(name, parent);
            Place((RectTransform)go.transform, minX, minY, maxX, maxY);
            var text = Undo.AddComponent<TextMeshProUGUI>(go);
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }

        static Button Button(Transform parent, string name, string label, TMP_FontAsset font,
            float minX, float minY, float maxX, float maxY)
        {
            var go = NewUI(name, parent);
            Place((RectTransform)go.transform, minX, minY, maxX, maxY);
            var image = Undo.AddComponent<Image>(go);
            image.color = new Color(.12f, .36f, .5f, 1f);
            var button = Undo.AddComponent<Button>(go);
            button.targetGraphic = image;
            Text(go.transform, "Label", label, font, 17, 0, 0, 1, 1);
            return button;
        }
    }
}
