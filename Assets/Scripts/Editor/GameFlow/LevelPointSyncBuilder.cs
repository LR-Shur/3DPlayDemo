#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Train.Architecture.Assets;
using Train.GameFlow.Data;
using Train.GameFlow.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

            MigrateLegacyMarkers(scene);
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
            MigrateLegacyMarkers(scene);
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
            serialized.FindProperty("_sceneLocation").stringValue = scene.path;

            var spawns = serialized.FindProperty("_enemySpawns");
            var points = FindEnemyPoints(scene);
            spawns.arraySize = points.Count;
            for (var index = 0; index < points.Count; index++)
            {
                var point = points[index];
                var spawn = spawns.GetArrayElementAtIndex(index);
                spawn.FindPropertyRelative("_spawnId").stringValue =
                    point.SpawnId;
                spawn.FindPropertyRelative("_archetypeId").stringValue =
                    point.ArchetypeId;
                spawn.FindPropertyRelative("_prefabLocation").stringValue =
                    ResolvePrefabLocation(point);
                spawn.FindPropertyRelative("_position").vector3Value =
                    point.transform.position;
                spawn.FindPropertyRelative("_eulerAngles").vector3Value =
                    point.transform.eulerAngles;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        private static string ResolvePrefabLocation(EnemySpawnPoint point)
        {
            if (point.EnemyPrefab != null)
            {
                var path = AssetDatabase.GetAssetPath(point.EnemyPrefab);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    return path;
                }
            }

            if (!string.IsNullOrWhiteSpace(point.PrefabLocation))
            {
                return point.PrefabLocation;
            }

            throw new InvalidOperationException(
                $"敌人点位 '{point.name}' 没有拖入敌人 Prefab。");
        }

        private static void MigrateLegacyMarkers(Scene scene)
        {
            var runtime = FindSceneComponent<LevelRuntimeController>(scene);
            var spawnsRoot = runtime != null
                ? runtime.transform.Find("Spawns")
                : null;
            if (spawnsRoot == null)
            {
                return;
            }

            var defaultPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                AssetLocations.KnightEnemyPrefab);
            for (var index = 0; index < spawnsRoot.childCount; index++)
            {
                var child = spawnsRoot.GetChild(index);
                if (child.name.StartsWith("EnemySpawn_", StringComparison.Ordinal))
                {
                    var point = child.GetComponent<EnemySpawnPoint>();
                    if (point == null)
                    {
                        point = child.gameObject.AddComponent<EnemySpawnPoint>();
                        point.Configure(
                            child.name.Replace("EnemySpawn_", "knight_"),
                            "kaykit_knight",
                            defaultPrefab,
                            AssetLocations.KnightEnemyPrefab);
                        EditorUtility.SetDirty(point);
                    }
                }
                else if (child.name == "PlayerSpawn" &&
                         child.GetComponent<PlayerSpawnPoint>() == null)
                {
                    child.gameObject.AddComponent<PlayerSpawnPoint>();
                    EditorUtility.SetDirty(child.gameObject);
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
