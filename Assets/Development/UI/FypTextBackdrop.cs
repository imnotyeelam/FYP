using TMPro;
using UnityEngine;

namespace Fyp.Development.UI
{
    /// <summary>
    /// Presentation only: sizes this RectTransform to the rendered bounds of a target TMP text
    /// (plus padding) and hides its graphic when the text is empty. Used for code wells whose
    /// text rect is larger than the content (UI_STYLE_GUIDE.md, section 6).
    /// The backdrop must be a sibling placed before the text so it draws behind it.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class FypTextBackdrop : MonoBehaviour
    {
        public TMP_Text target;
        public Vector2 padding = new Vector2(28f, 18f);
        UnityEngine.UI.Graphic graphic;
        string lastText;
        Vector3 lastPosition;

        void LateUpdate()
        {
            if (target == null) return;
            if (graphic == null) graphic = GetComponent<UnityEngine.UI.Graphic>();
            bool visible = target.isActiveAndEnabled && !string.IsNullOrWhiteSpace(target.text);
            if (graphic != null && graphic.enabled != visible) graphic.enabled = visible;
            if (!visible) return;
            if (target.text == lastText && target.rectTransform.position == lastPosition) return;
            lastText = target.text;
            lastPosition = target.rectTransform.position;
            target.ForceMeshUpdate();
            Bounds b = target.textBounds;
            var rt = (RectTransform)transform;
            var targetRt = target.rectTransform;
            // Same anchors/pivot frame as the text, centred on its rendered bounds.
            rt.anchorMin = targetRt.anchorMin;
            rt.anchorMax = targetRt.anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            // textBounds are in the text's local space; convert the centre to the shared parent space.
            Vector2 textCentre = (Vector2)targetRt.localPosition + (Vector2)b.center;
            rt.sizeDelta = new Vector2(b.size.x + padding.x * 2f, b.size.y + padding.y * 2f)
                - Vector2.Scale(targetRt.anchorMax - targetRt.anchorMin, ((RectTransform)rt.parent).rect.size);
            rt.localPosition = new Vector3(textCentre.x, textCentre.y, targetRt.localPosition.z);
        }
    }
}
