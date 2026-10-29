using UnityEngine;

namespace Fyp.Development.Recursion
{
    public sealed class Ruin02SpellbookSimulation : MonoBehaviour
    {
        public RecursionPanelView details;
        public Ruin02StaffDiagramView diagram;
        RecursionPlayback playback;
        bool initialized;
        public int CheckpointIndex => playback == null ? 0 : playback.Index;
        public RecursionStep Current => playback == null ? null : playback.Current;
        public RecursionPlayback Playback => playback;

        void EnsureInitialized()
        {
            if (initialized) return;
            playback = new RecursionPlayback(Ruin02ReturnLesson.Create());
            details.Initialize(NextStep,PreviousStep,Replay,()=>{},JumpToStep);
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
        }
        void OnDestroy() { if (initialized && details != null) details.Disconnect(); }
    }
}
