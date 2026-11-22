using UnityEngine;

namespace StickmanFight.Weapons
{
    /// <summary>
    /// Снаряд пистолета (FR-WEAP-08). Динамическое тело без гравитации с непрерывной
    /// детекцией: быстро летящая пуля не должна «проскакивать» тонкие стены.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        public float speed = 22f;
        public float damage = 12f;
        public float lifetime = 2f;
        public float impulse = 2f;
        public Vector2 direction = Vector2.right;
        public GameObject instigator;

        private Rigidbody2D _body;
        private int _sourceId;

        public void Configure(Vector2 dir, float dmg, float life, float knockback, GameObject shooter, int sourceId)
        {
            direction = dir.normalized;
            damage = dmg;
            lifetime = life;
            impulse = knockback;
            instigator = shooter;
            _sourceId = sourceId;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Dynamic;
            _body.gravityScale = 0f;
            _body.mass = 0.05f;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _sourceId = GetInstanceID();
        }

        private void Start()
        {
            _body.linearVelocity = direction * speed;
            Destroy(gameObject, lifetime);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleHit(collision.collider, collision.GetContact(0).point);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleHit(other, transform.position);
        }

        private void HandleHit(Component other, Vector2 point)
        {
            if (other == null) return;

            var character = other.GetComponentInParent<Characters.StickmanCharacter>();
            if (character != null)
            {
                // Дружественный огонь выключен (FR-COMBAT-10).
                var shooter = instigator != null ? instigator.GetComponentInParent<Characters.StickmanCharacter>() : null;
                if (character != shooter)
                {
                    var info = Combat.DamageInfo.Projectile(
                        damage,
                        direction * impulse,
                        point,
                        instigator,
                        _sourceId);
                    Combat.DamageSystem.Apply(character, info);
                }
            }

            Destroy(gameObject);
        }
    }
}
