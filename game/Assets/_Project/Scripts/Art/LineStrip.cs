using UnityEngine;

namespace StickmanFight.Art
{
    /// <summary>
    /// Отрезок-линия, который создаёт свой материал в Awake.
    /// Так избегаем «материала без ассета», который не переживает сборку APK (ADR-2).
    /// </summary>
    public class LineStrip : MonoBehaviour
    {
        public Vector2 from = new Vector2(-9f, 0f);
        public Vector2 to = new Vector2(9f, 0f);
        public float width = 0.12f;
        public string colorHex = "#2A3140";
        public int sortingOrder;

        private void Awake()
        {
            var lr = gameObject.GetComponent<LineRenderer>();
            if (lr == null) lr = gameObject.AddComponent<LineRenderer>();

            var shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader) { name = "LineStrip" };

            lr.material = mat;
            lr.useWorldSpace = false;
            lr.widthMultiplier = width;
            lr.numCapVertices = 2;
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(from.x, from.y, 0f));
            lr.SetPosition(1, new Vector3(to.x, to.y, 0f));

            var color = ColorUtility.TryParseHtmlString(colorHex, out var c) ? c : Color.white;
            lr.startColor = lr.endColor = color;
            lr.sortingOrder = sortingOrder;
        }
    }
}
