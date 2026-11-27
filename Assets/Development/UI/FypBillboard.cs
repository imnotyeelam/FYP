using UnityEngine;

namespace Fyp.Development.UI
{
    /// <summary>Presentation only: turns a world-space label around Y to face the main camera so it stays readable.</summary>
    public sealed class FypBillboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 away = transform.position - cam.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(away);
        }
    }
}
