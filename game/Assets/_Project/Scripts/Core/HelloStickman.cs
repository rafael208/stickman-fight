using UnityEngine;
using UnityEngine.UI;

namespace StickmanFight.Core
{
    /// <summary>
    /// Экран-заглушка сборки S0: показывает версию, FPS, разрешение и считает нажатия.
    /// Задача — доказать, что APK запускается на устройстве, тач и рендер работают.
    /// </summary>
    public class HelloStickman : MonoBehaviour
    {
        public Text infoText;
        public Text counterText;
        public Text touchText;

        private int _taps;
        private float _accum;
        private int _frames;

        private void Start()
        {
            UpdateCounter();
            Debug.Log($"[Hello] {BuildInfo.ProductName} v{BuildInfo.FullVersion} | {SystemInfo.deviceModel} | {SystemInfo.operatingSystem} | {Screen.width}x{Screen.height}");
        }

        private void Update()
        {
            _frames++;
            _accum += Time.unscaledDeltaTime;

            if (_accum < 0.5f) return;

            float fps = _frames / _accum;
            _frames = 0;
            _accum = 0f;

            if (infoText != null)
                infoText.text =
                    $"v{BuildInfo.FullVersion}\n" +
                    $"{fps:0} FPS\n" +
                    $"{Screen.width}x{Screen.height}\n" +
                    $"safe: {(int)Screen.safeArea.width}x{(int)Screen.safeArea.height}";

            if (touchText != null)
                touchText.text = $"TOUCH: {Input.touchCount}";
        }

        /// <summary>Вызывается из onClick кнопки.</summary>
        public void OnTap()
        {
            _taps++;
            UpdateCounter();
        }

        private void UpdateCounter()
        {
            if (counterText != null) counterText.text = $"НАЖАТИЙ: {_taps}";
        }
    }
}
