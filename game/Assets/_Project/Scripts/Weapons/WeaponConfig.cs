using UnityEngine;

namespace StickmanFight.Weapons
{
    public enum WeaponKind
    {
        Sword,
        Hammer,
        Pistol
    }

    /// <summary>Параметры оружия (раздел 8.2 ТЗ). Один ассет на тип, ADR-4.</summary>
    [CreateAssetMenu(menuName = "StickmanFight/Weapon Config", fileName = "Weapon_Sword")]
    public class WeaponConfig : ScriptableObject
    {
        public WeaponKind kind = WeaponKind.Sword;
        public string displayName = "Меч";

        [Header("Урон")]
        public float damage = 18f;
        [Tooltip("Опорная скорость для формулы урона, м/с")]
        public float refSpeed = 4f;
        public float impulse = 6f;

        [Header("Атака")]
        public float cooldown = 0.45f;
        [Tooltip("Длительность взмаха, с")]
        public float attackDuration = 0.28f;
        [Tooltip("Момент попадания внутри взмаха, с")]
        public float hitTime = 0.12f;
        [Tooltip("Дальность ближнего боя, м")]
        public float range = 0.95f;
        [Tooltip("Угол дуги, градусы")]
        public float arc = 100f;

        [Header("Пистолет")]
        public int ammo = 6;
        public float reloadTime = 1.4f;
        public float projectileSpeed = 22f;
        public float projectileLifetime = 2f;

        [Header("Прочее")]
        public float mass = 2f;
        public Vector2 visualSize = new Vector2(0.8f, 0.12f);
        public Color color = Color.white;
    }
}
