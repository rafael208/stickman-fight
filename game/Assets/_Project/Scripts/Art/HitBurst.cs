using UnityEngine;

namespace StickmanFight.Art
{
    /// <summary>
    /// Всплеск частиц на попадании: несколько мелких белых «искр» разлетаются и гаснут.
    /// Без внешних ассетов — спрайт-кружок генерируется процедурно.
    /// </summary>
    public class HitBurst : MonoBehaviour
    {
        private static Sprite _spark;
        private static Material _mat;

        private struct Spark
        {
            public Transform T;
            public SpriteRenderer R;
            public Vector2 Vel;
            public float Life;
        }

        private Spark[] _sparks;
        private float _maxLife = 0.32f;

        public static void Spawn(Vector2 pos, Color tint, int count = 8, float speed = 4.5f)
        {
            if (_spark == null)
            {
                _spark = SpriteFactory.DiscSprite(0.09f, Color.white, Color.white, 0);
                _mat = new Material(Shader.Find("Sprites/Default")) { name = "Spark" };
            }

            var go = new GameObject("HitBurst");
            go.transform.position = pos;
            var burst = go.AddComponent<HitBurst>();
            burst._sparks = new Spark[count];

            for (int i = 0; i < count; i++)
            {
                var sgo = new GameObject("S");
                sgo.transform.SetParent(go.transform, false);
                sgo.transform.position = pos;

                var sr = sgo.AddComponent<SpriteRenderer>();
                sr.sprite = _spark;
                sr.sharedMaterial = _mat;
                sr.color = tint;
                sr.sortingOrder = 30;

                float ang = Random.Range(0f, Mathf.PI * 2f);
                burst._sparks[i] = new Spark
                {
                    T = sgo.transform,
                    R = sr,
                    Vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(speed * 0.5f, speed),
                    Life = Random.Range(0.18f, 0.34f)
                };
            }

            Destroy(go, 0.5f);
        }

        private void Update()
        {
            if (_sparks == null) return;

            for (int i = 0; i < _sparks.Length; i++)
            {
                var s = _sparks[i];
                if (s.T == null) continue;

                s.Life -= Time.deltaTime;
                s.Vel += new Vector2(0f, -12f) * Time.deltaTime;
                s.T.position += (Vector3)(s.Vel * Time.deltaTime);

                var c = s.R.color;
                c.a = Mathf.Clamp01(s.Life / _maxLife);
                s.R.color = c;

                _sparks[i] = s;
            }
        }
    }
}
