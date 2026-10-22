
using UnityEngine;

public class TeleportManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    private CharacterController controller;

    void Awake()
    {
        if (player != null)
        {
            controller =
                player.GetComponent<CharacterController>();
        }
    }

    public void TeleportTo(Transform destination)
    {
        if (player == null || destination == null)
        {
            Debug.LogWarning("Teleport reference missing.");
            return;
        }

        if (controller != null)
            controller.enabled = false;

        player.position = destination.position;
        player.rotation = destination.rotation;

        if (controller != null)
            controller.enabled = true;

        Debug.Log("Teleported to: " + destination.name);
    }
}
