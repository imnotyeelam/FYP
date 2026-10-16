
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Header("Camera Settings")]
    public float distance = 6f;
    public float height = 1.5f;
    public float sensitivity = 0.15f;
    public float minPitch = -20f;
    public float maxPitch = 65f;

    private float yaw;
    private float pitch = 20f;

    void Start()
    {
        yaw = transform.eulerAngles.y;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        if (Cursor.lockState == CursorLockMode.Locked &&
            Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            yaw += mouseDelta.x * sensitivity;
            pitch -= mouseDelta.y * sensitivity;

            pitch = Mathf.Clamp(
                pitch,
                minPitch,
                maxPitch
            );
        }

        Quaternion rotation =
            Quaternion.Euler(pitch, yaw, 0f);

        Vector3 lookTarget =
            target.position + Vector3.up * height;

        transform.position =
            lookTarget +
            rotation * new Vector3(0f, 0f, -distance);

        transform.LookAt(lookTarget);
    }
}
