
using UnityEngine;
using UnityEngine.InputSystem;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    private bool isUIOpen = false;
    public static bool allowCursorLock = true;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        LockCursor();
    }

    void Update()
    {
        if (isUIOpen || !allowCursorLock)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            UnlockCursor();
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }
    }

    public void SetUIOpen(bool open)
    {
        isUIOpen = open;

        if (open)
        {
            allowCursorLock = false;
            UnlockCursor();
        }
        else
        {
            allowCursorLock = true;
            LockCursor();
        }
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
