using UnityEngine;

namespace StickmanFight.Inputs
{
    /// <summary>Клавиатурный ввод для отладки на ПК (FR-INPUT-06): A/D или стрелки, пробел, J, E, Q.</summary>
    public class KeyboardInput : MonoBehaviour, IInputSource
    {
        public float MoveX
        {
            get
            {
                float x = 0f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
                return x;
            }
        }

        public bool ConsumeJump() => Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W);
        public bool ConsumeAttack() => Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0);
        public bool ConsumeInteract() => Input.GetKeyDown(KeyCode.E);
        public bool ConsumeDrop() => Input.GetKeyDown(KeyCode.Q);
    }
}
