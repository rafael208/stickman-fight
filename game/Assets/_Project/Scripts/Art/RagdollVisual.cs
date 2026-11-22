using UnityEngine;
using StickmanFight.Characters;

namespace StickmanFight.Art
{
    /// <summary>
    /// Рисует стикмена по физическим телам ragdoll (ADR-2: только линии, без спрайтов).
    /// Позиции берутся из тел в LateUpdate — визуал не влияет на физику.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class RagdollVisual : MonoBehaviour
    {
        [SerializeField] private float lineWidth = 0.085f;
        [SerializeField] private Color color = Color.cyan;
        [SerializeField] private int sortingOrder = 10;

        private RagdollRig _rig;
        private LineRenderer _torso, _head, _armL, _armR, _legL, _legR;
        private Material _material;
        private float _headRadius = 0.16f;
        private const int HeadSegments = 14;

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
            foreach (var lr in All()) lr.startColor = lr.endColor = color;
        }

        private void Build()
        {
            if (_torso != null) return;

            _material = new Material(Shader.Find("Sprites/Default")) { name = "RagdollLine" };
            _torso = MakeLine("TorsoLine", 2);
            _armL = MakeLine("ArmLLine", 2);
            _armR = MakeLine("ArmRLine", 2);
            _legL = MakeLine("LegLLine", 2);
            _legR = MakeLine("LegRLine", 2);
            _head = MakeLine("HeadLine", HeadSegments + 1);
        }

        private LineRenderer MakeLine(string lineName, int points)
        {
            var go = new GameObject(lineName);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.material = _material;
            lr.useWorldSpace = true;
            lr.widthMultiplier = lineWidth;
            lr.numCapVertices = 3;
            lr.numCornerVertices = 2;
            lr.startColor = lr.endColor = color;
            lr.sortingOrder = sortingOrder;
            lr.positionCount = points;
            return lr;
        }

        private void LateUpdate()
        {
            if (_rig == null) _rig = GetComponentInParent<RagdollRig>();
            if (_rig == null || _rig.Torso == null) return;
            if (_torso == null) Build();

            var torso = _rig.Torso;

            // Корпус: от бёдер к шее вдоль оси тела (длина берётся из капсулы)
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

            SetLimb(_armL, _rig.ArmL);
            SetLimb(_armR, _rig.ArmR);
            SetLimb(_legL, _rig.LegL);
            SetLimb(_legR, _rig.LegR);
        }

        private static void SetLimb(LineRenderer lr, Rigidbody2D body)
        {
            if (body == null || lr == null) return;
            var box = body.GetComponent<BoxCollider2D>();
            float half = box != null ? box.size.y * 0.5f : 0.25f;
            Vector2 dir = body.transform.up;
            Vector2 center = body.position;
            lr.SetPosition(0, center - dir * half);
            lr.SetPosition(1, center + dir * half);
        }

        private static void SetSegment(LineRenderer lr, Vector2 from, Vector2 to)
        {
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
        }

        private void DrawCircle(LineRenderer lr, Vector2 center, float radius)
        {
            if (lr == null) return;
            for (int i = 0; i <= HeadSegments; i++)
            {
                float a = i / (float)HeadSegments * Mathf.PI * 2f;
                lr.SetPosition(i, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
            }
        }

        private LineRenderer[] All() => new[] { _torso, _head, _armL, _armR, _legL, _legR };
    }
}
