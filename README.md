# Stickman Fight

2D-игра про стикменов на физике: дуэли с оружием на арене + физическая песочница.
Мобильный прототип под Android. Полное ТЗ — [TZ_Stickman_Fight_v1.0.md](TZ_Stickman_Fight_v1.0.md).

**Текущий этап:** S0 завершён — пайплайн сборки проверен, первая APK собрана (`builds/StickmanFight_v0.1.0.1.apk`, 36 МБ). Дальше — S1 (физика персонажа и ragdoll).

---

## Требования

| Компонент | Версия | Где взять |
|---|---|---|
| Unity | 6000.3.29f1 (6.3 LTS) | Unity Hub |
| Модули Unity | Android Build Support + SDK/NDK/OpenJDK, Windows Build Support (IL2CPP) | ставится вместе с редактором |
| ОС | Windows 10/11 x64 | — |

Подробная инструкция по установке — [SETUP_dlya_razrabotki.md](SETUP_dlya_razrabotki.md).

## Сборка

Сборка выполняется **только из командной строки** — это делает её воспроизводимой (NFR-CODE-04).

```powershell
$u    = "C:\Program Files\Unity\Hub\Editor\6000.3.29f1\Editor\Unity.exe"
$game = "C:\Dev\stickman-fight\game"
$log  = "C:\Dev\stickman-fight\_logs\build.log"

$args = @("-batchmode", "-quit", "-nographics",
          "-projectPath", $game, "-buildTarget", "Android",
          "-executeMethod", "StickmanFight.EditorTools.BuildScript.BuildApk",
          "-logFile", $log)

$p = Start-Process -FilePath $u -ArgumentList $args -PassThru -Wait
"EXIT=" + $p.ExitCode
```

Результат: `builds/StickmanFight_v<версия>.apk`, отчёт — `_logs/build-result.txt`.

> ⚠️ `Unity.exe` — GUI-приложение: обычный `& $u ...` в PowerShell **не ждёт** завершения.
> Всегда через `Start-Process -Wait`, иначе скрипт вернётся раньше, чем закончится сборка.

Для `*.aab` (Google Play): заменить метод на `...BuildScript.BuildAab`.

## Полезные команды в редакторе

| Меню | Что делает |
|---|---|
| `Tools → Playground → 0. Configure Android Paths` | прописывает SDK/NDK/JDK из установки Unity в `EditorPrefs` |
| `Tools → Playground → 1. Configure Project` | применяет Player Settings, слои, физику, время (идемпотентно) |
| `Tools → Playground → 2. Rebuild Scenes` | пересобирает сцены кодом (ADR-3) |
| `Tools → Playground → 3. Build APK` / `4. Build AAB` | сборка из редактора |

## Структура

```
game/                 Unity-проект
  Assets/_Project/    весь код и ассеты проекта (вне сторонних пакетов)
builds/               артефакты сборки (в .gitignore)
_logs/                логи Unity (в .gitignore)
_tools/               вспомогательные скрипты (md2docx.py)
```

Ключевые решения:
- **ADR-1** — гибридный персонаж: капсула со стабилизацией вертикали + физические конечности; полный ragdoll только при падении.
- **ADR-2** — процедурная графика: ноль внешних ассетов, стикмен рисуется линиями в `StickmanFigure`.
- **ADR-3** — сцены и префабы собираются кодом (`SceneBuilder`), ручные правки сцен не коммитятся.
- **ADR-4** — весь баланс в ScriptableObject-конфигах.

## Управление

Появится в S2 (см. раздел 9 ТЗ). В сборке S0 есть только тестовая кнопка.

## Известные ограничения

- S0 — заглушка: физики персонажа нет, стикмен статичен.
- Подпись отладочная (`useCustomKeystore = false`); release-ключ появится перед финальной сборкой.
