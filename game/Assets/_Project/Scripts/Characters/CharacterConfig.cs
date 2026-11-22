using UnityEngine;

namespace StickmanFight.Characters
{
    /// <summary>
    /// Все числовые параметры персонажа (раздел 8.1 ТЗ). ADR-4: баланс живёт здесь, а не в коде.
    /// Значения — стартовые; правятся в инспекторе или через Tools → Playground → Reset Configs.
    /// </summary>
    [CreateAssetMenu(menuName = "StickmanFight/Character Config", fileName = "Character_Default")]
    public class CharacterConfig : ScriptableObject
    {
        [Header("Здоровье")]
        [Tooltip("Максимальный запас здоровья")]
        public float maxHp = 100f;

        [Header("Пропорции, м")]
        public float torsoWidth = 0.42f;
        public float torsoHeight = 0.85f;
        public float legHeight = 0.7f;
        public float armLength = 0.62f;
        public float headRadius = 0.16f;

        [Header("Массы, кг")]
        public float torsoMass = 30f;
        public float headMass = 5f;
        public float armMass = 4f;
        public float legMass = 8f;

        [Header("Движение")]
        public float moveSpeed = 4.5f;
        [Tooltip("Ускорение разгона к целевой скорости, м/с²")]
        public float acceleration = 30f;
        [Tooltip("Доля управления в воздухе, 0..1")]
        [Range(0f, 1f)] public float airControl = 0.35f;
        [Tooltip("Импульс прыжка, м/с (при g=22 даёт высоту ≈1.4 м)")]
        public float jumpSpeed = 7.85f;
        [Tooltip("Разрешение прыжка после схода с края, с")]
        public float coyoteTime = 0.1f;
        [Tooltip("Запас прыжка до приземления, с")]
        public float jumpBuffer = 0.12f;

        [Header("Стабилизация вертикали (ADR-1)")]
        [Tooltip("Скорость доворота корпуса к вертикали, градусы/с")]
        public float rightingRate = 720f;
        [Tooltip("Максимальная угловая скорость корпуса, рад/с")]
        public float maxAngularVelocity = 20f;

        [Header("Конечности")]
        [Tooltip("Момент удержания позы моторами суставов, Н·м")]
        public float limbMotorForce = 2500f;
        public float armLimit = 160f;
        public float legLimit = 90f;

        [Header("Нокдаун и стан")]
        [Tooltip("Импульс на корпус, после которого персонаж падает, Н·с")]
        public float knockdownImpulse = 12f;
        [Tooltip("Урон за один хит, после которого персонаж падает")]
        public float knockdownDamage = 25f;
        public float getUpTime = 0.9f;
        public float invulnerableTime = 0.35f;
        public float stunTime = 0.35f;
        [Tooltip("На сколько приподнять корпус при подъёме, м")]
        public float getUpLiftHeight = 0.45f;
        [Tooltip("Сколько секунд после отрыва от земли персонаж ещё считается на земле, с")]
        public float groundGrace = 0.12f;

        [Header("Прочее")]
        public float deathDelay = 1.5f;
        public float headDamageMultiplier = 1.5f;
        [Tooltip("Трение поверхности персонажа")]
        public float friction = 0.4f;
    }
}
