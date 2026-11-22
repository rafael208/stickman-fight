using UnityEngine;
using UnityEngine.EventSystems;

namespace StickmanFight.UI
{
    /// <summary>
    /// Кнопка «удержание» с отслеживанием конкретного пальца.
    /// Кнопка uGUI не даёт состояние «зажата», а для движения оно нужно.
    /// pointerId обязателен: без него второй палец (атака) сбрасывает движение.
    /// </summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool IsHeld { get; private set; }

        private int _pointerId = -1;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId >= 0) return;
            _pointerId = eventData.pointerId;
            IsHeld = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Палец ушёл за пределы кнопки — считаем отпусканием, иначе движение «залипнет».
            if (eventData.pointerId != _pointerId) return;
            Release();
        }

        private void Release()
        {
            _pointerId = -1;
            IsHeld = false;
        }

        private void OnDisable()
        {
            Release();
        }
    }
}
