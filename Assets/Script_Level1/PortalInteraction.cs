
using UnityEngine;
using UnityEngine.InputSystem;

public class PortalInteraction : MonoBehaviour
{
    public CursorManager cursorManager;

    [Header("References")]
    public Transform player;
    public GameObject interactionHint;
    public GameObject challengePanel;

    [Header("Interaction Settings")]
    public float interactionDistance = 3f;

    private bool challengeOpen = false;
    private bool portalCompleted = false;

    void Start()
    {
        if (interactionHint != null)
            interactionHint.SetActive(false);

        if (challengePanel != null)
            challengePanel.SetActive(false);
    }

    void Update()
    {
        if (player == null)
            return;

        float distance = Vector3.Distance(
            player.position,
            transform.position
        );

        bool isNear = distance <= interactionDistance;

        if (interactionHint != null)
        {
            interactionHint.SetActive(
                isNear &&
                !challengeOpen &&
                !portalCompleted
            );
        }

        if (!isNear || challengeOpen || portalCompleted)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenChallenge();
        }
    }

    void OpenChallenge()
    {
        challengeOpen = true;

        if (cursorManager != null)
            CursorManager.allowCursorLock = false;

        if (interactionHint != null)
            interactionHint.SetActive(false);

        if (challengePanel != null)
            challengePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Portal challenge opened!");
    }

    public void CloseChallenge()
    {
        challengeOpen = false;

        if (cursorManager != null)
            CursorManager.allowCursorLock = true;

        if (challengePanel != null)
            challengePanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void CompletePortal()
    {
        portalCompleted = true;
        challengeOpen = false;

        if (cursorManager != null)
            CursorManager.allowCursorLock = true;

        if (challengePanel != null)
            challengePanel.SetActive(false);

        if (interactionHint != null)
            interactionHint.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Portal restored!");
    }
}
