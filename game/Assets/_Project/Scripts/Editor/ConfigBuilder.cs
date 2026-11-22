using System.IO;
using UnityEditor;
using UnityEngine;
using StickmanFight.Characters;
using StickmanFight.Weapons;

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
        public static void EnsureAll()
        {
            EnsureConfig(true);
            EnsureWeapon(WeaponKind.Sword);
            EnsureWeapon(WeaponKind.Hammer);
            EnsureWeapon(WeaponKind.Pistol);
        }

        public static WeaponConfig EnsureWeapon(WeaponKind kind)
        {
            Directory.CreateDirectory(ConfigsDir);
            AssetDatabase.Refresh();

            string path = $"{ConfigsDir}/Weapon_{kind}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<WeaponConfig>(path);
            if (existing == null)
            {
                var created = ScriptableObject.CreateInstance<WeaponConfig>();
                AssetDatabase.CreateAsset(created, path);
                existing = AssetDatabase.LoadAssetAtPath<WeaponConfig>(path);
            }

            var fresh = ScriptableObject.CreateInstance<WeaponConfig>();
            ApplyWeaponDefaults(fresh, kind);
            EditorUtility.CopySerialized(fresh, existing);
            Object.DestroyImmediate(fresh);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            return existing;
        }

        /// <summary>Значения из раздела 8.2 ТЗ.</summary>
        private static void ApplyWeaponDefaults(WeaponConfig c, WeaponKind kind)
        {
            c.kind = kind;
            switch (kind)
            {
                case WeaponKind.Sword:
                    c.displayName = "Меч";
                    c.damage = 18f; c.refSpeed = 4f; c.impulse = 6f;
                    c.cooldown = 0.45f; c.attackDuration = 0.28f; c.hitTime = 0.12f;
                    c.range = 0.95f; c.arc = 100f;
                    c.mass = 2f; c.visualSize = new Vector2(0.9f, 0.08f);
                    c.color = Hex("#E0E0E0");
                    break;
                case WeaponKind.Hammer:
                    c.displayName = "Молот";
                    c.damage = 26f; c.refSpeed = 3.5f; c.impulse = 14f;
                    c.cooldown = 0.9f; c.attackDuration = 0.42f; c.hitTime = 0.22f;
                    c.range = 0.8f; c.arc = 70f;
                    c.mass = 6f; c.visualSize = new Vector2(0.85f, 0.12f);
                    c.color = Hex("#C8C8C8");
                    break;
                case WeaponKind.Pistol:
                    c.displayName = "Пистолет";
                    c.damage = 12f; c.impulse = 2f;
                    c.cooldown = 0.35f; c.attackDuration = 0.15f; c.hitTime = 0.05f;
                    c.ammo = 6; c.reloadTime = 1.4f;
                    c.projectileSpeed = 22f; c.projectileLifetime = 2f;
                    c.mass = 1.5f; c.visualSize = new Vector2(0.45f, 0.12f);
                    c.color = Hex("#FFD54F");
                    break;
            }
        }

        private static Color Hex(string hex) =>
            ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;

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
            return existing;
        }
    }
}
