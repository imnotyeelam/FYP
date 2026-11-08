using UnityEngine;

namespace Fyp.Development.Recursion
{
    /// <summary>A visual cue advanced only by a completed recursive jump segment.</summary>
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class Ruin01MagicJumpTrigger : MonoBehaviour
    {
        [Min(.1f)] public float fadeSeconds = .7f;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        static readonly Color RestColor = new Color(.72f, .42f, 1f, .38f);
        static readonly Color BrightColor = new Color(.91f, .7f, 1f, .58f);
        static readonly Color RestEmission = new Color(.3f, .1f, .5f);
        static readonly Color BrightEmission = new Color(3f, .9f, 4.5f);

        MeshRenderer visual;
        MaterialPropertyBlock properties;
        float pulseAt = -100f;

        public int PulseCount { get; private set; }
        public float Brightness { get; private set; }

        void Awake()
        {
            visual = GetComponent<MeshRenderer>();
            properties = new MaterialPropertyBlock();
            Apply(0f);
        }

        public void Pulse()
        {
            PulseCount++;
            pulseAt = Time.time;
            Apply(1f);
        }

        void Update()
        {
            if (Brightness <= 0f) return;
            Apply(1f - Mathf.Clamp01((Time.time - pulseAt) / fadeSeconds));
        }

        void Apply(float brightness)
        {
            Brightness = brightness;
            properties.SetColor(BaseColor, Color.Lerp(RestColor, BrightColor, brightness));
            properties.SetColor(EmissionColor, Color.Lerp(RestEmission, BrightEmission, brightness));
            visual.SetPropertyBlock(properties);
        }

        void OnDisable()
        {
            if (visual != null && properties != null) Apply(0f);
        }
    }
}
