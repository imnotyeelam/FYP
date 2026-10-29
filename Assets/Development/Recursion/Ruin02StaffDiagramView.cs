using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fyp.Development.Recursion
{
    public sealed class Ruin02StaffDiagramView : MonoBehaviour
    {
        public Image[] indicators;
        public TMP_Text status;
        public void Present(RecursionStep step)
        {
            if (indicators == null || indicators.Length != 4 || status == null) return;
            Color[] colors = {new Color(.62f,.38f,.95f),new Color(.54f,.83f,.28f),
                new Color(1f,.35f,.18f),new Color(.23f,.75f,1f)};
            for (int i=0;i<4;i++)
                indicators[i].color = i < step.ActionCount ? colors[i] : new Color(.33f,.36f,.40f);
            status.text = step.Phase == RecursionPhase.Complete ? "All four staffs restored (learning only)" :
                step.Phase == RecursionPhase.BaseCase ? "Base case: no magic absorbed" :
                "Restored: " + step.ActionCount + " / 4  |  Learning only";
        }
    }
}
