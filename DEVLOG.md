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
| Установка на устройство | ⏳ телефон подключён (`RFCXA1AJ0KN`), ждёт подтверждения отладки на экране |

### Найденные грабли (важно для будущих сборок)

1. **PowerShell не ждёт `Unity.exe`.** Это GUI-приложение: `& $unity ...` возвращает управление сразу, а процесс Unity продолжает работать и убивается вместе с деревом процессов. Любая CLI-сборка — только через `Start-Process -Wait`.
2. **BOM в JSON ломает Unity.** `Set-Content -Encoding UTF8` в этой среде пишет BOM, и Package Manager отказывается читать `manifest.json` («Non-whitespace before {»). Писать только через `[System.IO.File]::WriteAllText` с `UTF8Encoding($false)`.
3. **uGUI не входит в дефолтный manifest.** Без `com.unity.ugui` не компилируется `UnityEngine.UI`.
4. **Материал без ассета не выживает сборку.** Любой `new Material(...)` в редакторском коде нужно либо сохранять как ассет, либо создавать в рантайме (`Awake`) — иначе в APK получается «пропавший» материал.
5. **Unity подняла minSdk до 25** (Android 7.1), хотя запрашивали 24 — ограничение версии редактора.
6. **`BuildSummary.totalSize` — не размер APK.** Он показывает суммарный объём файлов сборки (435 МБ), фактический APK — 36 МБ.

### Артефакты

- `builds/StickmanFight_v0.1.0.1.apk`
- `_logs/setup.log`, `_logs/build.log`, `_logs/build-result.txt`
- сцена `game/Assets/_Project/Scenes/00_Boot.unity` (генерируется кодом)

### Следующий шаг — S1. Физика персонажа

1. `CharacterController` + `RagdollBinder`: капсула со стабилизацией вертикали (ADR-1: Kp 60, Kd 18, момент ≤ 2500).
2. Физические конечности на `HingeJoint2D` (6 тел, лимиты углов из раздела 7.6 ТЗ).
3. Конечный автомат состояний `Standing / Moving / Airborne / Stunned / Knockdown / Dead`.
4. Тестовый стенд: импульс 20 Н·с → падение → подъём за 0.9 с.
5. PlayMode-тест «стоит 10 с, угол < 15°».

**Критерий выхода (G1):** персонаж стоит 30 с без ввода; от импульса 20 Н·с падает и поднимается; 0 случаев «застрял в knockdown».
