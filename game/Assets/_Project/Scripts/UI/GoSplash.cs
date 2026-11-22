using System.Collections;
using StickmanFight.Characters;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanFight.UI
{
    /// <summary>
    /// Стартовый отсчёт «3 – 2 – 1 – GO!» как в трейлере.
    /// На время отсчёта физика заморожена, чтобы никто не начал драться раньше времени.
    /// </summary>
    public class GoSplash : MonoBehaviour
    {
        public Text label;
        public float stepDuration = 0.65f;
        public bool freezeGameplay = true;

        public bool Finished { get; private set; }

        private IEnumerator Start()
        {
            Finished = false;

            if (freezeGameplay)
            {
                Time.timeScale = 0f;
                Debug.Log("[Splash] старт отсчёта, timeScale=0");
            }

            foreach (string symbol in new[] { "3", "2", "1" })
            {
                yield return Pop(symbol);
            }

            yield return Pop("GO!");
            yield return new WaitForSecondsRealtime(0.35f);

            if (label != null) label.gameObject.SetActive(false);

            if (freezeGameplay)
            {
                Time.timeScale = 1f;
                Debug.Log("[Splash] GO, timeScale=1");
            }

            Finished = true;
        }

        private IEnumerator Pop(string symbol)
        {
            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.text = symbol;

                // Пружинка: крупно -> нормально, на unscaled-времени (timeScale = 0).
                float t = 0f;
                while (t < 0.25f)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / 0.25f);
                    float scale = Mathf.Lerp(1.8f, 1f, k * k);
                    label.transform.localScale = new Vector3(scale, scale, 1f);
                    yield return null;
                }
            }

            yield return new WaitForSecondsRealtime(stepDuration);
        }
    }
}
