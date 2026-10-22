
using UnityEngine;
using TMPro;

public class InteractionHintManager : MonoBehaviour
{
    public static InteractionHintManager Instance { get; private set; }

    [Header("Shared TMP")]
    public TMP_Text hintText;

    private object currentOwner;

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
}
