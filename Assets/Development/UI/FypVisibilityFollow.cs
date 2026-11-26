using UnityEngine;

namespace Fyp.Development.UI
{
    /// <summary>
    /// Presentation only: keeps a visual active exactly while a source behaviour is enabled and active
    /// (for example a teleport stone that should appear only once its RuinEntrance is unlocked).
    /// It never changes the source, its interaction or progression.
    /// </summary>
    public sealed class FypVisibilityFollow : MonoBehaviour
    {
        public Behaviour source;
        public GameObject visual;

        void LateUpdate()
        {
            if (visual == null) return;
            bool show = source != null && source.isActiveAndEnabled;
            if (visual.activeSelf != show) visual.SetActive(show);
        }
    }
}
