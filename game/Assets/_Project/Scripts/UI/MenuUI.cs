using StickmanFight.Core;
using StickmanFight.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanFight.UI
{
    /// <summary>Главное меню: выбор оружия, кнопка «ИГРАТЬ».</summary>
    public class MenuUI : MonoBehaviour
    {
        [Header("Кнопки выбора оружия (в порядке Меч, Молот, Пистолет)")]
        public Button[] weaponButtons;
        public Text[] weaponLabels;

        private void Start()
        {
            for (int i = 0; i < weaponButtons.Length; i++)
            {
                int index = i;
                if (weaponButtons[i] != null)
                    weaponButtons[i].onClick.AddListener(() => SelectWeapon(index));
            }
            Refresh();
        }

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

        public void SelectWeapon(int index)
        {
            MatchSettings.PlayerWeapon = (WeaponKind)Mathf.Clamp(index, 0, 2);
            Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < weaponButtons.Length; i++)
            {
                if (weaponButtons[i] == null) continue;
                bool selected = (int)MatchSettings.PlayerWeapon == i;
                var img = weaponButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = selected
                        ? new Color(0.95f, 0.72f, 0.15f, 1f)
                        : new Color(0.16f, 0.22f, 0.36f, 0.9f);
                if (weaponLabels != null && i < weaponLabels.Length && weaponLabels[i] != null)
                    weaponLabels[i].color = selected ? new Color(0.10f, 0.12f, 0.18f) : Color.white;
            }
        }

        public void OnPlay()
        {
            MatchSettings.RandomizeEnemy();
            UnityEngine.SceneManagement.SceneManager.LoadScene("20_Battle_Arena");
        }

        public void OnQuit()
        {
            Application.Quit();
        }
    }
}
