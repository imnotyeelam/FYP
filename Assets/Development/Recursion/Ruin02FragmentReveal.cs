using System.Collections;
using Fyp.Development.Progress;
using TMPro;
using UnityEngine;

namespace Fyp.Development.Recursion
{
    // D3-B extends D3-A with the shared E interaction; the practice trace remains the only reveal authority.
    public sealed class Ruin02FragmentReveal : MonoBehaviour, ISharedInteractionTarget
    {
        public Ruin02PracticeController practice;
        public Ruin02AirRestoration restoration;
        public GameObject fragmentVisual;
        [Header("D3-B collection")]
        public FragmentProgressTracker progress;
        public Transform player;
        public Transform interactionPoint;
        public TMP_Text confirmationText;
        [Min(1f)] public float interactionDistance=2.8f;
        [Min(.5f)] public float confirmationSeconds=3.5f;

        bool revealed;
        Coroutine waitForCompletion;
        float confirmationUntil;
        public bool IsRevealed => revealed;
        public bool FragmentCollected => progress!=null && progress.HasFragment(FragmentProgressTracker.Ruin02);
        public bool UsesSharedInteraction=>true;
        public Transform InteractionOrigin=>interactionPoint;
        public float SharedInteractionRange=>interactionDistance;
        public string SharedHint=>"Press [E] to Collect Fragment";
        public bool CanSharedInteract=>isActiveAndEnabled && revealed && !FragmentCollected && fragmentVisual!=null &&
            fragmentVisual.activeInHierarchy && player!=null && interactionPoint!=null && practice!=null && !practice.IsOpen &&
            Vector3.Distance(player.position,interactionPoint.position)<=interactionDistance;

        void Start()
        {
            if (practice == null || restoration == null || fragmentVisual == null || progress==null || player==null ||
                interactionPoint==null || confirmationText==null)
            {
                Debug.LogError("Ruin 2 fragment reveal references are incomplete.", this);
                enabled = false;
                return;
            }

            // The visual is a scene instance and always begins unavailable in a fresh Play session.
            fragmentVisual.SetActive(false);
            confirmationText.transform.parent.gameObject.SetActive(false);
            practice.ChallengeCompleted += OnChallengeCompleted;
            CheckCompletion();
        }

        void OnChallengeCompleted()
        {
            if (waitForCompletion == null) waitForCompletion = StartCoroutine(WaitForCompletion());
        }

        IEnumerator WaitForCompletion()
        {
            while (!CanReveal()) yield return null;
            Reveal();
            waitForCompletion = null;
        }

        void CheckCompletion()
        {
            if (!revealed && CanReveal()) Reveal();
        }

        bool CanReveal()
        {
            return practice != null && practice.IsSolved && practice.Session != null &&
                   practice.Session.Current.Frames.Count == 0 && restoration != null &&
                   restoration.RestoredCount == 4 && !restoration.IsPlaying;
        }

        void Reveal()
        {
            if (revealed) return;
            revealed = true;
            fragmentVisual.SetActive(true);
            Debug.Log("Ruin 2 Fragment of Returning Magic revealed after the completed restoration sequence.", this);
        }

        public void InteractFromShared()
        {
            if(!CanSharedInteract || !progress.TryCollect(FragmentProgressTracker.Ruin02))return;
            fragmentVisual.SetActive(false);
            InteractionHintManager.Instance?.HideAll();
            confirmationText.text="Fragment of Returning Magic collected  •  "+progress.CollectedCount+"/3";
            confirmationText.gameObject.SetActive(true);
            confirmationText.transform.parent.gameObject.SetActive(true);
            confirmationUntil=Time.unscaledTime+confirmationSeconds;
        }

        void Update()
        {
            if(confirmationText!=null && confirmationText.transform.parent.gameObject.activeSelf &&
                Time.unscaledTime>=confirmationUntil)confirmationText.transform.parent.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (practice != null) practice.ChallengeCompleted -= OnChallengeCompleted;
        }
    }
}
