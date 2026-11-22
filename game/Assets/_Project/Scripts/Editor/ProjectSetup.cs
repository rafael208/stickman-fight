using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace StickmanFight.EditorTools
{
    /// <summary>
    /// Приводит ProjectSettings к требованиям ТЗ (раздел 7 + NFR).
    /// Всё повторяемо: на чистом клоне репозитория достаточно вызвать ConfigureAll.
    /// Каждая группа настроек изолирована — сбой одной не ломает остальные.
    /// </summary>
    public static class ProjectSetup
    {
        [MenuItem("Tools/Playground/1. Configure Project", priority = 1)]
        public static void ConfigureAll()
        {
            Step("Identity", ApplyIdentity);
            Step("Android", ApplyAndroid);
            Step("Graphics", ApplyGraphics);
            Step("Physics2D", ApplyPhysics);
            Step("Layers", ApplyLayers);
            Step("Time", ApplyTime);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] ProjectSettings применены");
        }

        private static void Step(string name, Action action)
        {
            try
            {
                action();
                Debug.Log($"[Setup] OK  {name}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Setup] FAIL {name}: {e.GetType().Name}: {e.Message}");
            }
        }

        private static void ApplyIdentity()
        {
            PlayerSettings.companyName = BuildInfo.CompanyName;
            PlayerSettings.productName = BuildInfo.ProductName;
            PlayerSettings.bundleVersion = BuildInfo.FullVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BuildInfo.ApplicationId);
            PlayerSettings.Android.bundleVersionCode = BuildInfo.BuildNumber * 100;
        }

        private static void ApplyAndroid()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)24;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)35;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            PlayerSettings.Android.renderOutsideSafeArea = false;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.forceSDCardPermission = false;
            PlayerSettings.Android.useCustomKeystore = false;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Android, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);

            PlayerSettings.gcIncremental = true;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
        }

        private static void ApplyGraphics()
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        }

        /// <summary>Physics2DSettings.asset: итерации и гравитация из NFR-PERF-08.</summary>
        private static void ApplyPhysics()
        {
            var asset = LoadProjectAsset("ProjectSettings/Physics2DSettings.asset");
            if (asset == null) throw new InvalidOperationException("Physics2DSettings.asset не найден");

            var so = new SerializedObject(asset);
            SetInt(so, "m_VelocityIterations", 8);
            SetInt(so, "m_PositionIterations", 3);
            SetFloat(so, "m_Gravity.y", -30f);
            SetFloat(so, "m_Gravity.x", 0f);
            SetBool(so, "m_QueriesHitTriggers", false);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>TimeManager.asset: фиксированный шаг 1/60 (NFR-PERF-08).</summary>
        private static void ApplyTime()
        {
            var asset = LoadProjectAsset("ProjectSettings/TimeManager.asset");
            if (asset == null) throw new InvalidOperationException("TimeManager.asset не найден");

            var so = new SerializedObject(asset);
            if (SetFloat(so, "Fixed Timestep", 1f / 60f) ||
                SetFloat(so, "m_FixedTimestep", 1f / 60f))
            {
                SetFloat(so, "Maximum Allowed Timestep", 1f / 15f);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Слои 8..15 под физические категории из ТЗ, раздел 7.5.</summary>
        private static void ApplyLayers()
        {
            var asset = LoadProjectAsset("ProjectSettings/TagManager.asset");
            if (asset == null) throw new InvalidOperationException("TagManager.asset не найден");

            var so = new SerializedObject(asset);
            var layers = so.FindProperty("layers");
            if (layers == null) throw new InvalidOperationException("свойство layers не найдено");

            for (int i = 0; i < GameLayers.Names.Length; i++)
            {
                var el = layers.GetArrayElementAtIndex(GameLayers.Ground + i);
                el.stringValue = GameLayers.Names[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static UnityEngine.Object LoadProjectAsset(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            return assets != null && assets.Length > 0 ? assets[0] : null;
        }

        private static bool SetInt(SerializedObject so, string path, int value)
        {
            var p = so.FindProperty(path);
            if (p == null) return false;
            p.intValue = value;
            return true;
        }

        private static bool SetFloat(SerializedObject so, string path, float value)
        {
            var p = so.FindProperty(path);
            if (p == null) return false;
            p.floatValue = value;
            return true;
        }

        private static bool SetBool(SerializedObject so, string path, bool value)
        {
            var p = so.FindProperty(path);
            if (p == null) return false;
            p.boolValue = value;
            return true;
        }
    }
}
