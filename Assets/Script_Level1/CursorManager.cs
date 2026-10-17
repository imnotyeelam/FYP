
using UnityEngine;
using UnityEngine.InputSystem;

public class CursorManager : MonoBehaviour
{
    public bool lockCursorOnStart = false;
    public bool allowCursorLock = true;

    void Start()
    {
        SetCursorLock(lockCursorOnStart);
    }

    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetCursorLock(false);
        }

        if (allowCursorLock &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            Cursor.lockState != CursorLockMode.Locked)
        {
            SetCursorLock(true);
        }
    }

    public void SetCursorLock(bool isLocked)
    {
        Cursor.lockState = isLocked
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !isLocked;
    }
}
