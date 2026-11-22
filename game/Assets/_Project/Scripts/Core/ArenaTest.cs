using System.Collections;
using StickmanFight.Characters;
using StickmanFight.Weapons;
using UnityEngine;

namespace StickmanFight.Core
{
    /// <summary>
    /// Автопрогон G2/G3 на арене. Проверяет без участия человека:
    /// удар мечом (урон), молот (нокдаун), пистолет (урон + патроны), смерть,
    /// движение на 2 с и прыжок. Пишет PASS/FAIL в logcat с префиксами [SwordTest] и т.д.
    /// </summary>
    public class ArenaTest : MonoBehaviour
    {
        public StickmanCharacter player;
        public StickmanCharacter enemy;
        public Weapon sword;
        public Weapon hammer;
        public Weapon pistol;

        [Header("Автопрогон")]
        public bool autoRun = true;

        private float _elapsed;
        private StickmanFight.Inputs.InputRouter _router;

        private void Start()
        {
            // Во время автопрогона ввод идёт из теста, а не с кнопок:
            // иначе InputRouter каждый кадр перезатирает MoveX нулём.
            _router = player != null ? player.GetComponent<StickmanFight.Inputs.InputRouter>() : null;
            if (autoRun) StartCoroutine(RunAll());
        }

        private IEnumerator RunAll()
        {
            if (_router != null) _router.enabled = false;

            // Диагностика первых секунд: понять, падает ли персонаж до/после GO.
            float probeStart = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - probeStart < 4f)
            {
                if (player != null && player.Rig != null)
                    Debug.Log($"[Probe] t={Time.realtimeSinceStartup - probeStart:0.00} " +
                              $"torsoY={player.Rig.Torso.position.y:0.00} grounded={player.Ground.IsGrounded} " +
                              $"timeScale={Time.timeScale:0.0} state={player.State}");
                yield return new WaitForSecondsRealtime(0.5f);
            }

            yield return new WaitForSeconds(1.5f);
            _elapsed = 0f;

            yield return Approach();

            yield return SwordTest();
            yield return new WaitForSeconds(0.6f);

            yield return HammerTest();
            yield return new WaitForSeconds(1.5f);

            yield return PistolTest();
            yield return new WaitForSeconds(0.8f);

            yield return KillTest();
            yield return new WaitForSeconds(1.0f);

            yield return MoveTest();
            yield return JumpTest();

            if (_router != null) _router.enabled = true;
            Debug.Log("[ArenaTest] завершён");
        }

        // ------------------------------------------------------------------ тесты

        /// <summary>Подойти к противнику на дистанцию удара — заодно проверяет движение.</summary>
        private IEnumerator Approach(float targetDistance = 1.0f)
        {
            if (player == null || enemy == null) yield break;

            float t = 0f;
            while (t < 2.5f)
            {
                float dx = enemy.Rig.Torso.position.x - player.Rig.Torso.position.x;
                if (Mathf.Abs(dx) <= targetDistance) break;

                player.MoveX = Mathf.Sign(dx);
                t += Time.deltaTime;
                yield return null;
            }

            player.MoveX = 0f;
            yield return new WaitForSeconds(0.3f);

            float finalDx = Mathf.Abs(enemy.Rig.Torso.position.x - player.Rig.Torso.position.x);
            Debug.Log($"[Approach] дистанция={finalDx:0.00} м за {t:0.0} с");
        }

        private IEnumerator SwordTest()
        {
            if (player == null || enemy == null || sword == null) yield break;

            player.Combat.Equip(sword);
            float before = enemy.Hp;
            player.Combat.RequestAttack();

            yield return new WaitForSeconds(0.5f);

            float delta = before - enemy.Hp;
            bool pass = delta >= 14f && delta <= 22f;
            Debug.Log($"[SwordTest] {(pass ? "PASS" : "FAIL")} урон={delta:0.0} (ожидание 14..22) hp={enemy.Hp:0} state={enemy.State}");
        }

        private IEnumerator HammerTest()
        {
            if (player == null || enemy == null || hammer == null) yield break;

            player.Combat.Equip(hammer);
            int knockdownBefore = _knockdowns;
            player.Combat.RequestAttack();

            yield return new WaitForSeconds(1.2f);

            bool pass = _knockdowns > knockdownBefore;
            Debug.Log($"[HammerTest] {(pass ? "PASS" : "FAIL")} нокдаун={pass} hp={enemy.Hp:0} state={enemy.State}");
        }

        private IEnumerator PistolTest()
        {
            if (player == null || enemy == null || pistol == null) yield break;

            player.Combat.Equip(pistol);
            float before = enemy.Hp;
            float ammoBefore = player.Combat.Weapon != null ? player.Combat.Weapon.Ammo : 0f;

            player.Combat.RequestAttack();
            yield return new WaitForSeconds(0.2f);

            // Второй выстрел должен быть заблокирован кулдауном/временем, проверяем только один.
            yield return new WaitForSeconds(0.6f);

            float delta = before - enemy.Hp;
            float ammoAfter = player.Combat.Weapon != null ? player.Combat.Weapon.Ammo : 0f;

            bool pass = delta >= 9f && ammoAfter <= ammoBefore - 1f;
            Debug.Log($"[PistolTest] {(pass ? "PASS" : "FAIL")} урон={delta:0.0} патроны={ammoAfter:0}/{ammoBefore:0}");
        }

        private IEnumerator KillTest()
        {
            if (enemy == null) yield break;

            int guard = 0;
            while (!enemy.IsDead && guard++ < 40)
            {
                enemy.ApplyDamage(30f, Vector2.zero, (Vector2)enemy.Rig.Torso.position);
                yield return new WaitForSeconds(0.12f);
            }

            yield return new WaitForSeconds(0.3f);
            bool pass = enemy.IsDead;
            Debug.Log($"[KillTest] {(pass ? "PASS" : "FAIL")} hp={enemy.Hp:0} state={enemy.State}");
        }

        private IEnumerator MoveTest()
        {
            if (player == null) yield break;

            player.Combat.RequestDrop();
            yield return new WaitForSeconds(0.4f);

            float startX = player.Rig.Torso.position.x;
            float startTime = Time.time;

            player.MoveX = -1f;
            while (Time.time - startTime < 1.2f) yield return null;
            player.MoveX = 0f;

            float moved = startX - player.Rig.Torso.position.x;
            bool pass = moved >= 3.8f && !player.IsDead;
            Debug.Log($"[MoveTest] {(pass ? "PASS" : "FAIL")} прошёл={moved:0.0} м за 1.2 с (ожидание ≥3.8) жив={!player.IsDead}");
        }

        private IEnumerator JumpTest()
        {
            if (player == null) yield break;

            yield return new WaitForSeconds(0.6f);
            float baseY = player.Rig.Torso.position.y;
            Debug.Log($"[JumpTest] старт state={player.State} grounded={player.Grounded} y={baseY:0.00} vy={player.Rig.Torso.linearVelocity.y:0.00}");
            player.JumpQueued = true;

            float peak = baseY;
            float t = 0f;
            while (t < 1.2f)
            {
                t += Time.deltaTime;
                peak = Mathf.Max(peak, player.Rig.Torso.position.y);
                yield return null;
            }

            float height = peak - baseY;
            bool pass = height >= 1.0f;
            Debug.Log($"[JumpTest] {(pass ? "PASS" : "FAIL")} высота={height:0.00} м (ожидание ≥1.0)");
        }

        // ------------------------------------------------------------------ учёт

        private int _knockdowns;

        private void OnEnable()
        {
            if (enemy != null) enemy.StateChanged += OnEnemyState;
        }

        private void OnDisable()
        {
            if (enemy != null) enemy.StateChanged -= OnEnemyState;
        }

        private void OnEnemyState(CharacterState state)
        {
            if (state == CharacterState.Knockdown) _knockdowns++;
        }

        /// <summary>Перезапуск арены — вешается на кнопку «СБРОС».</summary>
        public void ResetScene()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
