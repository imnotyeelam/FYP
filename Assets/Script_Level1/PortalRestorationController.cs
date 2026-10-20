using System.Collections;
using UnityEngine;

public class PortalRestorationController : MonoBehaviour
{
    [Header("Portal References")]
    public GameObject brokenPortal;
    public GameObject completePortal;
    public GameObject challengePanel;

    [Header("Ruins / Pillars")]
    public GameObject[] ruinsToActivate;

    [Header("Optional VFX Objects")]
    public GameObject[] ruinEffects;
    public GameObject completePortalEffect;

    [Header("Timing")]
    public float ruinActivateDelay = 0.6f;
    public float portalRestoreDelay = 1f;

    private bool hasRestored = false;

    void Start()
    {
        if (completePortal != null)
            completePortal.SetActive(false);

        if (completePortalEffect != null)
            completePortalEffect.SetActive(false);

        if (ruinEffects != null)
        {
            foreach (GameObject fx in ruinEffects)
            {
                if (fx != null)
                    fx.SetActive(false);
            }
        }
    }

    public void RestoreMagic()
    {
        if (!hasRestored)
        {
            StartCoroutine(RestoreSequence());
        }
    }

    private IEnumerator RestoreSequence()
    {
        hasRestored = true;

        // Close UI first
        if (challengePanel != null)
            challengePanel.SetActive(false);

        // Unlock game view back to exploration mode if needed
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Activate ruin effects one by one
        for (int i = 0; i < ruinsToActivate.Length; i++)
        {
            if (ruinsToActivate[i] != null)
            {
                RuinGlow glow =
                    ruinsToActivate[i].GetComponent<RuinGlow>();

                if (glow != null)
                {
                    glow.ActivateGlow();
                }
            }

            if (ruinEffects != null && i < ruinEffects.Length && ruinEffects[i] != null)
            {
                ruinEffects[i].SetActive(true);
            }

            yield return new WaitForSeconds(ruinActivateDelay);
        }

        yield return new WaitForSeconds(portalRestoreDelay);

        // Swap broken portal to complete portal
        if (brokenPortal != null)
            brokenPortal.SetActive(false);

        if (completePortal != null)
            completePortal.SetActive(true);

        if (completePortalEffect != null)
            completePortalEffect.SetActive(true);

        Debug.Log("Magic restored!");
    }
}