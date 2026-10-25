
using UnityEngine;
using UnityEngine.InputSystem;

public class RuinEntrance : MonoBehaviour, ISharedInteractionTarget
{
    [Header("References")]
    public Transform player;
    public Transform destination;
    public TeleportManager teleportManager;
    public GameObject interactionHint;

    [Header("Settings")]
    public float interactionDistance = 3f;
    public bool useSharedInteraction;
    public string sharedHint = "Press [E] to Travel";

    public bool UsesSharedInteraction => useSharedInteraction;
    public Transform InteractionOrigin => transform;
    public float SharedInteractionRange => interactionDistance;
    public bool CanSharedInteract => isActiveAndEnabled && player != null && destination != null && teleportManager != null;
    public string SharedHint => sharedHint;
    public void InteractFromShared()
    {
        if (useSharedInteraction && CanSharedInteract) teleportManager.TeleportTo(destination);
    }

    void Update()
    {
        if (useSharedInteraction) return;
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
