using UnityEngine;

namespace TapLegends.UI
{
    /// <summary>
    /// Recorta los bordes superior/inferior de este RectTransform al Screen.safeArea
    /// (notch, camara en isla, barra de gestos de Android), preservando su ancla
    /// original en el resto — no lo reemplaza por el safe area completo, porque TopBar
    /// y BottomPanel ya ocupan solo una franja de la pantalla, no toda. Se re-evalua en
    /// Update porque el safe area puede cambiar en runtime (rotacion).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private bool insetTop;
        [SerializeField] private bool insetBottom;

        private RectTransform rectTransform;
        private Vector2 originalAnchorMin;
        private Vector2 originalAnchorMax;
        private Rect lastSafeArea;

        /// <summary>Asignado por SceneBuilder al construir la escena (sin prefab, no hay
        /// Inspector donde tildar los checkboxes a mano).</summary>
        public void Configure(bool top, bool bottom)
        {
            insetTop = top;
            insetBottom = bottom;
        }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            originalAnchorMin = rectTransform.anchorMin;
            originalAnchorMax = rectTransform.anchorMax;
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea) Apply();
        }

        private void Apply()
        {
            lastSafeArea = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            var min = originalAnchorMin;
            var max = originalAnchorMax;

            if (insetBottom) min.y = Mathf.Max(originalAnchorMin.y, lastSafeArea.yMin / Screen.height);
            if (insetTop) max.y = Mathf.Min(originalAnchorMax.y, lastSafeArea.yMax / Screen.height);

            rectTransform.anchorMin = min;
            rectTransform.anchorMax = max;
        }
    }
}
