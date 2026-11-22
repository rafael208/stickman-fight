using StickmanFight.Art;
using StickmanFight.Core;
using UnityEngine;

namespace StickmanFight.Levels
{
    /// <summary>
    /// Визуал платформы, создаваемый в рантайме: спрайт балки или кирпичной кладки
    /// ровно под размер коллайдера. В редакторе текстуры не сериализуются, поэтому строим в Awake.
    /// </summary>
    public class PlatformVisual : MonoBehaviour
    {
        public enum Kind
        {
            Plank,
            Brick,
            Post
        }

        public Kind kind = Kind.Plank;
        public Vector2 size = new Vector2(4f, 0.5f);
        public int sortingOrder;

        private void Awake()
        {
            Build();
        }

        public void Build()
        {
            if (transform.Find("Visual") != null) return;

            var go = new GameObject("Visual");
            go.transform.SetParent(transform, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;

            int w = Mathf.Max(8, Mathf.RoundToInt(size.x * SpriteFactory.PixelsPerUnit));
            int h = Mathf.Max(8, Mathf.RoundToInt(size.y * SpriteFactory.PixelsPerUnit));

            switch (kind)
            {
                case Kind.Brick:
                    sr.sprite = SpriteFactory.BrickWall(w, h, Hex(Palette.Brick), Hex(Palette.BrickMortar), 2, 6);
                    break;
                case Kind.Post:
                    // Solid() — это спрайт ровно 1x1 единицу, поэтому его надо масштабировать.
                    sr.sprite = SpriteFactory.Solid(Hex(Palette.Plank));
                    go.transform.localScale = new Vector3(size.x, size.y, 1f);
                    break;
                default:
                    sr.sprite = SpriteFactory.Plank(
                        w, h,
                        Hex(Palette.Plank),
                        Hex(Palette.PlankLight),
                        Hex(Palette.WaterCrest));
                    break;
            }
        }

        private static Color Hex(string hex) =>
            ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
    }
}
