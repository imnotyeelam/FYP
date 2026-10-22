
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

    private int currentPage = 0;

    public bool IsBookOpen =>
    spellbookPanel != null && spellbookPanel.activeSelf;

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
        currentPage = 0;
        UpdatePage();

        spellbookPanel.SetActive(true);

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (cameraFollow != null)
            cameraFollow.enabled = false;

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
        titleText.text = titles[currentPage];
        knowledgeText.text = explanations[currentPage];
        codeText.text = codeExamples[currentPage];

        pageNumberText.text =
            (currentPage + 1) + " / " + titles.Length;

        previousButton.interactable = currentPage > 0;
        nextButton.interactable =
            currentPage < titles.Length - 1;
    }

    public void NextPage()
    {
        if (currentPage < titles.Length - 1)
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
        spellbookPanel.SetActive(false);

        if (playerMovement != null)
            playerMovement.enabled = true;

        if (cameraFollow != null)
            cameraFollow.enabled = true;

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
