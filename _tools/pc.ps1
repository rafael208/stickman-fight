# Проверка игры на ПК без телефона: сборка Windows-плеера, запуск, скриншот, лог.
# Использование:
#   .\_tools\pc.ps1                    # собрать, снять кадр через 4 с, показать лог
#   .\_tools\pc.ps1 -Delay 8 -NoBuild  # только запуск (без пересборки)

param(
    [float]$Delay = 4.0,
    [string]$Shot = "",
    [switch]$NoBuild,
    [switch]$AutoPlay,
    [string]$OutName = "pc"
)

$ErrorActionPreference = "Stop"
$root = "C:\Dev\stickman-fight"
$unity = "C:\Program Files\Unity\Hub\Editor\6000.3.29f1\Editor\Unity.exe"
$game = Join-Path $root "game"
$exe = Join-Path $root "builds\win\StickmanFight.exe"
$playerLog = Join-Path $root "_logs\$OutName-player.log"
if ([string]::IsNullOrWhiteSpace($Shot)) { $Shot = Join-Path $root "_logs\$OutName.png" }

if (-not $NoBuild) {
    $buildLog = Join-Path $root "_logs\win-build.log"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $p = Start-Process -FilePath $unity -PassThru -Wait -ArgumentList @(
        "-batchmode", "-quit", "-nographics", "-projectPath", $game,
        "-buildTarget", "StandaloneWindows64",
        "-executeMethod", "StickmanFight.EditorTools.BuildScript.BuildWindows",
        "-logFile", $buildLog
    )
    $sw.Stop()
    if ($p.ExitCode -ne 0) {
        Write-Host "BUILD FAILED (exit $($p.ExitCode))"
        Select-String -Path $buildLog -Pattern "error CS" | Select-Object -First 10 | ForEach-Object { $_.Line }
        exit 1
    }
    Write-Host ("BUILD OK за {0:0.0} с" -f $sw.Elapsed.TotalSeconds)
}

if (-not (Test-Path $exe)) { Write-Host "Нет $exe"; exit 1 }

Remove-Item $Shot -ErrorAction SilentlyContinue
Remove-Item $playerLog -ErrorAction SilentlyContinue

$runArgs = @(
    "-screen-width", "1600", "-screen-height", "900", "-screen-fullscreen", "0",
    "-shot", $Shot, "-shotDelay", $Delay, "-logFile", $playerLog
)
if ($AutoPlay) { $runArgs += "-autoPlay" }

$run = Start-Process -FilePath $exe -PassThru -ArgumentList $runArgs

if (-not $run.WaitForExit(90000)) {
    Write-Host "Плеер не закрылся за 90 с — убиваю"
    $run.Kill()
}

Start-Sleep -Milliseconds 400

if (Test-Path $Shot) {
    $info = Get-Item $Shot
    Write-Host ("СКРИНШОТ: {0} ({1} КБ)" -f $info.FullName, [math]::Round($info.Length / 1KB))
} else {
    Write-Host "Скриншота нет"
}

if (Test-Path $playerLog) {
    Write-Host "--- лог игры (последние события) ---"
    Get-Content $playerLog -Encoding UTF8 |
        Select-String -Pattern "\[Probe\]|\[Splash\]|Test\]|\[Approach\]|\[Hazard\]|\[Boot\]|Exception|error" |
        Select-Object -First 40 |
        ForEach-Object { $_.Line }
}
