using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fyp.Development.Recursion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fyp.Development.Environment.Editor
{
    // Authoring/QA only. Never changes source prefabs, materials or gameplay components.
    public static class RuinOneDressing
    {
        private const string ScenePath = "Assets/Scenes/MagicWorld_Development.unity";
        private const string GroupName = "Ruin01_Dressing_Pass01";
        private const string F = "Assets/FantasyForest/Prefabs/";

        private readonly struct Placement
        {
            public readonly string Name, Asset;
            public readonly Vector3 Position;
            public readonly float Yaw, Height, Footprint;
            public Placement(string name, string asset, Vector3 position, float yaw, float height, float footprint)
            { Name = name; Asset = asset; Position = position; Yaw = yaw; Height = height; Footprint = footprint; }
        }

        [Serializable] private sealed class InstanceReport
        {
            public string name, prefab;
            public Vector3 position, eulerAngles, scale, meshSize;
            public int disabledColliders, disabledLights;
        }
        [Serializable] private sealed class DressingReport
        {
            public bool sceneWasInitiallyDirty, saved;
            public InstanceReport[] instances;
            public string[] skipped;
        }

        private sealed class TransformRecord
        {
            public Transform transform, parent;
            public Vector3 position, scale;
            public Quaternion rotation;
        }

        private static Bounds MeshBounds(GameObject root)
        {
            bool any = false; var bounds = new Bounds(root.transform.position, Vector3.zero);
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                Bounds mesh = filter.sharedMesh.bounds;
                foreach (Vector3 corner in Corners(mesh))
                {
                    Vector3 world = renderer.transform.TransformPoint(corner);
                    if (!any) { bounds = new Bounds(world, Vector3.zero); any = true; }
                    else bounds.Encapsulate(world);
                }
            }
            if (!any) throw new InvalidOperationException("No readable mesh bounds for " + root.name);
            return bounds;
        }

        private static IEnumerable<Vector3> Corners(Bounds bounds)
        {
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                        yield return bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
        }

        private static BridgeWorldView Bridge()
        {
            return Object.FindObjectsByType<BridgeWorldView>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(b => b.gameObject.scene.path == ScenePath);
        }

        private static Placement[] Plan(BridgeWorldView bridge)
        {
            var scene = bridge.gameObject.scene;
            var visualizer = Object.FindObjectsByType<RuinRecursionVisualizer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(v => v.gameObject.scene == scene);
            var book = Object.FindObjectsByType<KnowledgeInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(k => k.gameObject.scene == scene);
            Vector3 b = bridge.transform.position, o = visualizer.interactionPoint.position, kpos = book.transform.position;
            float ground = bridge.transform.parent.parent.Find("Environment/Level1").GetComponent<Collider>().bounds.max.y;
            Func<Vector3, float, float, Vector3> at = (anchor, dx, dz) => new Vector3(anchor.x + dx, ground, anchor.z + dz);
            return new[] {
                new Placement("NearBank_Rock_West", F+"Landscape/Rock_03.prefab", at(b,-4,-8.5f), 25, .65f, 2.4f),
                new Placement("NearBank_Rock_East", F+"Landscape/Rock_03.prefab", at(b,4,-8.5f), -35, .65f, 2.4f),
                new Placement("FarBank_Rock_West", F+"Landscape/Rock_05.prefab", at(b,-4,8.5f), 50, .65f, 2.4f),
                new Placement("FarBank_Rock_East", F+"Landscape/Rock_05.prefab", at(b,4,8.5f), -25, .65f, 2.4f),
                new Placement("Background_Tree_Right", F+"Vegetation/Tree_01.prefab", at(b,9,11.5f), 20, 5.2f, 5),
                new Placement("Background_Tree_Left", F+"Vegetation/Tree_04.prefab", at(b,-9,13), -20, 5.5f, 5),
                new Placement("Observation_MagicRock", F+"Landscape/MagicRock_01.prefab", at(o,-2,-1.8f), 15, 1.05f, 1.2f),
                new Placement("Observation_Brick_Left", F+"Ruins/Brick_01.prefab", at(o,-1,-1.8f), 15, .35f, .8f),
                new Placement("Observation_Brick_Right", F+"Ruins/Brick_01.prefab", at(o,1,-1.8f), -20, .35f, .8f),
                new Placement("Spellbook_Lantern", F+"Props/Lantern_01.prefab", at(kpos,1.8f,2.5f), 180, 1.3f, .8f),
                new Placement("Observation_Plant_Left", F+"Vegetation/Plant_03.prefab", at(o,-3,-3.2f), 35, .4f, .8f),
                new Placement("Observation_Plant_Right", F+"Vegetation/Plant_03.prefab", at(o,3,-3.2f), -35, .4f, .8f)
            };
        }

        [MenuItem("Tools/FYP/Ruin 1/Inspect Dressing Candidates")]
        public static void Inspect()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Inspect from the development scene in Edit Mode.");
            foreach (string path in Plan(Bridge()).Select(p => p.Asset).Distinct())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) { Debug.LogWarning("Unavailable dressing asset: " + path); continue; }
                Bounds bounds = MeshBounds(prefab);
                var colliders = prefab.GetComponentsInChildren<Collider>(true);
                Debug.Log("DRESSING CANDIDATE " + path + " | mesh size=" + bounds.size.ToString("F3") +
                    " | colliders=" + string.Join(",", colliders.Select(c => c.GetType().Name)) +
                    " | shaders=" + string.Join(",", prefab.GetComponentsInChildren<MeshRenderer>(true)
                        .SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.shader.name).Distinct()));
            }
        }

        [MenuItem("Tools/FYP/Ruin 1/Apply Limited Dressing Pass")]
        public static void Apply()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Open MagicWorld_Development in Edit Mode.");
            var bridge = Bridge();
            var environment = bridge.transform.parent.parent.Find("Environment");
            if (environment.Find(GroupName) != null) { Debug.LogWarning("Dressing already exists; no duplicates created."); return; }
            bool dirty = scene.isDirty;
            var originals = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Select(t => new TransformRecord { transform=t, parent=t.parent, position=t.localPosition,
                    rotation=t.localRotation, scale=t.localScale }).ToArray();
            var behaviours = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true))
                .Where(m => m != null).ToDictionary(m => m, EditorJsonUtility.ToJson);
            var camera = Object.FindObjectsByType<RuinRecursionVisualizer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(v => v.gameObject.scene == scene).observationCamera;
            Vector3 cameraPosition = camera.transform.position;
            var sweep = SweptBounds(bridge);
            var plan = Plan(bridge); var reports = new List<InstanceReport>(); var skipped = new List<string>();
            Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Ruin 1 minimal environment dressing");
            var group = new GameObject(GroupName); group.transform.SetParent(environment, false);
            Undo.RegisterCreatedObjectUndo(group, "Create Ruin 1 decorative group");
            try
            {
                foreach (var placement in plan)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(placement.Asset);
                    if (prefab == null) { skipped.Add(placement.Name+": prefab missing"); continue; }
                    var materials = prefab.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials).ToArray();
                    if (materials.Any(m => m == null || m.shader == null || !m.shader.isSupported ||
                        m.shader.name != "Universal Render Pipeline/Lit") || prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                    { skipped.Add(placement.Name+": unexpected shader/script dependency"); continue; }
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group.transform);
                    Undo.RegisterCreatedObjectUndo(instance, "Add " + placement.Name);
                    instance.name = placement.Name;
                    instance.transform.SetPositionAndRotation(placement.Position, Quaternion.Euler(0, placement.Yaw, 0));
                    Bounds native = MeshBounds(instance);
                    float scale = Mathf.Min(placement.Height / Mathf.Max(.001f,native.size.y),
                        placement.Footprint / Mathf.Max(.001f,Mathf.Max(native.size.x,native.size.z)));
                    instance.transform.localScale *= scale;
                    Bounds fitted = MeshBounds(instance);
                    instance.transform.position += Vector3.up * (placement.Position.y - fitted.min.y);
                    fitted = MeshBounds(instance);
                    // Disabled scene-instance colliders cannot block interaction routes or bridge motion.
                    var colliders = instance.GetComponentsInChildren<Collider>(true);
                    foreach (var collider in colliders) { collider.enabled=false; PrefabUtility.RecordPrefabInstancePropertyModifications(collider); }
                    var lights = instance.GetComponentsInChildren<Light>(true);
                    foreach (var light in lights) { light.enabled=false; PrefabUtility.RecordPrefabInstancePropertyModifications(light); }
                    if (sweep.Any(s => s.Intersects(fitted)) || OccludesBridge(fitted, bridge, cameraPosition))
                    {
                        skipped.Add(placement.Name+": conservative bridge envelope/camera clearance check");
                        Undo.DestroyObjectImmediate(instance); continue;
                    }
                    PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                    reports.Add(new InstanceReport { name=placement.Name, prefab=placement.Asset,
                        position=instance.transform.position, eulerAngles=instance.transform.eulerAngles,
                        scale=instance.transform.localScale, meshSize=fitted.size,
                        disabledColliders=colliders.Length, disabledLights=lights.Length });
                }
                if (reports.Count < 8) throw new InvalidOperationException("Fewer than 8 candidates passed safety checks; undoing this dressing pass.");
                foreach (var record in originals)
                    if (record.transform.parent != record.parent || record.transform.localPosition != record.position ||
                        record.transform.localRotation != record.rotation || record.transform.localScale != record.scale)
                        throw new InvalidOperationException("An existing transform changed: " + record.transform.name);
                foreach (var pair in behaviours)
                    if (EditorJsonUtility.ToJson(pair.Key) != pair.Value)
                        throw new InvalidOperationException("An existing gameplay component changed: " + pair.Key.name);
                Undo.CollapseUndoOperations(undo);
                EditorSceneManager.MarkSceneDirty(scene);
                bool saved = !dirty && EditorSceneManager.SaveScene(scene, ScenePath);
                Directory.CreateDirectory("Temp/Ruin1DressingChecks");
                File.WriteAllText("Temp/Ruin1DressingChecks/placement_report.json", JsonUtility.ToJson(new DressingReport {
                    sceneWasInitiallyDirty=dirty, saved=saved, instances=reports.ToArray(), skipped=skipped.ToArray() }, true));
                Selection.activeGameObject = group;
                Debug.Log("DRESSING PASS: " + reports.Count + " prefab instances. Original transforms and gameplay references unchanged. " +
                    "All new colliders disabled; no source asset writes. Saved=" + saved + ". " +
                    (dirty ? "Existing unsaved work preserved; scene deliberately left unsaved. " : "") +
                    "Skipped: " + string.Join("; ",skipped));
            }
            catch { Undo.RevertAllDownToGroup(undo); throw; }
        }

        private static List<Bounds> SweptBounds(BridgeWorldView bridge)
        {
            var bounds = new List<Bounds>();
            foreach (var stone in bridge.stones)
                for (int sample=0; sample<=20; sample++)
                {
                    float t=sample/20f;
                    var matrix=Matrix4x4.TRS(Vector3.Lerp(stone.brokenPose.position,stone.repairedPose.position,t),
                        Quaternion.Slerp(stone.brokenPose.rotation,stone.repairedPose.rotation,t),stone.transform.lossyScale);
                    bool first=true; Bounds b=default;
                    foreach(var corner in Corners(stone.GetComponent<MeshFilter>().sharedMesh.bounds))
                    { var point=matrix.MultiplyPoint3x4(corner);if(first){b=new Bounds(point,Vector3.zero);first=false;}else b.Encapsulate(point); }
                    b.Expand(.3f); bounds.Add(b);
                }
            return bounds;
        }

        private static bool OccludesBridge(Bounds decoration, BridgeWorldView bridge, Vector3 cameraPosition)
        {
            Vector3 forward=(bridge.ObservationBounds().center-cameraPosition).normalized;
            foreach(var stone in bridge.stones)
                for(int sample=0; sample<=20; sample++)
                {
                    float t=sample/20f;
                    var matrix=Matrix4x4.TRS(Vector3.Lerp(stone.brokenPose.position,stone.repairedPose.position,t),
                        Quaternion.Slerp(stone.brokenPose.rotation,stone.repairedPose.rotation,t),stone.transform.lossyScale);
                    foreach(var corner in Corners(stone.GetComponent<MeshFilter>().sharedMesh.bounds).Concat(new[]{Vector3.zero}))
                    {
                        Vector3 target=matrix.MultiplyPoint3x4(corner);
                        float depth=Vector3.Dot(target-cameraPosition,forward);
                        if(decoration.IntersectRay(new Ray(target-forward*depth,forward),out float distance) && distance<depth-.05f)
                            return true;
                    }
                }
            return false;
        }
    }
}
