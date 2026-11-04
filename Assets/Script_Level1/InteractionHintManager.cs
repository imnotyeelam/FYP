
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-200)]
public class InteractionHintManager : MonoBehaviour
{
    public static InteractionHintManager Instance { get; private set; }

    [Header("Shared TMP")]
    public TMP_Text hintText;

    [Header("Development scene: coordinated interaction (opt-in)")]
    public bool coordinateInteractions;
    public Transform player;
    public MonoBehaviour[] interactionTargets;
    public GameObject[] modalPanels;

    private readonly List<SharedInteractionCandidate> candidates = new List<SharedInteractionCandidate>();
    private readonly SharedInteractionPressGate pressGate = new SharedInteractionPressGate();

    private object currentOwner;
    private int lastSelectedIndex = int.MinValue;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (hintText != null)
            hintText.gameObject.SetActive(false);
    }

    public void ShowHint(object owner, string message)
    {
        // In coordinated mode only the selected target may drive this one TMP.
        if (coordinateInteractions) return;
        Present(owner, message);
    }

    private void Present(object owner, string message)
    {
        if (owner == null || hintText == null)
            return;

        currentOwner = owner;

        hintText.text = message;
        hintText.gameObject.SetActive(true);
    }

    public void HideHint(object owner)
    {
        if (currentOwner != owner)
            return;

        currentOwner = null;

        if (hintText != null)
            hintText.gameObject.SetActive(false);
    }

    public void HideAll()
    {
        currentOwner = null;

        if (hintText != null)
            hintText.gameObject.SetActive(false);
    }

    private bool ModalOpen()
    {
        if (!CursorManager.allowCursorLock || Cursor.lockState != CursorLockMode.Locked) return true;
        if (modalPanels != null)
            foreach (var panel in modalPanels)
                if (panel != null && panel.activeInHierarchy) return true;
        return false;
    }

    private void Update() { if (coordinateInteractions) Refresh(true); }

    // Catch UI opening, Escape unlocking or teleports after this manager's Update.
    private void LateUpdate() { if (coordinateInteractions) Refresh(false); }

    private void Refresh(bool dispatch)
    {
        if (player == null || interactionTargets == null || ModalOpen())
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                Debug.Log($"[Ruin02 Interaction] Shared interaction blocked before selection: player={(player!=null)}, targets={(interactionTargets!=null)}, modal={ModalOpen()}, cursorAllowed={CursorManager.allowCursorLock}, cursorLock={Cursor.lockState}");
            HideAll(); return;
        }
        candidates.Clear();
        foreach (var behaviour in interactionTargets)
        {
            var target = behaviour as ISharedInteractionTarget;
            bool eligible = behaviour != null && behaviour.isActiveAndEnabled && target != null &&
                target.UsesSharedInteraction && target.CanSharedInteract && target.InteractionOrigin != null;
            candidates.Add(new SharedInteractionCandidate(eligible,
                eligible ? (target.InteractionOrigin.position - player.position).sqrMagnitude : 0,
                eligible ? target.SharedInteractionRange : 0));
        }
        int selected = SharedInteractionSelection.Nearest(candidates);
        if (selected < 0)
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                Debug.Log("[Ruin02 Interaction] E pressed but no eligible shared target was within range.");
            lastSelectedIndex = -1; HideAll(); return;
        }
        var chosen = (ISharedInteractionTarget)interactionTargets[selected];
        if (selected != lastSelectedIndex && interactionTargets[selected] is Fyp.Development.Recursion.Ruin02PracticeController)
            Debug.Log($"[Ruin02 Interaction] Challenge landmark selected: index={selected}, hint='{chosen.SharedHint}', range={chosen.SharedInteractionRange:F2}.");
        lastSelectedIndex = selected;
        Present(interactionTargets[selected], chosen.SharedHint);
        if (!dispatch || !pressGate.TryConsume(Time.frameCount,
            Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)) return;
        // Consume before invoking: teleporting cannot activate a second target on this key edge.
        HideAll();
        if (interactionTargets[selected] is Fyp.Development.Recursion.Ruin02PracticeController)
            Debug.Log("[Ruin02 Interaction] E dispatched to selected challenge landmark.");
        chosen.InteractFromShared();
    }

    private void OnDisable() { HideAll(); }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
