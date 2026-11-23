using TMPro;
using UnityEngine;

namespace Fyp.Development.Progress
{
    // Every caller uses the same timer on the existing toast panel.
    public sealed class FragmentRewardToast : MonoBehaviour
    {
        float hideAt;

        public static void Show(TMP_Text label, string message, float seconds)
        {
            if (label == null) return;
            var panel = label.transform.parent.gameObject;
            if (!panel.TryGetComponent<FragmentRewardToast>(out var toast))
                toast = panel.AddComponent<FragmentRewardToast>();
            toast.hideAt = Time.unscaledTime + seconds;
            label.text = message;
            label.gameObject.SetActive(true);
            panel.SetActive(true);
        }

        void Update()
        {
            if (Time.unscaledTime >= hideAt) gameObject.SetActive(false);
        }
    }
}
