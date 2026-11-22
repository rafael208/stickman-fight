using StickmanFight.Characters;
using StickmanFight.Weapons;
using UnityEngine;

namespace StickmanFight.AI
{
    /// <summary>
    /// Бот-противник. Отдаёт команды через тот же IInputSource, что и тач/клавиатура
    /// (правило GDD: бот не трогает Rigidbody/Health напрямую). Простой FSM:
    /// подход → атака → отход; подбирает оружие; не сваливается с края.
    /// </summary>
    public class AIBrain : MonoBehaviour, Inputs.IInputSource
    {
        public StickmanCharacter self;
        public StickmanCharacter target;

        [Header("Сложность (Easy / Normal / Hard)")]
        public Difficulty difficulty = Difficulty.Normal;

        [Header("Границы арены, чтобы не упасть")]
        public float minX = -6.6f;
        public float maxX = 6.6f;

        public enum Difficulty { Easy, Normal, Hard }

        // Настройки сложности: реакция, кулдаун атаки, агрессия, шанс прыжка.
        private float Reaction => difficulty == Difficulty.Easy ? 0.55f : difficulty == Difficulty.Hard ? 0.18f : 0.32f;
        private float AttackCooldown => difficulty == Difficulty.Easy ? 1.0f : difficulty == Difficulty.Hard ? 0.5f : 0.75f;
        private float RetreatHp => difficulty == Difficulty.Easy ? 0.15f : difficulty == Difficulty.Hard ? 0.45f : 0.3f;
        private float AttackRange => 1.15f;

        private float _reEvaluateTimer;
        private float _attackTimer;

        private float _moveX;
        private bool _jumpRequested;
        private bool _attackRequested;
        private bool _interactRequested;

        private readonly Collider2D[] _buffer = new Collider2D[8];

        private void Awake()
        {
            if (self == null) self = GetComponent<StickmanCharacter>();
        }

        private void Start()
        {
            if (target == null)
            {
                var all = Object.FindObjectsByType<StickmanCharacter>(FindObjectsSortMode.None);
                foreach (var c in all)
                {
                    if (c != self)
                    {
                        target = c;
                        break;
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            _attackTimer = Mathf.Max(0f, _attackTimer - Time.fixedDeltaTime);
            _reEvaluateTimer -= Time.fixedDeltaTime;
            if (_reEvaluateTimer <= 0f)
            {
                Decide();
                _reEvaluateTimer = Reaction;
            }
        }

        private void Decide()
        {
            if (self == null || self.IsDead)
            {
                _moveX = 0f;
                return;
            }

            if (target == null || target.IsDead)
            {
                _moveX = 0f;
                return;
            }

            float dx = target.Rig.Torso.position.x - self.Rig.Torso.position.x;
            float dist = Mathf.Abs(dx);
            float dir = Mathf.Sign(dx);
            if (dir == 0f) dir = 1f;

            float selfX = self.Rig.Torso.position.x;
            if (selfX < minX && dir < 0f) dir = 1f;
            if (selfX > maxX && dir > 0f) dir = -1f;

            // Подобрать оружие, если рядом и руки пусты.
            if (self.Combat != null && self.Combat.Weapon == null && FindWeapon() != null)
                _interactRequested = true;

            // Прыжок, если цель заметно выше.
            float dy = target.Rig.Torso.position.y - self.Rig.Torso.position.y;
            if (dy > 1.1f && self.Grounded) _jumpRequested = true;

            if (dist > AttackRange)
            {
                _moveX = dir;
            }
            else
            {
                _moveX = 0f;
                if (_attackTimer <= 0f)
                {
                    _attackRequested = true;
                    _attackTimer = AttackCooldown;
                }
            }

            // Отступление при низком здоровье.
            if (self.Hp < self.config.maxHp * RetreatHp && dist < 2.2f)
                _moveX = -dir;
        }

        private Weapon FindWeapon()
        {
            int count = Physics2D.OverlapCircleNonAlloc(self.Rig.Torso.position, 1.3f, _buffer, 1 << GameLayers.Weapon);
            for (int i = 0; i < count; i++)
            {
                var w = _buffer[i] != null ? _buffer[i].GetComponentInParent<Weapon>() : null;
                if (w != null && !w.IsHeld) return w;
            }
            return null;
        }

        // ------------------------------------------------------------ IInputSource

        public float MoveX => _moveX;

        public bool ConsumeJump() => Take(ref _jumpRequested);
        public bool ConsumeAttack() => Take(ref _attackRequested);
        public bool ConsumeInteract() => Take(ref _interactRequested);
        public bool ConsumeDrop() => false;

        private static bool Take(ref bool flag)
        {
            if (!flag) return false;
            flag = false;
            return true;
        }
    }
}
