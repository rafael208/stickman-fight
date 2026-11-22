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
            const int bumps = 6;

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float best = 0f;
                for (int i = 0; i < bumps; i++)
                {
                    float t = i / (float)(bumps - 1);
                    float cx = w * Mathf.Lerp(0.14f, 0.86f, t);
                    float cy = h * (0.50f + 0.16f * Mathf.Sin(i * 1.9f + seed * 0.7f) + (i == 1 || i == bumps - 2 ? 0.10f : 0f));
                    float r = w * (0.12f + 0.045f * ((i * 37 + seed) % 4) / 3f);
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) / r;
                    if (d < 1.02f) best = Mathf.Max(best, Mathf.Clamp01((1f - d) * 10f));
                }

                // Плоский низ облака.
                if (y < h * 0.30f) best = 0f;
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
                var c = ty > 0.80f ? light : body;
                if (ty < 0.18f) c = Color.Lerp(body, Color.black, 0.30f);
                // Швы между досками: тёмные, а не белые — так спокойнее и ближе к оригиналу.
                if (ty > 0.20f && ty < 0.80f && x % 34 < 2) c = Color.Lerp(body, Color.black, 0.45f);
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

        /// <summary>Капсула (конечность) с вшитой обводкой: толстая скруглённая палочка, как у оригинала.
        /// radiusFraction = 1 — полная капсула (круглые торцы), меньше — скруглённый прямоугольник (торс).</summary>
        public static Sprite CapsuleSprite(float widthUnits, float heightUnits, Color fill, Color outline, int outlinePx = 3, float radiusFraction = 1f)
        {
            int w = Mathf.Max(6, Mathf.RoundToInt(widthUnits * PixelsPerUnit));
            int h = Mathf.Max(6, Mathf.RoundToInt(heightUnits * PixelsPerUnit));
            var tex = NewTex(w, h);

            float r = Mathf.Max(1f, Mathf.Min(w, h) * 0.5f * Mathf.Clamp01(radiusFraction));
            float halfW = w * 0.5f;
            float halfH = h * 0.5f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // SDF скруглённого прямоугольника.
                    float qx = Mathf.Abs(x + 0.5f - halfW) - (halfW - r);
                    float qy = Mathf.Abs(y + 0.5f - halfH) - (halfH - r);
                    float dx = Mathf.Max(qx, 0f), dy = Mathf.Max(qy, 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;

                    Color c;
                    if (d <= -outlinePx) c = fill;
                    else if (d <= 0f) c = outline;
                    else c = new Color(0, 0, 0, 0);
                    tex.SetPixel(x, y, c);
                }
            }
            return Finish(tex);
        }

        /// <summary>Круг (голова) с вшитой обводкой.</summary>
        public static Sprite DiscSprite(float diameterUnits, Color fill, Color outline, int outlinePx = 3)
        {
            int d = Mathf.Max(8, Mathf.RoundToInt(diameterUnits * PixelsPerUnit));
            var tex = NewTex(d, d);
            float r = d * 0.5f;

            for (int y = 0; y < d; y++)
            {
                for (int x = 0; x < d; x++)
                {
                    float px = x + 0.5f - r;
                    float py = y + 0.5f - r;
                    float dist = Mathf.Sqrt(px * px + py * py);

                    Color c;
                    if (dist <= r - outlinePx) c = fill;
                    else if (dist <= r) c = outline;
                    else c = new Color(0, 0, 0, 0);
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
