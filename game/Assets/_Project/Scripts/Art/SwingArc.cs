using UnityEngine;

namespace StickmanFight.Art
{
    /// <summary>
    /// Визуальный след взмаха: короткая белая дуга, которая быстро гаснет.
    /// Создаётся по месту попадания/замаха и не влияет на физику.
    /// </summary>
    public class SwingArc : MonoBehaviour
    {
        private static Material _material;

        private LineRenderer _line;
        private float _life = 0.16f;
        private float _duration = 0.16f;

        public static void Spawn(Vector2 center, float facing, float radius, Color color)
        {
            if (_material == null) _material = new Material(Shader.Find("Sprites/Default")) { name = "SwingArc" };

            var go = new GameObject("SwingArc");
            go.transform.position = center;

            var arc = go.AddComponent<SwingArc>();
            var lr = go.AddComponent<LineRenderer>();
            lr.material = _material;
            lr.useWorldSpace = true;
            lr.widthMultiplier = 0.11f;
            lr.numCapVertices = 2;
            lr.startColor = lr.endColor = color;
            lr.sortingOrder = 25;

            const int segments = 10;
            lr.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);
                float angle = Mathf.Lerp(-75f, 75f, t) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(angle) * facing, Mathf.Sin(angle));
                lr.SetPosition(i, center + dir * radius);
            }

            arc._line = lr;
        }

        private void Update()
        {
            _life -= Time.unscaledDeltaTime;
            if (_line != null)
            {
                var c = _line.startColor;
                c.a = Mathf.Clamp01(_life / _duration);
                _line.startColor = _line.endColor = c;
            }

            if (_life <= 0f) Destroy(gameObject);
        }
    }
}
