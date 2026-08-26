#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Train.EditorTools.GameFlow
{
    /// <summary>
    /// 编辑器侧读取 enemy_archetypes.csv 的最小目录。
    /// 运行时仍以 IEnemyArchetypeProvider 为唯一真源；这里仅用于同步场景兼容字段。
    /// </summary>
    internal static class EnemyArchetypeEditorCatalog
    {
        private const string CsvPath =
            "Assets/Config/Luban/Data/enemy_archetypes.csv";

        private static Dictionary<string, string> _prefabByArchetype;
        private static Dictionary<string, string> _archetypeByPrefab;

        public static bool TryGet(
            string archetypeId,
            out string prefabLocation)
        {
            EnsureLoaded();
            return _prefabByArchetype.TryGetValue(
                archetypeId ?? string.Empty,
                out prefabLocation);
        }

        public static IReadOnlyDictionary<string, string> Entries
        {
            get
            {
                EnsureLoaded();
                return _prefabByArchetype;
            }
        }

        public static bool TryGetArchetypeByPrefab(
            string prefabLocation,
            out string archetypeId)
        {
            EnsureLoaded();
            return _archetypeByPrefab.TryGetValue(
                Normalize(prefabLocation),
                out archetypeId);
        }

        private static void EnsureLoaded()
        {
            if (_prefabByArchetype != null)
            {
                return;
            }

            var csv = AssetDatabase.LoadAssetAtPath<TextAsset>(CsvPath);
            if (csv == null)
            {
                throw new InvalidOperationException(
                    $"Canonical enemy archetype table is missing: {CsvPath}");
            }

            _prefabByArchetype = new Dictionary<string, string>(
                StringComparer.Ordinal);
            _archetypeByPrefab = new Dictionary<string, string>(
                StringComparer.Ordinal);
            var lines = csv.text.Split(
                new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith("##", StringComparison.Ordinal) ||
                    !line.StartsWith(",", StringComparison.Ordinal))
                {
                    continue;
                }

                var columns = line.Split(',');
                if (columns.Length < 4 ||
                    string.IsNullOrWhiteSpace(columns[1]) ||
                    string.IsNullOrWhiteSpace(columns[3]))
                {
                    continue;
                }

                var archetypeId = columns[1].Trim();
                var prefabLocation = Normalize(columns[3].Trim());
                if (_prefabByArchetype.ContainsKey(archetypeId))
                {
                    throw new InvalidOperationException(
                        $"Duplicate canonical enemy archetype '{archetypeId}'.");
                }

                if (_archetypeByPrefab.ContainsKey(prefabLocation))
                {
                    throw new InvalidOperationException(
                        $"Prefab '{prefabLocation}' is used by multiple enemy archetypes.");
                }

                _prefabByArchetype.Add(archetypeId, prefabLocation);
                _archetypeByPrefab.Add(prefabLocation, archetypeId);
            }
        }

        private static string Normalize(string location)
        {
            return location?.Trim().Replace('\\', '/') ?? string.Empty;
        }
    }
}
#endif
