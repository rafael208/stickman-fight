using System.Collections.Generic;
using StickmanFight.Characters;
using UnityEngine;

namespace StickmanFight.Combat
{
    /// <summary>
    /// Единственная точка нанесения урона (ADR-6).
    /// Кулдаун попадания по паре (источник → цель) защищает от мультиудара:
    /// у персонажа 6 физических тел и один удар иначе засчитывался бы несколько раз.
    /// </summary>
    public static class DamageSystem
    {
        public const float HitCooldown = 0.25f;

        private struct HitKey
        {
            public int InstigatorId;
            public int TargetId;
            public int SourceId;

            public bool Equals(HitKey other) =>
                InstigatorId == other.InstigatorId && TargetId == other.TargetId && SourceId == other.SourceId;
        }

        private static readonly Dictionary<HitKey, float> LastHitTimes = new Dictionary<HitKey, float>();

        /// <summary>Событие реального попадания (для эффектов/звука). Вызывается после применения урона.</summary>
        public static event System.Action<StickmanCharacter, DamageInfo> HitApplied;

        public static bool Apply(StickmanCharacter target, in DamageInfo info)
        {
            if (target == null || target.IsDead) return false;

            int instigatorId = info.Instigator != null ? info.Instigator.GetInstanceID() : 0;
            var key = new HitKey
            {
                InstigatorId = instigatorId,
                TargetId = target.GetInstanceID(),
                SourceId = info.SourceId
            };

            float now = Time.time;
            if (LastHitTimes.TryGetValue(key, out float last) && now - last < HitCooldown)
                return false;

            LastHitTimes[key] = now;
            target.ApplyDamage(info.Amount, info.Impulse, info.Point);

            if (info.Source == DamageSource.Melee || info.Source == DamageSource.Projectile)
                Hitstop.Play(info.Amount);

            HitApplied?.Invoke(target, info);

            return true;
        }

        /// <summary>Нужно вызывать при старте игры, чтобы словарь не рос между раундами.</summary>
        public static void Reset()
        {
            LastHitTimes.Clear();
        }
    }
}
