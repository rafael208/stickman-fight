using UnityEngine;

namespace StickmanFight.Art
{
    /// <summary>
    /// Слой параллакса: сдвигается вслед за камерой с коэффициентом (0 — стоит, 1 — как мир).
    /// Из-за этого дальнее небо почти не двигается, а средние холмы плывут медленнее арены.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Range(0f, 1f)] public float factor = 0.2f;
        public Vector3 basePosition;

        private Transform _camera;
        private float _cameraStartX;

        public void Bind(Transform cameraTransform)
        {
            _camera = cameraTransform;
            basePosition = transform.position;
            _cameraStartX = cameraTransform != null ? cameraTransform.position.x : 0f;
        }

        private void LateUpdate()
        {
            if (_camera == null) return;
            float dx = _camera.position.x - _cameraStartX;
            transform.position = new Vector3(basePosition.x + dx * factor, basePosition.y, basePosition.z);
        }
    }
}
