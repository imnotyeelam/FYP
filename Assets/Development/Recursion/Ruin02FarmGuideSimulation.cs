using System;
using UnityEngine;

namespace Fyp.Development.Recursion
{
    /// <summary>
    /// Teaching playback for the Magical Farm Simulation Guide (pattern: Ruin02SpellbookSimulation).
    /// Next/Previous/Replay/slider only move teaching snapshots; nothing touches the real farm.
    /// </summary>
    public sealed class Ruin02FarmGuideSimulation : MonoBehaviour
    {
        public RecursionPanelView details;
        public Ruin02FarmGuideDiagramView diagram;
        RecursiveGatherGuideLesson lesson;
        RecursionPlayback playback;
        bool initialized, completed, reachedFinal;
        public int CheckpointIndex => playback == null ? 0 : playback.Index;
        public RecursionPlayback Playback => playback;
        public RecursiveGatherGuideLesson Lesson => lesson;
        public bool IsCompleted => completed;
        public bool HasReachedFinalCheckpoint => reachedFinal;
        public event Action FinalCheckpointReached;
        public Action CloseRequested;

        void EnsureInitialized()
        {
            if (initialized) return;
            lesson = RecursiveGatherGuideLesson.Create();
            playback = new RecursionPlayback(lesson.Trace);
            details.Initialize(NextStep, PreviousStep, Replay, () => CloseRequested?.Invoke(), JumpToStep);
            initialized = true;
        }
        void OnEnable() { EnsureInitialized(); Render(); }
        public void NextStep() { EnsureInitialized(); playback.Next(); Render(); }
        public void PreviousStep() { EnsureInitialized(); playback.Previous(); Render(); }
        public void Replay() { EnsureInitialized(); playback.Replay(); Render(); }
        public void JumpToStep(int index) { EnsureInitialized(); playback.JumpTo(index); Render(); }
        void Render()
        {
            details.Present(playback);
            details.title.text = "Harvest Guide | gather(4)";
            if (diagram != null) diagram.Present(lesson, playback.Index);
            if (!reachedFinal && playback.Index == playback.Trace.Steps.Count - 1)
            {
                reachedFinal = true;
                FinalCheckpointReached?.Invoke();
            }
        }
        /// <summary>Only the explicit Finish at the final checkpoint completes the guide.</summary>
        public bool CompleteLesson()
        {
            EnsureInitialized();
            if (completed || playback.Index != playback.Trace.Steps.Count - 1) return false;
            completed = true;
            return true;
        }
        void OnDestroy() { if (initialized && details != null) details.Disconnect(); }
    }
}
