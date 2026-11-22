using UnityEngine;

namespace StickmanFight.Core
{
    /// <summary>
    /// Камера-следопыт со сглаживанием и ограничением по границам уровня (FR-LEVEL-01).
    /// Работает в LateUpdate, чтобы не отставать от интерполированной физики.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        [Tooltip("Следить за корпусом персонажа: корневой объект не двигается, двигается только ragdoll")]
        public StickmanFight.Characters.StickmanCharacter characterTarget;
        [Tooltip("Смещение относительно цели, м")]
        public Vector2 offset = new Vector2(0f, 1.1f);
        [Tooltip("Скорость сглаживания, 1/с")]
        public float smoothing = 6f;
        [Tooltip("Границы, в которых может находиться центр камеры")]
        public Vector2 min = new Vector2(-10f, 2f);
        public Vector2 max = new Vector2(10f, 4f);

        private void Start()
        {
            ResolveTarget();
        }

        private void ResolveTarget()
        {
            if (characterTarget != null && characterTarget.Rig != null)
                target = characterTarget.Rig.Torso.transform;
        }

        private void LateUpdate()
        {
            if (target == null) ResolveTarget();
            if (target == null) return;

            Vector3 desired = new Vector3(
                Mathf.Clamp(target.position.x + offset.x, min.x, max.x),
                Mathf.Clamp(target.position.y + offset.y, min.y, max.y),
                -10f);

            float k = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, k);
        }

        public void SnapToTarget()
        {
            if (target == null) ResolveTarget();
            if (target == null) return;

            transform.position = new Vector3(
                Mathf.Clamp(target.position.x + offset.x, min.x, max.x),
                Mathf.Clamp(target.position.y + offset.y, min.y, max.y),
                -10f);
        }
    }
}
