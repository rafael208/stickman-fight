using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanFight.UI
{
    /// <summary>
    /// Всплывающие цифры урона (как «65» в трейлере). Создаёт собственный оверлей-канвас
    /// и пул текстов; позиция пересчитывается из мира в экран.
    /// </summary>
    public class DamageNumbers : MonoBehaviour
    {
        private static DamageNumbers _instance;

        private Camera _camera;
        private Canvas _canvas;
        private readonly Queue<Text> _pool = new Queue<Text>();
        private readonly List<(Text text, Vector2 world, float age)> _active = new List<(Text, Vector2, float)>();

        private void Awake()
        {
            _instance = this;
            _camera = Camera.main;

            var go = new GameObject("DamageCanvas");
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            for (int i = 0; i < 12; i++)
            {
                var t = MakeText();
                t.gameObject.SetActive(false);
                _pool.Enqueue(t);
            }
        }

        private Text MakeText()
        {
            var go = new GameObject("Dmg");
            go.transform.SetParent(_canvas.transform, false);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = 42;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.color = Color.white;
            return t;
        }

        public static void Show(Vector2 worldPos, int value)
        {
            if (_instance == null) return;
            _instance.Spawn(worldPos, value.ToString());
        }

        private void Spawn(Vector2 worldPos, string value)
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            Text t;
            if (_pool.Count > 0) t = _pool.Dequeue();
            else t = MakeText();

            t.text = value;
            t.gameObject.SetActive(true);

            Vector3 screen = _camera.WorldToScreenPoint(new Vector3(worldPos.x, worldPos.y, 0f));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_canvas.transform, (Vector2)screen, _camera, out var local);
            t.rectTransform.anchoredPosition = local + new Vector2(0f, 26f);

            _active.Add((t, worldPos, 0f));
        }

        private void Update()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var item = _active[i];
                item.age += Time.deltaTime;
                _active[i] = item;

                var t = item.text;
                var pos = t.rectTransform.anchoredPosition;
                pos.y += Time.deltaTime * 55f;
                t.rectTransform.anchoredPosition = pos;

                var c = t.color;
                c.a = 1f - Mathf.Clamp01(item.age / 0.65f);
                t.color = c;

                if (item.age >= 0.65f)
                {
                    t.gameObject.SetActive(false);
                    _active.RemoveAt(i);
                    _pool.Enqueue(t);
                }
            }
        }
    }
}
