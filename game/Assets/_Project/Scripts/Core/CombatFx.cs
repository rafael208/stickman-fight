using StickmanFight.Characters;
using StickmanFight.Combat;
using StickmanFight.Core;
using StickmanFight.UI;
using UnityEngine;

namespace StickmanFight.Core
{
    /// <summary>
    /// Слой «сока» на попаданиях: звук, вспышка цели, тряска камеры и цифры урона.
    /// Подписывается на событие DamageSystem.HitApplied — не знает, чем нанесён удар.
    /// </summary>
    public class CombatFx : MonoBehaviour
    {
        public CameraFollow cameraFollow;

        private void OnEnable()
        {
            DamageSystem.HitApplied += OnHit;
        }

        private void OnDisable()
        {
            DamageSystem.HitApplied -= OnHit;
        }

        private void OnHit(StickmanCharacter target, DamageInfo info)
        {
            SfxPlayer.Play("hit", 0.7f, Random.Range(0.9f, 1.15f));
            if (target != null && target.Visual != null) target.Visual.Flash(Color.white, 0.08f);
            if (cameraFollow != null) cameraFollow.Shake(0.14f, 0.22f);

            var pos = target != null && target.Rig != null ? target.Rig.Torso.position : Vector2.zero;
            StickmanFight.Art.HitBurst.Spawn(pos, Color.white, 8, 4.5f);
            DamageNumbers.Show(pos, Mathf.RoundToInt(info.Amount));
        }
    }
}
