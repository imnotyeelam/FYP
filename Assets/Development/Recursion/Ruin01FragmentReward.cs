using Fyp.Development.Progress;
using TMPro;
using UnityEngine;

namespace Fyp.Development.Recursion
{
    /// <summary>Ruin 1 reward unlocked by bridge restoration and a successful jump landing.</summary>
    public sealed class Ruin01FragmentReward : MonoBehaviour, ISharedInteractionTarget
    {
        public BridgePracticeController practice;
        public Ruin01MagicJumpController jump;
        public FragmentProgressTracker progress;
        public Transform player;
        public Transform interactionPoint;
        public GameObject gemVisual;
        public GameObject altarVisual;
        public RuinEntrance targetReturn;
        public TMP_Text confirmationText;
        [Min(1f)] public float interactionDistance = 2.6f;
        [Min(.5f)] public float confirmationSeconds = 3.5f;

        bool challengeSolved;
        public bool ChallengeSolved => challengeSolved;
        public bool FragmentCollected => progress != null && progress.HasFragment(FragmentProgressTracker.Ruin01);
        public int RevealCount { get; private set; }
        public int CollectionCount { get; private set; }
        public bool UsesSharedInteraction => true;
        public Transform InteractionOrigin => interactionPoint;
        public float SharedInteractionRange => interactionDistance;
        public string SharedHint => "Press [E] to Collect Fragment";
        public bool CanSharedInteract => isActiveAndEnabled && challengeSolved && !FragmentCollected &&
            gemVisual != null && gemVisual.activeInHierarchy && player != null && interactionPoint != null &&
            practice != null && !practice.IsOpen &&
            Vector3.Distance(player.position, interactionPoint.position) <= interactionDistance;

        void OnEnable()
        {
            if (jump != null) jump.LandedSuccessfully += OnSuccessfulLanding;
        }

        void OnDisable()
        {
            if (jump != null) jump.LandedSuccessfully -= OnSuccessfulLanding;
        }

        void Start()
        {
            if (practice == null || jump == null || progress == null || player == null || interactionPoint == null ||
                gemVisual == null || altarVisual == null || targetReturn == null || confirmationText == null)
            {
                Debug.LogError("Ruin 1 fragment is missing a reward or progression reference.", this);
                enabled = false;
                return;
            }
            confirmationText.transform.parent.gameObject.SetActive(false);
            RefreshReward();
        }

        void OnSuccessfulLanding()
        {
            if (challengeSolved || !practice.IsComplete || !jump.JumpChallengeCompleted) return;
            challengeSolved = true;
            if (!FragmentCollected) RevealCount++;
            RefreshReward();
        }

        void RefreshReward()
        {
            bool collected = FragmentCollected;
            gemVisual.SetActive(challengeSolved && !collected);
            altarVisual.SetActive(challengeSolved || collected);
            targetReturn.enabled = collected;
        }

        public void InteractFromShared()
        {
            if (!CanSharedInteract || !progress.TryCollect(FragmentProgressTracker.Ruin01)) return;
            CollectionCount++;
            RefreshReward();
            InteractionHintManager.Instance?.HideAll();
            FragmentRewardToast.Show(confirmationText,
                "Fragment of Calling collected  •  " + progress.FinalPortalProgress, confirmationSeconds);
        }
    }
}
