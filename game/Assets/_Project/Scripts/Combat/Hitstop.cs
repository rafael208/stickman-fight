using System.Collections;
using UnityEngine;

namespace StickmanFight.Combat
{
    /// <summary>
    /// Микро-пауза времени на попадании (FR-COMBAT-06): 40–70 мс, из-за неё удар «чувствуется».
    /// Работает на unscaled-времени, иначе сам себя заморозит.
    /// </summary>
    public class Hitstop : MonoBehaviour
    {
        private static Hitstop _instance;
        private Coroutine _running;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExists()
        {
            if (_instance != null) return;
            var go = new GameObject("[Hitstop]");
            _instance = go.AddComponent<Hitstop>();
            Object.DontDestroyOnLoad(go);
        }

        public static void Play(float damage)
        {
            if (_instance == null) EnsureExists();
            if (_instance == null) return;
            _instance.Run(damage);
        }

        private void Run(float damage)
        {
            if (_running != null) StopCoroutine(_running);
            float duration = Mathf.Clamp(0.04f + damage * 0.001f, 0.04f, 0.07f);
            _running = StartCoroutine(Routine(duration));
        }

        private IEnumerator Routine(float duration)
        {
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            _running = null;
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
        }
    }
}
