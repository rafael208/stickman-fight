using System.IO;
using UnityEditor;
using UnityEngine;
using StickmanFight.Characters;

namespace StickmanFight.EditorTools
{
    /// <summary>
    /// Создаёт ScriptableObject-конфиги (ADR-4).
    /// Важно: ассет не пересоздаётся (GUID должен сохраняться, иначе сцены теряют ссылку),
    /// а значения сбрасываются в дефолтные через CopySerialized.
    /// </summary>
    public static class ConfigBuilder
    {
        public const string ConfigsDir = "Assets/_Project/Configs";
        public const string CharacterPath = ConfigsDir + "/Character_Default.asset";

        [MenuItem("Tools/Playground/5. Ensure Configs", priority = 5)]
        public static void EnsureAll() => EnsureConfig(true);

        /// <param name="resetToDefaults">true — вернуть значения из кода (полезно на этапе балансировки).</param>
        public static CharacterConfig EnsureConfig(bool resetToDefaults = true)
        {
            Directory.CreateDirectory(ConfigsDir);
            AssetDatabase.Refresh();

            var existing = AssetDatabase.LoadAssetAtPath<CharacterConfig>(CharacterPath);

            if (existing == null)
            {
                var created = ScriptableObject.CreateInstance<CharacterConfig>();
                AssetDatabase.CreateAsset(created, CharacterPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Config] создан {CharacterPath}");
                return AssetDatabase.LoadAssetAtPath<CharacterConfig>(CharacterPath);
            }

            if (!resetToDefaults) return existing;

            // Значения по умолчанию живут в полях класса; здесь переносим их в ассет, не меняя GUID.
            var fresh = ScriptableObject.CreateInstance<CharacterConfig>();
            EditorUtility.CopySerialized(fresh, existing);
            Object.DestroyImmediate(fresh);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Config] значения сброшены к дефолтным: {CharacterPath}");
            return existing;
        }
    }
}
