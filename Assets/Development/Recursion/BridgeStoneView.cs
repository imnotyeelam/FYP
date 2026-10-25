using UnityEngine;

namespace Fyp.Development.Recursion
{
    public sealed class BridgeStoneView : MonoBehaviour
    {
        public Transform brokenPose;
        public Transform repairedPose;
        public MeshRenderer stoneRenderer;
        public BoxCollider crossingCollider;
        [Min(.05f)] public float moveDuration = .65f;

        private Vector3 startPosition, targetPosition;
        private Quaternion startRotation, targetRotation;
        private float elapsed;
        private bool moving, placed, returning, completed;
        private MaterialPropertyBlock properties;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public bool IsMoving => moving;

        public void Apply(bool shouldBePlaced, bool showReturn, bool isComplete, bool immediate)
        {
            if (brokenPose == null || repairedPose == null || crossingCollider == null) return;
            placed = shouldBePlaced;
            returning = showReturn;
            completed = isComplete;
            Transform pose = placed ? repairedPose : brokenPose;
            bool targetChanged = (pose.position - targetPosition).sqrMagnitude > .000001f ||
                Quaternion.Angle(pose.rotation, targetRotation) > .01f;
            targetPosition = pose.position;
            targetRotation = pose.rotation;
            if (immediate) Settle();
            else if (targetChanged || (!moving && (transform.position - targetPosition).sqrMagnitude > .000001f))
            {
                startPosition = transform.position;
                startRotation = transform.rotation;
                elapsed = 0;
                moving = true;
                crossingCollider.enabled = false;
            }
            else crossingCollider.enabled = placed && !moving;
            Tint();
        }

        public void Settle()
        {
            moving = false;
            transform.SetPositionAndRotation(targetPosition, targetRotation);
            if (crossingCollider != null) crossingCollider.enabled = placed;
        }

        private void Update()
        {
            if (moving)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(.05f, moveDuration));
                float eased = t * t * (3 - 2 * t);
                transform.SetPositionAndRotation(Vector3.Lerp(startPosition, targetPosition, eased),
                    Quaternion.Slerp(startRotation, targetRotation, eased));
                if (t >= 1) Settle();
            }
            Tint();
        }

        private void Tint()
        {
            if (stoneRenderer == null) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            Color color = returning ? new Color(1f, .64f, .12f) :
                completed ? new Color(.3f, .7f, .48f) :
                placed ? new Color(.28f, .58f, .65f) : new Color(.36f, .39f, .43f);
            float strength = returning ? 1.4f + .4f * Mathf.Sin(Time.unscaledTime * 6) :
                completed ? .3f : placed ? .12f : 0;
            stoneRenderer.GetPropertyBlock(properties);
            properties.SetColor(BaseColor, color);
            properties.SetColor(EmissionColor, color * strength);
            stoneRenderer.SetPropertyBlock(properties);
        }
    }
}
