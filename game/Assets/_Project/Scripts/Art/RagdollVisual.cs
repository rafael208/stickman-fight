using System.Collections;
using UnityEngine;
using StickmanFight.Characters;

namespace StickmanFight.Art
{
    /// <summary>
    /// Рисует стикмена по физическим телам ragdoll: белая обводка + цветной силуэт,
    /// как в трейлере (ADR-2: только линии, без спрайтов).
    /// Каждый сегмент — пара LineRenderer (обводка сзади, цвет сверху).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class RagdollVisual : MonoBehaviour
    {
        [SerializeField] private float lineWidth = 0.15f;
        [SerializeField] private Color color = Color.cyan;
        [SerializeField] private Color outlineColor = Color.white;
        [SerializeField] private int sortingOrder = 10;

        private RagdollRig _rig;
        private Segment _torso, _head, _armL, _armR, _legL, _legR;
        private Material _fillMaterial;
        private Material _outlineMaterial;
        private float _headRadius = 0.16f;
        private const int HeadSegments = 16;

        private sealed class Segment
        {
            public LineRenderer Fill;
            public LineRenderer Outline;
            public int Points;
        }

        public void Init(RagdollRig rig, Color tint, float width = -1f)
        {
            _rig = rig;
            color = tint;
            if (width > 0f) lineWidth = width;
            Build();
        }

        public void SetColor(Color tint)
        {
            color = tint;
            if (_torso == null) return;
            foreach (var s in All()) s.Fill.startColor = s.Fill.endColor = color;
        }

        /// <summary>Короткая вспышка другим цветом (попадание), потом возврат к базовому.</summary>
        public void Flash(Color tint, float duration = 0.08f)
        {
            StopAllCoroutines();
            StartCoroutine(FlashRoutine(tint, duration));
        }

        private IEnumerator FlashRoutine(Color tint, float duration)
        {
            SetColor(tint);
            yield return new WaitForSeconds(duration);
            SetColor(color);
        }

        private void Build()
        {
            if (_torso != null) return;

            _fillMaterial = new Material(Shader.Find("Sprites/Default")) { name = "RagdollFill" };
            _outlineMaterial = new Material(Shader.Find("Sprites/Default")) { name = "RagdollOutline" };

            _torso = MakeSegment("Torso", 2);
            _armL = MakeSegment("ArmL", 2);
            _armR = MakeSegment("ArmR", 2);
            _legL = MakeSegment("LegL", 2);
            _legR = MakeSegment("LegR", 2);
            _head = MakeSegment("Head", HeadSegments + 1);
        }

        private Segment MakeSegment(string name, int points)
        {
            var root = new GameObject(name + "Line");
            root.transform.SetParent(transform, false);

            var outlineGo = new GameObject("Outline");
            outlineGo.transform.SetParent(root.transform, false);
            var outline = outlineGo.AddComponent<LineRenderer>();
            outline.material = _outlineMaterial;
            outline.useWorldSpace = true;
            outline.widthMultiplier = lineWidth * 1.9f;
            outline.numCapVertices = 4;
            outline.numCornerVertices = 3;
            outline.startColor = outline.endColor = outlineColor;
            outline.sortingOrder = sortingOrder - 1;
            outline.positionCount = points;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(root.transform, false);
            var fill = fillGo.AddComponent<LineRenderer>();
            fill.material = _fillMaterial;
            fill.useWorldSpace = true;
            fill.widthMultiplier = lineWidth;
            fill.numCapVertices = 4;
            fill.numCornerVertices = 3;
            fill.startColor = fill.endColor = color;
            fill.sortingOrder = sortingOrder;
            fill.positionCount = points;

            return new Segment { Fill = fill, Outline = outline, Points = points };
        }

        private void LateUpdate()
        {
            if (_rig == null) _rig = GetComponentInParent<RagdollRig>();
            if (_rig == null || _rig.Torso == null) return;
            if (_torso == null) Build();

            var torso = _rig.Torso;

            Vector2 up = torso.transform.up;
            Vector2 center = torso.position;
            var capsule = torso.GetComponent<CapsuleCollider2D>();
            float halfHeight = capsule != null ? capsule.size.y * 0.5f : 0.4f;
            SetSegment(_torso, center - up * halfHeight, center + up * halfHeight);

            if (_rig.Head != null)
            {
                var circle = _rig.Head.GetComponent<CircleCollider2D>();
                if (circle != null) _headRadius = circle.radius;
                DrawCircle(_head, _rig.Head.position, _headRadius);
            }

            DrawLimb(_armL, _rig.ArmL);
            DrawLimb(_armR, _rig.ArmR);
            DrawLimb(_legL, _rig.LegL);
            DrawLimb(_legR, _rig.LegR);
        }

        private static void DrawLimb(Segment segment, Rigidbody2D body)
        {
            if (body == null || segment == null) return;

            var box = body.GetComponent<BoxCollider2D>();
            float half = box != null ? box.size.y * 0.5f : 0.25f;
            Vector2 dir = body.transform.up;
            Vector2 center = body.position;

            SetSegment(segment, center - dir * half, center + dir * half);
        }

        private static void SetSegment(Segment s, Vector2 from, Vector2 to)
        {
            s.Fill.SetPosition(0, from);
            s.Fill.SetPosition(1, to);
            s.Outline.SetPosition(0, from);
            s.Outline.SetPosition(1, to);
        }

        private void DrawCircle(Segment s, Vector2 center, float radius)
        {
            if (s == null) return;

            for (int i = 0; i <= HeadSegments; i++)
            {
                float a = i / (float)HeadSegments * Mathf.PI * 2f;
                var p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;

                s.Fill.SetPosition(i, p);
                s.Outline.SetPosition(i, p);
            }
        }

        private Segment[] All() => new[] { _torso, _head, _armL, _armR, _legL, _legR };
    }
}
