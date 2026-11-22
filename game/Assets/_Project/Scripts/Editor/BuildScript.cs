using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace StickmanFight.EditorTools
{
    /// <summary>
    /// Сборка из командной строки — единственный поддерживаемый способ (NFR-CODE-04).
    /// Пример:
    ///   Unity.exe -batchmode -quit -nographics -projectPath &lt;game&gt; -buildTarget Android
    ///             -executeMethod StickmanFight.EditorTools.BuildScript.BuildApk -logFile &lt;log&gt;
    /// </summary>
    public static class BuildScript
    {
        private const string BuildsFolderName = "builds";

        [MenuItem("Tools/Playground/3. Build APK", priority = 3)]
        public static void BuildApk() => Build(false);

        [MenuItem("Tools/Playground/4. Build AAB", priority = 4)]
        public static void BuildAab() => Build(true);

        /// <summary>
        /// Сборка под Windows для быстрой проверки без телефона (Mono, ~30 с вместо IL2CPP).
        /// Запускать так:
        ///   builds\win\StickmanFight.exe -screen-width 1280 -screen-height 720 -screen-fullscreen 0 ^
        ///       -shot C:\path\shot.png -shotDelay 3 -logFile C:\path\player.log
        /// </summary>
        [MenuItem("Tools/Playground/6. Build Windows (PC test)", priority = 6)]
        public static void BuildWindows()
        {
            int exitCode = 1;
            try
            {
                ConfigureAndroidPaths();
                ProjectSetup.ConfigureAll();
                var scenes = SceneBuilder.BuildAll();

                string gameRoot = Directory.GetParent(Application.dataPath)!.FullName;
                string repoRoot = Directory.GetParent(gameRoot)?.FullName ?? gameRoot;
                string outDir = Path.Combine(repoRoot, BuildsFolderName, "win");
                Directory.CreateDirectory(outDir);
                string outputPath = Path.Combine(outDir, "StickmanFight.exe");

                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);

                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outputPath,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                };

                Debug.Log($"[Build] старт Windows: {outputPath}");
                BuildReport report = BuildPipeline.BuildPlayer(options);

                if (report.summary.result == BuildResult.Succeeded)
                {
                    Debug.Log($"[Build] Windows OK {outputPath} | {report.summary.totalSize / (1024f * 1024f):0.0} MB | {report.summary.totalTime:mm\\:ss}");
                    WriteResult("OK-WIN", outputPath, report.summary.totalSize);
                    exitCode = 0;
                }
                else
                {
                    Debug.LogError($"[Build] Windows FAILED {report.summary.result}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Build] Windows ИСКЛЮЧЕНИЕ: {e}");
            }
            finally
            {
                if (Application.isBatchMode) EditorApplication.Exit(exitCode);
            }
        }

        /// <summary>Прописывает пути к SDK/NDK/JDK, чтобы сборка не зависела от кликов в GUI.</summary>
        [MenuItem("Tools/Playground/0. Configure Android Paths", priority = 0)]
        public static void ConfigureAndroidPaths()
        {
            string androidPlayer = Path.Combine(
                Path.GetDirectoryName(EditorApplication.applicationPath) ?? string.Empty,
                "Data", "PlaybackEngines", "AndroidPlayer");

            SetIfExists("AndroidSdkRoot", Path.Combine(androidPlayer, "SDK"));
            SetIfExists("AndroidNdkRoot", Path.Combine(androidPlayer, "NDK"));
            SetIfExists("JdkPath", Path.Combine(androidPlayer, "OpenJDK"));
            Debug.Log($"[Build] Android paths -> {androidPlayer}");
        }

        private static void SetIfExists(string key, string path)
        {
            if (Directory.Exists(path))
            {
                EditorPrefs.SetString(key, path);
            }
            else
            {
                Debug.LogWarning($"[Build] нет каталога {path} (ключ {key})");
            }
        }

        private static void Build(bool appBundle)
        {
            int exitCode = 1;
            try
            {
                ConfigureAndroidPaths();
                ProjectSetup.ConfigureAll();
                var scenes = SceneBuilder.BuildAll();

                string gameRoot = Directory.GetParent(Application.dataPath)!.FullName;
                string repoRoot = Directory.GetParent(gameRoot)?.FullName ?? gameRoot;
                string outDir = Path.Combine(repoRoot, BuildsFolderName);
                Directory.CreateDirectory(outDir);

                string ext = appBundle ? "aab" : "apk";
                string outputPath = Path.Combine(outDir, $"StickmanFight_v{BuildInfo.FullVersion}.{ext}");

                EditorUserBuildSettings.buildAppBundle = appBundle;

                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outputPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                };

                Debug.Log($"[Build] старт: {outputPath}");
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;

                if (summary.result == BuildResult.Succeeded)
                {
                    var fi = new FileInfo(outputPath);
                    Debug.Log($"[Build] OK {outputPath} | {summary.totalSize / (1024f * 1024f):0.0} MB | {summary.totalTime:mm\\:ss}");
                    WriteResult("OK", outputPath, summary.totalSize);
                    exitCode = 0;
                }
                else
                {
                    Debug.LogError($"[Build] FAILED {summary.result} | ошибок: {summary.totalErrors}");
                    WriteResult("FAILED", outputPath, 0);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Build] ИСКЛЮЧЕНИЕ: {e}");
            }
            finally
            {
                if (Application.isBatchMode) EditorApplication.Exit(exitCode);
            }
        }

        /// <summary>Маленький отчёт для скриптов-обёрток: не нужно парсить лог Unity.</summary>
        private static void WriteResult(string status, string path, ulong bytes)
        {
            try
            {
                string repoRoot = Directory.GetParent(Directory.GetParent(Application.dataPath)!.FullName)?.FullName
                                  ?? Application.dataPath;
                string logs = Path.Combine(repoRoot, "_logs");
                Directory.CreateDirectory(logs);
                File.WriteAllText(Path.Combine(logs, "build-result.txt"),
                    $"{status}|{path}|{bytes}|{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Build] не удалось записать результат: {e.Message}");
            }
        }
    }
}
