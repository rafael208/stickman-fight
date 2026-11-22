using StickmanFight.Weapons;

namespace StickmanFight.Core
{
    /// <summary>
    /// Выбор на матч: каким оружием играет каждый боец. Задаётся в меню и действует
    /// весь бой — как в оригинале, где оружие выбирается заранее и не меняется.
    /// </summary>
    public static class MatchSettings
    {
        public static WeaponKind PlayerWeapon = WeaponKind.Sword;
        public static WeaponKind EnemyWeapon = WeaponKind.Hammer;

        public static void RandomizeEnemy()
        {
            EnemyWeapon = (WeaponKind)UnityEngine.Random.Range(0, 3);
        }

        public static string NameOf(WeaponKind kind)
        {
            switch (kind)
            {
                case WeaponKind.Sword: return "МЕЧ";
                case WeaponKind.Hammer: return "МОЛОТ";
                default: return "ПИСТОЛЕТ";
            }
        }
    }
}
