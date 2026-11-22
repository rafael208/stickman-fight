using StickmanFight.Art;
using StickmanFight.Characters;
using UnityEngine;

namespace StickmanFight.UI
{
    /// <summary>
    /// Полоска здоровья над головой бойца, как в трейлере: красная подложка и зелёная заливка.
    /// Живёт в мире, не вращается вместе с телом и всегда повёрнута к камере.
    /// </summary>
    public class HealthBarAbove : MonoBehaviour
    {
        public StickmanCharacter character;
        public float width = 0.58f;
        public float height = 0.075f;
        public Vector3 offset = new Vector3(0f, 0.26f, 0f);
        public Color fillColor = new Color(0.35f, 0.85f, 0.28f);
        public Color backColor = new Color(0.89f, 0.23f, 0.23f);

        private SpriteRenderer _back;
        private SpriteRenderer _fill;
        private Transform _barRoot;
        private Transform _anchor;
        private float _maxHp = 100f;

        private void Start()
        {
            if (character == null) character = GetComponentInParent<StickmanCharacter>();
            if (character == null) return;

            _maxHp = character.config != null ? character.config.maxHp : 100f;
            _anchor = character.Rig != null && character.Rig.Head != null
                ? character.Rig.Head.transform
                : transform;

            // Полоска живёт на собственном объекте: если двигать transform компонента,
            // а он висит на корне персонажа, уедет весь персонаж (проверено — улетал вверх).
            var barRootGo = new GameObject("HealthBarRoot");
            _barRoot = barRootGo.transform;
            _barRoot.SetParent(transform.parent != null ? transform.parent : null, false);

            _back = MakeBar("BarBack", backColor, -1);
            _fill = MakeBar("BarFill", fillColor, 0);
        }

        private SpriteRenderer MakeBar(string barName, Color color, int order)
        {
            var go = new GameObject(barName);
            go.transform.SetParent(_barRoot, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Solid(color);
            sr.sortingOrder = 40 + order;
            sr.color = color;
            return sr;
        }

        private void LateUpdate()
        {
            if (character == null || _back == null || _barRoot == null) return;

            bool hide = character.IsDead || character.Rig == null || character.Rig.Head == null;
            _back.enabled = !hide;
            _fill.enabled = !hide;
            if (hide) return;

            Vector3 head = character.Rig.Head.position;
            _barRoot.position = head + offset;
            _barRoot.rotation = Quaternion.identity;

            float ratio = Mathf.Clamp01(character.Hp / Mathf.Max(1f, _maxHp));

            _back.transform.localScale = new Vector3(width, height, 1f);
            _fill.transform.localScale = new Vector3(width * ratio, height * 0.72f, 1f);
            // Заливка растёт слева направо: сдвигаем её центр к левому краю.
            _fill.transform.localPosition = new Vector3(-width * 0.5f + width * ratio * 0.5f, 0f, -0.01f);
        }
    }
}
