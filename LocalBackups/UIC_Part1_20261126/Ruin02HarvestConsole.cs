using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fyp.Development.Recursion
{
    public sealed class Ruin02HarvestConsole : MonoBehaviour, ISharedInteractionTarget
    {
        public Ruin02FarmSimulation simulation;
        public Transform player, cameraView;
        public PlayerMovement movement;
        public CameraFollow cameraFollow;
        public GameObject panel;
        public TMP_Text code, state, feedback;
        public TMP_InputField input;
        public UnityEngine.UI.Button submit, close;
        [Tooltip("Optional presentation view (UI-B). Reads state only.")]
        public Ruin02HarvestConsoleView view;
        public bool IsOpen { get; private set; }
        bool moveBefore, cameraBefore, allowBefore, visibleBefore, closePending;
        CursorLockMode lockBefore;
        Vector3 cameraPosition;
        Rect cameraRect;
        float cameraFov;
        Quaternion cameraRotation;
        string message = "";
        HarvestConsoleMessageKind messageKind = HarvestConsoleMessageKind.Info;
        int rowsAtMessage;
        // Wizard visuals are hidden while the console camera is active; controller/colliders are untouched.
        Renderer[] hiddenRenderers;
        bool[] hiddenRendererStates;
        public bool UsesSharedInteraction => true;
        public Transform InteractionOrigin => transform;
        public float SharedInteractionRange => 3f;
        public bool CanSharedInteract => isActiveAndEnabled && !IsOpen && simulation != null &&
            !simulation.IsRunning && player != null && CursorManager.allowCursorLock;
        public string SharedHint => "Press [E] to use the Magic Harvest Console";
        public void InteractFromShared() { if (CanSharedInteract) Open(); }

        void Start()
        {
            panel.SetActive(false);
            submit.onClick.AddListener(Submit);
            close.onClick.AddListener(RequestClose);
            input.onSubmit.AddListener(OnEnter);
        }
        void OnEnter(string value) { Submit(); }

        public static bool Matches(int row, string answer)
        {
            // Parentheses only group these side-effect-free expressions. Reject all other tokens.
            string compact = Regex.Replace(answer ?? "", @"\s+", "");
            int depth = 0;
            foreach (char c in compact)
            {
                if (c == '(') depth++;
                if (c == ')' && --depth < 0) return false;
            }
            if (depth != 0) return false;
            // Reduce only grouping around complete operands or expressions.
            string previous;
            do
            {
                previous = compact;
                compact = Regex.Replace(compact, @"\((n|row|0|1|n==0|0==n|n-1|hasCrop\(row,n\))\)", "$1");
            } while (compact != previous);
            return row == 1 ? compact == "n==0" || compact == "0==n" :
                row == 2 ? compact == "n-1" : row == 3 && compact == "hasCrop(row,n)";
        }

        void Open()
        {
            IsOpen = true;
            moveBefore = movement.enabled;
            cameraBefore = cameraFollow.enabled;
            allowBefore = CursorManager.allowCursorLock;
            visibleBefore = Cursor.visible;
            lockBefore = Cursor.lockState;
            cameraPosition = cameraFollow.transform.position;
            cameraRotation = cameraFollow.transform.rotation;
            var camera = cameraFollow.GetComponent<Camera>();
            cameraRect = camera.rect;
            cameraFov = camera.fieldOfView;
            movement.enabled = false;
            cameraFollow.enabled = false;
            cameraFollow.transform.SetPositionAndRotation(cameraView.position, cameraView.rotation);
            camera.rect = new Rect(0f, 0f, .65f, 1f);
            camera.fieldOfView = 66f;
            CursorManager.Instance.SetUIOpen(true);
            HidePlayerVisuals();
            panel.SetActive(true);
            message = simulation.IsComplete ? "" :
                $"Row {simulation.CompletedRows + 1} of 3: type the missing expression, then press Run Row.";
            messageKind = HarvestConsoleMessageKind.Info;
            closePending = false;
            input.text = "";
            Refresh();
            input.ActivateInputField();
        }

        public void Submit()
        {
            if (!IsOpen || simulation.IsRunning || simulation.IsComplete) return;
            int row = simulation.CompletedRows + 1;
            if (!Matches(row, input.text))
            {
                message = row == 1 ? "Which value means there are no positions left?" :
                    row == 2 ? "Pass one fewer position so the next call approaches the base case." :
                    "Test whether the current row and position contain a crop.";
                messageKind = HarvestConsoleMessageKind.Incorrect;
                Refresh();
                return;
            }
            message = "Correct! Watch the calls go down first, then each plot is checked on the way back.";
            messageKind = HarvestConsoleMessageKind.Correct;
            rowsAtMessage = simulation.CompletedRows;
            input.text = "";
            simulation.StartNextRow();
            Refresh();
        }

        void Update()
        {
            if (!IsOpen) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) RequestClose();
            if (closePending && !simulation.IsRunning) { ClosePanel(); return; }
            Refresh();
        }

        public void RequestClose()
        {
            if (!IsOpen) return;
            if (simulation.IsRunning)
            {
                closePending = true;
                message = "Closing when this row finishes. Row progress will be kept.";
                messageKind = HarvestConsoleMessageKind.Queued;
            }
            else ClosePanel();
        }

        void Refresh()
        {
            int row = Mathf.Min(3, simulation.CompletedRows + 1);
            if (!simulation.IsRunning && !simulation.IsComplete && !closePending &&
                messageKind == HarvestConsoleMessageKind.Correct && simulation.CompletedRows > rowsAtMessage)
            {
                message = $"Row {simulation.CompletedRows} harvested! Now fill in the missing expression for Row {row}.";
                messageKind = HarvestConsoleMessageKind.Info;
            }
            var step = simulation.IsRunning ? simulation.CurrentStep : null;
            code.text = Ruin02HarvestConsoleView.BuildCode(row, simulation.IsRunning, step?.Phase);
            string phase = step == null ? "Ready" : step.Phase == HarvestPhase.Call ? "Calling down" :
                step.Phase == HarvestPhase.Base ? "Base case reached" : "Returning";
            // "Magical Farm Completed!" is asserted by the H3 Play check; keep the phrase.
            state.text = simulation.IsComplete ? "Magical Farm Completed!" :
                $"Row {row} of 3  |  {phase}  |  n = {step?.N ?? 10}";
            feedback.text = simulation.IsComplete ? "" : Fyp.Development.UI.FypUiTheme.ForFont(message);
            if (view != null) view.Present(feedback.text, messageKind, closePending);
            input.gameObject.SetActive(!simulation.IsRunning && !simulation.IsComplete);
            submit.gameObject.SetActive(!simulation.IsRunning && !simulation.IsComplete);
            close.GetComponentInChildren<TMP_Text>().text = closePending ? "Closing after row..." : "Close [ESC]";
        }

        void HidePlayerVisuals()
        {
            hiddenRenderers = player != null ? player.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
            hiddenRendererStates = new bool[hiddenRenderers.Length];
            for (int i = 0; i < hiddenRenderers.Length; i++)
            {
                hiddenRendererStates[i] = hiddenRenderers[i].enabled;
                hiddenRenderers[i].enabled = false;
            }
        }

        void RestorePlayerVisuals()
        {
            if (hiddenRenderers == null) return;
            for (int i = 0; i < hiddenRenderers.Length; i++)
                if (hiddenRenderers[i] != null) hiddenRenderers[i].enabled = hiddenRendererStates[i];
            hiddenRenderers = null;
            hiddenRendererStates = null;
        }

        void ClosePanel()
        {
            IsOpen = false;
            panel.SetActive(false);
            RestorePlayerVisuals();
            movement.enabled = moveBefore;
            cameraFollow.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
            var camera = cameraFollow.GetComponent<Camera>();
            camera.rect = cameraRect;
            camera.fieldOfView = cameraFov;
            cameraFollow.enabled = cameraBefore;
            CursorManager.Instance.SetUIOpen(false);
            CursorManager.allowCursorLock = allowBefore;
            Cursor.visible = visibleBefore;
            Cursor.lockState = lockBefore;
            closePending = false;
        }

        void OnDisable() { if (IsOpen) ClosePanel(); }
        void OnDestroy()
        {
            if (submit != null) submit.onClick.RemoveListener(Submit);
            if (close != null) close.onClick.RemoveListener(RequestClose);
            if (input != null) input.onSubmit.RemoveListener(OnEnter);
        }
    }
}
