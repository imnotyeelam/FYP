using Fyp.Development.Progress;
using Fyp.Development.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fyp.Development.Recursion
{
    public enum HarvestConsoleMessageKind { Info, Correct, Incorrect, Queued }

    /// <summary>
    /// Presentation-only view for the Magic Harvest Console (UI-B prototype, UI_STYLE_GUIDE.md).
    /// Reads simulation/progress state; never changes simulation, answers, rewards or travel.
    /// </summary>
    public sealed class Ruin02HarvestConsoleView : MonoBehaviour
    {
        public Ruin02FarmSimulation simulation;
        [Tooltip("Read-only: FarmReturnPoint access, used only to word the next-step instruction.")]
        public Ruin02FarmAccess staffStatus;
        [Tooltip("Read-only: shared tracker, used only to word the next-step instruction.")]
        public FragmentProgressTracker progress;

        [Header("Work area (hidden on completion)")]
        public GameObject[] workArea;

        [Header("Call stack")]
        public Image[] stackRows;
        public TMP_Text[] stackLabels;
        public TMP_Text stackMore;
        public TMP_Text stackHeading;
        public Sprite stackIdleSprite;

        [Header("Progress")]
        public Image rowsFill, plotsFill, cropsFill;
        public TMP_Text rowsLabel, plotsLabel, cropsLabel;

        [Header("Feedback")]
        public GameObject feedbackBox;
        public Image feedbackFill, feedbackAccent, feedbackIcon;
        public Sprite iconInfo, iconCorrect, iconIncorrect, iconQueued;
        public TMP_Text feedbackText;

        [Header("Completion")]
        public GameObject completionCard;
        public TMP_Text completionStats, completionNext;

        public static string BuildCode(int row, bool running, HarvestPhase? phase)
        {
            string[] lines =
            {
                "void harvest(int row, int n) {",
                $"    if ({(row == 1 ? "____" : "n == 0")}) return;",
                $"    harvest(row, {(row == 2 ? "____" : "n - 1")});",
                $"    if ({(row == 3 ? "____" : "hasCrop(row, n)")})",
                "        collectCrop(row, n);",
                "}"
            };
            // Active line: the executing statement while a row plays, otherwise the line with the blank.
            // Colour only (no bold): bold widens leading spaces and would break the indentation.
            int active = running && phase.HasValue
                ? phase.Value == HarvestPhase.Base ? 1 : phase.Value == HarvestPhase.Call ? 2 : 3
                : row == 1 ? 1 : row == 2 ? 2 : 3;
            var text = new System.Text.StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                string number = FypUiTheme.LineNumberTag + (i + 1) + "</color>  ";
                text.Append(i == active
                    ? number + FypUiTheme.ActiveLineTag + lines[i] + "</color>"
                    : number + lines[i]);
                if (i < lines.Length - 1) text.Append('\n');
            }
            return text.ToString();
        }

        public void Present(string message, HarvestConsoleMessageKind kind, bool closePending)
        {
            if (simulation == null) return;
            bool complete = simulation.IsComplete;
            if (workArea != null) foreach (var go in workArea) if (go != null) go.SetActive(!complete);
            if (completionCard != null) completionCard.SetActive(complete);
            PresentStack();
            PresentProgress();
            PresentFeedback(complete, message, kind, closePending);
            if (complete) PresentCompletion();
        }

        void PresentStack()
        {
            if (stackRows == null || stackLabels == null) return;
            int depth = simulation.IsRunning ? simulation.StackDepth : 0;
            string activeTag = "";
            var step = simulation.CurrentStep;
            int row = step != null ? step.Row : Mathf.Min(3, simulation.CompletedRows + 1);
            for (int i = 0; i < stackRows.Length && i < stackLabels.Length; i++)
            {
                // Frame depth d holds harvest(row, 11 - d); the most recent frame is drawn first.
                int frameDepth = depth - i;
                bool visible = frameDepth >= 1;
                stackRows[i].gameObject.SetActive(visible);
                if (!visible) continue;
                int n = 11 - frameDepth;
                bool top = i == 0;
                // Only the top (active) frame carries a status tag; waiting frames show just the call.
                string tag = !top || step == null ? "" :
                    step.Phase == HarvestPhase.Base ? "BASE" :
                    step.Phase == HarvestPhase.Call ? "CALL" :
                    step.HasCrop ? "HARVEST" : "SKIP";
                if (top) activeTag = tag;
                stackLabels[i].text = top ? $"<b>harvest(row {row}, {n})</b>" : $"harvest(row {row}, {n})";
                stackRows[i].color = top ? FypUiTheme.GoldGlow : Color.white;
                stackLabels[i].color = top ? FypUiTheme.Ink : FypUiTheme.Parchment;
            }
            if (stackMore != null)
            {
                int hidden = depth - stackRows.Length;
                stackMore.text = depth == 0 ? "Stack empty" : hidden > 0 ? $"+ {hidden} more below" : "";
            }
            if (stackHeading != null)
                stackHeading.text = depth == 0 ? "Call Stack" :
                    $"Call Stack  <size=80%>depth {depth}  |  top: {activeTag}</size>";
        }

        void PresentProgress()
        {
            SetBar(rowsFill, rowsLabel, simulation.CompletedRows, 3, "Rows");
            SetBar(plotsFill, plotsLabel, simulation.PlotsChecked, 30, "Plots");
            SetBar(cropsFill, cropsLabel, simulation.CropsHarvested, 15, "Carrots");
        }

        static void SetBar(Image fill, TMP_Text label, int value, int max, string name)
        {
            if (fill != null) fill.fillAmount = Mathf.Clamp01(value / (float)max);
            if (label != null) label.text = $"{name}<pos=62%><b>{value} / {max}</b>";
        }

        void PresentFeedback(bool complete, string message, HarvestConsoleMessageKind kind, bool closePending)
        {
            if (feedbackBox != null) feedbackBox.SetActive(!complete && !string.IsNullOrEmpty(message));
            if (complete || string.IsNullOrEmpty(message)) return;
            if (closePending) kind = HarvestConsoleMessageKind.Queued;
            Color fill = kind == HarvestConsoleMessageKind.Correct ? FypUiTheme.CorrectFill :
                kind == HarvestConsoleMessageKind.Incorrect ? FypUiTheme.IncorrectFill :
                kind == HarvestConsoleMessageKind.Queued ? FypUiTheme.HintFill : FypUiTheme.InfoFill;
            Color accent = kind == HarvestConsoleMessageKind.Correct ? FypUiTheme.CorrectText :
                kind == HarvestConsoleMessageKind.Incorrect ? FypUiTheme.IncorrectText :
                kind == HarvestConsoleMessageKind.Queued ? FypUiTheme.SkyBlue : FypUiTheme.MagicGold;
            if (feedbackFill != null) feedbackFill.color = fill;
            if (feedbackAccent != null) feedbackAccent.color = accent;
            if (feedbackIcon != null)
            {
                feedbackIcon.sprite = kind == HarvestConsoleMessageKind.Correct ? iconCorrect :
                    kind == HarvestConsoleMessageKind.Incorrect ? iconIncorrect :
                    kind == HarvestConsoleMessageKind.Queued ? iconQueued : iconInfo;
                feedbackIcon.color = accent;
                feedbackIcon.preserveAspect = true;
            }
            if (feedbackText != null)
            {
                feedbackText.color = kind == HarvestConsoleMessageKind.Incorrect ? FypUiTheme.IncorrectText : FypUiTheme.Ink;
            }
        }

        void PresentCompletion()
        {
            if (completionStats != null)
                completionStats.text =
                    $"Rows harvested<pos=68%><b>{simulation.CompletedRows} / 3</b>\n" +
                    $"Plots checked<pos=68%><b>{simulation.PlotsChecked} / 30</b>\n" +
                    $"Carrots collected<pos=68%><b>{simulation.CropsHarvested} / 15</b>\n" +
                    $"Call stack<pos=68%><b>{(simulation.StackDepth == 0 ? "empty" : simulation.StackDepth.ToString())}</b>";
            if (completionNext == null) return;
            bool collected = progress != null && progress.HasFragment(FragmentProgressTracker.Ruin02);
            bool staffDone = staffStatus != null && staffStatus.StaffCompleted;
            completionNext.text = collected
                ? "<b>Next:</b> The Fragment of Returning Magic is already yours. Use the Return Stone to travel back."
                : staffDone
                    ? "<b>Next:</b> The Fragment of Returning Magic has awakened in the Ritual Chamber. Use the Return Stone to go back and collect it."
                    : "<b>Next:</b> The fragment is still asleep. Return to the Ritual Chamber and restore all four Elemental Staffs to awaken it.";
        }
    }
}
