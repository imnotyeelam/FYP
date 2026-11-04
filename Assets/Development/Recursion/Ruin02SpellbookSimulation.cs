using System;
using UnityEngine;

namespace Fyp.Development.Recursion
{
    public sealed class Ruin02SpellbookSimulation : MonoBehaviour
    {
        public RecursionPanelView details;
        public Ruin02StaffDiagramView diagram;
        RecursionPlayback playback;
        bool initialized;
        bool completed;
        bool reachedFinal;
        public int CheckpointIndex => playback == null ? 0 : playback.Index;
        public RecursionStep Current => playback == null ? null : playback.Current;
        public RecursionPlayback Playback => playback;
        public bool IsCompleted => completed;
        public bool HasReachedFinalCheckpoint => reachedFinal;
        public event Action Completed;
        public event Action FinalCheckpointReached;
        public Action CloseRequested;

        void EnsureInitialized()
        {
            if (initialized) return;
            playback = new RecursionPlayback(Ruin02ReturnLesson.Create());
            details.Initialize(NextStep,PreviousStep,Replay,()=>CloseRequested?.Invoke(),JumpToStep);
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
            details.title.text = "Guided return flow | sealMagic(4)";
            diagram.Present(playback.Current);
            if(!reachedFinal && playback.Index==playback.Trace.Steps.Count-1)
            {
                reachedFinal=true;
                FinalCheckpointReached?.Invoke();
            }
        }
        public bool CompleteLesson()
        {
            EnsureInitialized();
            if(completed || playback.Index!=playback.Trace.Steps.Count-1)return false;
            completed=true;Completed?.Invoke();return true;
        }
        void OnDestroy() { if (initialized && details != null) details.Disconnect(); }
    }
}
