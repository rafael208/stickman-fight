using System.Collections.Generic;
using StickmanFight.Art;
using StickmanFight.Characters;
using UnityEngine;

namespace StickmanFight.UI
{
    /// <summary>
    /// Значки жизней (stocks) над головой бойца, как в трейлере — ряд маленьких квадратов.
    /// Живёт на собственном объекте, чтобы не двигать корень персонажа (урок из HealthBarAbove).
    /// </summary>
    public class StockIndicator : MonoBehaviour
    {
        public StickmanCharacter character;
        public Vector3 offset = new Vector3(0f, 0.30f, 0f);
        public float square = 0.12f;
        public float spacing = 0.18f;
        public Color fillColor = Color.white;

        private readonly List<SpriteRenderer> _icons = new List<SpriteRenderer>();
        private Transform _root;
        private int _visible;

        private void Start()
        {
            if (character == null) character = GetComponentInParent<StickmanCharacter>();

            var go = new GameObject("StockRoot");
            _root = go.transform;
            _root.SetParent(transform.parent, false);
        }

        public void SetStocks(int count)
        {
            EnsureIcons(count);
            _visible = Mathf.Max(0, count);
        }

        private void EnsureIcons(int count)
        {
            while (_icons.Count < count)
            {
                var go = new GameObject($"Stock_{_icons.Count}");
                go.transform.SetParent(_root, false);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.Solid(fillColor);
                sr.color = fillColor;
                sr.sortingOrder = 45;
                sr.transform.localScale = new Vector3(square, square, 1f);
                _icons.Add(sr);
            }
        }

        private void LateUpdate()
        {
            if (_root == null || character == null || character.Rig == null || character.Rig.Head == null) return;

            bool hide = character.IsDead;
            _root.gameObject.SetActive(!hide);
            if (hide) return;

            Vector3 head = character.Rig.Head.position;
            _root.position = head + offset;
            _root.rotation = Quaternion.identity;

            float total = (_icons.Count - 1) * spacing;
            for (int i = 0; i < _icons.Count; i++)
            {
                bool on = i < _visible;
                _icons[i].enabled = on;
                float x = -total * 0.5f + i * spacing;
                _icons[i].transform.localPosition = new Vector3(x, 0f, 0f);
            }
        }
    }
}
