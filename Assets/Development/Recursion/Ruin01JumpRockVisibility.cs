using UnityEngine;

namespace Fyp.Development.Recursion
{
    /// <summary>
    /// Presentation only: the Ruin 1 Jump Rock (jump interaction anchor) is hidden once the jump challenge is
    /// completed, because it has no further use. Only renderers, colliders and lights are toggled; the
    /// transform stays in place so the jump controller's interactionAnchor reference remains valid. Reading
    /// JumpChallengeCompleted every frame means a developer reset shows the rock again.
    /// </summary>
    public sealed class Ruin01JumpRockVisibility : MonoBehaviour
    {
        public Ruin01MagicJumpController jump;
        [Tooltip("Optional: also hide once the Ruin 1 fragment is collected (the Return to Hub stone is then shown).")]
        public Fyp.Development.Progress.FragmentProgressTracker progress;
        public Renderer[] renderers;
        public Collider[] colliders;
        public Light[] lights;
        bool? shown;

        void LateUpdate()
        {
            bool jumpDone = jump != null && jump.JumpChallengeCompleted;
            bool fragmentCollected = progress != null &&
                progress.HasFragment(Fyp.Development.Progress.FragmentProgressTracker.Ruin01);
            bool show = !jumpDone && !fragmentCollected;
            if (shown == show) return;
            shown = show;
            foreach (var r in renderers) if (r != null) r.enabled = show;
            foreach (var c in colliders) if (c != null) c.enabled = show;
            foreach (var l in lights) if (l != null) l.enabled = show;
        }
    }
}
