using System.Collections.Generic;
using UnityEngine;

namespace StickmanFight.Characters
{
    /// <summary>
    /// Собирает ragdoll-скелет персонажа (раздел 7.6 ТЗ): 6 физических тел на шарнирах.
    /// Строится кодом по CharacterConfig — префабы руками не собираются (ADR-3).
    /// </summary>
    public class RagdollRig : MonoBehaviour
    {
        public Rigidbody2D Torso { get; private set; }
        public Rigidbody2D Head { get; private set; }
        public Rigidbody2D ArmL { get; private set; }
        public Rigidbody2D ArmR { get; private set; }
        public Rigidbody2D LegL { get; private set; }
        public Rigidbody2D LegR { get; private set; }

        public IReadOnlyList<Rigidbody2D> All => _all;
        public Collider2D TorsoCollider { get; private set; }

        private readonly List<Rigidbody2D> _all = new List<Rigidbody2D>(6);
        private readonly List<HingeJoint2D> _joints = new List<HingeJoint2D>(5);
        private readonly List<float> _jointTargets = new List<float>(5);
        private readonly List<Vector3> _initialLocalPositions = new List<Vector3>(6);
        private readonly List<Quaternion> _initialLocalRotations = new List<Quaternion>(6);

        /// <summary>Индексы суставов для адресного управления (например, взмах рукой).</summary>
        public const int JointArmL = 0;
        public const int JointArmR = 1;
        public const int JointLegL = 2;
        public const int JointLegR = 3;
        public const int JointHead = 4;

        /// <summary>Создаёт скелет как дочерние объекты этого трансформа (фигура стоит на земле, y=0 — ступни).</summary>
        public void Build(CharacterConfig cfg, int bodyLayer)
        {
            var material = new PhysicsMaterial2D("StickmanBody")
            {
                friction = cfg.friction,
                bounciness = 0f
            };

            float legTop = cfg.legHeight;
            float torsoCenterY = legTop + cfg.torsoHeight * 0.5f;
            float torsoHalf = cfg.torsoHeight * 0.5f;
            float shoulderY = cfg.torsoHeight * 0.30f;
            float hipY = -torsoHalf;
            float neckY = torsoHalf;

            // Плечи выносим за габарит корпуса, иначе руки рисуются внутри силуэта.
            float shoulderX = cfg.torsoWidth * 0.5f + 0.03f;
            float hipX = cfg.torsoWidth * 0.24f;

            // --- Корпус: главное тело, к нему крепится всё остальное ---
            Torso = MakeBody("Torso", new Vector2(0f, torsoCenterY), cfg.torsoMass, bodyLayer, cfg);
            var torsoCol = Torso.gameObject.AddComponent<CapsuleCollider2D>();
            torsoCol.size = new Vector2(cfg.torsoWidth, cfg.torsoHeight);
            torsoCol.direction = CapsuleDirection2D.Vertical;
            torsoCol.sharedMaterial = material;
            TorsoCollider = torsoCol;
            _all.Add(Torso);

            // --- Голова ---
            Head = MakeBody("Head", new Vector2(0f, torsoCenterY + neckY + cfg.headRadius), cfg.headMass, GameLayers.Limb, cfg);
            var headCol = Head.gameObject.AddComponent<CircleCollider2D>();
            headCol.radius = cfg.headRadius;
            headCol.sharedMaterial = material;
            _all.Add(Head);

            // --- Руки ---
            ArmL = MakeLimb("ArmL", new Vector2(-shoulderX, torsoCenterY + shoulderY - cfg.armLength * 0.5f),
                new Vector2(0.12f, cfg.armLength), cfg.armMass, cfg);
            ArmR = MakeLimb("ArmR", new Vector2(shoulderX, torsoCenterY + shoulderY - cfg.armLength * 0.5f),
                new Vector2(0.12f, cfg.armLength), cfg.armMass, cfg);

            // --- Ноги ---
            LegL = MakeLimb("LegL", new Vector2(-hipX, legTop - cfg.legHeight * 0.5f),
                new Vector2(0.14f, cfg.legHeight), cfg.legMass, cfg);
            LegR = MakeLimb("LegR", new Vector2(hipX, legTop - cfg.legHeight * 0.5f),
                new Vector2(0.14f, cfg.legHeight), cfg.legMass, cfg);

            // --- Шарниры: конечность -> корпус ---
            Connect(ArmL, new Vector2(0f, cfg.armLength * 0.5f), new Vector2(-shoulderX, shoulderY), cfg.armLimit, cfg);
            Connect(ArmR, new Vector2(0f, cfg.armLength * 0.5f), new Vector2(shoulderX, shoulderY), cfg.armLimit, cfg);
            Connect(LegL, new Vector2(0f, cfg.legHeight * 0.5f), new Vector2(-hipX, hipY), cfg.legLimit, cfg);
            Connect(LegR, new Vector2(0f, cfg.legHeight * 0.5f), new Vector2(hipX, hipY), cfg.legLimit, cfg);
            Connect(Head, new Vector2(0f, -cfg.headRadius), new Vector2(0f, neckY), 20f, cfg);

            // Конечности не сталкиваются с корпусом своего персонажа (FR-RAG-01).
            foreach (var rb in _all)
            {
                if (rb == Torso) continue;
                foreach (var col in rb.GetComponentsInChildren<Collider2D>())
                {
                    Physics2D.IgnoreCollision(torsoCol, col, true);
                }
            }

            // Запоминаем стартовую позу для возрождения.
            _initialLocalPositions.Clear();
            _initialLocalRotations.Clear();
            foreach (var rb in _all)
            {
                _initialLocalPositions.Add(rb.transform.localPosition);
                _initialLocalRotations.Add(rb.transform.localRotation);
            }
        }

        /// <summary>
        /// Возвращает скелет в стартовую стойку: корень в rootWorldPos, все тела в исходные
        /// локальные позы, скорости обнулены, моторы позы включены. Используется при респавне.
        /// </summary>
        public void ResetPose(Vector3 rootWorldPos)
        {
            transform.position = rootWorldPos;

            for (int i = 0; i < _all.Count && i < _initialLocalPositions.Count; i++)
            {
                var rb = _all[i];
                rb.transform.localPosition = _initialLocalPositions[i];
                rb.transform.localRotation = _initialLocalRotations[i];
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.WakeUp();
            }

            ResetJointTargets();
            SetPoseHeld(true);
        }

        private Rigidbody2D MakeBody(string partName, Vector2 localPos, float mass, int layer, CharacterConfig cfg)
        {
            var go = new GameObject(partName) { layer = layer };
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = mass;
            rb.gravityScale = 1f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.5f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            // Unity 6.3: у Rigidbody2D больше нет maxAngularVelocity — ограничиваем вручную в StickmanCharacter.
            return rb;
        }

        private Rigidbody2D MakeLimb(string partName, Vector2 localPos, Vector2 size, float mass, CharacterConfig cfg)
        {
            var rb = MakeBody(partName, localPos, mass, GameLayers.Limb, cfg);
            var col = rb.gameObject.AddComponent<BoxCollider2D>();
            col.size = size;
            _all.Add(rb);
            return rb;
        }

        private void Connect(Rigidbody2D limb, Vector2 anchor, Vector2 connectedAnchor, float limit, CharacterConfig cfg)
        {
            var joint = limb.gameObject.AddComponent<HingeJoint2D>();
            joint.connectedBody = Torso;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = anchor;
            joint.connectedAnchor = connectedAnchor;

            joint.useLimits = true;
            // Unity 6.3: JointLimits переименован в JointAngleLimits2D.
            joint.limits = new JointAngleLimits2D { min = -limit, max = limit };

            joint.useMotor = true;
            // Unity 6.3: maxMotorForce -> maxMotorTorque.
            joint.motor = new JointMotor2D { motorSpeed = 0f, maxMotorTorque = cfg.limbMotorForce };

            _joints.Add(joint);
        }

        /// <summary>
        /// Поза «держится» (моторы включены) или «обмяк» (полный ragdoll).
        /// </summary>
        public void SetPoseHeld(bool held)
        {
            _poseHeld = held;
            foreach (var joint in _joints)
            {
                joint.useMotor = held;
            }
        }

        /// <summary>
        /// Сервопривод суставов: тянет угол конечности к нулю.
        /// Важно: мотор со speed = 0 — это тормоз, а не удержание угла. Чтобы конечность
        /// действительно стояла, скорость задаётся пропорционально ошибке угла (P-регулятор),
        /// а момент ограничивается maxMotorTorque. Вызывать каждый шаг физики, пока стоит.
        /// </summary>
        public void HoldPose()
        {
            if (!_poseHeld) return;

            for (int i = 0; i < _joints.Count; i++)
            {
                var joint = _joints[i];
                float target = i < _jointTargets.Count ? _jointTargets[i] : 0f;
                float err = Mathf.DeltaAngle(joint.jointAngle, target);
                var motor = joint.motor;
                motor.motorSpeed = Mathf.Clamp(err * ServoGain, -ServoSpeedLimit, ServoSpeedLimit);
                joint.motor = motor;
            }
        }

        /// <summary>Целевой угол сустава в градусах (0 — прямая поза).</summary>
        public void SetJointTarget(int jointIndex, float degrees)
        {
            while (_jointTargets.Count <= jointIndex) _jointTargets.Add(0f);
            _jointTargets[jointIndex] = degrees;
        }

        public void ResetJointTargets()
        {
            for (int i = 0; i < _jointTargets.Count; i++) _jointTargets[i] = 0f;
        }

        private const float ServoGain = 6f;
        private const float ServoSpeedLimit = 200f; // градусов/с
        private bool _poseHeld;

        /// <summary>Ограничения углов ослабляются при падении, чтобы конечности не «складывались» в шпагат.</summary>
        public void SetLimitsScale(float scale)
        {
            // оставлено точкой расширения для S3 (реакция на попадания по конечностям)
        }
    }
}
