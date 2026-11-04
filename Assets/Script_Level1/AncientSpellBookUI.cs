
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Fyp.Development.Recursion;

public class AncientSpellbookUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject spellbookPanel;

    public TMP_Text titleText;
    public TMP_Text pageNumberText;
    public TMP_Text knowledgeText;
    public TMP_Text codeText;

    public Button previousButton;
    public Button nextButton;
    public Button closeButton;

    [Header("Player References")]
    public MonoBehaviour playerMovement;
    public MonoBehaviour cameraFollow;

    [Header("Optional development-only fourth page (leave empty for the original three pages)")]
    public SpellbookRecursionSimulation guidedSimulation;
    private bool movementWasEnabled, cameraWasEnabled, sessionOpen;
    private int PageCount => titles.Length + (guidedSimulation != null ? 1 : 0);

    private int currentPage = 0;

    public bool IsBookOpen =>
    spellbookPanel != null && spellbookPanel.activeSelf;
    public bool GuidedLessonCompleted => guidedSimulation != null && guidedSimulation.IsCompleted;

    private readonly string[] titles =
    {
        "What Is Recursion?",
        "Recursive Calls",
        "Changing Parameters"
    };

    private readonly string[] explanations =
    {
        "Recursion is a programming technique where a function calls itself to solve a smaller version of the same problem.",

        "A recursive call happens when a function calls itself. Each new call has its own parameter values.",

        "In buildBridge(n - 1), the argument decreases by one each time. The calls continue until the base case is reached."
    };

    private readonly string[] codeExamples =
    {
        "void buildBridge(int n)\n" +
        "{\n" +
        "    if (n == 0) return;\n" +
        "    placeStone(n);\n" +
        "    buildBridge(n - 1);\n" +
        "}",

        "buildBridge(3)\n" +
        "  -> buildBridge(2)\n" +
        "  -> buildBridge(1)\n" +
        "  -> buildBridge(0)",

        "n = 3\n" +
        "n = 2\n" +
        "n = 1\n" +
        "n = 0  // Base case"
    };

    void Start()
    {
        previousButton.onClick.AddListener(PreviousPage);
        nextButton.onClick.AddListener(NextPage);
        closeButton.onClick.AddListener(CloseBook);

        spellbookPanel.SetActive(false);
    }

    public void OpenBook()
    {
        OpenBookAtPage(0);
    }

    public void OpenGuidedSimulationForChallenge()
    {
        OpenBookAtPage(titles.Length);
    }

    private void OpenBookAtPage(int requestedPage)
    {
        if (sessionOpen || spellbookPanel == null || !CursorManager.allowCursorLock) return;
        sessionOpen = true;
        movementWasEnabled = playerMovement != null && playerMovement.enabled;
        cameraWasEnabled = cameraFollow != null && cameraFollow.enabled;
        currentPage = Mathf.Clamp(requestedPage, 0, PageCount - 1);
        UpdatePage();

        spellbookPanel.SetActive(true);

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (cameraFollow != null)
            cameraFollow.enabled = false;

        InteractionHintManager.Instance?.HideAll();

        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetUIOpen(true);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void UpdatePage()
    {
        bool simulationPage = guidedSimulation != null && currentPage == titles.Length;
        if (guidedSimulation != null)
        {
            guidedSimulation.gameObject.SetActive(simulationPage);
            knowledgeText.gameObject.SetActive(!simulationPage);
            codeText.gameObject.SetActive(!simulationPage);
        }
        titleText.text = simulationPage
            ? "Guided Bridge Simulation — Complete the guided simulation to unlock this challenge"
            : titles[currentPage];
        if (!simulationPage)
        {
            knowledgeText.text = explanations[currentPage];
            codeText.text = codeExamples[currentPage];
        }

        pageNumberText.text =
            (currentPage + 1) + " / " + PageCount;

        previousButton.interactable = currentPage > 0;
        nextButton.interactable =
            currentPage < PageCount - 1;
    }

    public void NextPage()
    {
        if (currentPage < PageCount - 1)
        {
            currentPage++;
            UpdatePage();
        }
    }

    public void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdatePage();
        }
    }

    public void CloseBook()
    {
        if (!sessionOpen) return;
        sessionOpen = false;
        spellbookPanel.SetActive(false);

        if (playerMovement != null)
            playerMovement.enabled = guidedSimulation != null ? movementWasEnabled : true;

        if (cameraFollow != null)
            cameraFollow.enabled = guidedSimulation != null ? cameraWasEnabled : true;

        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetUIOpen(false);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
