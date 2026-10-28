using Fyp.Development.Progress;
using TMPro;
using UnityEngine;

namespace Fyp.Development.Recursion
{
    /// <summary>Ruin 1 reward presentation; the six-decision practice remains the source of completion.</summary>
    public sealed class Ruin01FragmentReward : MonoBehaviour, ISharedInteractionTarget
    {
        public BridgePracticeController practice;
        public FragmentProgressTracker progress;
        public Transform player;
        public Transform interactionPoint;
        public GameObject gemVisual;
        public TMP_Text confirmationText;
        [Min(1f)] public float interactionDistance = 2.6f;
        [Min(.5f)] public float confirmationSeconds = 3.5f;

        bool challengeSolved;
        float confirmationUntil;
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
            if (practice != null) practice.Completed += OnChallengeCompleted;
        }

        void Start()
        {
            if (practice == null || progress == null || player == null || interactionPoint == null ||
                gemVisual == null || confirmationText == null)
            {
                Debug.LogError("Ruin 1 fragment requires explicit practice, progress, player, marker, gem and confirmation references.", this);
                enabled = false;
                return;
            }
            gemVisual.SetActive(false);
            confirmationText.transform.parent.gameObject.SetActive(false);
            if (practice.IsComplete) OnChallengeCompleted();
        }

        void OnDisable()
        {
            if (practice != null) practice.Completed -= OnChallengeCompleted;
        }

        void OnChallengeCompleted()
        {
            if (challengeSolved || practice == null) return;
            if (!practice.IsComplete || practice.CheckpointIndex != 21 || practice.Current.Frames.Count != 0 ||
                practice.RestorationCount != 3 ||
                !practice.rig.placeStone3.IsRepaired || !practice.rig.placeStone2.IsRepaired || !practice.rig.placeStone1.IsRepaired)
            {
                Debug.LogError("Fragment reveal rejected: bridge practice has not completed and settled.", this);
                return;
            }
            challengeSolved = true;
            RevealCount++;
            if (!FragmentCollected) gemVisual.SetActive(true);
        }

        public void InteractFromShared()
        {
            if (!CanSharedInteract || !progress.TryCollect(FragmentProgressTracker.Ruin01)) return;
            CollectionCount++;
            gemVisual.SetActive(false);
            InteractionHintManager.Instance?.HideAll();
            confirmationText.text = "Fragment of Calling collected  •  " + progress.CollectedCount + "/3";
            confirmationText.gameObject.SetActive(true);
            confirmationText.transform.parent.gameObject.SetActive(true);
            confirmationUntil = Time.unscaledTime + confirmationSeconds;
        }

        void Update()
        {
            if (confirmationText != null && confirmationText.transform.parent.gameObject.activeSelf &&
                Time.unscaledTime >= confirmationUntil) confirmationText.transform.parent.gameObject.SetActive(false);
        }
    }
}
