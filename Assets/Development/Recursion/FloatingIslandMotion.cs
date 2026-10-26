using UnityEngine;

namespace Fyp.Development.Recursion
{
    /// <summary>Independent visual prototype. Does not own recursion, input, or progression.</summary>
    [DisallowMultipleComponent]
    public sealed class FloatingIslandMotion : MonoBehaviour
    {
        public Transform brokenPose;
        public Transform repairedPose;
        [Tooltip("Edit Mode remains at the authored repaired pose. Applied only when Play Mode starts.")]
        public bool startBroken = true;
        [Min(.1f)] public float moveDuration = 4f;
        [Tooltip("Rebuilt from this module's children on Awake, including inactive child colliders.")]
        public Collider[] walkableColliders;

        private Vector3 startPosition, targetPosition;
        private Quaternion startRotation, targetRotation;
        private float elapsed;
        private bool moving, wantsRepaired, configured;
        private object practiceOwner;
        public bool IsPracticeOwned => practiceOwner != null;
        public bool TryClaimForPractice(object owner)
        {
            if (owner == null || (practiceOwner != null && !ReferenceEquals(practiceOwner, owner))) return false;
            practiceOwner = owner;
            return true;
        }
        public bool RepairForPractice(object owner)
        {
            if (!ReferenceEquals(practiceOwner, owner) || owner == null || !Application.isPlaying || !isActiveAndEnabled || !configured) return false;
            Request(true);
            return true;
        }
        public bool IsMoving => moving;
        public bool IsRepaired => configured && wantsRepaired && !moving && AtPose(repairedPose);

        private bool AtPose(Transform pose) => pose != null &&
            (transform.position - pose.position).sqrMagnitude < .000001f &&
            Quaternion.Angle(transform.rotation, pose.rotation) < .01f;

        private void Awake()
        {
            walkableColliders = GetComponentsInChildren<Collider>(true);
            SetCollisions(false);
            configured = brokenPose != null && repairedPose != null && brokenPose != repairedPose &&
                !brokenPose.IsChildOf(transform) && !repairedPose.IsChildOf(transform) && walkableColliders.Length > 0;
            foreach (var body in GetComponentsInChildren<Rigidbody>(true))
                if (!body.isKinematic) configured = false;
            if (!configured)
            {
                Debug.LogError("Floating island needs independent pose markers, child colliders, and no dynamic Rigidbody.", this);
                return;
            }
            SetTarget(!startBroken, true);
        }

        [ContextMenu("Prototype/Repair (Play Mode)")]
        public void Repair() { if (!IsPracticeOwned) Request(true); }

        [ContextMenu("Prototype/Reset to Broken (Play Mode)")]
        public void ResetToBroken() { if (!IsPracticeOwned) Request(false); }

        private void Request(bool repaired)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || !configured) return;
            SetTarget(repaired, false);
        }

        private void SetTarget(bool repaired, bool immediate)
        {
            Transform pose = repaired ? repairedPose : brokenPose;
            if (moving && wantsRepaired == repaired &&
                (targetPosition-pose.position).sqrMagnitude < .000001f &&
                Quaternion.Angle(targetRotation,pose.rotation) < .01f) return;
            wantsRepaired = repaired;
            SetCollisions(false); // Remove walkable surfaces before either direction starts moving.
            targetPosition = pose.position;
            targetRotation = pose.rotation;
            startPosition = transform.position;
            startRotation = transform.rotation;
            elapsed = 0;
            moving = !immediate && !AtPose(pose);
            if (!moving) Settle();
        }

        private void Update()
        {
            if (!configured) return;
            if (moving)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(.1f,moveDuration));
                float eased = t*t*(3-2*t);
                transform.SetPositionAndRotation(Vector3.Lerp(startPosition,targetPosition,eased),
                    Quaternion.Slerp(startRotation,targetRotation,eased));
                if (t>=1) Settle();
            }
            // Fail closed if a pose marker or the island is moved externally during Play Mode.
            SetCollisions(IsRepaired);
        }

        private void Settle()
        {
            transform.SetPositionAndRotation(targetPosition,targetRotation);
            moving = false;
            Physics.SyncTransforms();
            SetCollisions(IsRepaired);
        }

        private void SetCollisions(bool enabledState)
        {
            if (walkableColliders==null) return;
            foreach (var collider in walkableColliders)
                if (collider!=null && collider.enabled!=enabledState) collider.enabled=enabledState;
        }

        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            moving=false;
            SetCollisions(false);
        }
    }
}
