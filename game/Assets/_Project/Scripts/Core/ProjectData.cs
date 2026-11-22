// Единый источник версии, слоёв и палитры проекта.
// Правило проекта (NFR-CODE-01): числа и имена — здесь или в ScriptableObject, не в логике.
namespace StickmanFight
{
    /// <summary>Версия сборки. Обновляется вместе с тегом git.</summary>
    public static class BuildInfo
    {
        public const string Version = "0.1.0";
        public const int BuildNumber = 10;
        public const string ProductName = "Stickman Fight";
        public const string CompanyName = "StickmanFight";
        public const string ApplicationId = "com.stickmanfight.game";

        public static string FullVersion => $"{Version}.{BuildNumber}";
        public static string BuildStamp => $"{FullVersion} ({System.DateTime.Now:yyyy-MM-dd HH:mm})";
        public static string Label => $"v{FullVersion}";
    }

    /// <summary>
    /// Слои физики. Встроенные 0-7 заняты Unity (5 = UI), поэтому свои начинаются с 8.
    /// Матрица столкновений задаётся кодом в <see cref="GameBootstrap"/> — она в git, а не в бинарном ProjectSettings.
    /// </summary>
    public static class GameLayers
    {
        public const int Ground = 8;
        public const int PlayerBody = 9;
        public const int EnemyBody = 10;
        public const int Limb = 11;
        public const int Weapon = 12;
        public const int Projectile = 13;
        public const int Props = 14;
        public const int Grab = 15;

        public static readonly string[] Names =
        {
            "Ground", "PlayerBody", "EnemyBody", "Limb", "Weapon", "Projectile", "Props", "Grab"
        };
    }

    /// <summary>Палитра из ТЗ, раздел 10.2.</summary>
    public static class Palette
    {
        public const string BackgroundArena = "#10131A";
        public const string BackgroundSandbox = "#141A14";
        public const string Ground = "#2A3140";
        public const string Player = "#4FC3F7";
        public const string Enemy = "#EF5350";
        public const string Dummy = "#9E9E9E";
        public const string Weapon = "#E0E0E0";
        public const string Crate = "#C08A4A";
        public const string Metal = "#9AA5B1";
        public const string Ball = "#FFD54F";
        public const string Accent = "#FFB300";
        public const string Text = "#ECEFF1";
        public const string Danger = "#FF5252";
    }
}
