using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    public static class Ruin02FarmH4Tools
    {
        [MenuItem("Tools/FYP/Ruin 2/Magical Farm H4/Install Access")]
        public static void Install()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/MagicWorld_Development.unity" || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Open development scene in Edit Mode.");
            var chamber = Exact("Ruin02_Area/Gameplay/RitualChamber");
            var farm = Exact("Ruin02_MagicalFarm");
            var console = farm.Find("HarvestConsole").GetComponent<Ruin02HarvestConsole>();
            if (chamber.Find("MagicalFarmGateway") != null || farm.Find("FarmReturnPoint").GetComponent<Ruin02FarmAccess>() != null)
                throw new InvalidOperationException("Farm access already exists.");
            var hints = Exact("InteractionHintManager").GetComponent<InteractionHintManager>();
            var practice = console.gameObject.scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Ruin02PracticeController>(true)).Single();
            var restoration = UnityEngine.Object.FindFirstObjectByType<Ruin02AirRestoration>();
            var teleport = UnityEngine.Object.FindObjectsByType<TeleportManager>(FindObjectsSortMode.None)
                .Single(t => t.player == console.player);
            var toast = Exact("Canvas/FragmentRewardToast").GetComponentInChildren<TMP_Text>(true);
            var player = console.player.GetComponent<CharacterController>();
            var ground = Exact("Ruin02_Area/Environment/Ground").GetComponent<BoxCollider>();
            Physics.SyncTransforms();
            Vector3 foot = FindSite(ground, player, hints);
            CheckSafe(farm.Find("FarmEntrySpawn").position, player);
            CheckSafe(farm.Find("FarmReturnPoint").position, player);
            Directory.CreateDirectory("LocalBackups/Ruin02FarmH4_20261119");
            if (!EditorSceneManager.SaveScene(scene, $"LocalBackups/Ruin02FarmH4_20261119/Before_{DateTime.Now:HHmmssfff}.unity", true))
                throw new IOException("Backup failed.");
            var gateway = new GameObject("MagicalFarmGateway");
            Undo.RegisterCreatedObjectUndo(gateway, "Add Magical Farm gateway");
            gateway.transform.SetParent(chamber, true);
            gateway.transform.position = foot;
            var landing = new GameObject("FarmRitualReturnSpawn");
            Undo.RegisterCreatedObjectUndo(landing, "Add farm return spawn");
            landing.transform.SetParent(gateway.transform, false);
            landing.transform.position = foot;
            var entry = Undo.AddComponent<Ruin02FarmAccess>(gateway);
            var exit = Undo.AddComponent<Ruin02FarmAccess>(farm.Find("FarmReturnPoint").gameObject);
            foreach (var gate in new[] { entry, exit })
            {
                gate.practice = practice; gate.restoration = restoration; gate.console = console;
                gate.teleport = teleport; gate.toast = toast;
            }
            entry.destination = farm.Find("FarmEntrySpawn");
            exit.destination = landing.transform; exit.isFarmReturn = true;
            AddSign(gateway.transform, "MAGICAL FARM", foot + Vector3.up * 1.7f);
            AddSign(exit.transform, "RETURN TO RITUAL", exit.transform.position + Vector3.up * 1.5f);
            var rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FantasyForest/Prefabs/Landscape/MagicRock_01.prefab");
            var rock = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, scene);
            Undo.RegisterCreatedObjectUndo(rock, "Add farm gateway landmark");
            rock.name = "FarmGatewayLandmark";
            rock.transform.SetParent(gateway.transform, false);
            rock.transform.localPosition = new Vector3(0, -.12f, 1f);
            rock.transform.localScale = Vector3.one * .75f;
            foreach (var collider in rock.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            Undo.RecordObject(hints, "Register farm access");
            hints.interactionTargets = hints.interactionTargets.Concat(new MonoBehaviour[] { entry, exit }).ToArray();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Farm H4 saved. Gateway/return spawn {foot:F3}. Farm return {exit.transform.position:F3}. Both ranges 1.4m; console separation {Vector3.Distance(exit.transform.position, console.transform.position):F2}m.");
        }

        static Vector3 FindSite(BoxCollider ground, CharacterController player, InteractionHintManager hints)
        {
            foreach (float x in new[] { 790f, 792f, 788f, 808f, 810f })
            foreach (float z in new[] { 196f, 198f, 192f, 216f })
            {
                if (!ground.Raycast(new Ray(new Vector3(x, 30f, z), Vector3.down), out var hit, 35f)) continue;
                var foot = hit.point + Vector3.up * .12f;
                if (hints.interactionTargets.OfType<ISharedInteractionTarget>().Any(t =>
                    t.InteractionOrigin != null && Vector3.Distance(t.InteractionOrigin.position, foot) < t.SharedInteractionRange + 2.1f))
                    continue;
                if (!Safe(foot, player)) continue;
                return foot;
            }
            throw new InvalidOperationException("No clear ritual gateway site with separated interaction ranges.");
        }
        static bool Safe(Vector3 foot, CharacterController player)
        {
            var center = foot + player.center;
            var half = Vector3.up * Mathf.Max(0f, player.height * .5f - player.radius);
            return Physics.OverlapCapsule(center - half, center + half, player.radius + .05f, ~0,
                QueryTriggerInteraction.Ignore).All(c => c == player || c.transform.IsChildOf(player.transform));
        }
        static void CheckSafe(Vector3 foot, CharacterController player)
        { if (!Safe(foot, player)) throw new InvalidOperationException("Blocked farm marker: " + foot); }
        static void AddSign(Transform parent, string text, Vector3 position)
        {
            var sign = new GameObject("GatewayLabel", typeof(TextMesh));
            Undo.RegisterCreatedObjectUndo(sign, "Add farm sign");
            sign.transform.SetParent(parent, false); sign.transform.position = position;
            var label = sign.GetComponent<TextMesh>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            label.text = text; label.anchor = TextAnchor.MiddleCenter; label.characterSize = .07f;
            label.fontSize = 48; label.color = new Color(.65f, .85f, 1f);
            sign.transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        static Transform Exact(string path) => FloatingIslandPrototypeTools.Exact(path);
    }
}
