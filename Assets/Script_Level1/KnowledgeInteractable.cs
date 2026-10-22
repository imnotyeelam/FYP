
using UnityEngine;
using UnityEngine.InputSystem;

public class KnowledgeInteractable : MonoBehaviour
{
    public Transform player;
    public AncientSpellbookUI spellbookUI;

    public float interactionDistance = 3f;

    private bool hintShowing = false;

    void Update()
    {
        if (player == null || spellbookUI == null)
            return;

        bool isNear = Vector3.Distance(
            player.position,
            transform.position
        ) <= interactionDistance;

        bool canInteract = isNear && !spellbookUI.IsBookOpen;

        if (canInteract && !hintShowing)
        {
            InteractionHintManager.Instance?.ShowHint(
                this,
                "Press [E] to Read"
            );

            hintShowing = true;
        }
        else if (!canInteract && hintShowing)
        {
            InteractionHintManager.Instance?.HideHint(this);
            hintShowing = false;
        }

        if (canInteract &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            InteractionHintManager.Instance?.HideHint(this);
            hintShowing = false;

            spellbookUI.OpenBook();
        }
    }

    void OnDisable()
    {
        InteractionHintManager.Instance?.HideHint(this);
        hintShowing = false;
    }
}
