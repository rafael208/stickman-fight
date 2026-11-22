using StickmanFight.UI;
using UnityEngine;

namespace StickmanFight.Inputs
{
    /// <summary>
    /// Сенсорное управление (FR-INPUT-01/03): движение — удержанием, действия — тапом.
    /// Мультитач работает потому, что каждая кнопка отслеживает свой pointerId.
    /// </summary>
    public class TouchInput : MonoBehaviour, IInputSource
    {
        [Header("Движение")]
        public StickmanFight.UI.VirtualJoystick joystick;
        [Tooltip("Резерв: кнопки-стрелки, если джойстик не задан")]
        public HoldButton left;
        public HoldButton right;

        private bool _jump;
        private bool _attack;

        private void Awake()
        {
            if (joystick != null) joystick.OnTap += () => _attack = true;
        }

        private void Update()
        {
            // Держишь джойстик вверху — прыгает сам, отпускать не нужно (как в оригинале).
            if (joystick != null && joystick.Value.y > 0.55f) _jump = true;
        }
        private bool _interact;
        private bool _drop;

        public float MoveX
        {
            get
            {
                if (joystick != null) return joystick.DeadZonedX;
                float x = 0f;
                if (left != null && left.IsHeld) x -= 1f;
                if (right != null && right.IsHeld) x += 1f;
                return x;
            }
        }

        // Методы ниже вешаются на onClick кнопок в сцене.
        public void OnJump() => _jump = true;
        public void OnAttack() => _attack = true;
        public void OnInteract() => _interact = true;
        public void OnDrop() => _drop = true;

        public bool ConsumeJump() => Take(ref _jump);
        public bool ConsumeAttack() => Take(ref _attack);
        public bool ConsumeInteract() => Take(ref _interact);
        public bool ConsumeDrop() => Take(ref _drop);

        private static bool Take(ref bool flag)
        {
            if (!flag) return false;
            flag = false;
            return true;
        }
    }
}
