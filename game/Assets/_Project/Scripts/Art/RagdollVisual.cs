using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StickmanFight.Characters;

namespace StickmanFight.Art
{
    /// <summary>
    /// Рисует 10-сегментного стикмена по физическим телам: белая обводка + цветной силуэт.
    /// Каждая часть — пара LineRenderer (обводка сзади, цвет сверху). Только визуал.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class RagdollVisual : MonoBehaviour
    {
        [SerializeField] private float lineWidth = 0.13f;
        [SerializeField] private Color color = Color.cyan;
        [SerializeField] private Color outlineColor = Color.white;
        [SerializeField] private int sortingOrder = 10;

        private RagdollRig _rig;
        private Segment _torso, _head;
        private readonly List<Segment> _limbs = new List<Segment>(8);
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
            ApplyFill(color);
        }

        public void Flash(Color tint, float duration = 0.08f)
        {
            StopAllCoroutines();
            StartCoroutine(FlashRoutine(tint, duration));
        }

        private IEnumerator FlashRoutine(Color tint, float duration)
        {
            Color original = color;
            ApplyFill(tint);
            yield return new WaitForSeconds(duration);
            ApplyFill(original);
        }

        private void ApplyFill(Color c)
        {
            if (_torso == null) return;
            _torso.Fill.startColor = _torso.Fill.endColor = c;
            if (_head != null) _head.Fill.startColor = _head.Fill.endColor = c;
            foreach (var s in _limbs)
                if (s.Fill != null) s.Fill.startColor = s.Fill.endColor = c;
        }

        private void Build()
        {
            if (_torso != null) return;

            _fillMaterial = new Material(Shader.Find("Sprites/Default")) { name = "RagdollFill" };
            _outlineMaterial = new Material(Shader.Find("Sprites/Default")) { name = "RagdollOutline" };

            _torso = MakeSegment("Torso", 2);
            _head = MakeSegment("Head", HeadSegments + 1);
            for (int i = 0; i < 8; i++) _limbs.Add(MakeSegment($"Limb_{i}", 2));
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

            // Корпус.
            var torso = _rig.Torso;
            var capsule = torso.GetComponent<CapsuleCollider2D>();
            float halfHeight = capsule != null ? capsule.size.y * 0.5f : 0.4f;
            SetSegment(_torso, torso.position - (Vector2)torso.transform.up * halfHeight,
                torso.position + (Vector2)torso.transform.up * halfHeight);

            // Голова.
            if (_rig.Head != null)
            {
                var circle = _rig.Head.GetComponent<CircleCollider2D>();
                if (circle != null) _headRadius = circle.radius;
                DrawCircle(_head, _rig.Head.position, _headRadius);
            }

            // Восемь конечностей.
            DrawLimb(0, _rig.UpperArmL);
            DrawLimb(1, _rig.UpperArmR);
            DrawLimb(2, _rig.ForearmL);
            DrawLimb(3, _rig.ForearmR);
            DrawLimb(4, _rig.ThighL);
            DrawLimb(5, _rig.ThighR);
            DrawLimb(6, _rig.ShinL);
            DrawLimb(7, _rig.ShinR);
        }

        private void DrawLimb(int index, Rigidbody2D body)
        {
            if (index >= _limbs.Count) return;
            var s = _limbs[index];
            if (body == null) { s.Fill.enabled = s.Outline.enabled = false; return; }
            s.Fill.enabled = s.Outline.enabled = true;

            var box = body.GetComponent<BoxCollider2D>();
            float half = box != null ? box.size.y * 0.5f : 0.15f;
            Vector2 dir = body.transform.up;
            Vector2 center = body.position;
            SetSegment(s, center - dir * half, center + dir * half);
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
            for (int i = 0; i <= HeadSegments; i++)
            {
                float a = i / (float)HeadSegments * Mathf.PI * 2f;
                var p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                s.Fill.SetPosition(i, p);
                s.Outline.SetPosition(i, p);
            }
        }
    }
}
