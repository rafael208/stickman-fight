using System.Collections.Generic;
using UnityEngine;

namespace StickmanFight.Characters
{
    /// <summary>
    /// 10-сегментный ragdoll: корпус, голова, плечи×2, предплечья×2, бёдра×2, голени×2 — 9 шарниров.
    /// Суставы держатся PD-контроллером (крутящий момент по ошибке угла и относительной скорости),
    /// как в референсе: тела гнутся в локтях и коленях, а не поворачиваются кинематически.
    /// </summary>
    public class RagdollRig : MonoBehaviour
    {
        public Rigidbody2D Torso { get; private set; }
        public Rigidbody2D Head { get; private set; }
        public Rigidbody2D UpperArmL { get; private set; }
        public Rigidbody2D UpperArmR { get; private set; }
        public Rigidbody2D ForearmL { get; private set; }
        public Rigidbody2D ForearmR { get; private set; }
        public Rigidbody2D ThighL { get; private set; }
        public Rigidbody2D ThighR { get; private set; }
        public Rigidbody2D ShinL { get; private set; }
        public Rigidbody2D ShinR { get; private set; }

        // Совместимость со старым кодом: ArmL/R = плечи, LegL/R = бёдра.
        public Rigidbody2D ArmL => UpperArmL;
        public Rigidbody2D ArmR => UpperArmR;
        public Rigidbody2D LegL => ThighL;
        public Rigidbody2D LegR => ThighR;

        public IReadOnlyList<Rigidbody2D> All => _all;
        public Collider2D TorsoCollider { get; private set; }

        public const int JointNeck = 0;
        public const int JointShoulderL = 1;
        public const int JointShoulderR = 2;
        public const int JointElbowL = 3;
        public const int JointElbowR = 4;
        public const int JointHipL = 5;
        public const int JointHipR = 6;
        public const int JointKneeL = 7;
        public const int JointKneeR = 8;
        public const int JointArmR = JointShoulderR; // для взмаха правой рукой

        private sealed class Joint
        {
            public HingeJoint2D hinge;
            public float target;
            public float gain;       // усиление сервопривода (°/с на ° ошибки)
            public float maxTorque;  // предел момента мотора
        }

        private readonly List<Rigidbody2D> _all = new List<Rigidbody2D>(10);
        private readonly List<Joint> _joints = new List<Joint>(9);
        private readonly List<Vector3> _initialLocalPositions = new List<Vector3>(10);
        private readonly List<Quaternion> _initialLocalRotations = new List<Quaternion>(10);

        private bool _poseHeld = true;
        private float _strengthScale = 1f;

        public void Build(CharacterConfig cfg, int bodyLayer)
        {
            var material = new PhysicsMaterial2D("StickmanBody")
            {
                friction = cfg.friction,
                bounciness = 0f
            };

            float shin = cfg.shinLength;
            float thigh = cfg.thighLength;
            float uarm = cfg.upperArmLength;
            float farm = cfg.forearmLength;
            float hipY = shin + thigh;
            float torsoCenterY = hipY + cfg.torsoHeight * 0.5f;
            float torsoHalf = cfg.torsoHeight * 0.5f;
            float shoulderY = cfg.torsoHeight * 0.32f;
            float shoulderX = cfg.torsoWidth * 0.5f + 0.03f;
            float legX = cfg.torsoWidth * 0.26f;

            // Корпус.
            Torso = MakeBody("Torso", new Vector2(0f, torsoCenterY), cfg.torsoMass, bodyLayer);
            var torsoCol = Torso.gameObject.AddComponent<CapsuleCollider2D>();
            torsoCol.size = new Vector2(cfg.torsoWidth, cfg.torsoHeight);
            torsoCol.direction = CapsuleDirection2D.Vertical;
            torsoCol.sharedMaterial = material;
            TorsoCollider = torsoCol;

            // Голова.
            Head = MakeBody("Head", new Vector2(0f, torsoCenterY + torsoHalf + cfg.headRadius),
                cfg.headMass, GameLayers.Limb);
            var headCol = Head.gameObject.AddComponent<CircleCollider2D>();
            headCol.radius = cfg.headRadius;
            headCol.sharedMaterial = material;

            // Плечи и предплечья.
            UpperArmL = MakeLimb("UpperArmL", new Vector2(-shoulderX, torsoCenterY + shoulderY - uarm * 0.5f),
                new Vector2(0.12f, uarm), cfg.upperArmMass);
            UpperArmR = MakeLimb("UpperArmR", new Vector2(shoulderX, torsoCenterY + shoulderY - uarm * 0.5f),
                new Vector2(0.12f, uarm), cfg.upperArmMass);
            ForearmL = MakeLimb("ForearmL", new Vector2(-shoulderX, torsoCenterY + shoulderY - uarm - farm * 0.5f),
                new Vector2(0.11f, farm), cfg.forearmMass);
            ForearmR = MakeLimb("ForearmR", new Vector2(shoulderX, torsoCenterY + shoulderY - uarm - farm * 0.5f),
                new Vector2(0.11f, farm), cfg.forearmMass);

            // Бёдра и голени.
            ThighL = MakeLimb("ThighL", new Vector2(-legX, shin + thigh * 0.5f),
                new Vector2(0.13f, thigh), cfg.thighMass);
            ThighR = MakeLimb("ThighR", new Vector2(legX, shin + thigh * 0.5f),
                new Vector2(0.13f, thigh), cfg.thighMass);
            ShinL = MakeLimb("ShinL", new Vector2(-legX, shin * 0.5f),
                new Vector2(0.12f, shin), cfg.shinMass);
            ShinR = MakeLimb("ShinR", new Vector2(legX, shin * 0.5f),
                new Vector2(0.12f, shin), cfg.shinMass);

            // Шарниры (child -> parent), с PD-коэффициентами по типу сустава.
            Connect(JointNeck, Head, Torso, new Vector2(0f, -cfg.headRadius), new Vector2(0f, torsoHalf),
                cfg.neckKp, cfg.neckKd, cfg.neckMax, cfg.neckLimit);
            Connect(JointShoulderL, UpperArmL, Torso, new Vector2(0f, uarm * 0.5f), new Vector2(-shoulderX, shoulderY),
                cfg.shoulderKp, cfg.shoulderKd, cfg.shoulderMax, cfg.shoulderLimit);
            Connect(JointShoulderR, UpperArmR, Torso, new Vector2(0f, uarm * 0.5f), new Vector2(shoulderX, shoulderY),
                cfg.shoulderKp, cfg.shoulderKd, cfg.shoulderMax, cfg.shoulderLimit);
            Connect(JointElbowL, ForearmL, UpperArmL, new Vector2(0f, farm * 0.5f), new Vector2(0f, -uarm * 0.5f),
                cfg.elbowKp, cfg.elbowKd, cfg.elbowMax, cfg.elbowLimit);
            Connect(JointElbowR, ForearmR, UpperArmR, new Vector2(0f, farm * 0.5f), new Vector2(0f, -uarm * 0.5f),
                cfg.elbowKp, cfg.elbowKd, cfg.elbowMax, cfg.elbowLimit);
            Connect(JointHipL, ThighL, Torso, new Vector2(0f, thigh * 0.5f), new Vector2(-legX, -torsoHalf),
                cfg.hipKp, cfg.hipKd, cfg.hipMax, cfg.hipLimit);
            Connect(JointHipR, ThighR, Torso, new Vector2(0f, thigh * 0.5f), new Vector2(legX, -torsoHalf),
                cfg.hipKp, cfg.hipKd, cfg.hipMax, cfg.hipLimit);
            Connect(JointKneeL, ShinL, ThighL, new Vector2(0f, shin * 0.5f), new Vector2(0f, -thigh * 0.5f),
                cfg.kneeKp, cfg.kneeKd, cfg.kneeMax, cfg.kneeLimit);
            Connect(JointKneeR, ShinR, ThighR, new Vector2(0f, shin * 0.5f), new Vector2(0f, -thigh * 0.5f),
                cfg.kneeKp, cfg.kneeKd, cfg.kneeMax, cfg.kneeLimit);

            // Конечности не сталкиваются с собственным корпусом.
            foreach (var rb in _all)
            {
                if (rb == Torso) continue;
                foreach (var col in rb.GetComponentsInChildren<Collider2D>())
                    Physics2D.IgnoreCollision(torsoCol, col, true);
            }

            _initialLocalPositions.Clear();
            _initialLocalRotations.Clear();
            foreach (var rb in _all)
            {
                _initialLocalPositions.Add(rb.transform.localPosition);
                _initialLocalRotations.Add(rb.transform.localRotation);
            }
        }

        /// <summary>
        /// Один шаг сервопривода суставов: скорость мотора пропорциональна ошибке угла.
        /// Это стабильная форма активного ragdoll (проверено на 6 телах в S1): конечности
        /// гнутся и возвращаются к целевой позе, момент ограничен maxTorque.
        /// </summary>
        public void HoldPose()
        {
            if (!_poseHeld) return;

            for (int i = 0; i < _joints.Count; i++)
            {
                var j = _joints[i];
                float err = Mathf.DeltaAngle(j.hinge.jointAngle, j.target);
                float speed = Mathf.Clamp(err * j.gain * _strengthScale, -ServoSpeedLimit, ServoSpeedLimit);

                var motor = j.hinge.motor;
                motor.motorSpeed = speed;
                motor.maxMotorTorque = j.maxTorque * _strengthScale;
                j.hinge.motor = motor;
            }
        }

        private const float ServoSpeedLimit = 360f; // °/с

        public void SetPoseHeld(bool held)
        {
            _poseHeld = held;
        }

        /// <summary>Ослабление моторов при ударе (0..1). 1 — полная сила.</summary>
        public void SetMotorStrengthScale(float scale)
        {
            _strengthScale = Mathf.Clamp01(scale);
        }

        public void SetJointTarget(int jointIndex, float degrees)
        {
            if (jointIndex >= 0 && jointIndex < _joints.Count)
                _joints[jointIndex].target = degrees;
        }

        public void ResetJointTargets()
        {
            foreach (var j in _joints) j.target = 0f;
        }

        public float GetFeetCenterX()
        {
            return (ShinL.position.x + ShinR.position.x) * 0.5f;
        }

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
            _strengthScale = 1f;
            SetPoseHeld(true);
        }

        private Rigidbody2D MakeBody(string partName, Vector2 localPos, float mass, int layer)
        {
            var go = new GameObject(partName) { layer = layer };
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = mass;
            rb.gravityScale = 1f;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 2.5f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            _all.Add(rb);
            return rb;
        }

        private Rigidbody2D MakeLimb(string partName, Vector2 localPos, Vector2 size, float mass)
        {
            var rb = MakeBody(partName, localPos, mass, GameLayers.Limb);
            var col = rb.gameObject.AddComponent<BoxCollider2D>();
            col.size = size;
            return rb;
        }

        private void Connect(int jointIndex, Rigidbody2D child, Rigidbody2D parent,
            Vector2 anchor, Vector2 connectedAnchor,
            float kp, float kd, float maxTorque, float limit)
        {
            var hinge = child.gameObject.AddComponent<HingeJoint2D>();
            hinge.connectedBody = parent;
            hinge.autoConfigureConnectedAnchor = false;
            hinge.anchor = anchor;
            hinge.connectedAnchor = connectedAnchor;
            hinge.useLimits = true;
            hinge.limits = new JointAngleLimits2D { min = -limit, max = limit };
            hinge.useMotor = true;
            hinge.motor = new JointMotor2D { motorSpeed = 0f, maxMotorTorque = maxTorque };

            _joints.Add(new Joint
            {
                hinge = hinge,
                target = 0f,
                gain = kp,
                maxTorque = maxTorque
            });
        }
    }
}
