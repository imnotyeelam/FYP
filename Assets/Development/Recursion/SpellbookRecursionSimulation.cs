using UnityEngine;
using System;

namespace Fyp.Development.Recursion
{
    public sealed class SpellbookRecursionSimulation : MonoBehaviour
    {
        public RecursionPanelView details;
        public BridgeDiagramView diagram;
        private RecursionPlayback playback;
        private bool initialized;
        private bool completed;
        private bool reachedFinalCheckpoint;
        public int CheckpointIndex => playback==null?0:playback.Index;
        public RecursionStep Current => playback==null?null:playback.Current;
        public RecursionPlayback Playback { get { EnsureInitialized(); return playback; } }
        // Reaching the final snapshot only enables Finish.  The lesson is completed
        // deliberately, so closing early can never unlock Bridge Practice.
        public bool IsCompleted => completed;
        public bool HasReachedFinalCheckpoint => reachedFinalCheckpoint;
        public event Action Completed;
        public event Action FinalCheckpointReached;
        public Action CloseRequested;

        private void EnsureInitialized()
        {
            if(initialized)return;
            playback=new RecursionPlayback(BridgeRecursionLesson.Create(3));
            details.Initialize(NextStep,PreviousStep,Replay,()=>CloseRequested?.Invoke(),JumpToStep);
            initialized=true;
        }
        private void OnEnable(){EnsureInitialized();Render();}
        public void NextStep(){EnsureInitialized();playback.Next();Render();}
        public void PreviousStep(){EnsureInitialized();playback.Previous();Render();}
        public void Replay(){EnsureInitialized();playback.Replay();Render();}
        public void JumpToStep(int index){EnsureInitialized();playback.JumpTo(index);Render();}
        private void Render()
        {
            details.Present(playback);
            details.title.text="Guided simulation | buildBridge(3)";
            diagram.Present(playback.Current);
            if(!reachedFinalCheckpoint && playback.Index==playback.Trace.Steps.Count-1)
            {
                reachedFinalCheckpoint=true;
                FinalCheckpointReached?.Invoke();
            }
        }
        public bool CompleteLesson()
        {
            EnsureInitialized();
            if(completed || playback.Index!=playback.Trace.Steps.Count-1)return false;
            completed=true;
            Completed?.Invoke();
            return true;
        }
        private void OnDestroy(){if(initialized && details!=null)details.Disconnect();}
    }
}
