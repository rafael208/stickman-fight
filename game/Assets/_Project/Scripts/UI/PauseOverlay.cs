using UnityEngine;
using UnityEngine.UI;

namespace StickmanFight.UI
{
    /// <summary>
    /// Кнопка паузы в левом верхнем углу и оверлей паузы (FR-CORE-01/02).
    /// Работает и от системной кнопки Back — через метод Toggle.
    /// </summary>
    public class PauseOverlay : MonoBehaviour
    {
        public GameObject panel;
        public Button pauseButton;
        public Text hint;

        public bool IsPaused { get; private set; }

        private float _savedTimeScale = 1f;

        private void Start()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void Update()
        {
            // Системная кнопка «Назад» на Android (и Esc в редакторе).
            if (Input.GetKeyDown(KeyCode.Escape))
                Toggle();
        }

        public void Toggle()
        {
            if (IsPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (IsPaused) return;
            IsPaused = true;
            _savedTimeScale = Time.timeScale > 0.01f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            if (panel != null) panel.SetActive(true);
            if (pauseButton != null) pauseButton.gameObject.SetActive(false);
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = _savedTimeScale;
            if (panel != null) panel.SetActive(false);
            if (pauseButton != null) pauseButton.gameObject.SetActive(true);
        }

        /// <summary>Вешается на кнопку «Продолжить» в панели паузы.</summary>
        public void OnResumeClicked() => Resume();

        /// <summary>Вешается на кнопку «Заново».</summary>
        public void OnRestartClicked()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
