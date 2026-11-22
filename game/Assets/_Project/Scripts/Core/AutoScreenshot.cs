using System.Collections;
using System.IO;
using UnityEngine;

namespace StickmanFight.Core
{
    /// <summary>
    /// Автоскриншот для ПК-проверок: игра сама делает снимок и выходит.
    /// Запуск:
    ///   StickmanFight.exe -shot C:\path\shot.png -shotDelay 3 -logFile C:\path\player.log
    /// Без ключа -shot компонент ничего не делает.
    /// </summary>
    public class AutoScreenshot : MonoBehaviour
    {
        private string _path;
        private float _delay = 2f;
        private bool _quitAfter = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExists()
        {
            if (FindFirstObjectByType<AutoScreenshot>() != null) return;
            var go = new GameObject("AutoScreenshot");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<AutoScreenshot>();
        }

        private void Awake()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-shot" && i + 1 < args.Length) _path = args[i + 1];
                else if (args[i] == "-shotDelay" && i + 1 < args.Length)
                    float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out _delay);
                else if (args[i] == "-shotNoQuit") _quitAfter = false;
            }
        }

        private IEnumerator Start()
        {
            if (string.IsNullOrEmpty(_path)) yield break;

            yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, _delay));

            Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? ".");
            ScreenCapture.CaptureScreenshot(_path);
            Debug.Log($"[Shot] сохранён {_path}");

            // Ждём, пока файл реально запишется, и только потом закрываемся.
            float t = 0f;
            while (t < 1.5f && !File.Exists(_path))
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_quitAfter) Application.Quit();
        }
    }
}
