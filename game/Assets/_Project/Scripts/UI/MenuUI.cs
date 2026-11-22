using UnityEngine;

namespace StickmanFight.UI
{
    /// <summary>Главное меню: заголовок и кнопка «ИГРАТЬ».</summary>
    public class MenuUI : MonoBehaviour
    {
        private void Awake()
        {
            // Для авто-проверок на ПК: ключ -autoPlay сразу загружает арену.
            var args = System.Environment.GetCommandLineArgs();
            foreach (var a in args)
                if (a == "-autoPlay")
                {
                    OnPlay();
                    return;
                }
        }

        public void OnPlay()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("20_Battle_Arena");
        }

        public void OnQuit()
        {
            Application.Quit();
        }
    }
}
