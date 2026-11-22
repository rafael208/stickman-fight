using System.Collections.Generic;
using UnityEngine;

namespace StickmanFight.Core
{
    /// <summary>
    /// Процедурные звуковые эффекты без внешних ассетов (ADR-2): синтезируются в AudioClip
    /// при первом использовании. Лицензионных рисков нет, файлов нет.
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        private static SfxPlayer _instance;
        private AudioSource _source;
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Ensure()
        {
            if (_instance != null) return;
            var go = new GameObject("[Sfx]");
            Object.DontDestroyOnLoad(go);
            _instance = go.AddComponent<SfxPlayer>();
        }

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.volume = 0.9f;
            _source.spatialBlend = 0f;
        }

        public static void Play(string name, float volume = 0.7f, float pitch = 1f)
        {
            if (!Application.isPlaying) return; // headless-стенд редактора — звука нет
            if (_instance == null) Ensure();
            if (_instance == null) return;
            _instance.PlayInternal(name, volume, pitch);
        }

        private void PlayInternal(string name, float volume, float pitch)
        {
            // Awake не вызывается в headless-стенде редактора — создаём источник лениво.
            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
                _source.playOnAwake = false;
                _source.volume = 0.9f;
                _source.spatialBlend = 0f;
            }

            if (!_clips.TryGetValue(name, out var clip))
            {
                clip = BuildClip(name);
                if (clip == null) return;
                _clips[name] = clip;
            }

            _source.pitch = pitch;
            _source.PlayOneShot(clip, volume);
        }

        private AudioClip BuildClip(string name)
        {
            const int rate = 44100;
            switch (name)
            {
                case "hit":
                    return Noise(rate, 0.09f, 0.5f, 500f);
                case "swing":
                    return Sweep(rate, 0.10f, 900f, 200f, 0.4f);
                case "shoot":
                    return Sweep(rate, 0.06f, 1400f, 300f, 0.5f);
                case "pickup":
                    return Sweep(rate, 0.10f, 500f, 1100f, 0.4f);
                case "jump":
                    return Sweep(rate, 0.09f, 300f, 700f, 0.35f);
                case "ko":
                    return Sweep(rate, 0.22f, 220f, 60f, 0.7f);
                default:
                    return null;
            }
        }

        private static AudioClip Noise(int rate, float duration, float decay, float filter)
        {
            int n = Mathf.RoundToInt(rate * duration);
            var data = new float[n];
            var rnd = new System.Random(12345);
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                // простой one-pole low-pass на шуме + экспоненциальное затухание
                float white = (float)(rnd.NextDouble() * 2.0 - 1.0);
                prev = prev + filter / rate * (white - prev);
                float t = i / (float)n;
                data[i] = prev * Mathf.Exp(-decay * 60f * t);
            }
            return MakeClip(rate, data);
        }

        private static AudioClip Sweep(int rate, float duration, float fromHz, float toHz, float decay)
        {
            int n = Mathf.RoundToInt(rate * duration);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float freq = Mathf.Lerp(fromHz, toHz, t);
                phase += 2f * Mathf.PI * freq / rate;
                data[i] = Mathf.Sin(phase) * Mathf.Exp(-decay * 30f * t);
            }
            return MakeClip(rate, data);
        }

        private static AudioClip MakeClip(int rate, float[] data)
        {
            var clip = AudioClip.Create("sfx", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
