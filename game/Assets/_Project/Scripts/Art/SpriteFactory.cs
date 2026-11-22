using UnityEngine;

namespace StickmanFight.Art
{
    /// <summary>
    /// Процедурные спрайты в стиле трейлера (ADR-2, без внешних ассетов):
    /// градиентное небо, мягкие облака, красные балки, кирпич, вода с барашками.
    /// Текстуры генерируются в рантайме ровно под нужный размер в пикселях (100 px = 1 м),
    /// поэтому рисунок не растягивается.
    /// </summary>
    public static class SpriteFactory
    {
        public const float PixelsPerUnit = 100f;

        /// <summary>Однотонный спрайт ровно 1×1 мировых единиц — удобно масштабировать как «квадрат».</summary>
        public static Sprite Solid(Color color)
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = color;
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        /// <summary>Вертикальный градиент — небо.</summary>
        public static Sprite Gradient(int w, int h, Color top, Color bottom)
        {
            var tex = NewTex(w, h);
            for (int y = 0; y < h; y++)
            {
                float t = h <= 1 ? 0f : y / (float)(h - 1);
                var c = Color.Lerp(bottom, top, t);
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
            }
            return Finish(tex);
        }

        /// <summary>Облако: три крупных перекрывающихся круга и плоское основание.</summary>
        public static Sprite Cloud(int w, int h, Color color, int seed)
        {
            var tex = NewTex(w, h);
            var rnd = new System.Random(seed);
            float jitter = (float)rnd.NextDouble() * 0.06f;

            var centers = new[]
            {
                new Vector2(w * (0.28f + jitter), h * 0.52f),
                new Vector2(w * 0.50f, h * (0.64f + jitter)),
                new Vector2(w * (0.72f - jitter), h * 0.52f)
            };
            var radii = new[] { w * 0.21f, w * 0.24f, w * 0.21f };

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float best = 0f;
                for (int i = 0; i < centers.Length; i++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), centers[i]) / radii[i];
                    if (d < 1f) best = Mathf.Max(best, Mathf.SmoothStep(0f, 1f, 1f - d));
                }
                // Плоское основание облака.
                if (y < h * 0.34f) best = 0f;
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * best));
            }
            return Finish(tex);
        }

        /// <summary>Балка-мост: тёмная основа, светлая верхняя грань, белые засечки.</summary>
        public static Sprite Plank(int w, int h, Color body, Color light, Color mark)
        {
            var tex = NewTex(w, h);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float ty = y / (float)Mathf.Max(1, h - 1);
                var c = ty > 0.78f ? light : body;
                if (ty < 0.16f) c = Color.Lerp(body, Color.black, 0.25f);
                // засечки каждые 30 px
                if (ty > 0.45f && ty < 0.62f && x % 30 < 4) c = mark;
                tex.SetPixel(x, y, c);
            }
            return Finish(tex);
        }

        /// <summary>Кирпичная кладка с раствором.</summary>
        public static Sprite BrickWall(int w, int h, Color brick, Color mortar, int rows, int cols)
        {
            var tex = NewTex(w, h);
            int bw = Mathf.Max(2, w / cols);
            int bh = Mathf.Max(2, h / rows);

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int row = y / bh;
                int offset = (row % 2 == 0) ? 0 : bw / 2;
                int col = ((x + offset) % bw);
                bool isMortar = (y % bh) < 2 || col < 2;
                var c = isMortar ? mortar : Color.Lerp(brick, Color.white, ((x * 7 + y * 13) % 17) / 90f);
                tex.SetPixel(x, y, c);
            }
            return Finish(tex);
        }

        /// <summary>Полоса воды: сверху барашки по синусоиде, ниже — основной цвет.</summary>
        public static Sprite WaterBand(int w, int h, Color water, Color crest)
        {
            var tex = NewTex(w, h);
            for (int x = 0; x < w; x++)
            {
                float wave = Mathf.Sin(x * 0.055f) * 0.5f + Mathf.Sin(x * 0.021f + 1.7f) * 0.5f;
                int crestHeight = Mathf.RoundToInt(h * (0.10f + 0.04f * wave));
                for (int y = 0; y < h; y++)
                {
                    var c = y >= h - crestHeight ? crest : water;
                    // лёгкие блики
                    if (y < h - crestHeight && ((x + y * 3) % 97 == 0)) c = Color.Lerp(water, crest, 0.35f);
                    tex.SetPixel(x, y, c);
                }
            }
            return Finish(tex);
        }

        /// <summary>Холм: сплошная заливка с бугристым верхним краем (для дальнего плана).</summary>
        public static Sprite Hill(int w, int h, Color body, Color top, int seed)
        {
            var tex = NewTex(w, h);
            var rnd = new System.Random(seed);
            float phase1 = (float)rnd.NextDouble() * 6.28f;
            float phase2 = (float)rnd.NextDouble() * 6.28f;

            for (int x = 0; x < w; x++)
            {
                float t = x / (float)Mathf.Max(1, w - 1);
                float bumps = Mathf.Sin(t * 5.2f + phase1) * 0.16f + Mathf.Sin(t * 11.7f + phase2) * 0.07f;
                int surface = Mathf.RoundToInt(h * (0.45f + bumps));
                surface = Mathf.Clamp(surface, 2, h - 1);

                for (int y = 0; y < h; y++)
                {
                    bool filled = y < surface;
                    var c = filled ? (y > surface - 4 ? top : body) : new Color(0, 0, 0, 0);
                    tex.SetPixel(x, y, c);
                }
            }
            return Finish(tex);
        }

        private static Texture2D NewTex(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            return tex;
        }

        private static Sprite Finish(Texture2D tex)
        {
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }
    }
}
