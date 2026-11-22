using System;
using UnityEngine;
using StickmanFight.Art;
using StickmanFight.Core;
using StickmanFight.Weapons;

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
        public Combat.CharacterCombat Combat { get; private set; }

        [Tooltip("Оружие, выбранное в меню: выдаётся при старте боя")]
        public Weapon startWeapon;
        public CharacterState State { get; private set; } = CharacterState.Standing;
        public float Hp { get; private set; }
        public bool IsDead => State == CharacterState.Dead;

        /// <summary>Направление взгляда: 1 вправо, -1 влево. Влияет на удары и выстрелы.</summary>
        public int Facing { get; private set; } = 1;

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
        private float _motorScale = 1f;
        private float _walkPhase;

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
            float legTop = config.shinLength + config.thighLength;
            float torsoHalf = config.torsoHeight * 0.5f;
            // Точка проверки — между ступнями, чуть ниже уровня ног.
            Ground.Configure(0.18f, new Vector2(0f, -(legTop + torsoHalf) + 0.08f));

            Visual = gameObject.GetComponent<RagdollVisual>();
            if (Visual == null) Visual = gameObject.AddComponent<RagdollVisual>();
            Visual.Init(Rig, tint);

            Rig.SetPoseHeld(true);
            SetState(CharacterState.Standing);

            BuildCombat();
        }

        /// <summary>Кисть для оружия и боевой компонент. Кисть — на конце предплечья.</summary>
        private void BuildCombat()
        {
            if (Rig.ForearmR == null) return;

            var hand = new GameObject("HandAnchor");
            hand.transform.SetParent(Rig.ForearmR.transform, false);
            hand.transform.localPosition = new Vector3(0f, -config.forearmLength * 0.5f, 0f);

            Combat = GetComponent<Combat.CharacterCombat>();
            if (Combat == null) Combat = gameObject.AddComponent<Combat.CharacterCombat>();
            Combat.character = this;
            Combat.handAnchor = hand.transform;

            if (startWeapon != null) Combat.Equip(startWeapon);
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

            // Если персонаж завалился без «официального» нокдауна (толкнул предмет, споткнулся),
            // сам он не встанет: доворот из лежачего положения упирается в пол.
            // Поэтому любое падение переводим в штатный цикл «упал → подъём».
            if (State != CharacterState.Knockdown && State != CharacterState.Dead
                && Grounded && Mathf.Abs(TorsoAngle) > 75f)
            {
                Debug.Log($"[Character] упал без удара (angle={TorsoAngle:0}°), подъём через нокдаун");
                EnterKnockdown();
                return;
            }

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

            ApplyBalance(dt);
            ApplyMovement(dt);
            ApplyJump();
            TickWalkPose(dt);

            if (_getUpTimer > 0f)
            {
                // Короткий переход из лежачего положения в стойку: PD-момент не может
                // поднять тело, лежащее на полу (оно упирается), поэтому 0.35 с кинематического доворота.
                _getUpTimer -= dt;
                Rig.Torso.MoveRotation(Mathf.MoveTowardsAngle(Rig.Torso.rotation, 0f, config.rightingRate * dt));
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

        private void ApplyBalance(float dt)
        {
            var body = Rig.Torso;
            bool upright = State != CharacterState.Knockdown && State != CharacterState.Dead;

            if (!upright)
            {
                // Нокдаун/смерть: корпус «отпускаем», тело падает как настоящий рэгдолл.
                if (body.constraints != RigidbodyConstraints2D.None)
                    body.constraints = RigidbodyConstraints2D.None;
                Rig.SetPoseHeld(false);
                return;
            }

            // Пока персонаж на ногах — корпус жёстко вертикален. Это и есть ответ на «не падали после
            // каждого действия»: конечности живут физикой и болтаются, а корпус не заваливается.
            if (body.constraints != RigidbodyConstraints2D.FreezeRotation)
            {
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.rotation = 0f;
                body.angularVelocity = 0f;
            }

            Rig.SetPoseHeld(true);

            // Плавное восстановление силы моторов после удара.
            if (State == CharacterState.Stunned)
                _motorScale = Mathf.Lerp(_motorScale, config.hitMotorScale, 0.4f);
            else
                _motorScale = Mathf.MoveTowards(_motorScale, 1f, (1f / Mathf.Max(0.01f, config.recoverRampTime)) * dt);
            Rig.SetMotorStrengthScale(_motorScale);

            // Сервопривод всех суставов: локти и колени гнутся, но держат целевую позу.
            Rig.HoldPose();

            // Баланс корпуса: момент за угол (к вертикали) + демпфер угловой скорости.
            float angleErr = Mathf.DeltaAngle(body.rotation, 0f);   // °
            float torque = -angleErr * config.torsoAngleKp - body.angularVelocity * config.torsoKd;
            torque = Mathf.Clamp(torque, -config.torsoMaxTorque, config.torsoMaxTorque);
            body.AddTorque(torque, ForceMode2D.Force);

            // Горизонтальная корректирующая сила: тянет корпус обратно над стопами.
            float dx = body.position.x - Rig.GetFeetCenterX();      // м
            body.AddForce(new Vector2(-dx * config.torsoPosKp * body.mass, 0f), ForceMode2D.Force);

            // Страховка от заваливания: при заметном наклоне жёстко доворачиваем корпус к вертикали.
            // Без неё тело медленно «уезжает» и падает от любого действия — как раз то, что мешало играть.
            if (Mathf.Abs(angleErr) > 6f)
            {
                body.MoveRotation(Mathf.MoveTowardsAngle(body.rotation, 0f, config.rightingRate * dt));
                body.angularVelocity *= 0.4f;
            }

            // Ограничение угловой скорости (в Unity 6.3 Rigidbody2D.maxAngularVelocity удалён).
            float maxDeg = config.maxAngularVelocity * Mathf.Rad2Deg;
            if (Mathf.Abs(body.angularVelocity) > maxDeg)
                body.angularVelocity = Mathf.Sign(body.angularVelocity) * maxDeg;
        }

        private void UpdateGrounded(float dt)
        {
            bool now = Ground != null && Ground.IsGrounded;
            if (now) _groundedGrace = config.groundGrace;
            else _groundedGrace = Mathf.Max(0f, _groundedGrace - dt);
            Grounded = _groundedGrace > 0f;
        }


        /// <summary>
        /// Процедурная ходьба: ноги и руки качаются в противофазе, колено сгибается на шаге.
        /// Без этого персонаж «скользит» статичной позой — главная причина, что он казался болванчиком.
        /// </summary>
        private void TickWalkPose(float dt)
        {
            if (Rig == null) return;
            if (Combat != null && Combat.IsAttacking) return; // во время удара рукой управляет атака
            if (State == CharacterState.Knockdown || State == CharacterState.Dead) return;
            if (!Rig.PoseHeld) return;

            float speed = Mathf.Abs(Rig.Torso.linearVelocity.x);
            bool walking = Grounded && speed > 0.35f;
            float amp = 26f * Mathf.Clamp01(speed / Mathf.Max(1f, config.moveSpeed));

            if (!walking)
            {
                // Стоим: плавно возвращаемся в боевую стойку.
                _walkPhase = 0f;
                if (!Grounded)
                {
                    // В воздухе — поджатые ноги.
                    Rig.SetJointTarget(RagdollRig.JointHipL, 12f);
                    Rig.SetJointTarget(RagdollRig.JointHipR, 12f);
                    Rig.SetJointTarget(RagdollRig.JointKneeL, 34f);
                    Rig.SetJointTarget(RagdollRig.JointKneeR, 34f);
                }
                else
                {
                    Rig.SetStance();
                }
                return;
            }

            _walkPhase += dt * (5.5f + speed * 1.1f);
            float s = Mathf.Sin(_walkPhase);

            Rig.SetJointTarget(RagdollRig.JointHipL, s * amp);
            Rig.SetJointTarget(RagdollRig.JointHipR, -s * amp);

            Rig.SetJointTarget(RagdollRig.JointKneeL, Mathf.Max(0f, -s) * amp * 1.7f + 7f);
            Rig.SetJointTarget(RagdollRig.JointKneeR, Mathf.Max(0f, s) * amp * 1.7f + 7f);

            Rig.SetJointTarget(RagdollRig.JointShoulderL, -s * amp * 0.9f - 22f * Facing);
            Rig.SetJointTarget(RagdollRig.JointShoulderR, s * amp * 0.9f - 22f * Facing);
        }

        private void ApplyMovement(float dt)
        {
            if (State == CharacterState.Knockdown || State == CharacterState.Dead) return;

            bool grounded = Grounded;
            float control = grounded ? 1f : config.airControl;
            float target = Mathf.Clamp(MoveX, -1f, 1f) * config.moveSpeed * control;

            // Скорость задаём всем телам: ноги на шарнирах иначе тащатся и тормозят корпус.
            foreach (var part in Rig.All)
            {
                if (part == null) continue;
                var v = part.linearVelocity;
                v.x = Mathf.MoveTowards(v.x, target, config.acceleration * dt * control);
                part.linearVelocity = v;
            }

            if (Mathf.Abs(MoveX) > 0.1f) Facing = MoveX > 0f ? 1 : -1;
            if (Rig != null) Rig.Facing = Facing;

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

            // Скорость задаётся всем телам скелета: если поднять только корпус, шарниры
            // ног гасят импульс и персонаж едва подпрыгивает (замер: 0.33 м вместо 1.4).
            foreach (var part in Rig.All)
            {
                if (part == null) continue;
                var pv = part.linearVelocity;
                pv.y = config.jumpSpeed;
                part.linearVelocity = pv;
            }

            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            SetState(CharacterState.Airborne);
            SfxPlayer.Play("jump", 0.5f);
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
            // Ослабляем моторы на время стана: тело заметно реагирует на удар.
            _motorScale = config.hitMotorScale;
        }

        private void StartGettingUp()
        {
            _getUpTimer = 0.35f;
            _invulnerableTimer = config.invulnerableTime;
            _motorScale = 1f;
            Rig.SetMotorStrengthScale(1f);

            // PD-момент не может поднять тело, лежащее на полу (упирается), а доворот
            // заваливает его через голову. Надёжнее полный сброс в стойку на месте.
            float x = Rig.Torso != null ? Rig.Torso.position.x : 0f;
            Rig.ResetPose(new Vector3(x, transform.position.y, 0f));

            SetState(CharacterState.Standing);
        }

        private void Die()
        {
            SetState(CharacterState.Dead);
            _deadTimer = 0f;
            Rig.SetPoseHeld(false);

            // Без толчка труп может остаться стоять «столом» из выпрямленных ног:
            // шарниры в равновесии не требуют момента. Даём случайный опрокидывающий импульс.
            var body = Rig.Torso;
            float dir = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            body.AddTorque(dir * UnityEngine.Random.Range(70f, 130f), ForceMode2D.Impulse);
            body.AddForce(new Vector2(dir * UnityEngine.Random.Range(0.5f, 1.5f) * body.mass, 0f), ForceMode2D.Impulse);

            Debug.Log($"[Character] умер: {name}");
            SfxPlayer.Play("ko", 0.8f);
        }

        /// <summary>
        /// Возрождение: полный сброс здоровья, состояния, таймеров и позы скелета.
        /// Оружие выбрасывается — после респавна бой начинается на равных.
        /// </summary>
        public void Respawn(Vector3 worldPos)
        {
            Hp = config != null ? config.maxHp : 100f;
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
            _stunTimer = 0f;
            _knockdownTimer = 0f;
            _invulnerableTimer = config != null ? config.invulnerableTime : 0.35f;
            _getUpTimer = 0f;
            _deadTimer = 0f;
            _groundedGrace = 0f;
            Grounded = false;
            Facing = 1;
            MoveX = 0f;
            JumpQueued = false;

            if (Combat != null && Combat.Weapon != null) Combat.RequestDrop();

            Rig.ResetPose(worldPos);
            SetState(CharacterState.Standing);
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
