using UnityEngine;

namespace StickmanFight
{
    /// <summary>
    /// Применяет настройки физики и матрицу столкновений при старте игры.
    /// Матрица живёт в коде (ADR-6): её видно в git-diff, в отличие от бинарного ProjectSettings.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            // Фиксированный шаг физики (NFR-PERF-08) и «тяжёлая» гравитация для быстрого ощущения.
            Time.fixedDeltaTime = 1f / 60f;
            Physics2D.gravity = new Vector2(0f, -22f);

            ApplyCollisionMatrix();

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Debug.Log($"[Boot] {BuildInfo.ProductName} {BuildInfo.FullVersion} на {Application.platform}");
        }

        /// <summary>Правила из ТЗ, раздел 7.5.</summary>
        private static void ApplyCollisionMatrix()
        {
            int[] all =
            {
                GameLayers.Ground, GameLayers.PlayerBody, GameLayers.EnemyBody, GameLayers.Limb,
                GameLayers.Weapon, GameLayers.Projectile, GameLayers.Props, GameLayers.Grab
            };

            // По умолчанию всё сталкивается со всем, кроме явно запрещённых пар.
            foreach (int a in all)
            foreach (int b in all)
                Physics2D.IgnoreLayerCollision(a, b, false);

            // Конечности не сталкиваются ни с чьим телом: кто свой, кто чужой — решает IgnoreCollision на префабе.
            Physics2D.IgnoreLayerCollision(GameLayers.Limb, GameLayers.PlayerBody, true);
            Physics2D.IgnoreLayerCollision(GameLayers.Limb, GameLayers.EnemyBody, true);
            Physics2D.IgnoreLayerCollision(GameLayers.Limb, GameLayers.Limb, true);

            // Снаряды не мешают друг другу и не толкают оружие.
            Physics2D.IgnoreLayerCollision(GameLayers.Projectile, GameLayers.Projectile, true);
            Physics2D.IgnoreLayerCollision(GameLayers.Projectile, GameLayers.Weapon, true);

            // Слой захвата в песочнице — служебный, без коллайдеров.
            Physics2D.IgnoreLayerCollision(GameLayers.Grab, GameLayers.Grab, true);

            // Свой игрок / свой противник — раздельные тела, но они должны сталкиваться друг с другом.
            Physics2D.IgnoreLayerCollision(GameLayers.PlayerBody, GameLayers.PlayerBody, false);
            Physics2D.IgnoreLayerCollision(GameLayers.EnemyBody, GameLayers.EnemyBody, false);
        }
    }
}
