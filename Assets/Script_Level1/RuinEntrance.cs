
using UnityEngine;
using UnityEngine.InputSystem;

public class RuinEntrance : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform destination;
    public TeleportManager teleportManager;
    public GameObject interactionHint;

    [Header("Settings")]
    public float interactionDistance = 3f;

    void Update()
    {
        if (player == null)
            return;

        bool isNear = Vector3.Distance(
            player.position,
            transform.position
        ) <= interactionDistance;

        if (interactionHint != null)
            interactionHint.SetActive(isNear);

        if (isNear &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (teleportManager != null)
                teleportManager.TeleportTo(destination);
        }
    }
}
