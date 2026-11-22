using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using StickmanFight.Characters;

namespace StickmanFight.EditorTools
{
    /// <summary>
    /// Headless-стенд физики: та же логика персонажа, но шаг физики выполняется вручную
    /// (Physics2D.Simulate), без устройства и без сборки APK. Один прогон — ~30 секунд.
    /// Нужен для подбора констант равновесия (ADR-1) без цикла «сборка 4 минуты».
    /// </summary>
    public static class PhysicsHarness
    {
        private const float Dt = 1f / 60f;

        [MenuItem("Tools/Playground/9. Harness: полный прогон", priority = 9)]
        public static void RunAll()
        {
            var report = new StringBuilder();
            report.AppendLine($"# Physics Harness {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"# Unity {Application.unityVersion}");

            StandTest(report, seconds: 10f);
            ImpulseTest(report, 20f);
            ImpulseTest(report, 40f);

            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;

            string path = Path.Combine(RepoRoot(), "_logs", "physics-harness.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? RepoRoot());
            File.WriteAllText(path, report.ToString(), new UTF8Encoding(false));

            Debug.Log(report.ToString());
            Debug.Log($"[Harness] отчёт: {path}");

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        // ------------------------------------------------------------------ тесты

        private static void StandTest(StringBuilder report, float seconds)
        {
            var sim = new Simulation();

            float worstAngle = 0f;
            float lowestTorso = float.MaxValue;
            int knockdowns = 0;
            var lastState = CharacterState.Standing;

            int steps = Mathf.RoundToInt(seconds / Dt);
            for (int i = 0; i < steps; i++)
            {
                sim.Step();

                var c = sim.Character;
                if (c.State != lastState)
                {
                    if (c.State == CharacterState.Knockdown) knockdowns++;
                    report.AppendLine($"[stand] t={sim.Time:0.00} state {lastState} -> {c.State} angle={c.TorsoAngle:0.0}");
                    lastState = c.State;
                }

                if (c.State == CharacterState.Standing || c.State == CharacterState.Moving)
                    worstAngle = Mathf.Max(worstAngle, Mathf.Abs(c.TorsoAngle));

                lowestTorso = Mathf.Min(lowestTorso, c.Rig.Torso.position.y);

                if (i % 30 == 0)
                    report.AppendLine($"[stand] t={sim.Time:0.0} angle={c.TorsoAngle,6:0.0} torsoY={c.Rig.Torso.position.y:0.00} " +
                                      $"headY={(c.Rig.Head != null ? c.Rig.Head.position.y : 0f):0.00} " +
                                      $"legL=({c.Rig.LegL.position.x:0.00},{c.Rig.LegL.position.y:0.00}) " +
                                      $"state={c.State} grounded={c.Ground.IsGrounded}");
            }

            bool pass = worstAngle < 15f && knockdowns == 0 && lowestTorso > 0.9f;
            report.AppendLine($"[stand] RESULT={(pass ? "PASS" : "FAIL")} maxAngle={worstAngle:0.0} " +
                              $"minTorsoY={lowestTorso:0.00} knockdowns={knockdowns}");
            report.AppendLine();
        }

        private static void ImpulseTest(StringBuilder report, float magnitude)
        {
            var sim = new Simulation();

            // 2 секунды на стабилизацию
            for (int i = 0; i < 120; i++) sim.Step();

            var c = sim.Character;
            float before = c.TorsoAngle;
            c.Push(new Vector2(magnitude, magnitude * 0.3f));
            report.AppendLine($"[impulse {magnitude:0}] старт angle={before:0.0} state={c.State}");

            bool fell = false;
            float fellAt = -1f;
            float gotUpAt = -1f;

            for (int i = 0; i < 60 * 5; i++)
            {
                sim.Step();
                if (!fell && c.State == CharacterState.Knockdown)
                {
                    fell = true;
                    fellAt = sim.Time;
                    report.AppendLine($"[impulse {magnitude:0}] упал t={sim.Time:0.00}");
                }
                else if (fell && gotUpAt < 0f && c.State == CharacterState.Standing)
                {
                    gotUpAt = sim.Time;
                    report.AppendLine($"[impulse {magnitude:0}] встал t={sim.Time:0.00} angle={c.TorsoAngle:0.0}");
                    break;
                }

                if (i % 30 == 0)
                    report.AppendLine($"[impulse {magnitude:0}] t={sim.Time:0.00} state={c.State} angle={c.TorsoAngle:0.0} " +
                                      $"torsoY={c.Rig.Torso.position.y:0.00} grounded={c.Ground.IsGrounded}");
            }

            // Ключевая проверка: после подъёма персонаж должен ОСТАТЬСЯ стоять, а не свалиться снова.
            float holdTime = 3f;
            float worstAfter = 0f;
            float minTorsoAfter = float.MaxValue;
            bool stayedStanding = gotUpAt > 0f;
            for (int i = 0; i < Mathf.RoundToInt(holdTime / Dt) && stayedStanding; i++)
            {
                sim.Step();
                if (c.State == CharacterState.Knockdown || c.State == CharacterState.Dead)
                {
                    stayedStanding = false;
                    break;
                }
                worstAfter = Mathf.Max(worstAfter, Mathf.Abs(c.TorsoAngle));
                minTorsoAfter = Mathf.Min(minTorsoAfter, c.Rig.Torso.position.y);
            }

            if (gotUpAt > 0f)
                report.AppendLine($"[impulse {magnitude:0}] после подъёма {holdTime:0}с: стоит={stayedStanding} " +
                                  $"maxAngle={worstAfter:0.0} minTorsoY={minTorsoAfter:0.00} " +
                                  $"конец: angle={c.TorsoAngle:0.0} torsoY={c.Rig.Torso.position.y:0.00} state={c.State}");

            bool pass = fell && gotUpAt > 0f && stayedStanding;
            report.AppendLine($"[impulse {magnitude:0}] RESULT={(pass ? "PASS" : "FAIL")} упал={fell} " +
                              $"встал={gotUpAt > 0f} удержался={stayedStanding} " +
                              $"tFell={(fellAt < 0 ? "-" : fellAt.ToString("0.00", CultureInfo.InvariantCulture))} " +
                              $"tUp={(gotUpAt < 0 ? "-" : gotUpAt.ToString("0.00", CultureInfo.InvariantCulture))}");
            report.AppendLine();
        }

        // -------------------------------------------------------------- окружение

        private static string RepoRoot()
        {
            return Directory.GetParent(Directory.GetParent(Application.dataPath)!.FullName)?.FullName
                   ?? Application.dataPath;
        }

        /// <summary>Мини-сцена: земля + один персонаж, физика шагается вручную.</summary>
        private sealed class Simulation
        {
            public StickmanCharacter Character { get; }
            public float Time { get; private set; }

            public Simulation()
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                var ground = new GameObject("Ground");
                ground.layer = GameLayers.Ground;
                var box = ground.AddComponent<BoxCollider2D>();
                box.size = new Vector2(30f, 0.2f);
                box.offset = new Vector2(0f, -0.1f);

                var go = new GameObject("Char");
                go.transform.position = new Vector3(0f, 0.05f, 0f);
                Character = go.AddComponent<StickmanCharacter>();
                Character.config = ConfigBuilder.EnsureConfig();
                Character.buildOnAwake = false;
                Character.Build();

                Physics2D.simulationMode = SimulationMode2D.Script;
            }

            public void Step()
            {
                Character.Tick(Dt);
                Physics2D.Simulate(Dt);
                Time += Dt;
            }
        }
    }
}
