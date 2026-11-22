using UnityEngine;

namespace StickmanFight.Art
{
    /// <summary>
    /// Декорация арены в стиле трейлера: градиентное небо, облака, дальние холмы, вода.
    /// Всё процедурное (SDF-спрайты из SpriteFactory), физика сюда не входит.
    /// </summary>
    public class ArenaBackdrop : MonoBehaviour
    {
        [Header("Ссылки")]
        public Camera cam;

        [Header("Уровень")]
        public float waterLevel = -3.4f;
        public Vector2 levelBounds = new Vector2(-14f, 14f);

        [Header("Палитра")]
        public Color skyTop = new Color(0.56f, 0.56f, 0.91f);
        public Color skyBottom = new Color(0.80f, 0.82f, 0.96f);
        public Color cloudColor = new Color(0.91f, 0.95f, 0.99f, 0.95f);
        public Color hillColor = new Color(0.35f, 0.55f, 0.85f);
        public Color hillTop = new Color(0.55f, 0.72f, 0.95f);
        public Color waterColor = new Color(0.18f, 0.61f, 0.77f);
        public Color crestColor = new Color(1f, 1f, 1f, 0.95f);

        [Header("Облака")]
        public int cloudCount = 9;

        private void Awake()
        {
            Build();
        }

        public void Build()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            BuildSky();
            BuildClouds();
            BuildHills();
            BuildWater();
        }

        private void BuildSky()
        {
            // Трёхкратный запас: камера меняет orthographicSize при зуме на группу,
            // иначе по краям проступает цвет очистки экрана.
            float height = cam.orthographicSize * 2f * 3f;
            float width = height * cam.aspect;

            var go = new GameObject("Sky");
            go.transform.SetParent(cam.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 20f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Gradient(96, 96, skyTop, skyBottom);
            sr.sortingOrder = -100;
            sr.sprite.texture.wrapMode = TextureWrapMode.Clamp;

            float unitsW = sr.sprite.rect.width / SpriteFactory.PixelsPerUnit;
            float unitsH = sr.sprite.rect.height / SpriteFactory.PixelsPerUnit;
            go.transform.localScale = new Vector3(width / unitsW, height / unitsH, 1f);
        }

        private void BuildClouds()
        {
            var layer = new GameObject("Clouds");
            layer.transform.position = new Vector3(0f, 0f, 0f);
            var parallax = layer.AddComponent<ParallaxLayer>();
            parallax.factor = 0.12f;
            parallax.Bind(cam.transform);

            for (int i = 0; i < cloudCount; i++)
            {
                var go = new GameObject($"Cloud_{i}");
                go.transform.SetParent(layer.transform, false);

                float x = Mathf.Lerp(levelBounds.x - 6f, levelBounds.y + 6f, (i + 0.5f) / cloudCount);
                float y = 2.5f + (i % 3) * 1.3f + ((i * 37) % 11) * 0.12f;
                go.transform.position = new Vector3(x, y, 0f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.Cloud(320, 130, cloudColor, 1000 + i * 17);
                sr.sortingOrder = -90;

                float scale = 1.0f + ((i * 13) % 7) * 0.14f;
                go.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void BuildHills()
        {
            var layer = new GameObject("Hills");
            var parallax = layer.AddComponent<ParallaxLayer>();
            parallax.factor = 0.35f;
            parallax.Bind(cam.transform);

            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject($"Hill_{i}");
                go.transform.SetParent(layer.transform, false);

                float x = Mathf.Lerp(levelBounds.x - 8f, levelBounds.y + 8f, (i + 0.5f) / 4f);
                go.transform.position = new Vector3(x, waterLevel - 0.6f, 0f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.Hill(420, 240, hillColor, hillTop, 300 + i * 41);
                sr.sortingOrder = -80;
                sr.flipX = i % 2 == 0;

                go.transform.localScale = new Vector3(1.3f, 1.1f, 1f);
            }
        }

        private void BuildWater()
        {
            var go = new GameObject("Water");
            float width = (levelBounds.y - levelBounds.x) + 12f;
            go.transform.position = new Vector3((levelBounds.x + levelBounds.y) * 0.5f, waterLevel, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.WaterBand(Mathf.RoundToInt(width * SpriteFactory.PixelsPerUnit), 90, waterColor, crestColor);
            sr.sortingOrder = -20;

            // Вода чуть покачивается, чтобы сцена не выглядела мёртвой.
            var bob = go.AddComponent<WaterBob>();
            bob.amplitude = 0.06f;
            bob.speed = 0.9f;
        }
    }
}
