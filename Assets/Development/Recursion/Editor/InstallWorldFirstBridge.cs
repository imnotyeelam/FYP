using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Fyp.Development.Recursion.Editor
{
    public static class InstallWorldFirstBridge
    {
        private const string ScenePath = "Assets/Scenes/MagicWorld_Development.unity";
        private const string MaterialPath = "Assets/Development/M_Ruin01_BridgeDemo.mat";

        [MenuItem("Tools/FYP/Ruin 1/Preview Observation (Play Mode)")]
        public static void Preview()
        {
            if (!EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                Debug.LogWarning("Enter Play Mode in MagicWorld_Development first. Preview does not move the player or change teleport destinations.");
                return;
            }
            var visualizer = Object.FindObjectsByType<RuinRecursionVisualizer>(FindObjectsSortMode.None)
                .Single(v => v.gameObject.scene.path == ScenePath);
            visualizer.Open();
        }

        [MenuItem("Tools/FYP/Ruin 1/Install World-First Presentation")]
        public static void Install()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
                throw new InvalidOperationException("Open MagicWorld_Development in Edit Mode. This installer never opens or saves scenes.");
            var visualizers = Object.FindObjectsByType<RuinRecursionVisualizer>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).Where(v => v.gameObject.scene == scene).ToArray();
            if (visualizers.Length != 1) throw new InvalidOperationException("Expected exactly one existing RuinRecursionVisualizer.");
            var visualizer = visualizers[0];
            var area = scene.GetRootGameObjects().Single(g => g.name == "Ruin01_Area").transform;
            var gameplay = area.Find("Gameplay");
            if (gameplay == null || visualizer.interactionPoint == null || visualizer.existingCanvas == null)
                throw new InvalidOperationException("Preserved Gameplay, observation point and Canvas references are required.");
            if (gameplay.Find("BridgeDemonstration") != null || visualizer.panelView != null ||
                (visualizer.worldViews != null && visualizer.worldViews.Length > 0) ||
                visualizer.existingCanvas.transform.Find("RecursionVisualizationPanel") != null)
            {
                Debug.LogWarning("World bridge or authored panel already exists. Nothing changed; edit its Inspector instead of installing twice.");
                return;
            }
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP/Lit is not available. No scene changes made.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null && material.shader != shader)
                throw new InvalidOperationException("Existing development material uses a different shader; inspect it before proceeding.");
            if (material == null)
            {
                if (System.IO.File.Exists(MaterialPath)) throw new InvalidOperationException("Material path is occupied; refusing to overwrite.");
                material = new Material(shader) { name = "M_Ruin01_BridgeDemo" };
                material.SetColor("_BaseColor", new Color(.42f, .46f, .50f));
                material.SetFloat("_Metallic", 0); material.SetFloat("_Smoothness", .25f);
                material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", Color.black);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Install Ruin 1 world-first bridge presentation");
            var root = new GameObject("BridgeDemonstration");
            root.transform.SetParent(gameplay, true);
            Undo.RegisterCreatedObjectUndo(root, "Create bridge demonstration");
            var floor = area.Find("Environment/Level1");
            var floorCollider = floor != null ? floor.GetComponent<Collider>() : null;
            float floorY = floorCollider != null ? floorCollider.bounds.max.y : visualizer.interactionPoint.position.y - .5f;
            Vector3 position = visualizer.interactionPoint.position + Vector3.forward * 12;
            position.y = floorY + .7f;
            root.transform.position = position;
            var bridge = root.AddComponent<BridgeWorldView>();
            var banks = new List<Renderer>();
            banks.Add(Cube("NearBank", root.transform, new Vector3(0, 0, -6.5f), new Vector3(5, .6f, 4), material).GetComponent<Renderer>());
            banks.Add(Cube("FarBank", root.transform, new Vector3(0, 0, 6.5f), new Vector3(5, .6f, 4), material).GetComponent<Renderer>());
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 3; i++)
                {
                    float height = .25f * (3 - i);
                    banks.Add(Cube((side < 0 ? "NearStep_" : "FarStep_") + (i + 1), root.transform,
                        new Vector3(0, -.7f + height / 2, side * (9 + i)), new Vector3(5, height, 1), material).GetComponent<Renderer>());
                }
            bridge.banksAndSteps = banks.ToArray();
            var poses = Empty("StonePoses", root.transform, Vector3.zero);
            var stones = Empty("Stones", root.transform, Vector3.zero);
            bridge.stones = new BridgeStoneView[3];
            Vector3[] broken = { new Vector3(-5, 2, -3), new Vector3(5, 3.5f, 0), new Vector3(-4, 2.8f, 4) };
            for (int i = 0; i < 3; i++)
            {
                string name = "Stone_" + (i + 1) + "_placeStone_" + (3 - i);
                var brokenPose = Empty(name + "_Broken", poses, broken[i]);
                brokenPose.localRotation = Quaternion.Euler(12 * (i - 1), 15 * (i - 1), 10 * (i - 1));
                var repairedPose = Empty(name + "_Repaired", poses, new Vector3(0, 0, (i - 1) * 3));
                var stone = Cube(name, stones, broken[i], new Vector3(4, .6f, 3), material);
                stone.transform.localRotation = brokenPose.localRotation;
                var view = stone.AddComponent<BridgeStoneView>();
                view.brokenPose = brokenPose; view.repairedPose = repairedPose;
                view.stoneRenderer = stone.GetComponent<MeshRenderer>();
                view.crossingCollider = stone.GetComponent<BoxCollider>();
                view.crossingCollider.enabled = false;
                bridge.stones[i] = view;
            }

            // Verify real Transform/Collider wiring before installing the new UI layout.
            ValidateSnapshots(bridge);

            var cameraObject = new GameObject("BridgeObservationCamera");
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = new Vector3(-15, 13, -17);
            cameraObject.transform.LookAt(root.transform.position);
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false; camera.orthographic = true; camera.orthographicSize = 10;
            camera.nearClipPlane = .1f; camera.farClipPlane = 200;
            camera.rect = new Rect(0, 0, .65f, 1);
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;
            cameraData.renderShadows = true;
            // No AudioListener: the existing player's listener remains the only listener.

            var hint = Object.FindObjectsByType<InteractionHintManager>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).Single(m => m.gameObject.scene == scene);
            TMP_FontAsset font = hint.hintText != null ? hint.hintText.font : TMP_Settings.defaultFontAsset;
            var panel = RecursionPanelView.Create(visualizer.existingCanvas.transform, font);
            Undo.RegisterCreatedObjectUndo(panel.gameObject, "Create editable recursion panel");
            panel.gameObject.SetActive(false);
            Undo.RecordObject(visualizer, "Connect bridge presentation");
            visualizer.panelView = panel;
            visualizer.worldViews = new RecursionWorldView[] { bridge };
            visualizer.observationBridge = bridge;
            visualizer.observationCamera = camera;
            visualizer.gameplayCamera = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true))
                .FirstOrDefault(c => c != camera && c.CompareTag("MainCamera"));
            visualizer.useObservationCamera = true;
            Undo.RecordObject(hint, "Register recursion modal panel");
            var modals = hint.modalPanels != null ? hint.modalPanels.ToList() : new List<GameObject>();
            modals.Add(panel.gameObject); hint.modalPanels = modals.ToArray();
            EditorUtility.SetDirty(visualizer); EditorUtility.SetDirty(hint);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = root;
            Debug.Log("World-first bridge installed in the OPEN development scene. No scene was saved. " +
                "Three reusable stones, 22 original checkpoints, editable right panel, fixed camera. " +
                "Review and save MagicWorld_Development when ready. Undo reverses scene installation.", root);
        }

        private static Transform Empty(string name, Transform parent, Vector3 localPosition)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition; return go.transform;
        }

        private static void ValidateSnapshots(BridgeWorldView bridge)
        {
            var trace = BridgeRecursionLesson.Create();
            if (trace.Steps.Count != 22) throw new InvalidOperationException("The original 22-checkpoint lesson changed.");
            try
            {
                for (int direction = 0; direction < 2; direction++)
                    for (int index = 0; index < trace.Steps.Count; index++)
                    {
                        var step = trace.Steps[direction == 0 ? index : trace.Steps.Count - 1 - index];
                        bridge.ApplySnapshot(step, true);
                        for (int i = 0; i < bridge.stones.Length; i++)
                        {
                            var stone = bridge.stones[i];
                            bool placed = i < step.ActionCount;
                            Transform target = placed ? stone.repairedPose : stone.brokenPose;
                            if ((stone.transform.position - target.position).sqrMagnitude > .000001f ||
                                Quaternion.Angle(stone.transform.rotation, target.rotation) > .01f ||
                                stone.crossingCollider.enabled != placed)
                                throw new InvalidOperationException("Bridge snapshot pose/collider validation failed at " + index);
                        }
                    }
                Debug.Log("PASS: 22 bridge checkpoints forward and backward; three persistent stones have correct poses and collider states.");
            }
            finally { bridge.ApplySnapshot(trace.Steps[0], true); }
        }

        private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position;
            go.transform.localScale = size; go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }
    }
}
