using UnityEngine;
using UnityEngine.Rendering;

namespace Fyp.Development.Recursion
{
    internal static class Ruin01JumpTrail
    {
        internal static TrailRenderer Create(Animator wizard, Material material)
        {
            var anchor = new GameObject("Ruin 1 Magic Jump Trail");
            var hips = wizard.isHuman ? wizard.GetBoneTransform(HumanBodyBones.Hips) : null;
            anchor.transform.SetParent(hips != null ? hips : wizard.transform, false);
            if (hips == null) anchor.transform.localPosition = Vector3.up;

            var trail = anchor.AddComponent<TrailRenderer>();
            trail.emitting = false;
            trail.sharedMaterial = material;
            trail.time = .32f;
            trail.minVertexDistance = .15f;
            trail.widthMultiplier = .48f;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.numCornerVertices = 2;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.colorGradient = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(.37f, .58f, 1f), 0f),
                    new GradientColorKey(new Color(.64f, .32f, 1f), 1f)
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(.05f, 0f),
                    new GradientAlphaKey(.48f, .2f),
                    new GradientAlphaKey(0f, 1f)
                }
            };
            return trail;
        }
    }
}
