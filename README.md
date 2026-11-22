# Stickman Fight

2D-игра про стикменов на физике: быстрые дуэли с оружием на аренах, где край платформы так же опасен, как противник.
Ориентир — трейлер референсной игры (58 с, разобран покадрово); полная спецификация — [TZ_Stickman_Fight_v1.0.md](TZ_Stickman_Fight_v1.0.md), история этапов — [DEVLOG.md](DEVLOG.md).

\*\*Текущий\ этап:\*\*\ играбельный\ цикл\ —\ меню,\ бот-противник,\ матч\ со\ stocks/раундами/рематчем,\ звук\ и\ эффекты\ \(uilds/StickmanFight_v0\.1\.0\.18\.apk\)\.\ Дальше\ —\ два\ игрока\ на\ одном\ экране\ и\ PD-рэгдолл\.

---

## Требования

| Компонент | Версия | Где взять |
|---|---|---|
| Unity | 6000.3.29f1 (6.3 LTS) | Unity Hub |
| Модули Unity | Android Build Support + SDK/NDK/OpenJDK, Windows Build Support | ставится вместе с редактором |
| ОС | Windows 10/11 x64 | — |

Подробная инструкция по установке — [SETUP_dlya_razrabotki.md](SETUP_dlya_razrabotki.md).

## Быстрая проверка на ПК (основной цикл разработки)

Телефон нужен только для финальной приёмки. Повседневные проверки идут на Windows-плеере: **11–25 секунд на сборку** вместо 4–5 минут у APK.

```powershell
# собрать Windows-плеер, запустить, снять кадр и показать лог
.\_tools\pc.ps1

# только запуск без пересборки, кадр через 8 секунд
.\_tools\pc.ps1 -NoBuild -Delay 8
```

Что делает скрипт:

1. собирает `builds/win/StickmanFight.exe` (Mono, `BuildTarget.StandaloneWindows64`);
2. запускает окно 1600×900 и передаёт `-shot <путь> -shotDelay <сек>`;
3. `AutoScreenshot` внутри игры делает кадр и закрывает приложение;
4. печатает хвост `Player.log` с тестами (`[SwordTest]`, `[MoveTest]`, `[Probe]`).

Скриншот — `_logs/pc.png`, лог — `_logs/pc-player.log`.

## Сборка Android (для устройства)

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

Для `*.aab` (Google Play): заменить метод на `...BuildScript.BuildAab`.

> ⚠️ `Unity.exe` — GUI-приложение: обычный `& $u ...` в PowerShell **не ждёт** завершения.
> Всегда через `Start-Process -Wait`, иначе скрипт вернётся раньше, чем закончится сборка.

## Полезные команды в редакторе

| Меню | Что делает |
|---|---|
| `Tools → Playground → 0. Configure Android Paths` | прописывает SDK/NDK/JDK из установки Unity в `EditorPrefs` |
| `Tools → Playground → 1. Configure Project` | применяет Player Settings, слои, физику, время (идемпотентно) |
| `Tools → Playground → 2. Rebuild Scenes` | пересобирает сцены кодом (ADR-3) |
| `Tools → Playground → 3. Build APK` / `4. Build AAB` | сборка Android из редактора |
| `Tools → Playground → 6. Build Windows (PC test)` | сборка Windows-плеера для быстрой проверки |
| `Tools → Playground → 8. Harness: арена` | headless-проверка физики арены (без сборки) |
| `Tools → Playground → 9. Harness: полный прогон` | headless-тесты стойки, импульсов, прыжка, движения |

## Управление

| Действие | Телефон | ПК (отладка) |
|---|---|---|
| Движение | кнопки ◀ ▶ (удержание) | A / D или ← / → |
| Прыжок | ⤒ | Space или W |
| Атака | УДАР | J или левая кнопка мыши |
| Взять / бросить | ВЗЯТЬ / БРОС | E / Q |
| Пауза | II | Esc |

## Структура

```
game/                 Unity-проект
  Assets/_Project/    весь код и ассеты проекта (вне сторонних пакетов)
builds/               артефакты сборки: *.apk, win/ (в .gitignore)
_logs/                логи сборок, скриншоты, отчёты стендов (в .gitignore)
_tools/               вспомогательные скрипты: pc.ps1, md2docx.py
```

Ключевые решения:
- **ADR-1** — гибридный персонаж: капсула с удержанием вертикали + физические конечности на шарнирах; полный ragdoll при падении.
- **ADR-2** — процедурная графика: ноль внешних ассетов, стикмен рисуется линиями, фон — сгенерированными спрайтами.
- **ADR-3** — сцены и префабы собираются кодом (`SceneBuilder`), ручные правки сцен не коммитятся.
- **ADR-4** — весь баланс в ScriptableObject-конфигах.

## Известные ограничения

- Второго игрока пока нет: на арене один управляемый боец и манекен без ИИ.
- Матч-луп не реализован: нет stocks, раундов и экрана результата.
- Нет звука и эффектов попадания (следы удара, частицы, цифры урона).
- Корпус держится кинематическим доворотом, а не PD-моторами (запланировано на этап «ragdoll»).
- Подпись отладочная (`useCustomKeystore = false`); release-ключ появится перед финальной сборкой.
