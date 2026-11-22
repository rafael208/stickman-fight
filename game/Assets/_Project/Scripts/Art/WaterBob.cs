using UnityEngine;

namespace StickmanFight.Art
{
    /// <summary>Лёгкое покачивание воды, чтобы фон не выглядел статичной картинкой.</summary>
    public class WaterBob : MonoBehaviour
    {
        public float amplitude = 0.06f;
        public float speed = 0.9f;

        private Vector3 _base;
        private float _phase;

        private void Awake()
        {
            _base = transform.position;
            _phase = Random.value * 6.28f;
        }

        private void Update()
        {
            transform.position = _base + Vector3.up * (Mathf.Sin(Time.time * speed + _phase) * amplitude);
        }
    }
}
