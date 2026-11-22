using System.Collections.Generic;
using UnityEngine;

namespace StickmanFight.Art
{
    /// <summary>
    /// Рисует стикмена линиями (ADR-2: процедурная графика, ноль внешних ассетов).
    /// Это только визуал: физику делает отдельный ragdoll, фигура лишь повторяет позу.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class StickmanFigure : MonoBehaviour
    {
        [SerializeField] private float height = 1.8f;
        [SerializeField] private float lineWidth = 0.09f;
        [SerializeField] private Color color = Color.white;

        private LineRenderer _torso;
        private LineRenderer _armL, _armR, _legL, _legR, _head;

        private void Awake()
        {
            EnsureRenderers();
            ApplyPose();
        }

        private void OnValidate()
        {
            height = Mathf.Max(0.5f, height);
            lineWidth = Mathf.Max(0.02f, lineWidth);
            if (_torso != null) ApplyPose();
        }

        public void SetColor(Color c)
        {
            color = c;
            EnsureRenderers();
            foreach (var lr in All()) lr.startColor = lr.endColor = color;
        }

        public void SetVisible(bool visible)
        {
            EnsureRenderers();
            foreach (var lr in All()) lr.enabled = visible;
        }

        /// <summary>Поза «стоит» — базовая для стенда S0.</summary>
        public void ApplyPose()
        {
            float h = height;
            float hip = h * 0.45f;
            float shoulder = h * 0.80f;
            float headR = h * 0.11f;

            SetLine(_torso, new[] { new Vector3(0f, hip, 0f), new Vector3(0f, shoulder, 0f) });
            SetLine(_head, Circle(new Vector3(0f, shoulder + headR * 1.1f, 0f), headR, 14));

            SetLine(_armL, new[] { new Vector3(0f, shoulder - 0.02f, 0f), new Vector3(-h * 0.20f, hip + h * 0.10f, 0f) });
            SetLine(_armR, new[] { new Vector3(0f, shoulder - 0.02f, 0f), new Vector3(h * 0.20f, hip + h * 0.10f, 0f) });

            SetLine(_legL, new[] { new Vector3(0f, hip, 0f), new Vector3(-h * 0.12f, 0f, 0f) });
            SetLine(_legR, new[] { new Vector3(0f, hip, 0f), new Vector3(h * 0.12f, 0f, 0f) });
        }

        private IEnumerable<LineRenderer> All()
        {
            yield return _torso;
            yield return _head;
            yield return _armL;
            yield return _armR;
            yield return _legL;
            yield return _legR;
        }

        private void EnsureRenderers()
        {
            if (_torso != null) return;

            var shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader) { name = "StickmanLine" };

            _torso = MakeLine("Torso", mat);
            _head = MakeLine("Head", mat);
            _armL = MakeLine("ArmL", mat);
            _armR = MakeLine("ArmR", mat);
            _legL = MakeLine("LegL", mat);
            _legR = MakeLine("LegR", mat);
        }

        private LineRenderer MakeLine(string partName, Material mat)
        {
            var go = new GameObject(partName);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.material = mat;
            lr.useWorldSpace = false;
            lr.widthMultiplier = lineWidth;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 2;
            lr.startColor = lr.endColor = color;
            lr.sortingOrder = 10;
            lr.positionCount = 2;
            lr.textureMode = LineTextureMode.Tile;
            return lr;
        }

        private static void SetLine(LineRenderer lr, IReadOnlyList<Vector3> points)
        {
            lr.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++)
                lr.SetPosition(i, points[i]);
        }

        private static Vector3[] Circle(Vector3 center, float radius, int segments)
        {
            var pts = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                pts[i] = center + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
            }
            return pts;
        }
    }
}
