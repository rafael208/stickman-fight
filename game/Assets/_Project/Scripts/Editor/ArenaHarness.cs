using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using StickmanFight.Characters;

namespace StickmanFight.EditorTools
{
    /// <summary>
    /// Диагностика арены без устройства и без сборки APK:
    /// воспроизводит геометрию «мост над водой» и печатает, что происходит с телами.
    /// </summary>
    public static class ArenaHarness
    {
        private const float Dt = 1f / 60f;

        [MenuItem("Tools/Playground/8. Harness: арена", priority = 8)]
        public static void RunArenaProbe()
        {
            var report = new StringBuilder();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Балка как в арене: центр (0,0), размер 16x0.5, слой Ground
            var bridge = new GameObject("Bridge") { layer = GameLayers.Ground };
            bridge.transform.position = Vector3.zero;
            var box = bridge.AddComponent<BoxCollider2D>();
            box.size = new Vector2(16f, 0.5f);

            report.AppendLine($"[arena] bridge layer={bridge.layer} bounds={box.bounds}");

            // Персонаж как в арене: корень на y=0.6
            var go = new GameObject("Player");
            go.transform.position = new Vector3(0f, 0.6f, 0f);
            var ch = go.AddComponent<StickmanCharacter>();
            ch.config = ConfigBuilder.EnsureConfig();
            ch.buildOnAwake = false;
            ch.Build();

            report.AppendLine($"[arena] torso start y={ch.Rig.Torso.position.y:0.00} " +
                              $"torsoLayer={ch.Rig.Torso.gameObject.layer} legLayer={ch.Rig.LegL.gameObject.layer}");

            // Матрица коллизий (то же, что делает GameBootstrap на устройстве)
            GameBootstrapProbe.ApplyMatrix();

            report.AppendLine($"[arena] ignore Ground(8) vs PlayerBody(9) = {Physics2D.GetIgnoreLayerCollision(GameLayers.Ground, GameLayers.PlayerBody)}");
            report.AppendLine($"[arena] ignore Ground(8) vs Limb(11) = {Physics2D.GetIgnoreLayerCollision(GameLayers.Ground, GameLayers.Limb)}");

            var hit = Physics2D.Raycast(new Vector2(0f, 1.2f), Vector2.down, 3f, 1 << GameLayers.Ground);
            report.AppendLine($"[arena] raycast вниз: {(hit.collider != null ? hit.collider.name : "ПУСТО")} y={hit.point.y:0.00}");

            Physics2D.simulationMode = SimulationMode2D.Script;

            for (int i = 0; i < 180; i++)
            {
                ch.Tick(Dt);
                Physics2D.Simulate(Dt);

                if (i % 20 == 0)
                    report.AppendLine($"[arena] t={i * Dt:0.00} torsoY={ch.Rig.Torso.position.y:0.00} " +
                                      $"legY={ch.Rig.LegL.position.y:0.00} grounded={ch.Ground.IsGrounded} state={ch.State}");
            }

            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;

            bool ok = ch.Rig.Torso.position.y > 0.5f;
            report.AppendLine($"[arena] RESULT={(ok ? "OK" : "FAIL")} torsoY={ch.Rig.Torso.position.y:0.00}");

            Debug.Log(report.ToString());
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath)!.FullName, "..", "_logs", "arena-harness.txt"),
                report.ToString(), new System.Text.UTF8Encoding(false));

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }

    /// <summary>Повторяет матрицу коллизий из GameBootstrap, чтобы стенд совпадал с устройством.</summary>
    internal static class GameBootstrapProbe
    {
        public static void ApplyMatrix()
        {
            int[] all =
            {
                GameLayers.Ground, GameLayers.PlayerBody, GameLayers.EnemyBody, GameLayers.Limb,
                GameLayers.Weapon, GameLayers.Projectile, GameLayers.Props, GameLayers.Grab
            };

            foreach (int a in all)
            foreach (int b in all)
                Physics2D.IgnoreLayerCollision(a, b, false);

            Physics2D.IgnoreLayerCollision(GameLayers.Limb, GameLayers.PlayerBody, true);
            Physics2D.IgnoreLayerCollision(GameLayers.Limb, GameLayers.EnemyBody, true);
            Physics2D.IgnoreLayerCollision(GameLayers.Limb, GameLayers.Limb, true);
            Physics2D.IgnoreLayerCollision(GameLayers.Projectile, GameLayers.Projectile, true);
            Physics2D.IgnoreLayerCollision(GameLayers.Projectile, GameLayers.Weapon, true);
            Physics2D.IgnoreLayerCollision(GameLayers.Grab, GameLayers.Grab, true);
        }
    }
}
