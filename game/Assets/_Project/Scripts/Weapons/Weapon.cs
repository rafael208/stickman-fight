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
        private Transform _hand;
        public float Ammo { get; private set; }
        public bool IsReloading { get; set; }

        private WeaponVisual _visual;
        private Transform _defaultParent;
        private Vector3 _defaultScale;

        public bool IsPistol => config != null && config.kind == WeaponKind.Pistol;

        public void Initialize()
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

            // Не делаем оружие ребёнком кисти: иначе при зеркале тела оно остаётся
            // на физической руке и «лагает» при развороте. Держим позицию сами.
            _hand = hand;
            transform.SetParent(null, true);
            transform.localScale = _defaultScale;
            SnapToHand();

            Ammo = config.ammo;
            Debug.Log($"[Weapon] {name} взят {owner?.name}");
        }

        private void LateUpdate()
        {
            if (Owner == null || !IsHeld) return;
            SnapToHand();
        }

        /// <summary>Ставит оружие в кисть с учётом зеркала персонажа.</summary>
        private void SnapToHand()
        {
            if (_hand == null || Owner == null || Owner.Rig == null || Owner.Rig.Torso == null) return;

            Vector3 pos = _hand.position;
            Quaternion rot = _hand.rotation;

            if (Owner.Facing < 0)
            {
                float torsoX = Owner.Rig.Torso.position.x;
                pos.x = 2f * torsoX - pos.x;
                rot = Quaternion.Euler(0f, 0f, 180f - _hand.eulerAngles.z);
            }

            pos += rot * new Vector3(0.22f, 0f, 0f);
            transform.SetPositionAndRotation(pos, rot);
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
