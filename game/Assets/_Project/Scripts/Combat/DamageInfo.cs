using UnityEngine;

namespace StickmanFight.Combat
{
    public enum DamageSource
    {
        Melee,
        Projectile,
        Fall,
        Environment
    }

    /// <summary>Единое описание урона (ADR-6). Никто, кроме DamageSystem, не меняет HP.</summary>
    public struct DamageInfo
    {
        public float Amount;
        public Vector2 Impulse;
        public Vector2 Point;
        public GameObject Instigator;
        public DamageSource Source;
        public int SourceId;

        public static DamageInfo Melee(float amount, Vector2 impulse, Vector2 point, GameObject instigator, int sourceId)
        {
            return new DamageInfo
            {
                Amount = amount,
                Impulse = impulse,
                Point = point,
                Instigator = instigator,
                Source = DamageSource.Melee,
                SourceId = sourceId
            };
        }

        public static DamageInfo Projectile(float amount, Vector2 impulse, Vector2 point, GameObject instigator, int sourceId)
        {
            return new DamageInfo
            {
                Amount = amount,
                Impulse = impulse,
                Point = point,
                Instigator = instigator,
                Source = DamageSource.Projectile,
                SourceId = sourceId
            };
        }
    }
}
