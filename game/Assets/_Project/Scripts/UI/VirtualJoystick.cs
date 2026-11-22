using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StickmanFight.UI
{
    /// <summary>
    /// Виртуальный джойстик как в Supreme Duelist: полупрозрачный круг + «шайба» внутри.
    /// Тянешь в сторону — персонаж идёт; отпустил — останавливается. Мультитач дружит с кнопками.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform knob;
        public float radius = 88f;
        [Range(0f, 0.5f)] public float deadZone = 0.14f;

        /// <summary>Нормализованный вектор отклонения: x — вправо, y — вверх.</summary>
        public Vector2 Value { get; private set; }

        private RectTransform _area;
        private bool _active;
        private int _pointerId = -1;

        private void Awake()
        {
            _area = (RectTransform)transform;
            if (radius <= 1f) radius = _area.rect.width * 0.4f;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (_active) return;
            _active = true;
            _pointerId = e.pointerId;
            Apply(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != _pointerId) return;
            Apply(e);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != _pointerId) return;
            _active = false;
            _pointerId = -1;
            Value = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
        }

        private void Apply(PointerEventData e)
        {
            var cam = e.pressEventCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, e.position, cam, out var local))
                return;

            var v = local / radius;
            v = Vector2.ClampMagnitude(v, 1f);
            Value = v;
            if (knob != null) knob.anchoredPosition = v * radius;
        }

        /// <summary>Значение с мёртвой зоной — чтобы не дёргался от случайного касания.</summary>
        public float DeadZonedX
        {
            get
            {
                float x = Value.x;
                if (Mathf.Abs(x) < deadZone) return 0f;
                return Mathf.Sign(x) * (Mathf.Abs(x) - deadZone) / (1f - deadZone);
            }
        }

        public void SetVisible(bool on) => gameObject.SetActive(on);
    }
}
