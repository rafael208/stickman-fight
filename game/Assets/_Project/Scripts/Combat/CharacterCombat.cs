using StickmanFight.Art;
using StickmanFight.Characters;
using StickmanFight.Combat;
using StickmanFight.Core;
using StickmanFight.Weapons;
using UnityEngine;

namespace StickmanFight.Combat
{
    /// <summary>
    /// Боевая часть персонажа (S3): взмах, попадание, подбор и бросок оружия, выстрел.
    /// Всё через Tick(dt) — чтобы работало и в headless-стенде без Update-цикла редактора.
    /// </summary>
    public class CharacterCombat : MonoBehaviour
    {
        private enum Phase
        {
            None,
            Attack
        }

        public StickmanCharacter character;
        public Transform handAnchor;
        public Weapon weapon;

        [Tooltip("Слои существ и предметов, по которым можно попасть")]
        public LayerMask targetMask;
        [Tooltip("Слой оружия для подбора")]
        public LayerMask weaponMask;
        [Tooltip("Радиус подбора оружия, м")]
        public float pickupRadius = 1.2f;

        public bool IsAttacking => _phase != Phase.None;
        public Weapon Weapon => weapon;

        private Phase _phase;
        private float _timer;
        private float _cooldown;
        private float _reload;
        private bool _hitDone;
        private int _sourceId;

        private readonly Collider2D[] _buffer = new Collider2D[16];

        private void Awake()
        {
            if (character == null) character = GetComponent<StickmanCharacter>();
            if (targetMask.value == 0)
                targetMask = (1 << GameLayers.PlayerBody) | (1 << GameLayers.EnemyBody) | (1 << GameLayers.Props);
            if (weaponMask.value == 0) weaponMask = 1 << GameLayers.Weapon;
            _sourceId = GetInstanceID();
        }

        private void FixedUpdate()
        {
            Tick(Time.fixedDeltaTime);
        }

        public void Tick(float dt)
        {
            if (character == null) return;

            _cooldown = Mathf.Max(0f, _cooldown - dt);

            if (_reload > 0f)
            {
                _reload -= dt;
                if (_reload <= 0f && weapon != null)
                {
                    weapon.Reload();
                    Debug.Log("[Weapon] перезарядка завершена");
                }
            }

            if (_phase == Phase.Attack) TickAttack(dt);
        }

        // ------------------------------------------------------------------ API

        public void RequestAttack()
        {
            if (_phase != Phase.None || _cooldown > 0f) return;
            if (character == null || character.IsDead) return;
            if (character.State == CharacterState.Knockdown) return;

            if (weapon == null)
            {
                // Без оружия — короткий пинок-толчок, чтобы игрок не был беспомощен.
                DoUnarmedHit();
                _cooldown = 0.55f;
                return;
            }

            if (weapon.config == null) return;

            if (weapon.IsPistol)
            {
                if (_reload > 0f) return;
                if (weapon.Ammo <= 0f)
                {
                    StartReload();
                    return;
                }
            }

            _phase = Phase.Attack;
            _timer = 0f;
            _hitDone = false;

            SfxPlayer.Play("swing", 0.45f, Random.Range(0.9f, 1.1f));
        }

        public void RequestInteract()
        {
            if (weapon != null) return;
            var found = FindNearestWeapon();
            if (found != null) Equip(found);
        }

        public void RequestDrop()
        {
            if (weapon == null) return;
            var dropped = weapon;
            weapon = null;
            if (character != null && character.Rig != null) character.Rig.ResetJointTargets();
            dropped.Drop(new Vector2(character != null ? character.Facing * 2f : 2f, 1.5f));
        }

        /// <summary>Подбор без поиска — используется тестами и фабрикой сцены.</summary>
        public void Equip(Weapon target)
        {
            if (target == null || character == null) return;
            if (weapon != null) RequestDrop();

            weapon = target;
            weapon.Equip(character, handAnchor != null ? handAnchor : transform);
            SfxPlayer.Play("pickup", 0.6f);
        }

        // --------------------------------------------------------------- атака

        private void TickAttack(float dt)
        {
            var cfg = weapon != null ? weapon.config : null;
            float duration = cfg != null ? cfg.attackDuration : 0.25f;
            float hitTime = cfg != null ? cfg.hitTime : 0.12f;

            _timer += dt;

            // Взмах: рука поднимается и резко идёт вперёд.
            if (cfg != null && character != null && character.Rig != null)
            {
                int face = character.Facing;
                float t = Mathf.Clamp01(_timer / Mathf.Max(0.05f, duration));
                float angle = t < 0.5f
                    ? Mathf.Lerp(0f, -70f, t * 2f)
                    : Mathf.Lerp(-70f, 55f, (t - 0.5f) * 2f);
                character.Rig.SetJointTarget(RagdollRig.JointArmR, angle * face);
            }

            if (!_hitDone && _timer >= hitTime)
            {
                _hitDone = true;
                if (weapon != null && weapon.IsPistol) FireProjectile();
                else DoMeleeHit();
            }

            if (_timer >= duration)
            {
                _phase = Phase.None;
                _cooldown = cfg != null ? cfg.cooldown : 0.4f;
                if (character != null && character.Rig != null) character.Rig.ResetJointTargets();

                if (weapon != null && weapon.IsPistol && weapon.Ammo <= 0f) StartReload();
            }
        }

        private void StartReload()
        {
            if (weapon == null || !weapon.IsPistol) return;
            if (_reload > 0f) return;
            _reload = weapon.config.reloadTime;
            Debug.Log("[Weapon] перезарядка начата");
        }

        /// <summary>Один запрос на взмах: попадание засчитывается ровно один раз (FR-COMBAT-02).</summary>
        private void DoMeleeHit()
        {
            var cfg = weapon.config;
            int face = character != null ? character.Facing : 1;
            Vector2 origin = (Vector2)character.Rig.Torso.position + new Vector2(face * 0.35f, 0.15f);

            float speed = Mathf.Abs(character.Rig.Torso.linearVelocity.x);
            float speedFactor = Mathf.Clamp01(speed / Mathf.Max(0.1f, cfg.refSpeed));

            // След взмаха перед персонажем.
            SwingArc.Spawn(origin, face, cfg.range, Color.white);

            // Меч бьёт за счёт взмаха, а не ходьбы: базовый урон постоянный (AC: 14–21 с места).
            // Молот — наоборот, «разгоняемый»: от скорости зависит и урон, и нокбэк.
            float damage, knockback;
            if (cfg.kind == WeaponKind.Hammer)
            {
                damage = cfg.damage * Mathf.Lerp(0.8f, 1.3f, speedFactor);
                knockback = cfg.impulse * Mathf.Lerp(1.0f, 1.5f, speedFactor);
            }
            else
            {
                damage = cfg.damage;
                knockback = cfg.impulse * Mathf.Lerp(1.0f, 1.2f, speedFactor);
            }

            int count = Physics2D.OverlapCircleNonAlloc(origin, cfg.range, _buffer, targetMask);
            for (int i = 0; i < count; i++)
            {
                var col = _buffer[i];
                if (col == null) continue;

                var target = col.GetComponentInParent<StickmanCharacter>();
                if (target == null || target == character) continue;

                Vector2 point = col.ClosestPoint(origin);
                var info = DamageInfo.Melee(damage, new Vector2(face * knockback, knockback * 0.35f), point, gameObject, _sourceId);
                bool applied = DamageSystem.Apply(target, info);
                Debug.Log($"[Hit] {cfg.displayName} -> {target.name} dmg={damage:0.0} applied={applied}");
            }
        }

        private void DoUnarmedHit()
        {
            int face = character != null ? character.Facing : 1;
            Vector2 origin = (Vector2)character.Rig.Torso.position + new Vector2(face * 0.3f, 0.1f);
            int count = Physics2D.OverlapCircleNonAlloc(origin, 0.7f, _buffer, targetMask);

            for (int i = 0; i < count; i++)
            {
                var target = _buffer[i] != null ? _buffer[i].GetComponentInParent<StickmanCharacter>() : null;
                if (target == null || target == character) continue;

                var info = DamageInfo.Melee(6f, new Vector2(face * 4f, 2f), origin, gameObject, _sourceId);
                DamageSystem.Apply(target, info);
                return;
            }
        }

        private void FireProjectile()
        {
            var cfg = weapon.config;
            int face = character != null ? character.Facing : 1;
            Vector2 dir = new Vector2(face, 0f);
            Vector2 spawn = weapon.MuzzlePosition;

            var go = new GameObject("Bullet") { layer = GameLayers.Projectile };
            go.transform.position = (Vector3)spawn + (Vector3)(dir * 0.1f);

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.mass = 0.05f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.07f;

            var visual = new GameObject("Trail");
            visual.transform.SetParent(go.transform, false);
            var lr = visual.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.useWorldSpace = false;
            lr.positionCount = 2;
            lr.startWidth = lr.endWidth = 0.06f;
            lr.SetPosition(0, new Vector3(-0.18f, 0f, 0f));
            lr.SetPosition(1, new Vector3(0.18f, 0f, 0f));
            lr.startColor = lr.endColor = new Color(1f, 0.85f, 0.35f);
            lr.sortingOrder = 30;

            var projectile = go.AddComponent<Projectile>();
            projectile.Configure(dir, cfg.damage, cfg.projectileLifetime, cfg.impulse / 10f, gameObject, _sourceId);

            // Пуля не должна задевать стрелявшего.
            if (character != null && character.Rig != null)
            {
                foreach (var part in character.Rig.All)
                foreach (var own in part.GetComponentsInChildren<Collider2D>())
                    Physics2D.IgnoreCollision(col, own, true);
            }

            weapon.ConsumeAmmo();
            SfxPlayer.Play("shoot", 0.55f, Random.Range(0.95f, 1.05f));
            Debug.Log($"[Hit] выстрел, патронов осталось {weapon.Ammo:0}");
        }

        private Weapon FindNearestWeapon()
        {
            if (character == null) return null;

            Vector2 origin = character.Rig.Torso.position;
            int count = Physics2D.OverlapCircleNonAlloc(origin, pickupRadius, _buffer, weaponMask);

            Weapon best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var col = _buffer[i];
                if (col == null) continue;
                var w = col.GetComponentInParent<Weapon>();
                if (w == null || w.IsHeld) continue;

                float d = Vector2.SqrMagnitude((Vector2)w.transform.position - origin);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = w;
                }
            }

            return best;
        }
    }
}
