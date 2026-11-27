using Fyp.Development.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fyp.Development.Recursion
{
    /// <summary>UI-only four-plot diagram for the Magical Farm Simulation Guide. No farm or world references.</summary>
    public sealed class Ruin02FarmGuideDiagramView : MonoBehaviour
    {
        public Image[] plots;         // soil card per plot (index 0 = Plot 1)
        public Image[] plants;        // plant icon per plot (may be null for an empty plot)
        public Image[] focusFrames;   // gold frame shown on the plot being checked
        public TMP_Text[] plotLabels; // "Plot k" captions
        public TMP_Text status;

        static readonly Color Soil = new Color(0.54f, 0.40f, 0.27f);

        public void Present(RecursiveGatherGuideLesson lesson, int index)
        {
            if (lesson == null || plots == null) return;
            int checkedBits = lesson.Checked[index], pickedBits = lesson.Picked[index], focus = lesson.Focus[index];
            int checkedCount = 0, pickedCount = 0;
            for (int i = 0; i < plots.Length && i < RecursiveGatherGuideLesson.PlotCount; i++)
            {
                bool hasPlant = RecursiveGatherGuideLesson.Plants[i];
                bool isChecked = (checkedBits & (1 << i)) != 0, isPicked = (pickedBits & (1 << i)) != 0;
                if (isChecked) checkedCount++;
                if (isPicked) pickedCount++;
                plots[i].color = !isChecked ? Soil : hasPlant ? FypUiTheme.FarmHarvested : FypUiTheme.FarmEmpty;
                if (plants != null && i < plants.Length && plants[i] != null)
                    plants[i].gameObject.SetActive(hasPlant && !isPicked);
                if (focusFrames != null && i < focusFrames.Length && focusFrames[i] != null)
                    focusFrames[i].gameObject.SetActive(focus == i + 1);
                if (plotLabels != null && i < plotLabels.Length && plotLabels[i] != null)
                    plotLabels[i].text = $"Plot {i + 1}" + (!hasPlant ? "\n<size=80%>empty</size>" : isPicked ? "\n<size=80%>picked</size>" : "");
            }
            if (status != null)
                status.text = $"Checked <b>{checkedCount} / 4</b>  |  Picked <b>{pickedCount} / 3</b>  |  Learning only";
        }
    }
}
