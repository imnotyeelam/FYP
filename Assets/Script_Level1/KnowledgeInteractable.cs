
using UnityEngine;
using UnityEngine.InputSystem;

public class KnowledgeInteractable : MonoBehaviour, ISharedInteractionTarget
{
    public Transform player;
    public AncientSpellbookUI spellbookUI;

    public float interactionDistance = 3f;
    public bool useSharedInteraction;

    public bool UsesSharedInteraction => useSharedInteraction;
    public Transform InteractionOrigin => transform;
    public float SharedInteractionRange => interactionDistance;
    public bool CanSharedInteract => isActiveAndEnabled && player != null && spellbookUI != null &&
        spellbookUI.isActiveAndEnabled && !spellbookUI.IsBookOpen;
    public string SharedHint => "Press [E] to Read the Ancient Spellbook";
    public void InteractFromShared()
    {
        if (useSharedInteraction && CanSharedInteract) spellbookUI.OpenBook();
    }

    private bool hintShowing = false;

    void Update()
    {
        if (useSharedInteraction) return;
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
        if (useSharedInteraction) return;
        InteractionHintManager.Instance?.HideHint(this);
        hintShowing = false;
    }
}
