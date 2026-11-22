using UnityEngine;

namespace StickmanFight.UI
{
    /// <summary>
    /// Подгоняет RectTransform под Screen.safeArea (вырез, скругления, навигационная полоса).
    /// Требование FR-UI-07: ни одна кнопка не должна оказаться под вырезом.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreen;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            // Ориентация/размер меняются редко — дешёвая проверка раз в кадр.
            if (Screen.safeArea == _lastSafeArea && _lastScreen.x == Screen.width && _lastScreen.y == Screen.height)
                return;

            Apply();
        }

        public void Apply()
        {
            if (_rect == null) return;

            Rect safe = Screen.safeArea;
            _lastSafeArea = safe;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);

            if (Screen.width <= 0 || Screen.height <= 0) return;

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
