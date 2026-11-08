using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    public static class RuinJumpUIAndDeveloperTools
    {
        const string ScenePath = "Assets/Scenes/MagicWorld_Development.unity";
        const string BackupPath = "LocalBackups/Ruin01JumpUIDevMode_20261108/BeforeLive.unity";
        const string FontPath = "Assets/Font/Chomage SDF.asset";

        [MenuItem("Tools/FYP/Ruin 1/Jump UI + Developer Mode/Install")]
        public static void Install()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
                throw new InvalidOperationException("Open MagicWorld_Development in Edit Mode.");
            var roots = scene.GetRootGameObjects();
            if (roots.Any(x => x.name == "EditorDeveloperMode") ||
                roots.SelectMany(x => x.GetComponentsInChildren<RuinDeveloperPanel>(true)).Any())
                throw new InvalidOperationException("Developer Mode is already installed.");

            Directory.CreateDirectory(Path.GetDirectoryName(BackupPath));
            if (!File.Exists(BackupPath) && !EditorSceneManager.SaveScene(scene, BackupPath, true))
                throw new IOException("Could not create the live scene backup.");
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not preserve the open scene before installation.");

            var canvas = Exact("Canvas");
            var player = Exact("Player");
            var camera = Exact("Main Camera").GetComponent<CameraFollow>();
            var hints = Exact("InteractionHintManager").GetComponent<InteractionHintManager>();
            var jump = Exact("Ruin01_RelocationRoot/Ruin01JumpRoute/JumpChallenge")
                .GetComponent<Ruin01MagicJumpController>();
            var hubSpawn = Exact("MainHub/Ruin1/Destination");
            var ruin01Start = Exact("Ruin01_RelocationRoot/Ruin01JumpRoute/JumpStartPoint");
            var ruin02Spawn = Exact("Ruin02_Area/SpawnPoints/EntrySpawn");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var movement = player.GetComponent<PlayerMovement>();
            var character = player.GetComponent<CharacterController>();
            if (canvas.GetComponent<Canvas>() == null || camera == null || hints == null || jump == null ||
                movement == null || character == null || font == null)
                throw new InvalidOperationException("Existing UI, player, jump or Chomage references are incomplete.");

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Install editor-only Developer Mode");
            try
            {
                var panel = CreatePanel(canvas, font, out var status, out var hub, out var ruin01,
                    out var ruin02, out var unlock, out var reset, out var close);
                var host = new GameObject("EditorDeveloperMode");
                Undo.RegisterCreatedObjectUndo(host, "Create Developer Mode host");
                var developer = Undo.AddComponent<RuinDeveloperPanel>(host);
                developer.panel = panel;
                developer.statusText = status;
                developer.hubButton = hub;
                developer.ruin01Button = ruin01;
                developer.ruin02Button = ruin02;
                developer.unlockJumpButton = unlock;
                developer.resetJumpButton = reset;
                developer.closeButton = close;
                developer.player = player;
                developer.playerMovement = movement;
                developer.cameraFollow = camera;
                developer.characterController = character;
                developer.mainHubSpawn = hubSpawn;
                developer.ruin01JumpStart = ruin01Start;
                developer.ruin02EntrySpawn = ruin02Spawn;
                developer.jumpController = jump;
                developer.interactionHints = hints;

                Undo.RecordObject(hints, "Register Developer Panel modal");
                hints.modalPanels = (hints.modalPanels ?? Array.Empty<GameObject>())
                    .Concat(new[] { panel }).Where(x => x != null).Distinct().ToArray();
                panel.SetActive(false);
                EditorUtility.SetDirty(developer);
                EditorUtility.SetDirty(hints);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new IOException("Could not save the Developer Mode scene setup.");
                Undo.CollapseUndoOperations(group);
                Debug.Log("Jump UI spoiler fix + editor-only Developer Mode installed. " +
                    "Hub=" + PathOf(hubSpawn) + ", Ruin1=" + PathOf(ruin01Start) +
                    ", Ruin2=" + PathOf(ruin02Spawn) + ", backup=" + BackupPath);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        [MenuItem("Tools/FYP/Ruin 1/Jump UI + Developer Mode/Verify Installed References")]
        public static void VerifyInstalled()
        {
            var developer = Exact("EditorDeveloperMode").GetComponent<RuinDeveloperPanel>();
            var hints = Exact("InteractionHintManager").GetComponent<InteractionHintManager>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (developer == null || developer.panel == null || developer.panel.activeSelf ||
                developer.statusText == null || developer.hubButton == null || developer.ruin01Button == null ||
                developer.ruin02Button == null || developer.unlockJumpButton == null ||
                developer.resetJumpButton == null || developer.closeButton == null ||
                developer.mainHubSpawn != Exact("MainHub/Ruin1/Destination") ||
                developer.ruin01JumpStart != Exact("Ruin01_RelocationRoot/Ruin01JumpRoute/JumpStartPoint") ||
                developer.ruin02EntrySpawn != Exact("Ruin02_Area/SpawnPoints/EntrySpawn") ||
                hints.modalPanels.Count(x => x == developer.panel) != 1 ||
                developer.panel.GetComponentsInChildren<TMP_Text>(true).Any(x => x.font != font))
                throw new InvalidOperationException("Developer Mode references or Chomage UI setup are invalid.");
            Debug.Log("PASS Developer Mode references: one hidden uGUI panel, Chomage text, three existing spawn markers, one modal registration.");
        }

        static GameObject CreatePanel(Transform canvas, TMP_FontAsset font, out TMP_Text status,
            out UnityEngine.UI.Button hub, out UnityEngine.UI.Button ruin01, out UnityEngine.UI.Button ruin02,
            out UnityEngine.UI.Button unlock, out UnityEngine.UI.Button reset, out UnityEngine.UI.Button close)
        {
            var panel = NewUI("DeveloperPanel", canvas);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(560, 500);
            rect.anchoredPosition = Vector2.zero;
            var image = Undo.AddComponent<UnityEngine.UI.Image>(panel);
            image.color = new Color(.035f, .025f, .075f, .96f);
            Text(panel.transform, "Title", "DEVELOPER MODE", font, 30, .06f, .87f, .94f, .97f,
                TextAlignmentOptions.Center);
            Text(panel.transform, "Subtitle", "EDITOR PLAY MODE ONLY  •  TAB TO TOGGLE", font, 17,
                .06f, .79f, .94f, .87f, TextAlignmentOptions.Center);
            hub = Button(panel.transform, "MainHubButton", "Teleport: Main Hub Player Spawn", font, .10f, .67f, .90f, .76f);
            ruin01 = Button(panel.transform, "Ruin01Button", "Teleport: Ruin 1 JumpStartPoint", font, .10f, .56f, .90f, .65f);
            ruin02 = Button(panel.transform, "Ruin02Button", "Teleport: Ruin 2 EntrySpawn", font, .10f, .45f, .90f, .54f);
            unlock = Button(panel.transform, "UnlockJumpButton", "Unlock Ruin 1 Jump (Session)", font, .10f, .34f, .90f, .43f);
            reset = Button(panel.transform, "ResetJumpButton", "Reset Ruin 1 Jump Attempt", font, .10f, .23f, .90f, .32f);
            close = Button(panel.transform, "CloseButton", "Close", font, .62f, .08f, .90f, .17f);
            status = Text(panel.transform, "Status", "Editor Play Mode shortcuts. No progression or rewards are changed.",
                font, 16, .08f, .07f, .58f, .19f, TextAlignmentOptions.MidlineLeft);
            return panel;
        }

        static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            return go;
        }

        static TMP_Text Text(Transform parent, string name, string value, TMP_FontAsset font, float size,
            float minX, float minY, float maxX, float maxY, TextAlignmentOptions alignment)
        {
            var go = NewUI(name, parent);
            Place((RectTransform)go.transform, minX, minY, maxX, maxY);
            var text = Undo.AddComponent<TextMeshProUGUI>(go);
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = 13;
            text.fontSizeMax = size;
            text.color = new Color(.94f, .91f, 1f);
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        static UnityEngine.UI.Button Button(Transform parent, string name, string label, TMP_FontAsset font,
            float minX, float minY, float maxX, float maxY)
        {
            var go = NewUI(name, parent);
            Place((RectTransform)go.transform, minX, minY, maxX, maxY);
            var image = Undo.AddComponent<UnityEngine.UI.Image>(go);
            image.color = new Color(.30f, .19f, .52f, .96f);
            var button = Undo.AddComponent<UnityEngine.UI.Button>(go);
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(.52f, .34f, .82f, 1f);
            colors.pressedColor = new Color(.22f, .13f, .42f, 1f);
            button.colors = colors;
            Text(go.transform, "Label", label, font, 19, .04f, .08f, .96f, .92f, TextAlignmentOptions.Center);
            return button;
        }

        static void Place(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static Transform Exact(string path)
        {
            var scene = EditorSceneManager.GetActiveScene();
            Transform current = null;
            foreach (var part in path.Split('/'))
            {
                current = current == null
                    ? scene.GetRootGameObjects().Single(x => x.name == part).transform
                    : current.Cast<Transform>().Single(x => x.name == part);
            }
            return current;
        }

        static string PathOf(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
    }
}
