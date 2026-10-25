using UnityEngine;

namespace Fyp.Development.Recursion
{
    public sealed class SpellbookRecursionSimulation : MonoBehaviour
    {
        public RecursionPanelView details;
        public BridgeDiagramView diagram;
        private RecursionPlayback playback;
        private bool initialized;
        public int CheckpointIndex => playback==null?0:playback.Index;
        public RecursionStep Current => playback==null?null:playback.Current;

        private void EnsureInitialized()
        {
            if(initialized)return;
            playback=new RecursionPlayback(BridgeRecursionLesson.Create(3));
            details.Initialize(NextStep,PreviousStep,Replay,()=>{},JumpToStep);
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
        }
        private void OnDestroy(){if(initialized && details!=null)details.Disconnect();}
    }
}
