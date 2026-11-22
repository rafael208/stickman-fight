using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StickmanFight.Characters;

namespace StickmanFight.Art
{
    /// <summary>
    /// Рисует стикмена спрайтами: под каждой частью — белая капсула-обводка (крупнее),
    /// сверху — цветная капсула-заливка. Как в оригинале (OutlineSprites + BodySprites):
    /// толстые скруглённые конечности с белой рамкой. Спрайты чуть перекрываются на стыках,
    /// чтобы не было щелей в суставах.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class RagdollVisual : MonoBehaviour
    {
        [SerializeField] private Color color = Color.cyan;
        [SerializeField] private int sortingOrder = 10;
        [Tooltip("Толщина белой обводки, м")]
        [SerializeField] private float outlineMargin = 0.07f;
        [Tooltip("Перекрытие частей на стыках, чтобы не было щелей")]
        [SerializeField] private float overlap = 1.0f;
        [Tooltip("Тоньше коллайдера — для тонкого силуэта стикмена")]
        [SerializeField] private float visualThinness = 0.72f;

        private RagdollRig _rig;
        private Material _mat;

        private sealed class Part
        {
            public Rigidbody2D Body;
            public SpriteRenderer Fill;
            public SpriteRenderer Outline;
        }

        private readonly List<Part> _parts = new List<Part>(10);

        public void Init(RagdollRig rig, Color tint, float width = -1f)
        {
            _rig = rig;
            color = tint;
            Build();
        }

        public void SetColor(Color tint)
        {
            color = tint;
            foreach (var p in _parts)
                if (p.Fill != null) p.Fill.color = color;
        }

        /// <summary>Белая вспышка на попадании, не меняет базовый цвет.</summary>
        public void Flash(Color tint, float duration = 0.08f)
        {
            StopAllCoroutines();
            StartCoroutine(FlashRoutine(tint, duration));
        }

        private IEnumerator FlashRoutine(Color tint, float duration)
        {
            foreach (var p in _parts)
                if (p.Fill != null) p.Fill.color = tint;
            yield return new WaitForSeconds(duration);
            foreach (var p in _parts)
                if (p.Fill != null) p.Fill.color = color;
        }

        private void Build()
        {
            if (_parts.Count > 0 || _rig == null) return;
            if (Shader.Find("Sprites/Default") == null) return;

            _mat = new Material(Shader.Find("Sprites/Default")) { name = "StickmanSprite" };

            AddPart(_rig.Torso, false);
            AddPart(_rig.Head, true);
            AddPart(_rig.UpperArmL, false);
            AddPart(_rig.UpperArmR, false);
            AddPart(_rig.ForearmL, false);
            AddPart(_rig.ForearmR, false);
            AddPart(_rig.ThighL, false);
            AddPart(_rig.ThighR, false);
            AddPart(_rig.ShinL, false);
            AddPart(_rig.ShinR, false);
        }

        private struct Size { public float W, H; }

        private Size GetSize(Rigidbody2D body)
        {
            var box = body.GetComponent<BoxCollider2D>();
            if (box != null) return new Size { W = box.size.x, H = box.size.y };
            var cap = body.GetComponent<CapsuleCollider2D>();
            if (cap != null) return new Size { W = cap.size.x, H = cap.size.y };
            var circle = body.GetComponent<CircleCollider2D>();
            if (circle != null) return new Size { W = circle.radius * 2f, H = circle.radius * 2f };
            return new Size { W = 0.15f, H = 0.3f };
        }

        private void AddPart(Rigidbody2D body, bool disc)
        {
            if (body == null) return;

            var s = GetSize(body);
            if (!disc) { s.W *= visualThinness; s.H *= Mathf.Lerp(1f, visualThinness, 0.35f); }
            float m = outlineMargin;
            float p = overlap;

            Sprite outlineSprite, fillSprite;
            if (disc)
            {
                outlineSprite = SpriteFactory.DiscSprite((s.W + m * 2f) * p, Color.white, Color.white, 0);
                fillSprite = SpriteFactory.DiscSprite(s.W * p, Color.white, Color.white, 0);
            }
            else
            {
                float rf = body == _rig.Torso ? 0.30f : 1f;
                outlineSprite = SpriteFactory.CapsuleSprite((s.W + m * 2f) * p, (s.H + m * 2f) * p, Color.white, Color.white, 0, rf);
                fillSprite = SpriteFactory.CapsuleSprite(s.W * p, s.H * p, Color.white, Color.white, 0, rf);
            }

            var root = new GameObject(body.name + "Sprite");
            root.transform.SetParent(transform, false);

            var outlineGo = new GameObject("Outline");
            outlineGo.transform.SetParent(root.transform, false);
            var outline = outlineGo.AddComponent<SpriteRenderer>();
            outline.sprite = outlineSprite;
            outline.sharedMaterial = _mat;
            outline.color = new Color(0.055f, 0.06f, 0.13f, 1f);
            outline.sortingOrder = sortingOrder - 1;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(root.transform, false);
            var fill = fillGo.AddComponent<SpriteRenderer>();
            fill.sprite = fillSprite;
            fill.sharedMaterial = _mat;
            fill.color = color;
            fill.sortingOrder = sortingOrder;

            _parts.Add(new Part { Body = body, Fill = fill, Outline = outline });
        }

        private void LateUpdate()
        {
            if (_rig == null || _rig.Torso == null) return;
            if (_parts.Count == 0) Build();

            // Зеркалим весь силуэт относительно корпуса, когда персонаж идёт влево.
            // Без этого тело «всегда смотрит вправо», сколько ни разворачивай суставы.
            float torsoX = _rig.Torso.position.x;
            int face = _rig.Facing;

            for (int i = 0; i < _parts.Count; i++)
            {
                var p = _parts[i];
                if (p.Body == null) continue;

                var t = p.Body.transform;
                var pos = t.position;
                var rot = t.rotation;

                if (face < 0)
                {
                    pos.x = 2f * torsoX - pos.x;
                    rot = Quaternion.Euler(0f, 0f, -t.eulerAngles.z);
                }

                p.Outline.transform.SetPositionAndRotation(pos, rot);
                p.Fill.transform.SetPositionAndRotation(pos, rot);
            }
        }
    }
}
