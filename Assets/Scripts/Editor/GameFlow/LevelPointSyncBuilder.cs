#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Train.Architecture.Assets;
using Train.GameFlow.Data;
using Train.GameFlow.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Train.Gameplay.Enemy.Core;

namespace Train.EditorTools.GameFlow
{
    /// <summary>
    /// 关卡点位编辑工具：创建点位预制体，并把当前场景中的点位同步成 LevelDefinition。
    /// </summary>
    public static class LevelPointSyncBuilder
    {
        private const string PrefabFolder = "Assets/Prefabs/Level";
        private const string LevelDataFolder = "Assets/Data/Levels";

        private const string EnemyPointPrefabPath =
            PrefabFolder + "/EnemySpawnPoint.prefab";
        private const string PlayerPointPrefabPath =
            PrefabFolder + "/PlayerSpawnPoint.prefab";
        private const string LevelRootPrefabPath =
            PrefabFolder + "/LevelRuntimeRoot.prefab";

        /// <summary>创建可直接拖入场景的出生点预制体。</summary>
        [MenuItem("Tools/Train/Level Design/Create Spawn Point Prefabs")]
        public static void CreateSpawnPointPrefabs()
        {
            EnsureFolder(PrefabFolder);
            EnsureSpawnPointPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Level spawn point prefabs are ready.");
        }

        private static void EnsureSpawnPointPrefabs()
        {
            EnsureFolder(PrefabFolder);
            CreatePrefabIfMissing<EnemySpawnPoint>(
                EnemyPointPrefabPath,
                "EnemySpawnPoint");
            CreatePrefabIfMissing<PlayerSpawnPoint>(
                PlayerPointPrefabPath,
                "PlayerSpawnPoint");
            CreateLevelRootPrefabIfMissing();
        }

        /// <summary>
        /// 扫描当前场景的出生点，并生成或更新同名关卡 SO。
        /// </summary>
        [MenuItem("Tools/Train/Level Design/Sync Current Scene To Level Definition")]
        public static void SyncCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || string.IsNullOrWhiteSpace(scene.path))
            {
                throw new InvalidOperationException(
                    "请先打开一个已保存的关卡场景。");
            }

            EnsureSpawnPointPrefabs();
            var runtime = FindSceneComponent<LevelRuntimeController>(scene);
            var definition = runtime != null ? runtime.Definition : null;
            if (definition == null)
            {
                EnsureFolder(LevelDataFolder);
                var path =
                    $"{LevelDataFolder}/{scene.name}.asset";
                definition = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<LevelDefinition>();
                    AssetDatabase.CreateAsset(definition, path);
                }
            }

            SyncDefinition(scene, definition);

            if (runtime != null)
            {
                SetObjectReference(runtime, "_definition", definition);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"关卡配置同步完成：{definition.name}，" +
                $"敌人点位 {FindEnemyPoints(scene).Count} 个。");
        }

        /// <summary>
        /// 统一所有可游玩关卡的敌人兼容字段，并移除场景内残留的敌人副本。
        /// </summary>
        [MenuItem("Tools/Train/Level Design/Normalize All Enemy Archetypes")]
        public static void NormalizeAllEnemyArchetypes()
        {
            var activeScenePath = SceneManager.GetActiveScene().path;
            NormalizeLevelDefinitions();

            foreach (var guid in AssetDatabase.FindAssets(
                         "t:Scene",
                         new[] { "Assets/Scenes/Playable" }))
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Single);
                var definition = FindDefinition(scene);
                if (definition == null)
                {
                    continue;
                }

                NormalizeSceneEnemyPoints(scene, definition);
                RemoveSceneEnemyCopies(scene);
                EditorSceneManager.SaveScene(scene, scenePath);
            }

            if (!string.IsNullOrWhiteSpace(activeScenePath) &&
                File.Exists(activeScenePath))
            {
                EditorSceneManager.OpenScene(
                    activeScenePath,
                    OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("All playable enemy archetypes were normalized.");
        }

        /// <summary>供 PlayableCombatSliceBuilder 在生成场景后调用。</summary>
        public static void SyncScene(
            Scene scene,
            LevelDefinition definition)
        {
            if (!scene.IsValid() || definition == null)
            {
                throw new ArgumentException(
                    "同步关卡点位需要有效场景和关卡定义。");
            }

            EnsureSpawnPointPrefabs();
            SyncDefinition(scene, definition);
            EditorUtility.SetDirty(definition);
        }

        private static void SyncDefinition(
            Scene scene,
            LevelDefinition definition)
        {
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("_levelId").stringValue =
                string.IsNullOrWhiteSpace(
                    serialized.FindProperty("_levelId").stringValue)
                    ? $"level.{scene.name.ToLowerInvariant()}"
                    : serialized.FindProperty("_levelId").stringValue;
            var sceneLocation = serialized.FindProperty("_sceneLocation");
            if (string.IsNullOrWhiteSpace(sceneLocation.stringValue))
            {
                sceneLocation.stringValue = scene.path;
            }

            var spawns = serialized.FindProperty("_enemySpawns");
            var previousArchetypes = ReadPreviousArchetypes(spawns);
            var points = FindEnemyPoints(scene);
            var existingCount = spawns.arraySize;
            var preserveExistingSpawns = points.Count < existingCount;
            var existingIndexes = ReadSpawnIndexes(spawns);
            if (!preserveExistingSpawns)
            {
                spawns.arraySize = points.Count;
            }

            var usedIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < points.Count; index++)
            {
                var point = points[index];
                var spawnId = point.SpawnId;
                if (string.IsNullOrWhiteSpace(spawnId) ||
                    !usedIds.Add(spawnId))
                {
                    spawnId = $"{point.ArchetypeId}_{index + 1:00}";
                    point.Configure(
                        spawnId,
                        point.ArchetypeId,
                        point.EnemyPrefab,
                        point.PrefabLocation);
                    EditorUtility.SetDirty(point);
                    usedIds.Add(spawnId);
                }

                var archetypeId = ResolveArchetypeId(
                    point,
                    previousArchetypes,
                    spawnId);
                if (!EnemyArchetypeEditorCatalog.TryGet(
                        archetypeId,
                        out var prefabLocation))
                {
                    throw new InvalidOperationException(
                        $"Enemy spawn '{spawnId}' references unknown archetype " +
                        $"'{archetypeId}'.");
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    prefabLocation);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Canonical enemy prefab '{prefabLocation}' for " +
                        $"archetype '{archetypeId}' cannot be loaded.");
                }

                point.Configure(
                    spawnId,
                    archetypeId,
                    prefab,
                    prefabLocation);
                EditorUtility.SetDirty(point);

                var spawnIndex = index;
                if (preserveExistingSpawns &&
                    !existingIndexes.TryGetValue(spawnId, out spawnIndex))
                {
                    continue;
                }

                var spawn = spawns.GetArrayElementAtIndex(spawnIndex);

                spawn.FindPropertyRelative("_spawnId").stringValue =
                    spawnId;
                spawn.FindPropertyRelative("_archetypeId").stringValue =
                    archetypeId;
                spawn.FindPropertyRelative("_prefabLocation").stringValue =
                    prefabLocation;
                spawn.FindPropertyRelative("_position").vector3Value =
                    point.transform.position;
                spawn.FindPropertyRelative("_eulerAngles").vector3Value =
                    point.transform.eulerAngles;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        private static void NormalizeSceneEnemyPoints(
            Scene scene,
            LevelDefinition definition)
        {
            var serialized = new SerializedObject(definition);
            var previousArchetypes = ReadPreviousArchetypes(
                serialized.FindProperty("_enemySpawns"));
            foreach (var point in FindEnemyPoints(scene))
            {
                var spawnId = point.SpawnId;
                var archetypeId = ResolveArchetypeId(
                    point,
                    previousArchetypes,
                    spawnId);
                if (!EnemyArchetypeEditorCatalog.TryGet(
                        archetypeId,
                        out var prefabLocation))
                {
                    throw new InvalidOperationException(
                        $"Enemy spawn '{spawnId}' references unknown archetype " +
                        $"'{archetypeId}'.");
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    prefabLocation);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Canonical enemy prefab '{prefabLocation}' for " +
                        $"archetype '{archetypeId}' cannot be loaded.");
                }

                point.Configure(
                    spawnId,
                    archetypeId,
                    prefab,
                    prefabLocation);
                EditorUtility.SetDirty(point);
            }
        }

        private static string ResolveArchetypeId(
            EnemySpawnPoint point,
            IReadOnlyDictionary<string, string> previousArchetypes,
            string spawnId)
        {
            if (spawnId.IndexOf(
                    "sentinel",
                    StringComparison.OrdinalIgnoreCase) >= 0 &&
                EnemyArchetypeEditorCatalog.TryGet(
                    "overload_sentinel",
                    out _))
            {
                return "overload_sentinel";
            }

            var prefabPath = point.EnemyPrefab != null
                ? AssetDatabase.GetAssetPath(point.EnemyPrefab)
                : point.PrefabLocation;
            if (EnemyArchetypeEditorCatalog.TryGetArchetypeByPrefab(
                    prefabPath,
                    out var prefabArchetype))
            {
                return prefabArchetype;
            }

            if (EnemyArchetypeEditorCatalog.TryGet(
                    point.ArchetypeId,
                    out _))
            {
                return point.ArchetypeId;
            }

            if (previousArchetypes.TryGetValue(
                    spawnId,
                    out var previousArchetype) &&
                EnemyArchetypeEditorCatalog.TryGet(
                    previousArchetype,
                    out _))
            {
                return previousArchetype;
            }

            throw new InvalidOperationException(
                $"敌人点位 '{point.name}' 无法解析 canonical archetype。");
        }

        private static Dictionary<string, string> ReadPreviousArchetypes(
            SerializedProperty spawns)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 0; index < spawns.arraySize; index++)
            {
                var spawn = spawns.GetArrayElementAtIndex(index);
                var spawnId = spawn.FindPropertyRelative("_spawnId").stringValue;
                var archetypeId = spawn.FindPropertyRelative("_archetypeId").stringValue;
                if (!string.IsNullOrWhiteSpace(spawnId) &&
                    !result.ContainsKey(spawnId))
                {
                    result.Add(spawnId, archetypeId);
                }
            }

            return result;
        }

        private static Dictionary<string, int> ReadSpawnIndexes(
            SerializedProperty spawns)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var index = 0; index < spawns.arraySize; index++)
            {
                var spawnId = spawns
                    .GetArrayElementAtIndex(index)
                    .FindPropertyRelative("_spawnId")
                    .stringValue;
                if (!string.IsNullOrWhiteSpace(spawnId) &&
                    !result.ContainsKey(spawnId))
                {
                    result.Add(spawnId, index);
                }
            }

            return result;
        }

        private static void NormalizeLevelDefinitions()
        {
            foreach (var guid in AssetDatabase.FindAssets(
                         "t:LevelDefinition",
                         new[] { LevelDataFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var definition = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if (definition == null)
                {
                    continue;
                }

                var serialized = new SerializedObject(definition);
                var spawns = serialized.FindProperty("_enemySpawns");
                for (var index = 0; index < spawns.arraySize; index++)
                {
                    var spawn = spawns.GetArrayElementAtIndex(index);
                    var spawnId = spawn.FindPropertyRelative("_spawnId").stringValue;
                    var archetypeId = spawn.FindPropertyRelative("_archetypeId").stringValue;
                    if (spawnId.IndexOf(
                            "sentinel",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        archetypeId = "overload_sentinel";
                    }

                    if (!EnemyArchetypeEditorCatalog.TryGet(
                            archetypeId,
                            out var prefabLocation))
                    {
                        throw new InvalidOperationException(
                            $"Level '{definition.name}' spawn '{spawnId}' references " +
                            $"unknown archetype '{archetypeId}'.");
                    }

                    spawn.FindPropertyRelative("_archetypeId").stringValue = archetypeId;
                    spawn.FindPropertyRelative("_prefabLocation").stringValue = prefabLocation;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(definition);
            }
        }

        private static LevelDefinition FindDefinition(Scene scene)
        {
            foreach (var guid in AssetDatabase.FindAssets(
                         "t:LevelDefinition",
                         new[] { LevelDataFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var definition = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if (definition != null &&
                    MatchesSceneLocation(definition.SceneLocation, scene))
                {
                    return definition;
                }
            }

            return null;
        }

        private static bool MatchesSceneLocation(
            string sceneLocation,
            Scene scene)
        {
            if (string.Equals(
                    sceneLocation,
                    scene.path,
                    StringComparison.Ordinal))
            {
                return true;
            }

            return string.Equals(
                sceneLocation,
                "Playable_" + scene.name,
                StringComparison.Ordinal);
        }

        private static void RemoveSceneEnemyCopies(Scene scene)
        {
            foreach (var enemy in UnityEngine.Object.FindObjectsByType<EnemyController>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (enemy != null && enemy.gameObject.scene == scene)
                {
                    UnityEngine.Object.DestroyImmediate(enemy.gameObject);
                }
            }
        }

        private static List<EnemySpawnPoint> FindEnemyPoints(Scene scene)
        {
            var result = new List<EnemySpawnPoint>();
            foreach (var point in UnityEngine.Object.FindObjectsByType<EnemySpawnPoint>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (point.gameObject.scene == scene)
                {
                    result.Add(point);
                }
            }

            result.Sort((left, right) =>
                string.CompareOrdinal(left.SpawnId, right.SpawnId));
            return result;
        }

        private static T FindSceneComponent<T>(Scene scene)
            where T : Component
        {
            foreach (var component in UnityEngine.Object.FindObjectsByType<T>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (component.gameObject.scene == scene)
                {
                    return component;
                }
            }

            return null;
        }

        private static void CreatePrefabIfMissing<T>(
            string path,
            string name)
            where T : Component
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                return;
            }

            var temporary = new GameObject(name);
            temporary.AddComponent<T>();
            PrefabUtility.SaveAsPrefabAsset(temporary, path);
            UnityEngine.Object.DestroyImmediate(temporary);
        }

        private static void CreateLevelRootPrefabIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(LevelRootPrefabPath) != null)
            {
                return;
            }

            var root = new GameObject("LevelRuntimeRoot");
            root.AddComponent<Train.Architecture.Bootstrap.SceneBootstrap>();
            root.AddComponent<LevelRuntimeController>();
            new GameObject("Spawns").transform.SetParent(root.transform, false);
            new GameObject("SpawnedActors").transform.SetParent(root.transform, false);
            PrefabUtility.SaveAsPrefabAsset(root, LevelRootPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void SetObjectReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name} 没有属性 {propertyName}。");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var slash = path.LastIndexOf('/');
            var parent = path.Substring(0, slash);
            var name = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
