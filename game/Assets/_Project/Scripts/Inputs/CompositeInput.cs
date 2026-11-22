using System.Collections.Generic;
using StickmanFight.Inputs;
using UnityEngine;

namespace StickmanFight.Inputs
{
    /// <summary>
    /// Объединяет несколько источников ввода: тач на телефоне и клавиатуру на ПК.
    /// Нужно, чтобы один и тот же билд был играбелен и на устройстве, и на компьютере.
    /// </summary>
    public class CompositeInput : MonoBehaviour, IInputSource
    {
        public MonoBehaviour[] sources;

        private readonly List<IInputSource> _resolved = new List<IInputSource>();

        private void Awake()
        {
            _resolved.Clear();
            if (sources == null) return;

            foreach (var s in sources)
            {
                if (s is IInputSource input) _resolved.Add(input);
            }
        }

        public float MoveX
        {
            get
            {
                float best = 0f;
                foreach (var s in _resolved)
                {
                    float v = s.MoveX;
                    if (Mathf.Abs(v) > Mathf.Abs(best)) best = v;
                }
                return best;
            }
        }

        public bool ConsumeJump()
        {
            bool any = false;
            foreach (var s in _resolved) any |= s.ConsumeJump();
            return any;
        }

        public bool ConsumeAttack()
        {
            bool any = false;
            foreach (var s in _resolved) any |= s.ConsumeAttack();
            return any;
        }

        public bool ConsumeInteract()
        {
            bool any = false;
            foreach (var s in _resolved) any |= s.ConsumeInteract();
            return any;
        }

        public bool ConsumeDrop()
        {
            bool any = false;
            foreach (var s in _resolved) any |= s.ConsumeDrop();
            return any;
        }
    }
}
