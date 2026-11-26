using UnityEngine;

namespace Fyp.Development.UI
{
    /// <summary>
    /// Presentation only: applies a per-instance emission colour to renderers through a
    /// MaterialPropertyBlock, so shared vendor materials are never modified. Property blocks are not
    /// serialized, so this component re-applies the tint whenever it is enabled (Edit and Play Mode).
    /// </summary>
    [ExecuteAlways]
    public sealed class FypEmissionTint : MonoBehaviour
    {
        public Renderer[] targets;
        [ColorUsage(false, true)] public Color emission = new Color(0.25f, 1.6f, 2.0f);
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        MaterialPropertyBlock block;

        void OnEnable() { Apply(); }
        void OnValidate() { Apply(); }

        public void Apply()
        {
            if (targets == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            foreach (var r in targets)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(EmissionId, emission);
                r.SetPropertyBlock(block);
            }
        }

        void OnDisable()
        {
            if (targets == null) return;
            foreach (var r in targets) if (r != null) r.SetPropertyBlock(null);
        }
    }
}
