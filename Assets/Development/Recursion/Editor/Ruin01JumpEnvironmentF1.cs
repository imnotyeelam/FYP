using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    /// <summary>Live, Edit Mode collider audit and marker-only setup for Ruin 1 F1.</summary>
    public static class Ruin01JumpEnvironmentF1
    {
        const string BackupDirectory = "LocalBackups/Ruin01JumpF1_20261108";
        const string BackupScene = BackupDirectory + "/BeforeLive.unity";
        const string Report = BackupDirectory + "/RouteAudit.txt";
        const string RouteName = "Ruin01JumpRoute";
        const float FootOffset = .08f;

        struct Route
        {
            public Transform root, start, target;
            public MeshCollider startCollider, targetCollider;
            public CharacterController player;
            public Vector3 takeoff, landing;
            public Vector3[] points;
            public float arcHeight;
            public int airSamples;
        }

        static void Require(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException("Ruin 1 F1: " + message);
        }

        static Route Locate()
        {
            var scene = EditorSceneManager.GetActiveScene();
            Require(scene.path == "Assets/Scenes/MagicWorld_Development.unity", "open MagicWorld_Development.unity first");
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit Mode is required");
            var root = scene.GetRootGameObjects().Single(g => g.name == "Ruin01_RelocationRoot").transform;
            var start = root.Find("flying_stone_003");
            var target = root.Find("Ruin01_JumpTargetStone");
            Require(start != null && target != null && start != target, "both distinct jump islands are required");
            var startCollider = start.GetComponent<MeshCollider>();
            var targetCollider = target.GetComponent<MeshCollider>();
            Require(startCollider != null && targetCollider != null && startCollider.enabled && targetCollider.enabled &&
                    !startCollider.isTrigger && !targetCollider.isTrigger, "both jump islands need active solid MeshColliders");
            Require(AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(start.gameObject)) ==
                    "Assets/ithappy/Fantasy/Prefabs/flying_stone/flying_stone_003.prefab" &&
                    AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(target.gameObject)) ==
                    "Assets/ithappy/Fantasy/Prefabs/flying_stone/flying_stone_003.prefab", "the jump islands must retain their source prefab");
            var player = scene.GetRootGameObjects().Single(g => g.name == "Player").GetComponent<CharacterController>();
            Require(player != null, "Player CharacterController is missing");
            Physics.SyncTransforms();
            return new Route { root = root, start = start, target = target, startCollider = startCollider,
                targetCollider = targetCollider, player = player };
        }

        static bool SafeFoot(Route route, MeshCollider surface, float x, float z, out Vector3 foot)
        {
            foot = default;
            float rayY = surface.bounds.max.y + 5f;
            if (!surface.Raycast(new Ray(new Vector3(x, rayY, z), Vector3.down), out var centre, 100f) ||
                Vector3.Angle(centre.normal, Vector3.up) > route.player.slopeLimit) return false;
            float span = route.player.radius + .25f;
            foreach (float dx in new[] { -span, 0f, span })
            foreach (float dz in new[] { -span, 0f, span })
            {
                if (!surface.Raycast(new Ray(new Vector3(x + dx, rayY, z + dz), Vector3.down), out var hit, 100f) ||
                    Vector3.Angle(hit.normal, Vector3.up) > route.player.slopeLimit ||
                    Mathf.Abs(hit.point.y - centre.point.y) > .22f) return false;
            }
            foot = centre.point + Vector3.up * FootOffset;
            return !Blockers(route, foot, surface).Any();
        }

        static Collider[] Blockers(Route route, Vector3 feet, MeshCollider allowedSurface = null)
        {
            var centre = feet + route.player.center;
            float half = Mathf.Max(0f, route.player.height * .5f - route.player.radius);
            return Physics.OverlapCapsule(centre + Vector3.up * half, centre - Vector3.up * half,
                    route.player.radius + .05f, ~0, QueryTriggerInteraction.Ignore)
                .Where(c => c != allowedSurface && c != route.startCollider && c != route.targetCollider &&
                    c != route.player && !c.transform.IsChildOf(route.player.transform)).ToArray();
        }

        static Vector3 FindFoot(Route route, MeshCollider collider, bool forward)
        {
            float x = collider.transform.position.x;
            float centreZ = collider.transform.position.z;
            float edgeZ = forward ? collider.bounds.max.z - 2f : collider.bounds.min.z + 2f;
            float step = forward ? -.25f : .25f;
            for (float z = edgeZ; forward ? z >= centreZ : z <= centreZ; z += step)
                if (SafeFoot(route, collider, x, z, out var foot)) return foot;
            throw new InvalidOperationException("Ruin 1 F1: no controller-safe " +
                (forward ? "takeoff" : "landing") + " sample on " + collider.name);
        }

        static Vector3 Point(Route route, float t, float height)
        {
            return Vector3.Lerp(route.takeoff, route.landing, t) + Vector3.up *
                (height * Mathf.Sin(Mathf.PI * t));
        }

        static bool ClearAir(Route route, float height, out int samples, out string obstruction)
        {
            samples = 0;
            obstruction = null;
            float length = Vector3.Distance(route.takeoff, route.landing);
            int divisions = Mathf.CeilToInt(length / .5f);
            for (int i = 0; i <= divisions; i++)
            {
                var feet = Point(route, (float)i / divisions, height);
                var hits = Blockers(route, feet);
                samples++;
                if (hits.Length == 0) continue;
                obstruction = hits[0].name + " at " + feet.ToString("F3");
                return false;
            }
            return true;
        }

        static Route Measure()
        {
            var route = Locate();
            route.takeoff = FindFoot(route, route.startCollider, true);
            route.landing = FindFoot(route, route.targetCollider, false);
            string lastObstruction = null;
            foreach (float height in new[] { 6f, 8f, 10f, 12f, 16f, 20f, 28f })
            {
                if (!ClearAir(route, height, out route.airSamples, out lastObstruction)) continue;
                route.arcHeight = height;
                route.points = Enumerable.Range(0, 6).Select(i => Point(route, i / 5f, height)).ToArray();
                return route;
            }
            throw new InvalidOperationException("Ruin 1 F1: no clear aerial route found; last obstruction: " + lastObstruction);
        }

        static string Describe(Route route)
        {
            var s = new StringBuilder();
            s.AppendLine("Ruin 1 F1 live MeshCollider audit, world coordinates, five jumps");
            s.AppendLine("Start: Ruin01_RelocationRoot/flying_stone_003 " + route.start.position.ToString("F3") +
                " bounds=" + route.startCollider.bounds);
            s.AppendLine("Target: Ruin01_RelocationRoot/Ruin01_JumpTargetStone " + route.target.position.ToString("F3") +
                " bounds=" + route.targetCollider.bounds);
            s.AppendLine("Centre horizontal distance=" + Vector2.Distance(new Vector2(route.start.position.x, route.start.position.z),
                new Vector2(route.target.position.x, route.target.position.z)).ToString("F3"));
            s.AppendLine("Takeoff/landing horizontal distance=" + Vector2.Distance(new Vector2(route.takeoff.x, route.takeoff.z),
                new Vector2(route.landing.x, route.landing.z)).ToString("F3"));
            s.AppendLine("Collider envelope Z gap=" + (route.targetCollider.bounds.min.z - route.startCollider.bounds.max.z).ToString("F3"));
            s.AppendLine("Arc height=" + route.arcHeight.ToString("F3") + " air capsule samples=" + route.airSamples +
                " blockers=0; radius=" + (route.player.radius + .05f).ToString("F3"));
            string[] names = { "JumpStartPoint", "MagicStep01", "MagicStep02", "MagicStep03", "MagicStep04", "JumpLandingPoint" };
            for (int i = 0; i < names.Length; i++) s.AppendLine(names[i] + " " + route.points[i].ToString("F4"));
            return s.ToString();
        }

        [MenuItem("Tools/FYP/Ruin 1/Jump Environment F1/Audit and Save Backup")]
        public static void AuditAndSaveBackup()
        {
            var route = Measure();
            Require(!File.Exists(BackupScene), "backup already exists; refusing to overwrite it");
            Directory.CreateDirectory(BackupDirectory);
            var scene = EditorSceneManager.GetActiveScene();
            Require(EditorSceneManager.SaveScene(scene, BackupScene, true), "live scene backup failed");
            Require(EditorSceneManager.SaveScene(scene), "original scene save failed after backup");
            File.WriteAllText(Report, Describe(route));
            Debug.Log("Ruin 1 F1 backed up and saved the live scene; collider audit: " + Report);
        }

        [MenuItem("Tools/FYP/Ruin 1/Jump Environment F1/Install Empty Markers")]
        public static void Install()
        {
            Require(File.Exists(BackupScene), "run Audit and Save Backup first");
            Require(!EditorSceneManager.GetActiveScene().isDirty, "save unrelated scene edits before marker installation");
            var route = Measure();
            Require(route.root.Find(RouteName) == null, "route markers already exist");
            string[] names = { "JumpStartPoint", "MagicStep01", "MagicStep02", "MagicStep03", "MagicStep04", "JumpLandingPoint" };
            var parent = new GameObject(RouteName);
            Undo.RegisterCreatedObjectUndo(parent, "Install Ruin 1 F1 jump markers");
            parent.transform.SetParent(route.root, false);
            for (int i = 0; i < names.Length; i++)
            {
                var marker = new GameObject(names[i]);
                Undo.RegisterCreatedObjectUndo(marker, "Install Ruin 1 F1 jump markers");
                marker.transform.SetParent(parent.transform, false);
                marker.transform.position = route.points[i];
            }
            Require(parent.GetComponentsInChildren<Collider>(true).Length == 0 &&
                    parent.GetComponentsInChildren<Renderer>(true).Length == 0 &&
                    parent.transform.childCount == 6, "marker hierarchy validation failed");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Require(EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()), "marker scene save failed");
            File.WriteAllText(Report, Describe(route) + "\nInstalled hierarchy: Ruin01_RelocationRoot/" + RouteName + "\n");
            Debug.Log("Ruin 1 F1 installed six empty markers and saved the scene; " + Report);
        }
    }
}
