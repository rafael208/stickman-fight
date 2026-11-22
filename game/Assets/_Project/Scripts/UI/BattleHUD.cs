using StickmanFight.Characters;
using StickmanFight.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace StickmanFight.UI
{
    /// <summary>HUD боя (FR-UI-02/03): полосы здоровья, таймер-заглушка, текущее оружие и патроны.</summary>
    public class BattleHUD : MonoBehaviour
    {
        public StickmanCharacter player;
        public StickmanCharacter enemy;

        public Image playerFill;
        public Image enemyFill;
        public Text weaponText;
        public Text ammoText;

        private float _fpsAccum;
        private int _fpsFrames;
        private float _fps;

        private void Update()
        {
            if (player != null && playerFill != null)
            {
                float max = player.config != null ? player.config.maxHp : 100f;
                playerFill.fillAmount = Mathf.Clamp01(player.Hp / max);
            }

            if (enemy != null && enemyFill != null)
            {
                float max = enemy.config != null ? enemy.config.maxHp : 100f;
                enemyFill.fillAmount = Mathf.Clamp01(enemy.Hp / max);
            }

            var weapon = player != null && player.Combat != null ? player.Combat.Weapon : null;
            if (weaponText != null)
                weaponText.text = weapon != null ? weapon.config.displayName : "без оружия";

            if (ammoText != null)
            {
                if (weapon != null && weapon.IsPistol)
                    ammoText.text = weapon.IsReloading ? "перезарядка" : $"патроны: {weapon.Ammo:0}";
                else
                    ammoText.text = string.Empty;
            }

            _fpsAccum += Time.unscaledDeltaTime;
            _fpsFrames++;
            if (_fpsAccum >= 0.5f)
            {
                _fps = _fpsFrames / _fpsAccum;
                _fpsAccum = 0f;
                _fpsFrames = 0;
            }
        }
    }
}
