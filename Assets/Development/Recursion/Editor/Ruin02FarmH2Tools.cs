using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fyp.Development.Recursion.Editor
{
    [InitializeOnLoad]
    public static class Ruin02FarmH2Tools
    {
        const string Key = "FYP.FarmH2.Test";
        static Ruin02FarmSimulation simulation;
        static int errors, events;
        static double started;
        static Ruin02FarmH2Tools() { EditorApplication.update += Tick; }
        static Transform Farm() => FloatingIslandPrototypeTools.Exact("Ruin02_MagicalFarm");

        [MenuItem("Tools/FYP/Ruin 2/Magical Farm H2/Install Core")]
        public static void Install()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Install in Edit Mode.");
            var root = Farm();
            var scene = EditorSceneManager.GetActiveScene();
            Require(scene.path == "Assets/Scenes/MagicWorld_Development.unity", "Open development scene.");
            Directory.CreateDirectory("LocalBackups/Ruin02FarmH2_20261119");
            Require(EditorSceneManager.SaveScene(scene,
                $"LocalBackups/Ruin02FarmH2_20261119/Before_{DateTime.Now:HHmmssfff}.unity", true), "Scene backup failed.");
            var controller = root.GetComponent<Ruin02FarmSimulation>();
            if (controller == null) controller = Undo.AddComponent<Ruin02FarmSimulation>(root.gameObject);
            var marker = root.Find("HarvestDebugDisplay");
            if (marker == null)
            {
                var go = new GameObject("HarvestDebugDisplay");
                Undo.RegisterCreatedObjectUndo(go, "Add farm debug display");
                marker = go.transform;
                marker.SetParent(root, false);
                marker.localPosition = new Vector3(0, 3.5f, 6f);
                marker.rotation = Quaternion.LookRotation(marker.position - root.Find("FarmCameraView").position);
                var text = go.AddComponent<TextMesh>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                go.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.fontSize = 64;
                text.characterSize = .055f;
                text.color = Color.white;
                text.text = "HARVEST SIMULATION\nReady — manual Play Mode trigger\nPlots Checked 0/30   Crops Harvested 0/15";
            }
            Undo.RecordObject(controller, "Wire farm debug display");
            controller.debugDisplay = marker.GetComponent<TextMesh>();
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene), "Scene save failed.");
            CheckTrace();
            Debug.Log("Farm H2 installed and saved. Edit Mode recursive trace checks passed.");
        }

        [MenuItem("Tools/FYP/Ruin 2/Magical Farm H2/Start Simulation in Play Mode")]
        public static void StartManual()
        {
            Require(EditorApplication.isPlaying, "Enter Play Mode first.");
            Farm().GetComponent<Ruin02FarmSimulation>().StartSimulation();
        }

        [MenuItem("Tools/FYP/Ruin 2/Magical Farm H2/Run Targeted Edit and Play Checks")]
        public static void Run()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Begin in Edit Mode.");
            CheckTrace();
            simulation = null;
            started = 0;
            events = errors = 0;
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }

        static bool[,] Occupancy()
        {
            var root = Farm();
            var occupied = new bool[3, 10];
            for (int r = 0; r < 3; r++)
            for (int n = 1; n <= 10; n++)
                occupied[r, n - 1] = root.Find($"Row{r + 1:00}/Plot{n:00}/Carrot") != null;
            return occupied;
        }

        static void CheckTrace()
        {
            var occupancy = Occupancy();
            var steps = RecursiveHarvestLesson.Create(occupancy).ToArray();
            Require(steps.Length == 66, "Expected 11 calls, one base and ten returns per row.");
            int total = 0;
            for (int row = 1; row <= 3; row++)
            {
                var trace = steps.Where(s => s.Row == row).ToArray();
                Require(trace.Take(11).All(s => s.Phase == HarvestPhase.Call && !s.HasCrop) &&
                    trace.Take(11).Select(s => s.N).SequenceEqual(Enumerable.Range(0, 11).Reverse()),
                    "Call descent must reach zero without harvesting.");
                Require(trace[11].Phase == HarvestPhase.Base && trace[11].N == 0 && trace[11].Depth == 11,
                    "Base case must stop at depth eleven.");
                Require(trace.Skip(12).Select(s => s.N).SequenceEqual(Enumerable.Range(1, 10)) &&
                    trace.Skip(12).All(s => s.Phase == HarvestPhase.Return), "Plots must be visited in return order.");
                total += trace.Count(s => s.HasCrop);
            }
            Require(total == 15 && steps.Select(s => s.Row).SequenceEqual(
                Enumerable.Repeat(1, 22).Concat(Enumerable.Repeat(2, 22)).Concat(Enumerable.Repeat(3, 22))),
                "Expected fifteen crops and sequential rows.");
        }

        static void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
            try
            {
                if (started == 0)
                {
                    started = EditorApplication.timeSinceStartup;
                    Application.logMessageReceived += OnLog;
                    return;
                }
                Require(EditorApplication.timeSinceStartup - started < 60, "Simulation timed out.");
                if (simulation == null)
                {
                    if (EditorApplication.timeSinceStartup - started < 1) return;
                    simulation = Farm().GetComponent<Ruin02FarmSimulation>();
                    Require(simulation != null && !simulation.IsComplete, "Fresh simulation is missing.");
                    simulation.StepPresented += Observe;
                    simulation.StartSimulation();
                    simulation.StartSimulation();
                }
                if (!simulation.IsComplete) return;
                Require(simulation.PlotsChecked == 30 && simulation.CropsHarvested == 15 &&
                    simulation.StackDepth == 0 && !simulation.IsRunning && events == 66, "Completion counters differ.");
                Require(!Farm().GetComponentsInChildren<Transform>(true).Any(t => t.name == "Carrot" && t.gameObject.activeSelf),
                    "All existing carrots should be hidden.");
                simulation.StartSimulation();
                Require(events == 66 && simulation.IsComplete && !simulation.IsRunning, "Completion must remain latched.");
                Require(errors == 0, "Play Mode Console errors occurred.");
                Finish("PASS Edit/Play: 66 recursive steps, 30 ordered plot checks, 15 unique crops, all empty plots counted, sequential rows, zero-depth completion latch. Play errors=0");
            }
            catch (Exception e) { Finish("FAIL " + e); }
        }

        static void Observe(HarvestStep step)
        {
            events++;
            Require(!simulation.IsComplete && simulation.StackDepth == step.Depth, "Premature completion or wrong stack depth.");
            int completedRows = step.Row - 1;
            int expectedChecks = completedRows * 10 + (step.Phase == HarvestPhase.Return ? step.N : 0);
            Require(simulation.PlotsChecked == expectedChecks, "Call phase changed plot counters or return order differs.");
            int cropCount = simulation.History.Count(s => s.Phase == HarvestPhase.Return && s.HasCrop);
            Require(simulation.CropsHarvested == cropCount, "Harvesting occurred outside the return phase.");
        }

        static void Finish(string result)
        {
            SessionState.SetBool(Key, false);
            Application.logMessageReceived -= OnLog;
            if (simulation != null) simulation.StepPresented -= Observe;
            Directory.CreateDirectory("Temp/Ruin02FarmH2");
            File.WriteAllText("Temp/Ruin02FarmH2/PlayChecks.txt", result);
            if (result.StartsWith("PASS")) Debug.Log(result); else Debug.LogError(result);
            EditorApplication.isPlaying = false;
        }

        static void Require(bool condition, string reason)
        {
            if (!condition) throw new InvalidOperationException("Farm H2: " + reason);
        }
    }
}
