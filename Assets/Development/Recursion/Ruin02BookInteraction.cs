using UnityEngine;

public sealed class Ruin02BookInteraction : MonoBehaviour, ISharedInteractionTarget
{
    public Transform player;
    public Ruin02SpellbookUI book;
    public float interactionDistance = 3f;
    public bool UsesSharedInteraction => true;
    public Transform InteractionOrigin => transform;
    public float SharedInteractionRange => interactionDistance;
    public bool CanSharedInteract => isActiveAndEnabled && player != null && book != null &&
        book.isActiveAndEnabled && !book.IsBookOpen && CursorManager.allowCursorLock;
    public string SharedHint => "Press [E] to Read the Ancient Spellbook";
    public void InteractFromShared() { if (CanSharedInteract) book.OpenBook(); }
}
