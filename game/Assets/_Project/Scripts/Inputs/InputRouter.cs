using StickmanFight.Characters;
using StickmanFight.Combat;
using UnityEngine;

namespace StickmanFight.Inputs
{
    /// <summary>
    /// Перекладывает намерения источника ввода в персонажа (ADR-8).
    /// Персонаж не знает, кто им управляет: тач, клавиатура или ИИ (S4).
    /// </summary>
    public class InputRouter : MonoBehaviour
    {
        public StickmanCharacter target;
        public MonoBehaviour source;

        private IInputSource _source;

        private void Awake()
        {
            _source = source as IInputSource;
            if (_source == null) _source = GetComponent<IInputSource>();
        }

        private void Update()
        {
            if (target == null || _source == null) return;

            target.MoveX = _source.MoveX;

            if (_source.ConsumeJump()) target.JumpQueued = true;

            var combat = target.Combat;
            if (combat == null) return;

            if (_source.ConsumeAttack()) combat.RequestAttack();
            if (_source.ConsumeInteract()) combat.RequestInteract();
            if (_source.ConsumeDrop()) combat.RequestDrop();
        }
    }
}
