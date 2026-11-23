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
        public bool IsOpen { get; private set; }
        bool moveBefore, cameraBefore, allowBefore, visibleBefore, closePending;
        CursorLockMode lockBefore;
        Vector3 cameraPosition;
        Rect cameraRect;
        float cameraFov;
        Quaternion cameraRotation;
        string message = "";
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
            panel.SetActive(true);
            message = "";
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
                Refresh();
                return;
            }
            message = "Correct — follow the calls down, then watch plot checks on return.";
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
            }
            else ClosePanel();
        }

        void Refresh()
        {
            int row = Mathf.Min(3, simulation.CompletedRows + 1);
            code.text = "void harvest(int row, int n) {\n" +
                $"  if ({(row == 1 ? "____" : "n == 0")}) return;\n" +
                $"  harvest(row, {(row == 2 ? "____" : "n - 1")});\n" +
                $"  if ({(row == 3 ? "____" : "hasCrop(row, n)")})\n" +
                "    collectCrop(row, n);\n}";
            state.text = simulation.IsComplete ? "Magical Farm Completed!\nPlots Checked 30/30 · Crops Harvested 15/15\nStack depth 0" :
                $"Row {row}/3 · {(simulation.IsRunning ? simulation.CurrentStep.Phase.ToString() : "Ready")} " +
                $"· n = {simulation.CurrentStep?.N ?? 10}\nStack depth {simulation.StackDepth}\n" +
                $"Plots Checked {simulation.PlotsChecked}/30 · Crops Harvested {simulation.CropsHarvested}/15";
            feedback.text = simulation.IsComplete ? "Magical Farm Completed!" : message;
            input.gameObject.SetActive(!simulation.IsRunning && !simulation.IsComplete);
            submit.gameObject.SetActive(!simulation.IsRunning && !simulation.IsComplete);
            close.GetComponentInChildren<TMP_Text>().text = closePending ? "Close queued" : "Close [ESC]";
        }

        void ClosePanel()
        {
            IsOpen = false;
            panel.SetActive(false);
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
