using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fyp.Development.Recursion
{
    public sealed class Ruin02FarmSimulation : MonoBehaviour
    {
        public TextMesh debugDisplay;
        [Min(.01f)] public float callSeconds = .12f;
        [Min(.01f)] public float baseSeconds = .55f;
        [Min(.01f)] public float returnSeconds = .4f;
        public bool IsRunning { get; private set; }
        public bool IsComplete { get; private set; }
        public int PlotsChecked { get; private set; }
        public int CropsHarvested { get; private set; }
        public int StackDepth { get; private set; }
        public HarvestStep CurrentStep { get; private set; }
        public int TotalCrops { get; private set; }
        public int CompletedRows { get; private set; }
        public event Action<HarvestStep> StepPresented;
        public event Action Completed;

        readonly MeshRenderer[,] soils = new MeshRenderer[3, 10];
        readonly Transform[,] crops = new Transform[3, 10];
        readonly Vector3[,] cropPositions = new Vector3[3, 10];
        readonly Vector3[,] cropScales = new Vector3[3, 10];
        readonly bool[,] occupancy = new bool[3, 10];
        readonly bool[,] visited = new bool[3, 10];
        readonly List<HarvestStep> history = new List<HarvestStep>();
        public IReadOnlyList<HarvestStep> History => history.AsReadOnly();
        MaterialPropertyBlock highlight;
        MeshRenderer highlightedSoil;
        Coroutine playback;
        bool initialized;

        void Start()
        {
            try
            {
                Initialize();
                Present("Ready — use Tools/FYP/Ruin 2/Magical Farm H2/Start Simulation in Play Mode.");
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
                enabled = false;
            }
        }

        void Initialize()
        {
            if (initialized) return;
            if (debugDisplay == null) throw new InvalidOperationException("Farm debug display is missing.");
            highlight = new MaterialPropertyBlock();
            for (int row = 0; row < 3; row++)
            for (int plot = 0; plot < 10; plot++)
            {
                var marker = transform.Find($"Row{row + 1:00}/Plot{plot + 1:00}");
                if (marker == null || marker.Find("Soil") == null)
                    throw new InvalidOperationException("Farm plot hierarchy is incomplete.");
                soils[row, plot] = marker.Find("Soil").GetComponent<MeshRenderer>();
                crops[row, plot] = marker.Find("Carrot");
                occupancy[row, plot] = crops[row, plot] != null;
                if (!occupancy[row, plot]) continue;
                TotalCrops++;
                cropPositions[row, plot] = crops[row, plot].localPosition;
                cropScales[row, plot] = crops[row, plot].localScale;
            }
            if (TotalCrops != 15) throw new InvalidOperationException("H2 expects the existing fifteen carrots.");
            initialized = true;
        }

        [ContextMenu("Start Harvest Simulation (Play Mode)")]
        public void StartSimulation()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || IsRunning || IsComplete) return;
            Initialize();
            IsRunning = true;
            playback = StartCoroutine(Play(3));
        }

        public void StartNextRow()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || IsRunning || IsComplete) return;
            Initialize();
            IsRunning = true;
            playback = StartCoroutine(Play(CompletedRows + 1));
        }

        IEnumerator Play(int throughRow)
        {
            foreach (var step in RecursiveHarvestLesson.Create(occupancy))
            {
                if (step.Row <= CompletedRows || step.Row > throughRow) continue;
                CurrentStep = step;
                StackDepth = step.Depth;
                history.Add(step);
                if (step.Phase == HarvestPhase.Return)
                {
                    int row = step.Row - 1, plot = step.N - 1;
                    if (visited[row, plot]) throw new InvalidOperationException("A plot was visited twice.");
                    visited[row, plot] = true;
                    PlotsChecked++;
                    highlightedSoil = soils[row, plot];
                    highlight.SetColor("_BaseColor", step.HasCrop ? new Color(.55f, .8f, 1f) : new Color(1f, .78f, .4f));
                    highlightedSoil.SetPropertyBlock(highlight);
                    if (step.HasCrop) CropsHarvested++;
                    Present(step.HasCrop ? $"Plot {step.N:00} — Carrot Collected" : $"Plot {step.N:00} — Empty Plot — Skipped");
                    StepPresented?.Invoke(step);
                    if (step.HasCrop) yield return Collect(row, plot);
                    else yield return new WaitForSeconds(returnSeconds);
                    highlightedSoil.SetPropertyBlock(null);
                    highlightedSoil = null;
                }
                else
                {
                    Present(step.Phase == HarvestPhase.Base ? "Base Case — n == 0. Begin returning." :
                        $"harvest(row {step.Row}, {step.N}) — descend");
                    StepPresented?.Invoke(step);
                    yield return new WaitForSeconds(step.Phase == HarvestPhase.Base ? baseSeconds : callSeconds);
                }
            }
            StackDepth = 0;
            CompletedRows = throughRow;
            if (throughRow == 3 && (PlotsChecked != 30 || CropsHarvested != TotalCrops))
                throw new InvalidOperationException("Harvest ended before all plots and crops were processed.");
            IsComplete = throughRow == 3;
            IsRunning = false;
            playback = null;
            Present(IsComplete ? "Farm Complete — final recursion stack empty." : $"Row {throughRow} complete — stack empty.");
            if (IsComplete) Completed?.Invoke();
        }

        IEnumerator Collect(int row, int plot)
        {
            var crop = crops[row, plot];
            float elapsed = 0f;
            while (elapsed < returnSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / returnSeconds);
                crop.localPosition = cropPositions[row, plot] + Vector3.up * (.8f * t);
                crop.localScale = cropScales[row, plot] * (1f - t);
                yield return null;
            }
            crop.gameObject.SetActive(false);
            crop.localPosition = cropPositions[row, plot];
            crop.localScale = cropScales[row, plot];
        }

        void Present(string explanation)
        {
            debugDisplay.text = $"HARVEST SIMULATION\nRow {CurrentStep?.Row ?? 0}/3   n = {CurrentStep?.N ?? 0}" +
                $"   Phase {CurrentStep?.Phase.ToString() ?? "Ready"}   Stack {StackDepth}\n" +
                $"Plots Checked {PlotsChecked}/30   Crops Harvested {CropsHarvested}/15\n{explanation}";
        }

        void OnDisable()
        {
            if (playback != null) StopCoroutine(playback);
            playback = null;
            IsRunning = false;
            if (highlightedSoil != null) highlightedSoil.SetPropertyBlock(null);
            highlightedSoil = null;
            if (!initialized || IsComplete) return;
            // An interrupted test restarts from a coherent initial snapshot.
            for (int row = 0; row < 3; row++)
            for (int plot = 0; plot < 10; plot++)
            {
                visited[row, plot] = false;
                var crop = crops[row, plot];
                if (crop == null) continue;
                crop.localPosition = cropPositions[row, plot];
                crop.localScale = cropScales[row, plot];
                crop.gameObject.SetActive(true);
            }
            PlotsChecked = CropsHarvested = StackDepth = 0;
            CompletedRows = 0;
            CurrentStep = null;
            history.Clear();
        }
    }
}
