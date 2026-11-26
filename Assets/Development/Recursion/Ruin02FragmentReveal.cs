using System.Collections;
using Fyp.Development.Progress;
using TMPro;
using UnityEngine;

namespace Fyp.Development.Recursion
{
    // The original ritual prerequisites and the completed farm jointly authorize the existing reward.
    public sealed class Ruin02FragmentReveal : MonoBehaviour, ISharedInteractionTarget
    {
        public Ruin02PracticeController practice;
        public Ruin02AirRestoration restoration;
        public Ruin02FarmSimulation farm;
        public GameObject fragmentVisual;
        [Header("D3-B collection")]
        public FragmentProgressTracker progress;
        public Transform player;
        public Transform interactionPoint;
        public TMP_Text confirmationText;
        [Min(1f)] public float interactionDistance=2.8f;
        [Min(.5f)] public float confirmationSeconds=3.5f;
        [Header("Phase C reveal notification (presentation only)")]
        public string revealMessage="The Fragment of Returning Magic has appeared in the Ritual Chamber!";
        [Min(.5f)] public float revealMessageSeconds=4.5f;

        bool revealed;
        Coroutine waitForCompletion;
        public bool IsRevealed => revealed;
        public bool FragmentCollected => progress!=null && progress.HasFragment(FragmentProgressTracker.Ruin02);
        public bool UsesSharedInteraction=>true;
        public Transform InteractionOrigin=>interactionPoint;
        public float SharedInteractionRange=>interactionDistance;
        public string SharedHint=>"Press [E] to Collect Fragment";
        public bool CanSharedInteract=>isActiveAndEnabled && revealed && CanReveal() && !FragmentCollected && fragmentVisual!=null &&
            fragmentVisual.activeInHierarchy && player!=null && interactionPoint!=null && practice!=null && !practice.IsOpen &&
            Vector3.Distance(player.position,interactionPoint.position)<=interactionDistance;

        void Start()
        {
            if (practice == null || restoration == null || farm == null || fragmentVisual == null || progress==null || player==null ||
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
            farm.Completed += CheckCompletion;
            CheckCompletion();
        }

        void OnChallengeCompleted()
        {
            if (waitForCompletion == null) waitForCompletion = StartCoroutine(WaitForCompletion());
        }

        IEnumerator WaitForCompletion()
        {
            while (!revealed && !FragmentCollected && !CanReveal()) yield return null;
            CheckCompletion();
            waitForCompletion = null;
        }

        void CheckCompletion()
        {
            if (!revealed && !FragmentCollected && CanReveal()) Reveal();
        }

        bool CanReveal()
        {
            return practice != null && practice.IsSolved && practice.Session != null &&
                   practice.Session.Current.Frames.Count == 0 && restoration != null &&
                   restoration.RestoredCount == 4 && !restoration.IsPlaying &&
                   restoration.IsStaffRestored(1) && restoration.IsStaffRestored(2) &&
                   restoration.IsStaffRestored(3) && restoration.IsStaffRestored(4) &&
                   farm != null && farm.IsComplete && !farm.IsRunning && farm.CompletedRows == 3 &&
                   farm.PlotsChecked == 30 && farm.CropsHarvested == 15 && farm.StackDepth == 0;
        }

        void Reveal()
        {
            if (revealed) return;
            revealed = true;
            fragmentVisual.SetActive(true);
            // Shown once: Reveal() is guarded by 'revealed'. Collection is still a separate E action.
            FragmentRewardToast.Show(confirmationText, revealMessage, revealMessageSeconds);
            Debug.Log("Ruin 2 Fragment of Returning Magic revealed after Staff Restoration and Magical Farm completion.", this);
        }

        public void InteractFromShared()
        {
            if(!CanSharedInteract || !progress.TryCollect(FragmentProgressTracker.Ruin02))return;
            fragmentVisual.SetActive(false);
            InteractionHintManager.Instance?.HideAll();
            FragmentRewardToast.Show(confirmationText,
                "Fragment of Returning Magic collected  |  " + progress.FinalPortalProgress, confirmationSeconds);
        }

        void OnDestroy()
        {
            if (practice != null) practice.ChallengeCompleted -= OnChallengeCompleted;
            if (farm != null) farm.Completed -= CheckCompletion;
        }
    }
}
