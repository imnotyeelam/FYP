using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fyp.Development.Recursion
{
    // UI-only projection. Intentionally has no world, interaction or reward references.
    public sealed class BridgeDiagramView : MonoBehaviour
    {
        public Image[] stones;
        public TMP_Text status;
        public Color brokenColor = new Color(.27f,.3f,.35f);
        public Color placedColor = new Color(.24f,.63f,.69f);
        public Color returnColor = new Color(1,.72f,.22f);
        public Color completeColor = new Color(.32f,.72f,.48f);
        public void Present(RecursionStep step)
        {
            var state=new BridgeSnapshotProjection(step,3);
            for(int i=0;i<stones.Length;i++)
            {
                bool placed=state.IsPlaced(i);
                stones[i].color=state.ReturningStone==i?returnColor:state.Completed?completeColor:placed?placedColor:brokenColor;
                // Persistent Images move between diagram poses; no Instantiate/Destroy.
                var r=stones[i].rectTransform;
                r.anchoredPosition=new Vector2(0,placed?0:-(i%2==0?45:75));
                r.localRotation=Quaternion.Euler(0,0,placed?0:(i%2==0?-10:12));
            }
            status.text=state.Completed?"Simulation complete | stack empty":
                step.Phase==RecursionPhase.BaseCase?"Base case: no fourth stone":
                state.ReturningStone>=0?"Returning to callers | no new placements":
                "Placed "+state.PlacedCount+" / 3 | teal = restored";
        }
    }
}
