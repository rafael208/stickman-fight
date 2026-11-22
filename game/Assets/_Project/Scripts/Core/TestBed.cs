using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using StickmanFight.Characters;

namespace StickmanFight.Core
{
    /// <summary>
    /// Тестовый стенд S1: проверяет G1 автоматически.
    /// 1) 30 с без ввода — персонаж должен стоять (|угол| &lt; 15°, без падений).
    /// 2) Импульс 20 Н·с — должен упасть и подняться за ~1.3 с.
    /// 3) Импульс 40 Н·с — падение и подъём.
    /// Результаты пишутся в logcat с префиксами [Stand], [StandTest], [ImpulseTest] — их можно
    /// забрать с устройства командой adb logcat и получить машинную приёмку.
    /// </summary>
    public class TestBed : MonoBehaviour
    {
        [Header("Сцена")]
        public StickmanCharacter character;

        [Header("HUD")]
        public Text hudState;
        public Text hudAngle;
        public Text hudHp;
        public Text hudFps;
        public Text hudNote;

        [Header("Автопрогон")]
        public bool autoRun = true;

        [Header("Кнопки (блокируются на время автопрогона)")]
        public UnityEngine.UI.Button[] manualButtons;

        private float _elapsed;
        private float _maxAngle;
        private int _knockdowns;
        private bool _standTestDone;
        private bool _impulseDone;

        private float _fpsAccum;
        private int _fpsFrames;

        private void Start()
        {
            if (character == null) character = FindFirstObjectByType<StickmanCharacter>();

            if (character != null)
            {
                character.StateChanged += OnStateChanged;
                character.Damaged += OnDamaged;
            }

            if (hudNote != null)
                hudNote.text = autoRun ? "автопрогон G1 идёт…" : "ручной режим";

            if (autoRun) StartCoroutine(RunAutoSequence());
        }

        private void SetManualButtons(bool interactable)
        {
            if (manualButtons == null) return;
            foreach (var b in manualButtons)
                if (b != null) b.interactable = interactable;
        }

        private void OnDestroy()
        {
            if (character == null) return;
            character.StateChanged -= OnStateChanged;
            character.Damaged -= OnDamaged;
        }

        private void OnStateChanged(CharacterState state)
        {
            Debug.Log($"[State] {_elapsed:0.0}s -> {state}");
            if (state == CharacterState.Knockdown) _knockdowns++;
        }

        private void OnDamaged(float amount)
        {
            Debug.Log($"[Damage] {amount:0.0}");
        }

        // ------------------------------------------------------------------ HUD

        private void Update()
        {
            _elapsed += Time.deltaTime;

            if (!_standTestDone && _elapsed < 30.5f && character != null)
            {
                var state = character.State;
                if (state == CharacterState.Standing || state == CharacterState.Moving)
                    _maxAngle = Mathf.Max(_maxAngle, Mathf.Abs(character.TorsoAngle));
            }

            _fpsAccum += Time.unscaledDeltaTime;
            _fpsFrames++;
            float fps = 0f;
            if (_fpsAccum >= 0.5f)
            {
                fps = _fpsFrames / _fpsAccum;
                _fpsAccum = 0f;
                _fpsFrames = 0;
            }

            if (character == null) return;
            if (hudState != null) hudState.text = $"STATE: {character.State}";
            if (hudAngle != null) hudAngle.text = $"ANGLE: {character.TorsoAngle:0.0}°";
            if (hudHp != null) hudHp.text = $"HP: {character.Hp:0}";
            if (hudFps != null && fps > 0f) hudFps.text = $"{fps:0} FPS";
        }

        // ------------------------------------------------------- автоматический прогон

        private IEnumerator RunAutoSequence()
        {
            // Во время автопрогона ручные кнопки выключены: случайное касание не должно
            // ломать приёмочный тест (именно так один прогон дал ложный FAIL).
            SetManualButtons(false);

            yield return null;

            // --- 1. Стойка 30 секунд ---
            float t = 0f;
            float worst = 0f;
            int kdBefore = _knockdowns;

            while (t < 30f)
            {
                t += Time.deltaTime;
                if (character != null && (character.State == CharacterState.Standing || character.State == CharacterState.Moving))
                    worst = Mathf.Max(worst, Mathf.Abs(character.TorsoAngle));

                if (Mathf.Abs(t % 5f) < Time.deltaTime)
                {
                    Debug.Log($"[Stand] t={t:0.0} state={character?.State} angle={character?.TorsoAngle:0.0} hp={character?.Hp:0}");
                }
                yield return null;
            }

            _standTestDone = true;
            bool pass = worst < 15f && _knockdowns == kdBefore;
            Debug.Log($"[StandTest] {(pass ? "PASS" : "FAIL")} maxAngle={worst:0.0} knockdowns={_knockdowns - kdBefore}");
            if (hudNote != null) hudNote.text = pass ? "стойка: PASS" : $"стойка: FAIL ({worst:0.0}°)";

            yield return new WaitForSeconds(1f);

            // --- 2. Импульс 20 Н·с ---
            yield return TestImpulse(20f, 1.8f);

            yield return new WaitForSeconds(1f);

            // --- 3. Импульс 40 Н·с ---
            yield return TestImpulse(40f, 2.5f);

            _impulseDone = true;
            Debug.Log("[AutoTest] завершён");
            if (hudNote != null && _standTestDone) hudNote.text = "автопрогон завершён";
            SetManualButtons(true);
        }

        private IEnumerator TestImpulse(float magnitude, float wait)
        {
            if (character == null) yield break;

            int kdBefore = _knockdowns;
            Debug.Log($"[ImpulseTest] старт impulse={magnitude} angle={character.TorsoAngle:0.0}");
            character.Push(new Vector2(Mathf.Sign(Random.Range(-1f, 1f)) * magnitude, magnitude * 0.35f));

            float t = 0f;
            bool stoodUp = false;
            while (t < wait)
            {
                t += Time.deltaTime;
                if (_knockdowns > kdBefore && character.State != CharacterState.Knockdown && !character.IsDead)
                {
                    stoodUp = true;
                    Debug.Log($"[ImpulseTest] поднялся через {t:0.00}s after knockdown");
                    break;
                }
                yield return null;
            }

            bool fell = _knockdowns > kdBefore;
            Debug.Log($"[ImpulseTest] {(fell ? "PASS" : "FAIL")} упал={fell} встал={stoodUp} t={t:0.00}s");
        }

        // ------------------------------------------------------------- ручные кнопки

        public void ManualImpulse(float magnitude)
        {
            if (character == null) return;
            Debug.Log($"[Manual] impulse={magnitude} at {_elapsed:0.0}s");
            character.Push(new Vector2(magnitude, magnitude * 0.3f));
        }

        // Обёртки без аргументов — для onClick в инспекторе/сцене.
        public void Impulse20() => ManualImpulse(20f);
        public void Impulse40() => ManualImpulse(40f);

        public void ManualReset()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
