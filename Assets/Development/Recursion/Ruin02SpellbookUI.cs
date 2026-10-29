using Fyp.Development.Recursion;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class Ruin02SpellbookUI : MonoBehaviour
{
    public GameObject spellbookPanel;
    public TMP_Text titleText, pageNumberText, knowledgeText, codeText;
    public UnityEngine.UI.Button previousButton, nextButton, closeButton;
    public MonoBehaviour playerMovement, cameraFollow;
    public Ruin02SpellbookSimulation guidedSimulation;

    static readonly string[] Titles = {
        "The Returning Spell", "Calls and Returns", "The Sealing Function"
    };
    static readonly string[] Knowledge = {
        "Four ancient staffs have lost their magic. Their colours swirl together in the chamber. Follow the recursive calls down, then watch the waiting calls restore each staff on the way back.",
        "CALL: sealMagic(n) first calls sealMagic(n - 1). Every call keeps its own n on the stack. At n = 0, the base case stops new calls.\n\nRETURN: the newest unfinished call resumes first. absorbMagic(n) runs only after its smaller call has finished.",
        "The order of statements matters. Calling sealMagic(n - 1) comes before absorbMagic(n), so no staff is restored while the calls descend.\n\nReturn order: Air (1), Earth (2), Fire (3), Ice (4)."
    };
    const string FunctionCode = "void sealMagic(int n) {\n    if (n == 0) {\n        return;\n    }\n\n    sealMagic(n - 1);\n    absorbMagic(n);\n}\n\nInitial call: sealMagic(4);";
    int page;
    bool open, movementBefore, cameraBefore;
    public bool IsBookOpen => open;

    void Start()
    {
        previousButton.onClick.AddListener(PreviousPage);
        nextButton.onClick.AddListener(NextPage);
        closeButton.onClick.AddListener(CloseBook);
        spellbookPanel.SetActive(false);
    }
    void Update()
    {
        if (open && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CloseBook();
    }
    public void OpenBook()
    {
        if (open || spellbookPanel == null || !CursorManager.allowCursorLock) return;
        open = true;
        movementBefore = playerMovement != null && playerMovement.enabled;
        cameraBefore = cameraFollow != null && cameraFollow.enabled;
        page = 0;
        ShowPage();
        spellbookPanel.SetActive(true);
        if (playerMovement != null) playerMovement.enabled = false;
        if (cameraFollow != null) cameraFollow.enabled = false;
        InteractionHintManager.Instance?.HideAll();
        if (CursorManager.Instance != null) CursorManager.Instance.SetUIOpen(true);
        else { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
    void ShowPage()
    {
        bool sim = page == 3;
        guidedSimulation.gameObject.SetActive(sim);
        knowledgeText.gameObject.SetActive(!sim);
        codeText.gameObject.SetActive(!sim);
        titleText.text = sim ? "Guided Return-Flow Simulation" : Titles[page];
        if (!sim)
        {
            knowledgeText.text = Knowledge[page];
            codeText.text = page == 2 ? FunctionCode : page == 1 ?
                "CALL: 4 -> 3 -> 2 -> 1 -> 0\nBASE CASE: sealMagic(0) returns\nRETURN: absorbMagic(1) -> (2) -> (3) -> (4)" : "sealMagic(4)";
        }
        pageNumberText.text = (page+1) + " / 4";
        previousButton.interactable = page > 0;
        nextButton.interactable = page < 3;
    }
    public void NextPage() { if (page < 3) { page++; ShowPage(); } }
    public void PreviousPage() { if (page > 0) { page--; ShowPage(); } }
    public void CloseBook()
    {
        if (!open) return;
        open = false;
        spellbookPanel.SetActive(false);
        if (playerMovement != null) playerMovement.enabled = movementBefore;
        if (cameraFollow != null) cameraFollow.enabled = cameraBefore;
        if (CursorManager.Instance != null) CursorManager.Instance.SetUIOpen(false);
        else { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    }
    void OnDestroy()
    {
        if (previousButton != null) previousButton.onClick.RemoveListener(PreviousPage);
        if (nextButton != null) nextButton.onClick.RemoveListener(NextPage);
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseBook);
    }
}
