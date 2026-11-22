using StickmanFight.Characters;
using UnityEngine;

namespace StickmanFight.Core
{
    /// <summary>
    /// Общая камера на всех бойцов: центр — середина между ними, зум растёт с расстоянием.
    /// Обязательное требование для локального мультиплеера: никто не должен уезжать за экран.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Цели")]
        public StickmanCharacter[] targets;
        public StickmanCharacter characterTarget;
        public Transform target;

        [Header("Кадрирование")]
        public Vector2 offset = new Vector2(0f, 1.2f);
        public float baseSize = 4.6f;
        public float sizePerMeter = 0.42f;
        public float minSize = 4.2f;
        public float maxSize = 7.5f;
        public float smoothing = 6f;
        [Tooltip("Границы, в которых может находиться центр камеры")]
        public Vector2 min = new Vector2(-9.5f, 2f);
        public Vector2 max = new Vector2(9.5f, 4f);

        private Camera _camera;
        private float _shakeTime;
        private float _shakeDuration = 1f;
        private float _shakeAmplitude;

        /// <summary>Тряска камеры при попадании. amplitude — метры, duration — секунды.</summary>
        public void Shake(float amplitude, float duration)
        {
            _shakeAmplitude = Mathf.Max(_shakeAmplitude, amplitude);
            _shakeDuration = Mathf.Max(0.01f, duration);
            _shakeTime = _shakeDuration;
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            ResolveTargets();
        }

        private void ResolveTargets()
        {
            if (targets == null || targets.Length == 0)
            {
                if (characterTarget != null) targets = new[] { characterTarget };
            }

            if (target == null && characterTarget != null && characterTarget.Rig != null)
                target = characterTarget.Rig.Torso.transform;
        }

        private void LateUpdate()
        {
            ResolveTargets();

            Vector2 center;
            float spread;

            if (!TryGetGroupFrame(out center, out spread))
            {
                if (target == null) return;
                center = target.position;
                spread = 0f;
            }

            float desiredSize = Mathf.Clamp(baseSize + spread * sizePerMeter, minSize, maxSize);

            float k = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
            if (_camera != null && _camera.orthographic)
                _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, desiredSize, k);

            var desired = new Vector3(
                Mathf.Clamp(center.x + offset.x, min.x, max.x),
                Mathf.Clamp(center.y + offset.y, min.y, max.y),
                -10f);

            transform.position = Vector3.Lerp(transform.position, desired, k);

            // Тряска поверх позиции, с затуханием.
            if (_shakeTime > 0f)
            {
                _shakeTime -= Time.unscaledDeltaTime;
                float strength = _shakeAmplitude * (_shakeTime / _shakeDuration);
                var jitter = Random.insideUnitCircle * strength;
                transform.position += new Vector3(jitter.x, jitter.y, 0f);
            }
        }

        /// <summary>Центр и разброс живой группы бойцов. Мёртвых не учитываем, чтобы камера не прыгала.</summary>
        private bool TryGetGroupFrame(out Vector2 center, out float spread)
        {
            center = Vector2.zero;
            spread = 0f;
            if (targets == null || targets.Length == 0) return false;

            int count = 0;
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            Vector2 sum = Vector2.zero;

            foreach (var c in targets)
            {
                if (c == null || c.Rig == null || c.Rig.Torso == null) continue;
                if (c.IsDead) continue;

                var p = c.Rig.Torso.position;
                sum += p;
                count++;
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
            }

            if (count == 0) return false;

            center = sum / count;
            float spanX = maxX - minX;
            float spanY = maxY - minY;
            spread = Mathf.Max(spanX, spanY * 1.4f);
            return true;
        }
    }
}
