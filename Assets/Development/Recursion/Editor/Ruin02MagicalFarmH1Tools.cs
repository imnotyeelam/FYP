using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin02MagicalFarmH1Tools
    {
        const string ScenePath = "Assets/Scenes/MagicWorld_Development.unity";
        const string FarmName = "Ruin02_MagicalFarm";
        const string SoilPath = "Assets/Development/FW01_URP/Materials/M_FW01_Soil_01_63f1a6c9_URP.mat";
        const string FarmSoilPath = "Assets/Development/Recursion/Materials/Ruin02MagicalFarmSoil.mat";
        const string SoilTexturePath = "Assets/Fantasy Skybox FREE/Scenes/Textures (Terrain)/Texture_Dirt_Diffuse.png";
        const string PlatformPath = "Assets/Development/FW01_URP/Materials/M_fwOF_RocksClean_01_3099a060_URP.mat";
        const string CropFolder = "Assets/GabrielAguiarProductions/UniqueProjectilesVol_1/";
        const string Output = "Temp/Ruin02MagicalFarmH1";
        static readonly Vector3 Site = new Vector3(800f, 50f, 340f);
        // A C marks an occupied plot, from Plot01 to Plot10 in each row.
        static readonly string[] Occupancy = { "C.CC..C.C.", ".C..CC.C.C", "CC...C..CC" };
        static readonly Vector3 Entry = new Vector3(0f, .12f, -5.8f);
        static readonly Vector3 ReturnPoint = new Vector3(3.4f, .12f, -5.45f);
        static readonly Vector3 Console = new Vector3(0f, 0f, -4.35f);
        static readonly Vector3 CameraPosition = new Vector3(0f, 13f, -13f);
        static readonly Vector3 CameraTarget = new Vector3(0f, 0f, -.2f);
        const float CameraFov = 50f;

        [MenuItem("Tools/FYP/Ruin 2/Magical Farm H1/Install Environment Foundation")]
        public static void Install()
        {
            var scene = CurrentScene();
            Require(!scene.GetRootGameObjects().Any(g => g.name == FarmName), "Farm already exists. Use Verify instead.");
            var soil = GetFarmSoil();
            var slab = AssetDatabase.LoadAssetAtPath<Material>(PlatformPath);
            var carrot = AssetDatabase.LoadAllAssetsAtPath(CropFolder + "Models/ProjectileCarrot01.fbx").OfType<Mesh>().Single();
            var cropMaterial = AssetDatabase.LoadAssetAtPath<Material>(CropFolder + "Materials/Carrot.mat");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Require(soil != null && slab != null && cropMaterial != null && font != null, "Existing farm assets are missing.");

            Physics.SyncTransforms();
            var player = FindPlayer();
            VerifyClearSite(scene, player);
            const string backups = "LocalBackups/Ruin02MagicalFarmH1_20261119";
            Directory.CreateDirectory(backups);
            string backup = $"{backups}/Before_{DateTime.Now:HHmmssfff}.unity";
            Require(EditorSceneManager.SaveScene(scene, backup, true), "Could not back up the live scene.");

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Ruin 2 Magical Farm foundation");
            try
            {
                var farm = new GameObject(FarmName);
                Undo.RegisterCreatedObjectUndo(farm, "Create Magical Farm");
                var root = farm.transform;
                root.position = Site;

                var platform = Block(root, "FarmPlatform", new Vector3(0f, -.65f, 0f),
                    new Vector3(28f, 1.3f, 14f), slab);
                platform.gameObject.AddComponent<BoxCollider>();
                Marker(root, "HarvestConsole", Console);
                Marker(root, "FarmEntrySpawn", Entry);
                Marker(root, "FarmReturnPoint", ReturnPoint);
                var cameraView = Marker(root, "FarmCameraView", CameraPosition);
                cameraView.rotation = Quaternion.LookRotation(CameraTarget - CameraPosition);

                for (int rowIndex = 0; rowIndex < 3; rowIndex++)
                {
                    var row = Marker(root, $"Row{rowIndex + 1:00}", new Vector3(0f, 0f, -1.8f + 2.8f * rowIndex));
                    Number(row, "RowLabel", $"ROW {rowIndex + 1}", new Vector3(-12.1f, .4f, -.55f), .42f, font, cameraView);
                    for (int plotIndex = 0; plotIndex < 10; plotIndex++)
                    {
                        var plot = Marker(row, $"Plot{plotIndex + 1:00}", new Vector3((plotIndex - 4.5f) * 2.2f, 0f, 0f));
                        Block(plot, "Soil", new Vector3(0f, .09f, 0f), new Vector3(1.82f, .18f, 1.8f), soil);
                        Number(plot, "PlotNumber", (plotIndex + 1).ToString("00"), new Vector3(0f, .27f, -.7f),
                            .31f, font, cameraView);
                        if (Occupancy[rowIndex][plotIndex] == 'C') Crop(plot, carrot, cropMaterial);
                    }
                }

                Physics.SyncTransforms();
                string report = VerifyFarm(root, player);
                Capture(root);
                Undo.CollapseUndoOperations(group);
                EditorSceneManager.MarkSceneDirty(scene);
                Require(EditorSceneManager.SaveScene(scene), "Could not save the farm scene.");
                File.WriteAllText(Output + "/Verification.txt", report + "\nScene backup: " + backup);
                Selection.activeGameObject = farm;
                Debug.Log(report + "\nScene backup: " + backup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        [MenuItem("Tools/FYP/Ruin 2/Magical Farm H1/Verify and Capture Camera")]
        public static void Verify()
        {
            var root = CurrentScene().GetRootGameObjects().Single(g => g.name == FarmName).transform;
            Physics.SyncTransforms();
            Debug.Log(VerifyFarm(root, FindPlayer()));
            Capture(root);
        }

        [MenuItem("Tools/FYP/Ruin 2/Magical Farm H1/Apply Soil Variant and Camera Framing")]
        public static void ApplyPresentation()
        {
            var scene = CurrentScene();
            var root = scene.GetRootGameObjects().Single(g => g.name == FarmName).transform;
            var material = GetFarmSoil();
            var cameraView = root.Find("FarmCameraView");
            var player = FindPlayer();
            Physics.SyncTransforms();
            VerifyClearSite(scene, player);
            Undo.RecordObject(cameraView, "Frame the full farm");
            cameraView.localPosition = CameraPosition;
            cameraView.rotation = Quaternion.LookRotation(CameraTarget - CameraPosition);
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.name == "Soil")
                {
                    Undo.RecordObject(renderer, "Apply the farm-local soil material");
                    renderer.sharedMaterial = material;
                }
                else if (renderer.GetComponent<TextMesh>() != null)
                {
                    Undo.RecordObject(renderer.transform, "Face the farm label toward the camera");
                    renderer.transform.rotation = Quaternion.LookRotation(renderer.transform.position - cameraView.position);
                }
            }
            string report = VerifyFarm(root, player);
            Capture(root);
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene), "Could not save the farm presentation.");
            File.WriteAllText(Output + "/Verification.txt", report);
            Debug.Log(report);
        }

        static Material GetFarmSoil()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(FarmSoilPath);
            if (existing != null) return existing;
            var original = AssetDatabase.LoadAssetAtPath<Material>(SoilPath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SoilTexturePath);
            Require(original != null && texture != null, "The source soil material or dirt texture is missing.");
            var material = new Material(original) { name = "Ruin02MagicalFarmSoil" };
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", new Color(.75f, .56f, .39f));
            material.SetFloat("_Smoothness", .12f);
            AssetDatabase.CreateAsset(material, FarmSoilPath);
            return material;
        }

        static Scene CurrentScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            Require(scene.path == ScenePath && !EditorApplication.isPlayingOrWillChangePlaymode,
                "Open MagicWorld_Development in Edit Mode.");
            return scene;
        }

        static CharacterController FindPlayer() => FloatingIslandPrototypeTools.Exact("Player").GetComponent<CharacterController>();

        static void VerifyClearSite(Scene scene, CharacterController player)
        {
            var volume = new Bounds(Site + new Vector3(0f, 5.5f, -2.5f), new Vector3(30f, 17f, 24f));
            var overlap = scene.GetRootGameObjects().Where(g => g.name != "SkyDome" && g.name != FarmName)
                .SelectMany(g => g.GetComponentsInChildren<Renderer>(false))
                .Where(r => r.enabled && r.bounds.Intersects(volume)).Select(r => r.name).Distinct().ToArray();
            Require(overlap.Length == 0, "Candidate intersects rendered geometry: " + string.Join(", ", overlap));
            Require(Physics.OverlapBox(volume.center, volume.extents, Quaternion.identity, ~0,
                QueryTriggerInteraction.Ignore).All(c => c.transform.root.name == FarmName),
                "Candidate or camera volume intersects a scene collider.");
            foreach (var marker in new[] { Entry, ReturnPoint, Console + Vector3.up * .12f })
                VerifyCapsule(Site + marker, player);
        }

        static string VerifyFarm(Transform root, CharacterController player)
        {
            var platform = root.Find("FarmPlatform").GetComponent<BoxCollider>();
            Require(platform != null && platform.enabled && !platform.isTrigger &&
                root.GetComponentsInChildren<Collider>().Length == 1, "Expected one solid platform collider.");
            var cameraView = root.Find("FarmCameraView");
            var visiblePlots = 0;
            var crops = 0;
            float rowDepth = -1.8f;
            for (int rowIndex = 0; rowIndex < 3; rowIndex++)
            {
                var row = root.Find($"Row{rowIndex + 1:00}");
                Require(row != null && row.Cast<Transform>().Count(t => t.name.StartsWith("Plot")) == 10,
                    "Each row needs exactly ten plots.");
                Require(Mathf.Abs(row.localPosition.z - rowDepth) < .01f, "Plot rows are out of alignment.");
                rowDepth += 2.8f;
                for (int plotIndex = 0; plotIndex < 10; plotIndex++)
                {
                    var plot = row.Find($"Plot{plotIndex + 1:00}");
                    var soil = plot.Find("Soil").GetComponent<MeshRenderer>();
                    var crop = plot.Find("Carrot");
                    Require(soil.enabled && soil.gameObject.activeInHierarchy &&
                        AssetDatabase.GetAssetPath(soil.sharedMaterial) == FarmSoilPath &&
                        Mathf.Abs(soil.bounds.min.y - platform.bounds.max.y) < .01f, "Soil must sit on the platform.");
                    Require((crop != null) == (Occupancy[rowIndex][plotIndex] == 'C'), "Occupancy pattern differs.");
                    if (crop != null)
                    {
                        var bounds = crop.GetComponent<MeshRenderer>().bounds;
                        Require(bounds.min.y < soil.bounds.max.y && bounds.max.y > soil.bounds.max.y + .35f,
                            "Carrot does not emerge from the soil.");
                        crops++;
                    }
                    var fromCamera = Quaternion.Inverse(cameraView.rotation) * (soil.bounds.center - cameraView.position);
                    float halfHeight = fromCamera.z * Mathf.Tan(CameraFov * Mathf.Deg2Rad * .5f);
                    Require(fromCamera.z > 0 && Mathf.Abs(fromCamera.x) + .9f < halfHeight * 16f / 9f &&
                        Mathf.Abs(fromCamera.y) + .9f < halfHeight, "Plot is outside the gameplay camera frame.");
                    Require(!Physics.Linecast(cameraView.position, soil.bounds.center + Vector3.up * .2f, ~0,
                        QueryTriggerInteraction.Ignore), "Plot sightline is blocked.");
                    visiblePlots++;
                }
            }
            Require(root.Cast<Transform>().Count(t => t.name.StartsWith("Row")) == 3, "Expected exactly three rows.");
            foreach (var position in new[] { root.Find("FarmEntrySpawn").position, root.Find("FarmReturnPoint").position,
                root.Find("HarvestConsole").position + Vector3.up * .12f })
            {
                Require(platform.Raycast(new Ray(position + Vector3.up, Vector3.down), out var hit, 1.3f) &&
                    Vector3.Dot(hit.normal, Vector3.up) > .99f, "Marker has no flat platform support.");
                VerifyCapsule(position, player);
            }
            return $"PASS Magical Farm H1: site overlap clear; 3 rows, {visiblePlots} framed plots, {crops} carrots, " +
                $"{visiblePlots - crops} empty. Solid floor and entry/return/console capsule checks passed.\n" +
                $"Origin {root.position:F2}; entry {root.Find("FarmEntrySpawn").position:F2}; " +
                $"console {root.Find("HarvestConsole").position:F2}; return {root.Find("FarmReturnPoint").position:F2}; camera {cameraView.position:F2}.";
        }

        static void VerifyCapsule(Vector3 position, CharacterController player)
        {
            float radius = player.radius + player.skinWidth;
            var center = position + player.center;
            var axis = Vector3.up * Mathf.Max(0f, player.height * .5f - player.radius);
            Require(Physics.OverlapCapsule(center - axis, center + axis, radius, ~0, QueryTriggerInteraction.Ignore).Length == 0,
                $"Player capsule is blocked at {position:F2}.");
        }

        static void Capture(Transform root)
        {
            Directory.CreateDirectory(Output);
            var preview = EditorUtility.CreateGameObjectWithHideFlags("Farm QA camera", HideFlags.HideAndDontSave, typeof(Camera));
            var camera = preview.GetComponent<Camera>();
            var view = root.Find("FarmCameraView");
            preview.transform.SetPositionAndRotation(view.position, view.rotation);
            camera.enabled = false;
            camera.farClipPlane = 85f;
            camera.fieldOfView = CameraFov;
            var rt = RenderTexture.GetTemporary(1280, 720, 24);
            var previous = RenderTexture.active;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(Output + "/FarmCameraView.png", image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        static Transform Marker(Transform parent, string name, Vector3 localPosition)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = localPosition;
            return child;
        }

        static Transform Block(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var block = Marker(parent, name, position);
            block.localScale = scale;
            block.gameObject.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            block.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            return block;
        }

        static void Number(Transform parent, string name, string value, Vector3 position, float size, Font font, Transform view)
        {
            var marker = Marker(parent, name, position);
            marker.rotation = Quaternion.LookRotation(marker.position - view.position);
            var text = marker.gameObject.AddComponent<TextMesh>();
            text.font = font;
            text.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 64;
            text.characterSize = size / 6.4f;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(1f, .91f, .67f);
            text.text = value;
        }

        static void Crop(Transform plot, Mesh mesh, Material material)
        {
            var crop = Marker(plot, "Carrot", Vector3.zero);
            var bounds = mesh.bounds;
            Vector3 axis = bounds.size.y >= bounds.size.x && bounds.size.y >= bounds.size.z ? Vector3.up :
                bounds.size.x >= bounds.size.z ? Vector3.right : Vector3.forward;
            // The narrow end of the mesh is the root tip, so keep it toward the soil.
            float end = Vector3.Dot(bounds.extents, axis) * .60f;
            var positive = mesh.vertices.Where(v => Vector3.Dot(v - bounds.center, axis) > end).ToArray();
            var negative = mesh.vertices.Where(v => Vector3.Dot(v - bounds.center, axis) < -end).ToArray();
            float Width(Vector3[] points) => points.Length == 0 ? 0f :
                points.Max(v => Vector3.ProjectOnPlane(v - bounds.center, axis).sqrMagnitude);
            if (Width(positive) < Width(negative)) axis = -axis;
            crop.localRotation = Quaternion.FromToRotation(axis, Vector3.up);
            float scale = .95f / Vector3.Dot(bounds.size, new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z)));
            crop.localScale = Vector3.one * scale;
            crop.localPosition = Vector3.up * .54f - crop.localRotation * (bounds.center * scale);
            crop.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            crop.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Magical Farm H1: " + message);
        }
    }
}
