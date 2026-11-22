# Журнал этапов (DEVLOG)

Хронология разработки: что сделано, какими средствами проверено, что осталось.
Формат записи: дата → спринт → сделанное → проверка → артефакты → следующий шаг.

---

## 2026-11-22 — S0. Фундамент и пайплайн сборки

**Цель этапа:** доказать, что цепочка «код → сборка → APK» работает на этой машине, до написания игровой логики (RISK-02).

### Сделано

| # | Работа | Файл / артефакт |
|---|---|---|
| 1 | Перенос проекта из `Desktop\stickman fight` в `C:\Dev\stickman-fight` (без пробелов в пути) | — |
| 2 | Проверка установки Unity 6000.3.29f1: Android SDK (API 34–37), NDK, OpenJDK, лицензия | — |
| 3 | Репозиторий: `.gitignore` (Unity-шаблон + артефакты), `README.md` | корень репо |
| 4 | Каркас Unity-проекта без шаблона (свой `manifest.json`), версия зафиксирована в `ProjectVersion.txt` | `game/ProjectSettings/ProjectVersion.txt` |
| 5 | Пакеты: `com.unity.ugui 2.0.0`, `com.unity.test-framework 1.6.0`; удалён `com.unity.multiplayer.center` | `game/Packages/manifest.json` |
| 6 | Константы проекта: версия, package name, слои, палитра | `Scripts/Core/ProjectData.cs` |
| 7 | Применение ProjectSettings кодом: Player Settings, Android (API 24/35, IL2CPP, ARMv7+ARM64, landscape), Built-in рендер, OpenGL ES 3, физика 2D (8/3 итераций, g = −30), слои 8–15, шаг времени 1/60 | `Scripts/Editor/ProjectSetup.cs` |
| 8 | Матрица коллизий и стартовые настройки — кодом, а не в бинарных настройках | `Scripts/Core/GameBootstrap.cs` |
| 9 | Процедурная графика: стикмен линиями, отрезок-линия с материалом, создаваемым в `Awake` | `Scripts/Art/StickmanFigure.cs`, `LineStrip.cs` |
| 10 | Сборка сцены кодом + подключение UI-событий | `Scripts/Editor/SceneBuilder.cs` |
| 11 | CLI-сборка APK/AAB с отчётом в `_logs/build-result.txt` | `Scripts/Editor/BuildScript.cs` |
| 12 | Экран-заглушка S0: версия, FPS, разрешение, safe area, счётчик нажатий, индикатор касаний | `Scripts/Core/HelloStickman.cs` |

### Проверено

| Проверка | Результат |
|---|---|
| `Unity.exe -batchmode -executeMethod` (лицензия + компиляция) | ✅ `[SELFTEST] OK version=6000.3.29f1` |
| Применение настроек проекта | ✅ 6/6 групп `[Setup] OK`, ошибок компиляции 0 |
| Слои в `TagManager.asset` | ✅ Ground…Grab на индексах 8…15 |
| `ProductSettings` | ✅ `productName = Stickman Fight`, `applicationIdentifier = com.stickmanfight.game`, minSdk 24, targetSdk 35 |
| Сборка APK | ✅ `builds/StickmanFight_v0.1.0.1.apk`, **36 МБ** (NFR: ≤ 60 МБ) |
| Метаданные APK (`aapt2 dump badging`) | ✅ package `com.stickmanfight.game`, versionCode 100, versionName 0.1.0.1, minSdk **25**, targetSdk 35, ABI `arm64-v8a` + `armeabi-v7a`, разрешений кроме системного нет |
| Установка на устройство (Samsung SM-S921B, Android 17 / API 37) | ✅ `adb install` — Success, запуск без крашей; `[Boot] Stickman Fight 0.1.0.2 на Android`; 60–61 FPS; 2340×990 |
| Safe area (FR-UI-07) | ✅ в v0.1.0.2 инфо-панель и кнопка внутри safe area (2237×990) — вырез справа больше не перекрывает UI |

### Найденные грабли (важно для будущих сборок)

1. **PowerShell не ждёт `Unity.exe`.** Это GUI-приложение: `& $unity ...` возвращает управление сразу, а процесс Unity продолжает работать и убивается вместе с деревом процессов. Любая CLI-сборка — только через `Start-Process -Wait`.
2. **BOM в JSON ломает Unity.** `Set-Content -Encoding UTF8` в этой среде пишет BOM, и Package Manager отказывается читать `manifest.json` («Non-whitespace before {»). Писать только через `[System.IO.File]::WriteAllText` с `UTF8Encoding($false)`.
3. **uGUI не входит в дефолтный manifest.** Без `com.unity.ugui` не компилируется `UnityEngine.UI`.
4. **Материал без ассета не выживает сборку.** Любой `new Material(...)` в редакторском коде нужно либо сохранять как ассет, либо создавать в рантайме (`Awake`) — иначе в APK получается «пропавший» материал.
5. **Unity подняла minSdk до 25** (Android 7.1), хотя запрашивали 24 — ограничение версии редактора.
6. **`BuildSummary.totalSize` — не размер APK.** Он показывает суммарный объём файлов сборки (435 МБ), фактический APK — 36 МБ.
7. **Счётчик нажатий нарастал «сам» — ложная тревога.** Проверка показала `TOUCH: 1` и кнопку в состоянии pressed, а в сцене ровно один persistent-listener на `OnTap`: рост счётчика давали реальные касания по экрану, а не автоклик. Вывод на будущее: в диагностических экранах логировать `Input.touchCount` рядом со счётчиком — экономит полчаса разбирательств.

### Артефакты

- `builds/StickmanFight_v0.1.0.1.apk`
- `_logs/setup.log`, `_logs/build.log`, `_logs/build-result.txt`
- сцена `game/Assets/_Project/Scenes/00_Boot.unity` (генерируется кодом)

### Следующий шаг — S2. Управление и движение

1. `ICharacterInput` + `TouchInput`: экранные кнопки по раскладке раздела 9 ТЗ, мультитач.
2. `KeyboardInput` для отладки на ПК (стрелки, пробел).
3. Движение, разворот спрайта, прыжок с coyote time и jump buffer — проверить на устройстве.
4. Камера-следопыт со сглаживанием и ограничением по границам арены.
5. Ревизия тестового стенда: кнопки движения вместо только импульсов.

**Критерий выхода (G2):** 5/5 тестеров двигаются без подсказки; одновременное «вправо + атака» работает в 20/20 попыток.

---

## 2026-11-22 — S1. Физика персонажа и ragdoll

**Цель этапа:** персонаж стоит сам, падает от удара и поднимается (G1).

### Сделано

| # | Работа | Файл |
|---|---|---|
| 1 | Конфиг персонажа как ScriptableObject: массы, размеры, движение, стабилизация, пороги | `Characters/CharacterConfig.cs` |
| 2 | Сборка ragdoll кодом: 6 тел (корпус, голова, 2 руки, 2 ноги), 5 шарниров, лимиты углов | `Characters/RagdollRig.cs` |
| 3 | Проверка земли с гистерезисом (без мигания Airborne/Standing) | `Characters/GroundCheck.cs` |
| 4 | Автомат состояний: Standing / Moving / Airborne / Stunned / Knockdown / Dead | `Characters/CharacterState.cs`, `StickmanCharacter.cs` |
| 5 | Ввод как намерения (`MoveX`, `JumpQueued`) — одинаково для игрока, ИИ и автотестов | `StickmanCharacter.cs` |
| 6 | Здоровье, урон с импульсом, нокдаун, подъём с неуязвимостью | `StickmanCharacter.cs` |
| 7 | Визуал: линии по физическим телам (корпус, голова-окружность, конечности) | `Art/RagdollVisual.cs` |
| 8 | Headless-стенд физики: ручной шаг `Physics2D.Simulate`, отчёт в файл | `Editor/PhysicsHarness.cs` |
| 9 | Стенд на устройстве: автопрогон G1 с логами `[StandTest]`, `[ImpulseTest]` | `Core/TestBed.cs` |
| 10 | Генерация конфига с сохранением GUID (иначе сцены теряли ссылку) | `Editor/ConfigBuilder.cs` |

### Проверено

**Headless-стенд** (`_logs/physics-harness.txt`, прогон ~40 с вместо сборки APK на 4 минуты):

| Тест | Результат |
|---|---|
| Стойка 10 с | ✅ `maxAngle=0.3°`, `minTorsoY=1.05 м`, падений 0 |
| Импульс 20 Н·с | ✅ упал, встал через 0.9 с, через 3 с стоит (`angle=-0.3°`, `torsoY=1.07`) |
| Импульс 40 Н·с | ✅ упал, встал через 0.9 с, через 3 с стоит (`angle=-0.2°`, `torsoY=1.07`) |

**На устройстве** (Samsung SM-S921B, Android 17): `[StandTest] PASS maxAngle=0.3 knockdowns=0`, оба импульса `PASS`, подъём 0.93 с, 60 FPS. Скриншот: `_logs/s1e.png`.

### Найденные грабли

1. **Unity 6.3 переименовала 2D-API суставов:** `JointLimits` → `JointAngleLimits2D`, `JointMotor2D.maxMotorForce` → `maxMotorTorque`, у `Rigidbody2D` удалён `maxAngularVelocity`. Код из туториалов под 2020–2022 не компилируется.
2. **Мотор шарнира со скоростью 0 — это тормоз, а не удержание угла.** Ноги «раскладывались», тело проседало с 1.05 до 0.53 м. Лечится сервоприводом: `motorSpeed = clamp(err * 6, ±200)`.
3. **PD-момент не держит стойку.** На 10° наклона `Kp=300` даёт 52 Н·м против 276 Н·м опрокидывающего момента веса. Замена на жёсткий доворот (`MoveRotation` со скоростью 720°/с) дала 0.3° вместо 19.7°.
4. **Сервопривод ног «шагал» и уводил персонажа вбок** (дрейф 0.2 м/с), пока корпус был наклонён. Убралось вместе с наклоном.
5. **Лежачее тело нельзя довернуть в стойку:** вращение упирается в пол, решатель контактов возвращает его. Нужен приподъём корпуса на 0.45 м перед доворотом.
6. **Проверка земли ломается в лежачем положении** (точка проверки уходит под пол) — подъём нельзя привязывать к `IsGrounded`, только к таймеру (так и записано в FR-RAG-05).
7. **Конфиг-ассет нельзя пересоздавать** при смене дефолтов: новый GUID → сцена теряет ссылку. Сбрасывать значения через `EditorUtility.CopySerialized`.
8. **PowerShell здесь версии 5.1:** `-Encoding UTF8` пишет BOM, а `UTF8NoBOM` не существует. Из-за этого номер сборки молча не изменился, и установился старый APK. Писать файлы только через `[System.IO.File]::WriteAllText(..., UTF8Encoding($false))`.
9. **Приёмочный тест ломался от случайного касания кнопки:** `[State] 5.4s -> Knockdown` при HP 100 и без `[Damage]` — это нажатие «20» пользователем, а не дефект физики. Ручные кнопки теперь блокируются на время автопрогона (`TestBed.SetManualButtons`).

### Артефакты

- `builds/StickmanFight_v0.1.0.4.apk`
- `_logs/physics-harness.txt` — машинный отчёт стенда
- `game/Assets/_Project/Configs/Character_Default.asset`

### Следующий шаг — S2. Управление и движение
