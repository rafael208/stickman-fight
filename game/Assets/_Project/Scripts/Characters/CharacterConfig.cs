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
        [Tooltip("Голень (от стопы до колена)")]
        public float shinLength = 0.36f;
        [Tooltip("Бедро (от колена до бедра)")]
        public float thighLength = 0.42f;
        [Tooltip("Плечо (верхняя часть руки)")]
        public float upperArmLength = 0.30f;
        [Tooltip("Предплечье")]
        public float forearmLength = 0.30f;
        public float headRadius = 0.16f;
        public float legHeight = 0.7f;
        public float armLength = 0.62f;

        [Header("Массы, кг")]
        public float torsoMass = 20f;
        public float headMass = 7f;
        public float upperArmMass = 5f;
        public float forearmMass = 3.6f;
        public float thighMass = 9f;
        public float shinMass = 6f;
        public float armMass = 4f;
        public float legMass = 8f;

        [Header("Движение")]
        public float moveSpeed = 6.5f;
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

        [Header("PD-стабилизация (ADR-1, переработано в S7)")]
        [Tooltip("Скорость доворота корпуса, °/с — оставлено для плавного подъёма")]
        public float rightingRate = 720f;
        [Tooltip("Максимальная угловая скорость корпуса, рад/с")]
        public float maxAngularVelocity = 20f;

        [Tooltip("Усиление баланса: момент за угол корпуса, Н·м/°")]
        public float torsoAngleKp = 40f;
        [Tooltip("Горизонтальная корректирующая сила над стопами, 1/с² (0 = выкл)")]
        public float torsoPosKp = 0f;
        [Tooltip("Демпфирование угловой скорости корпуса, Н·м/(°/с)")]
        public float torsoKd = 35f;
        [Tooltip("Предел момента баланса корпуса, Н·м")]
        public float torsoMaxTorque = 2500f;

        [Tooltip("Ослабление моторов при ударе (0..1), затем плавное восстановление")]
        [Range(0.05f, 1f)] public float hitMotorScale = 0.25f;
        [Tooltip("Время плавного восстановления моторов после удара, с")]
        public float recoverRampTime = 0.35f;

        [Header("PD-коэффициенты суставов (Н·м/° и Н·м/(°/с))")]
        public float neckKp = 20f;  public float neckKd = 10f;  public float neckMax = 80f;
        public float shoulderKp = 30f; public float shoulderKd = 15f; public float shoulderMax = 150f;
        public float elbowKp = 30f; public float elbowKd = 15f; public float elbowMax = 120f;
        public float hipKp = 120f; public float hipKd = 40f; public float hipMax = 800f;
        public float kneeKp = 120f; public float kneeKd = 40f; public float kneeMax = 800f;

        [Header("Лимиты углов суставов, °")]
        public float neckLimit = 30f;
        public float shoulderLimit = 120f;
        public float elbowLimit = 140f;
        public float hipLimit = 90f;
        public float kneeLimit = 130f;
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
        public float friction = 0.35f;
    }
}
