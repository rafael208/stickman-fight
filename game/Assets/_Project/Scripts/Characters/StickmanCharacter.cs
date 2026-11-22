using System;
using UnityEngine;
using StickmanFight.Art;

namespace StickmanFight.Characters
{
    /// <summary>
    /// Персонаж-стикмен: собирает ragdoll, держит вертикаль (ADR-1), переключает состояния.
    /// Ввод приходит снаружи (MoveX/Jump), поэтому одинаково работает для игрока, ИИ и автотестов.
    /// </summary>
    public class StickmanCharacter : MonoBehaviour
    {
        [Header("Конфигурация")]
        public CharacterConfig config;

        [Header("Вид")]
        public Color tint = Color.cyan;

        [Header("Сборка")]
        public int bodyLayer = GameLayers.PlayerBody;
        public bool buildOnAwake = true;

        public RagdollRig Rig { get; private set; }
        public GroundCheck Ground { get; private set; }
        public RagdollVisual Visual { get; private set; }
        public CharacterState State { get; private set; } = CharacterState.Standing;
        public float Hp { get; private set; }
        public bool IsDead => State == CharacterState.Dead;

        /// <summary>Ввод по X: -1..1. Ставится InputRouter (S2) или ИИ (S4).</summary>
        public float MoveX { get; set; }
        public bool JumpQueued { get; set; }

        public event Action<CharacterState> StateChanged;
        public event Action<float> Damaged;

        /// <summary>Угол корпуса к вертикали в градусах — основной признак «стоит или упал».</summary>
        public float TorsoAngle => Rig != null && Rig.Torso != null ? Mathf.DeltaAngle(Rig.Torso.rotation, 0f) : 0f;

        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private float _stunTimer;
        private float _knockdownTimer;
        private float _invulnerableTimer;
        private float _getUpTimer;
        private float _deadTimer;
        private float _groundedGrace;

        /// <summary>«На земле» с гистерезисом: без него состояние мигает Airborne/Standing на стыках.</summary>
        public bool Grounded { get; private set; }

        private void Awake()
        {
            Hp = config != null ? config.maxHp : 100f;
            if (buildOnAwake) Build();
        }

        /// <summary>Создаёт скелет, визуал и проверку земли. Можно вызывать вручную (фабрика спавна).</summary>
        public void Build()
        {
            if (config == null)
            {
                Debug.LogError("[Character] не задан CharacterConfig", this);
                return;
            }
            if (Rig != null) return;

            Rig = gameObject.GetComponent<RagdollRig>();
            if (Rig == null) Rig = gameObject.AddComponent<RagdollRig>();
            Rig.Build(config, bodyLayer);

            Ground = Rig.Torso.gameObject.AddComponent<GroundCheck>();
            float legTop = config.legHeight;
            float torsoHalf = config.torsoHeight * 0.5f;
            // Точка проверки — между ступнями, чуть ниже уровня ног.
            Ground.Configure(0.18f, new Vector2(0f, -(legTop + torsoHalf) + 0.08f));

            Visual = gameObject.GetComponent<RagdollVisual>();
            if (Visual == null) Visual = gameObject.AddComponent<RagdollVisual>();
            Visual.Init(Rig, tint);

            Rig.SetPoseHeld(true);
            SetState(CharacterState.Standing);
        }

        // ------------------------------------------------------------------ цикл

        private void FixedUpdate()
        {
            Tick(Time.fixedDeltaTime);
        }

        /// <summary>
        /// Один шаг логики. Вызывается из FixedUpdate, а также вручную из headless-стенда
        /// (Editor/PhysicsHarness), где физика шагается через Physics2D.Simulate.
        /// </summary>
        public void Tick(float dt)
        {
            if (Rig == null || Rig.Torso == null) return;

            TickTimers(dt);
            UpdateGrounded(dt);

            switch (State)
            {
                case CharacterState.Dead:
                    _deadTimer += dt;
                    return;

                case CharacterState.Knockdown:
                    // FR-RAG-05: подъём по таймеру, без условия «есть земля под ногами» —
                    // в лежачем положении точка проверки земли уходит под пол.
                    if (_knockdownTimer <= 0f) StartGettingUp();
                    return;
            }

            ApplyUprightTorque(dt);
            ApplyMovement(dt);
            ApplyJump();

            if (_getUpTimer > 0f)
            {
                // Плавный возврат в вертикаль (FR-RAG-05): углы доворачиваются быстро, но не мгновенно.
                _getUpTimer -= dt;
                Rig.Torso.MoveRotation(Mathf.MoveTowardsAngle(Rig.Torso.rotation, 0f, config.rightingRate * dt));
                // Гасим вращение на время подъёма, иначе тело «кувыркается» через голову.
                Rig.Torso.angularVelocity = 0f;
            }
            else if (State != CharacterState.Stunned)
            {
                UpdateLocomotionState();
            }
        }

        private void TickTimers(float dt)
        {
            _invulnerableTimer = Mathf.Max(0f, _invulnerableTimer - dt);
            _stunTimer = Mathf.Max(0f, _stunTimer - dt);
            _knockdownTimer = Mathf.Max(0f, _knockdownTimer - dt);
            _coyoteTimer = Mathf.Max(0f, _coyoteTimer - dt);
            _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - dt);

            if (State == CharacterState.Stunned && _stunTimer <= 0f)
                UpdateLocomotionState();
        }

        private void ApplyUprightTorque(float dt)
        {
            var body = Rig.Torso;
            bool upright = State != CharacterState.Knockdown && State != CharacterState.Dead;

            if (!upright)
            {
                Rig.SetPoseHeld(false);
                return;
            }

            Rig.SetPoseHeld(true);
            // Сервопривод суставов: без него ноги «складываются» и тело проседает.
            Rig.HoldPose();

            float errDeg = Mathf.DeltaAngle(body.rotation, 0f);

            // Жёсткое удержание вертикали: корпус доворачивается к 0 со скоростью rightingRate.
            // Почему не только PD-момент: при наклоне сервопривод ног начинает «шагать»,
            // и персонаж уезжает вбок. При строго вертикальном корпусе этого не происходит.
            if (Mathf.Abs(errDeg) > 0.5f)
                body.MoveRotation(Mathf.MoveTowardsAngle(body.rotation, 0f, config.rightingRate * dt));

            body.angularVelocity *= 0.2f;

            // Ограничение угловой скорости вручную: в Unity 6.3 Rigidbody2D.maxAngularVelocity удалён.
            float maxW = config.maxAngularVelocity;
            if (Mathf.Abs(body.angularVelocity) > maxW)
                body.angularVelocity = Mathf.Sign(body.angularVelocity) * maxW;
        }

        private void UpdateGrounded(float dt)
        {
            bool now = Ground != null && Ground.IsGrounded;
            if (now) _groundedGrace = config.groundGrace;
            else _groundedGrace = Mathf.Max(0f, _groundedGrace - dt);
            Grounded = _groundedGrace > 0f;
        }

        private void ApplyMovement(float dt)
        {
            if (State == CharacterState.Knockdown || State == CharacterState.Dead) return;

            bool grounded = Grounded;
            float control = grounded ? 1f : config.airControl;
            float target = Mathf.Clamp(MoveX, -1f, 1f) * config.moveSpeed * control;

            var v = Rig.Torso.linearVelocity;
            v.x = Mathf.MoveTowards(v.x, target, config.acceleration * dt * control);
            Rig.Torso.linearVelocity = v;

            if (grounded) _coyoteTimer = config.coyoteTime;
        }

        private void ApplyJump()
        {
            if (State == CharacterState.Knockdown || State == CharacterState.Dead) return;

            if (JumpQueued)
            {
                JumpQueued = false;
                _jumpBufferTimer = config.jumpBuffer;
            }

            if (_jumpBufferTimer <= 0f) return;
            if (!Grounded && _coyoteTimer <= 0f) return;

            var v = Rig.Torso.linearVelocity;
            v.y = config.jumpSpeed;
            Rig.Torso.linearVelocity = v;

            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            SetState(CharacterState.Airborne);
        }

        private void UpdateLocomotionState()
        {
            if (State == CharacterState.Knockdown || State == CharacterState.Dead) return;

            bool grounded = Grounded;
            float speed = Mathf.Abs(Rig.Torso.linearVelocity.x);
            bool moving = Mathf.Abs(MoveX) > 0.1f && speed > 0.3f;

            if (!grounded) SetState(CharacterState.Airborne);
            else SetState(moving ? CharacterState.Moving : CharacterState.Standing);
        }

        private void SetState(CharacterState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke(next);
        }

        // --------------------------------------------------------------- боевые API

        /// <summary>
        /// Применяет урон и импульс. Кулдаун попадания обеспечивает DamageSystem (S3);
        /// на S1 метод вызывается напрямую тестовым стендом.
        /// </summary>
        public void ApplyDamage(float amount, Vector2 impulse, Vector2 point)
        {
            if (IsDead || _invulnerableTimer > 0f) return;

            Hp = Mathf.Max(0f, Hp - amount);
            Damaged?.Invoke(amount);

            Rig.Torso.AddForceAtPosition(impulse, point, ForceMode2D.Impulse);

            if (Hp <= 0f)
            {
                Die();
                return;
            }

            bool knockdown = amount >= config.knockdownDamage || impulse.magnitude >= config.knockdownImpulse;
            if (knockdown) EnterKnockdown();
            else EnterStun();
        }

        public void EnterKnockdown()
        {
            if (IsDead) return;
            SetState(CharacterState.Knockdown);
            _knockdownTimer = config.getUpTime;
            _stunTimer = 0f;
            Rig.SetPoseHeld(false);
        }

        private void EnterStun()
        {
            SetState(CharacterState.Stunned);
            _stunTimer = config.stunTime;
        }

        private void StartGettingUp()
        {
            _getUpTimer = 0.35f;
            _invulnerableTimer = config.invulnerableTime;

            var body = Rig.Torso;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;

            // Лежачее тело нельзя «довернуть» в стойку: вращение упирается в пол и решатель
            // контактов возвращает его назад. Поэтому сначала приподнимаем корпус над землёй.
            body.MovePosition(body.position + Vector2.up * config.getUpLiftHeight);

            Rig.SetPoseHeld(true);
            SetState(CharacterState.Standing);
        }

        private void Die()
        {
            SetState(CharacterState.Dead);
            _deadTimer = 0f;
            Rig.SetPoseHeld(false);
            Debug.Log($"[Character] умер: {name}");
        }

        /// <summary>Для тестового стенда: мгновенный «толчок» заданной силы в корпус.</summary>
        public void Push(Vector2 impulse)
        {
            if (Rig == null || Rig.Torso == null || IsDead) return;
            Rig.Torso.AddForce(impulse, ForceMode2D.Impulse);
            if (impulse.magnitude >= config.knockdownImpulse) EnterKnockdown();
            else EnterStun();
        }
    }
}
