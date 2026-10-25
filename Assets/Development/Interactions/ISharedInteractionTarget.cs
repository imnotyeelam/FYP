using UnityEngine;

// Opt-in only: scenes without coordinated interaction retain their legacy behaviour.
public interface ISharedInteractionTarget
{
    bool UsesSharedInteraction { get; }
    Transform InteractionOrigin { get; }
    float SharedInteractionRange { get; }
    bool CanSharedInteract { get; }
    string SharedHint { get; }
    void InteractFromShared();
}
