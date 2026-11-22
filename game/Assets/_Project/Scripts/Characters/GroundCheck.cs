using UnityEngine;

namespace StickmanFight.Characters
{
    /// <summary>
    /// Проверка «стоит на земле» (FR-CHAR-05): круг под персонажем на слое Ground.
    /// Опрос по требованию (а не кэш в FixedUpdate), чтобы то же самое работало
    /// в headless-стенде с ручным шагом физики.
    /// </summary>
    public class GroundCheck : MonoBehaviour
    {
        [SerializeField] private float radius = 0.18f;
        [SerializeField] private Vector2 offset = new Vector2(0f, -0.6f);
        [SerializeField] private LayerMask groundMask;

        private Rigidbody2D _body;
        private readonly Collider2D[] _buffer = new Collider2D[8];

        public Vector2 Origin => (_body != null ? _body.position : (Vector2)transform.position) + offset;

        public void Configure(float checkRadius, Vector2 worldOffset)
        {
            radius = checkRadius;
            offset = worldOffset;
            groundMask = 1 << GameLayers.Ground;
            _body = GetComponent<Rigidbody2D>();
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            if (groundMask.value == 0) groundMask = 1 << GameLayers.Ground;
        }

        public bool IsGrounded
        {
            get
            {
                int count = Physics2D.OverlapCircleNonAlloc(Origin, radius, _buffer, groundMask);
                for (int i = 0; i < count; i++)
                {
                    var col = _buffer[i];
                    if (col == null) continue;
                    // Свои коллайдеры (ноги) землёй не считаем.
                    if (_body != null && col.attachedRigidbody == _body) continue;
                    return true;
                }
                return false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(Origin, radius);
        }
    }
}
