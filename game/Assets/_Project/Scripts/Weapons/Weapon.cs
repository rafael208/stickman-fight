using StickmanFight.Art;
using StickmanFight.Characters;
using UnityEngine;

namespace StickmanFight.Weapons
{
    /// <summary>
    /// Предмет оружия на земле и в руках (FR-WEAP-01..04).
    /// В руках физика выключена и объект просто прикреплён к кисти — так проще и стабильнее,
    /// чем FixedJoint на подвижной конечности.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Weapon : MonoBehaviour
    {
        public WeaponConfig config;
        public Rigidbody2D body;
        public Collider2D hitbox;

        public bool IsHeld { get; private set; }
        public StickmanCharacter Owner { get; private set; }
        public float Ammo { get; private set; }
        public bool IsReloading { get; set; }

        private WeaponVisual _visual;
        private Transform _defaultParent;
        private Vector3 _defaultScale;

        public bool IsPistol => config != null && config.kind == WeaponKind.Pistol;

        private void Awake()
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (hitbox == null) hitbox = GetComponent<Collider2D>();

            _visual = GetComponent<WeaponVisual>();
            if (_visual == null) _visual = gameObject.AddComponent<WeaponVisual>();

            _defaultParent = transform.parent;
            _defaultScale = transform.localScale;

            if (config != null)
            {
                body.mass = config.mass;
                Ammo = config.ammo;
                _visual.Build(config);
            }
        }

        public void Equip(StickmanCharacter owner, Transform hand)
        {
            if (IsHeld) return;

            Owner = owner;
            IsHeld = true;

            body.simulated = false;
            if (hitbox != null) hitbox.enabled = false;

            transform.SetParent(hand, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = _defaultScale;

            Ammo = config.ammo;
            Debug.Log($"[Weapon] {name} взят {owner?.name}");
        }

        private void LateUpdate()
        {
            // Оружие поворачивается вместе с бойцом: при движении влево — зеркалится.
            if (Owner == null || Owner.Rig == null || Owner.Rig.Torso == null) return;
            if (!IsHeld) return;

            int face = Owner.Facing;
            transform.localScale = new Vector3(
                Mathf.Abs(_defaultScale.x) * face, _defaultScale.y, _defaultScale.z);

            if (face < 0)
            {
                float torsoX = Owner.Rig.Torso.position.x;
                var p = transform.position;
                p.x = 2f * torsoX - p.x;
                transform.position = p;
            }
        }

        public void Drop(Vector2 impulse)
        {
            if (!IsHeld) return;

            var previousOwner = Owner;
            Owner = null;
            IsHeld = false;

            transform.SetParent(_defaultParent, true);
            transform.localScale = _defaultScale;

            body.simulated = true;
            body.bodyType = RigidbodyType2D.Dynamic;
            if (hitbox != null) hitbox.enabled = true;

            body.AddForce(impulse, ForceMode2D.Impulse);
            Debug.Log($"[Weapon] {name} выброшен {(previousOwner != null ? previousOwner.name : "")}");
        }

        public void ConsumeAmmo()
        {
            Ammo = Mathf.Max(0f, Ammo - 1f);
        }

        public void Reload()
        {
            Ammo = config.ammo;
        }

        /// <summary>Точка вылета снаряда — конец ствола в мировых координатах.</summary>
        public Vector2 MuzzlePosition =>
            (Vector2)transform.position + (Vector2)(transform.right * config.visualSize.x * 0.5f);
    }
}
