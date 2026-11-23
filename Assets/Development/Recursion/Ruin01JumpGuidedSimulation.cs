using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Fyp.Development.Recursion
{
    /// <summary>Standalone UI-only example. No route markers, Wizard motion or rewards are used.</summary>
    public sealed class Ruin01JumpGuidedSimulation : MonoBehaviour
    {
        public Ruin01MagicJumpController jump;
        public GameObject panel;
        public TMP_Text heading, code, instruction, state;
        public TMP_Text[] stackFrames;
        public UnityEngine.UI.Image[] previewLandings;
        public RectTransform previewOrb;
        public UnityEngine.UI.Button previous, next, finish, close;
        public bool IsOpen { get; private set; }
        public bool IsCompleted { get; private set; }
        public int Checkpoint { get; private set; }
        public int StackDepth { get; private set; }
        public int ExampleLeaps { get; private set; }
        public bool PreviewRunning => unwind != null;
        bool movementBefore, cameraBefore, allowBefore, visibleBefore, unwound;
        CursorLockMode lockBefore;
        GameObject selectionBefore;
        Coroutine unwind;
        readonly string[] titles = { "A spell that calls itself", "Begin with magicJump(3)", "The n = 3 frame",
            "The n = 2 frame", "The n = 1 frame", "Base case: n = 0", "Return through the stack", "Try the real challenge" };
        readonly string[] explanations = {
            "Each nonzero call waits for a smaller call, then makes one leap when it returns. Use Next to follow a small example.",
            "This tutorial uses n = 3. Push the first call onto the stack. The illustration below is separate from the real world.",
            "3 is not zero. Call magicJump(2). The n = 3 frame waits; its leap has not happened yet.",
            "2 is not zero. Call magicJump(1). Another frame waits on the stack. No leaps have happened yet.",
            "1 is not zero. Call magicJump(0). The next call will stop the descent.",
            "n == 0 is true. Return immediately: no leap for this frame. The waiting calls can now resume.",
            "Watch the return phase: n = 1 makes Leap 1, n = 2 makes Leap 2, then n = 3 makes Leap 3. Each finished frame leaves the stack.",
            "The example's stack is empty. In the real challenge, choose n and observe where your spell stops. Use what you learned to adjust it. Click Finish to open the input panel."
        };
        void Awake()
        {
            panel.SetActive(false);
            previous.onClick.AddListener(Previous); next.onClick.AddListener(Next);
            finish.onClick.AddListener(Finish); close.onClick.AddListener(Close);
        }
        public void Open()
        {
            if (IsOpen || IsCompleted || jump == null || !jump.CanSharedInteract) return;
            movementBefore = jump.playerMovement.enabled; cameraBefore = jump.cameraFollow.enabled;
            allowBefore = CursorManager.allowCursorLock; lockBefore = Cursor.lockState; visibleBefore = Cursor.visible;
            selectionBefore = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            IsOpen = true; jump.playerMovement.enabled = false; jump.cameraFollow.enabled = false;
            InteractionHintManager.Instance?.HideAll();
            if (CursorManager.Instance != null) CursorManager.Instance.SetUIOpen(true);
            else { CursorManager.allowCursorLock = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            panel.SetActive(true); Refresh();
            EventSystem.current?.SetSelectedGameObject(next.gameObject);
        }
        // Close after CursorManager.Update so it cannot handle the same ESC again after restoration.
        void LateUpdate()
        {
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }
        public void Previous()
        {
            if (!IsOpen || Checkpoint == 0) return;
            StopPreview(); Checkpoint--; Refresh();
        }
        public void Next()
        {
            if (!IsOpen || Checkpoint >= 7 || PreviewRunning) return;
            Checkpoint++; Refresh();
        }
        public void Finish()
        {
            if (!IsOpen || Checkpoint != 7 || !unwound || PreviewRunning) return;
            IsCompleted = true; Close(); jump.Open();
        }
        void Refresh()
        {
            StopPreview();
            heading.text = $"Magic Jump Guided Simulation  ·  {Checkpoint + 1}/8\n{titles[Checkpoint]}";
            instruction.text = explanations[Checkpoint];
            previous.interactable = Checkpoint > 0; next.gameObject.SetActive(Checkpoint < 7);
            next.interactable = true; finish.gameObject.SetActive(Checkpoint == 7);
            ExampleLeaps = Checkpoint == 7 ? 3 : 0;
            for (int i=0;i<3;i++) previewLandings[i].color = Checkpoint == 7 ? new Color(.68f,.46f,.95f,1) : new Color(.28f,.18f,.48f,.8f);
            MoveOrb(ExampleLeaps,0);
            int depth = Checkpoint <= 0 || Checkpoint == 7 ? 0 : Mathf.Clamp(Checkpoint - 1,1,4);
            if (Checkpoint == 1) depth = 1;
            int n = Checkpoint <= 2 ? 3 : Checkpoint <= 5 ? 5-Checkpoint : 0;
            ShowStack(depth);
            ShowCode(Checkpoint == 0 ? 0 : Checkpoint == 1 ? 7 : Checkpoint == 5 ? 1 : Checkpoint == 7 ? -1 : 2);
            state.text = Checkpoint == 0 || Checkpoint == 7 ? $"Phase: {(Checkpoint==0 ? "Ready" : "Complete")}   ·   Stack depth: {StackDepth}" :
                $"n = {n}   ·   Phase: {(Checkpoint==5 ? "Base" : Checkpoint==6 ? "Return" : "Call")}   ·   Stack depth: {StackDepth}";
            if(Checkpoint==6){next.interactable=false;unwind=StartCoroutine(Unwind());}
        }
        void ShowCode(int highlighted)
        {
            string[] lines={"void magicJump(int n) {", "    if (n == 0) return;", "    magicJump(n - 1);", "    magicalLeap();", "}", "", "// tutorial example only", "magicJump(3);"};
            for(int i=0;i<lines.Length;i++)if(i==highlighted)lines[i]="<color=#E5B8FF>"+lines[i]+"  // executing</color>";
            code.text=string.Join("\n",lines);
        }
        void ShowStack(int depth)
        {
            StackDepth=depth;
            for(int i=0;i<stackFrames.Length;i++)
            {
                stackFrames[i].transform.parent.gameObject.SetActive(i<depth);
                stackFrames[i].text=$"magicJump({3-i})"+(i==depth-1 ? "  < TOP" : "  waiting");
            }
        }
        IEnumerator Unwind()
        {
            ShowStack(3); yield return new WaitForSecondsRealtime(.35f);
            for(int i=0;i<3;i++)
            {
                int n=i+1; ShowCode(3);
                state.text=$"n = {n}   ·   Phase: Return   ·   Stack depth: {StackDepth}   ·   Example leap {i+1}";
                float elapsed=0;
                while(elapsed<.65f){elapsed+=Time.unscaledDeltaTime;MoveOrb(i,Mathf.Clamp01(elapsed/.65f));yield return null;}
                ExampleLeaps=i+1; previewLandings[i].color=new Color(.68f,.46f,.95f,1);
                ShowStack(2-i); yield return new WaitForSecondsRealtime(.2f);
            }
            ShowCode(4); state.text="Phase: Return complete   ·   Stack depth: 0   ·   Example leaps: 3";
            unwound=true; unwind=null; next.interactable=true;
        }
        void MoveOrb(int start,float t)
        {
            float x=Mathf.Lerp(.1f+.26f*start,.1f+.26f*(start+1),t);
            previewOrb.anchorMin=previewOrb.anchorMax=new Vector2(x,.37f+Mathf.Sin(t*Mathf.PI)*.42f);
            previewOrb.anchoredPosition=Vector2.zero;
        }
        void StopPreview(){if(unwind!=null)StopCoroutine(unwind);unwind=null;}
        public void Close()
        {
            if(!IsOpen)return;
            StopPreview(); IsOpen=false; panel.SetActive(false);
            if(CursorManager.Instance!=null)CursorManager.Instance.SetUIOpen(false);
            CursorManager.allowCursorLock=allowBefore;Cursor.lockState=lockBefore;Cursor.visible=visibleBefore;
            jump.playerMovement.enabled=movementBefore;jump.cameraFollow.enabled=cameraBefore;
            EventSystem.current?.SetSelectedGameObject(selectionBefore);
        }
        void OnDisable(){Close();}
        void OnDestroy()
        {
            previous?.onClick.RemoveListener(Previous);next?.onClick.RemoveListener(Next);
            finish?.onClick.RemoveListener(Finish);close?.onClick.RemoveListener(Close);
        }
    }
}
