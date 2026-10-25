using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fyp.Development.Recursion
{
    // Serialized scene UI. Create() is also used by the Editor installer and as a legacy fallback.
    public sealed class RecursionPanelView : MonoBehaviour
    {
        public TMP_Text title, code, state, explanation, progress, emptyStack;
        public TMP_Text[] frameLabels;
        public Image[] frameBackgrounds;
        public Button previousButton, nextButton, replayButton, closeButton;
        public Slider checkpointSlider;

        private Action next, previous, replay, close;
        private Action<int> jump;
        private bool updating;

        public void Initialize(Action nextAction, Action previousAction, Action replayAction,
            Action closeAction, Action<int> jumpAction)
        {
            Disconnect();
            next = nextAction; previous = previousAction; replay = replayAction;
            close = closeAction; jump = jumpAction;
            nextButton.onClick.AddListener(Next);
            previousButton.onClick.AddListener(Previous);
            replayButton.onClick.AddListener(Replay);
            closeButton.onClick.AddListener(Close);
            checkpointSlider.onValueChanged.AddListener(Jump);
        }

        public void Disconnect()
        {
            if (nextButton != null) nextButton.onClick.RemoveListener(Next);
            if (previousButton != null) previousButton.onClick.RemoveListener(Previous);
            if (replayButton != null) replayButton.onClick.RemoveListener(Replay);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (checkpointSlider != null) checkpointSlider.onValueChanged.RemoveListener(Jump);
        }

        private void Next() => next?.Invoke();
        private void Previous() => previous?.Invoke();
        private void Replay() => replay?.Invoke();
        private void Close() => close?.Invoke();
        private void Jump(float value) { if (!updating) jump?.Invoke(Mathf.RoundToInt(value)); }

        public void Present(RecursionPlayback playback)
        {
            var step = playback.Current;
            title.text = "Ruin I | " + playback.Trace.FunctionName;
            var text = new StringBuilder("<mspace=11>");
            for (int i = 0; i < playback.Trace.Code.Count; i++)
            {
                string line = $"{i + 1} {playback.Trace.Code[i]}";
                text.AppendLine(i == step.ActiveLine ? "<color=#FFD166><b>>" + line + "</b></color>" : " " + line);
            }
            code.text = text + "</mspace>";
            state.text = $"{step.Phase} | n = {(step.Parameter.HasValue ? step.Parameter.ToString() : "--")}\n" +
                $"Line {(step.ActiveLine >= 0 ? (step.ActiveLine + 1).ToString() : "--")} | {playback.Trace.ActionName}: {step.ActionCount}";
            explanation.text = step.Explanation;
            progress.text = $"Checkpoint {playback.Index + 1} / {playback.Trace.Steps.Count} - drag to jump";
            previousButton.interactable = playback.CanPrevious;
            nextButton.interactable = playback.CanNext;
            updating = true;
            checkpointSlider.minValue = 0;
            checkpointSlider.maxValue = playback.Trace.Steps.Count - 1;
            checkpointSlider.wholeNumbers = true;
            checkpointSlider.SetValueWithoutNotify(playback.Index);
            updating = false;
            emptyStack.gameObject.SetActive(step.Frames.Count == 0);
            for (int row = 0; row < frameLabels.Length; row++)
            {
                bool visible = row < step.Frames.Count;
                frameBackgrounds[row].gameObject.SetActive(visible);
                if (!visible) continue;
                int index = step.Frames.Count - 1 - row;
                var block = frameBackgrounds[row].GetComponent<CallStackBlock>();
                if (block != null) block.Initialize(playback.Trace.FunctionName, step.Frames[index]);
                else frameLabels[row].text = $"{playback.Trace.FunctionName}({step.Frames[index]})";
                frameLabels[row].text += row == 0 ? " [" + step.Phase + "]" : " [waiting]";
                frameBackgrounds[row].color = row == 0 ? new Color(.15f, .34f, .40f) : new Color(.12f, .16f, .21f);
            }
        }

        private void OnDestroy() { Disconnect(); }

        public static RecursionPanelView Create(Transform canvas, TMP_FontAsset font)
        {
            var root = MakeRect("RecursionVisualizationPanel", canvas, new Vector2(.65f, 0), Vector2.one);
            root.gameObject.AddComponent<Image>().color = new Color(.025f, .045f, .07f, 1);
            var view = root.gameObject.AddComponent<RecursionPanelView>();
            view.title = Label("Title", root, "Ruin I | buildBridge", .05f, .92f, .95f, .98f, 28, font);
            view.code = Label("Code", root, "Code", .05f, .68f, .95f, .91f, 23, font);
            view.state = Label("State", root, "n = 3", .05f, .60f, .95f, .68f, 23, font);
            Label("StackHeading", root, "CALL STACK | active frame at top", .05f, .54f, .95f, .59f, 20, font);
            view.emptyStack = Label("EmptyStack", root, "Stack empty - initial call has returned.", .05f, .33f, .95f, .53f, 21, font);
            var frames = MakeRect("CallStack", root, new Vector2(.05f, .33f), new Vector2(.95f, .54f));
            view.frameLabels = new TMP_Text[4]; view.frameBackgrounds = new Image[4];
            for (int row = 0; row < 4; row++)
            {
                float top = 1 - row * .25f;
                var card = MakeRect("Frame_" + row, frames, new Vector2(0, top - .23f), new Vector2(1, top));
                view.frameBackgrounds[row] = card.gameObject.AddComponent<Image>();
                view.frameBackgrounds[row].color = new Color(.12f, .16f, .21f);
                view.frameLabels[row] = Label("Function", card, "", .03f, .08f, .97f, .92f, 21, font);
                var block = card.gameObject.AddComponent<CallStackBlock>();
                block.functionLabel = view.frameLabels[row];
            }
            view.explanation = Label("Explanation", root, "Observe the bridge, then press Next Step.", .05f, .18f, .95f, .32f, 22, font);
            view.progress = Label("Progress", root, "Checkpoint 1 / 22", .05f, .15f, .95f, .18f, 18, font);
            view.checkpointSlider = CreateSlider(root);
            view.previousButton = CreateButton("PreviousStep", "Previous Step", root, .05f, .06f, .48f, .105f, font);
            view.nextButton = CreateButton("NextStep", "Next Step", root, .52f, .06f, .95f, .105f, font);
            view.replayButton = CreateButton("Replay", "Replay", root, .05f, .01f, .48f, .055f, font);
            view.closeButton = CreateButton("Close", "Close [Esc]", root, .52f, .01f, .95f, .055f, font);
            return view;
        }

        private static RectTransform MakeRect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static TMP_Text Label(string name, Transform parent, string text, float x0, float y0, float x1,
            float y1, float size, TMP_FontAsset font)
        {
            var label = MakeRect(name, parent, new Vector2(x0, y0), new Vector2(x1, y1)).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.text = text; label.color = new Color(.92f, .95f, 1);
            label.fontSize = size; label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = size;
            label.alignment = TextAlignmentOptions.TopLeft; label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(string name, string text, Transform parent, float x0, float y0,
            float x1, float y1, TMP_FontAsset font)
        {
            var rect = MakeRect(name, parent, new Vector2(x0, y0), new Vector2(x1, y1));
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.17f, .28f, .36f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var label = Label("Label", rect, text, .03f, .05f, .97f, .95f, 22, font);
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private static Slider CreateSlider(Transform parent)
        {
            var root = MakeRect("CheckpointSlider", parent, new Vector2(.07f, .115f), new Vector2(.93f, .145f));
            var background = MakeRect("Background", root, new Vector2(0, .32f), new Vector2(1, .68f)).gameObject.AddComponent<Image>();
            background.color = new Color(.16f, .21f, .26f);
            var fill = MakeRect("Fill", root, new Vector2(0, .32f), new Vector2(1, .68f));
            fill.gameObject.AddComponent<Image>().color = new Color(.25f, .63f, .70f);
            var handleArea = MakeRect("HandleArea", root, new Vector2(.01f, 0), new Vector2(.99f, 1));
            var handle = MakeRect("Handle", handleArea, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            handle.sizeDelta = new Vector2(20, 28);
            var image = handle.gameObject.AddComponent<Image>(); image.color = new Color(1, .82f, .4f);
            var slider = root.gameObject.AddComponent<Slider>(); slider.fillRect = fill; slider.handleRect = handle;
            slider.targetGraphic = image; slider.wholeNumbers = true; slider.minValue = 0; slider.maxValue = 21;
            return slider;
        }
    }
}
