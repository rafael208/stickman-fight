// Единый источник версии, слоёв и палитры проекта.
// Правило проекта (NFR-CODE-01): числа и имена — здесь или в ScriptableObject, не в логике.
namespace StickmanFight
{
    /// <summary>Версия сборки. Обновляется вместе с тегом git.</summary>
    public static class BuildInfo
    {
        public const string Version = "0.1.0";
        public const int BuildNumber = 23;
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

    /// <summary>Палитра, снятая с кадров трейлера (см. DEVLOG, арт-этап).</summary>
    public static class Palette
    {
        // Фон и сцена
        public const string SkyTop = "#8F8FE8";
        public const string SkyBottom = "#CFD3F2";
        public const string Cloud = "#E8F1FB";
        public const string Hill = "#5A8CD8";
        public const string HillTop = "#7FA9E8";
        public const string Water = "#2E9BC4";
        public const string WaterCrest = "#FFFFFF";
        public const string Brick = "#C9CDD4";
        public const string BrickMortar = "#8E9299";
        public const string Plank = "#B03A2E";
        public const string PlankLight = "#D1483A";
        public const string Grass = "#3FA34D";
        public const string Outline = "#FFFFFF";

        // Бойцы и предметы
        public const string Player = "#37C8E0";
        public const string Enemy = "#F2E14C";
        public const string Dummy = "#9E9E9E";
        public const string Weapon = "#E0E0E0";
        public const string Crate = "#C08A4A";
        public const string Metal = "#9AA5B1";
        public const string Ball = "#FFD54F";

        // Интерфейс
        public const string Accent = "#FFB300";
        public const string Text = "#FFFFFF";
        public const string Danger = "#E23B3B";

        // Совместимость со старыми именами
        public const string BackgroundArena = SkyTop;
        public const string BackgroundSandbox = Grass;
        public const string Ground = BrickMortar;
    }
}
