#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Train.GameFlow.Data;
using Train.GameFlow.Runtime;
using Train.Gameplay.Combat;
using Train.Gameplay.Enemy.Core;
using Train.Gameplay.Enemy.Movement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Train.Tests.EditMode.GameFlow
{
    public sealed class CanonicalEnemyArchetypeTests
    {
        private const string CsvPath =
            "Assets/Config/Luban/Data/enemy_archetypes.csv";
        private const string LevelFolder = "Assets/Data/Levels";
        private const string SceneFolder = "Assets/Scenes/Playable";

        private static readonly Dictionary<string, int> HeadSpawnCounts =
            new(StringComparer.Ordinal)
            {
                ["Level_Boss_001"] = 3,
                ["Level_Combat_001"] = 3,
                ["Level_Combat_002"] = 6,
                ["Level_CryoGarden"] = 6,
                ["Level_EnergyRelay"] = 4,
                ["Level_OrbitalCargo"] = 6,
                ["MainScenes"] = 5,
            };

        [Test]
        public void CanonicalEnemyPrefabs_HaveRequiredRuntimeComponents()
        {
            var canonical = LoadCanonicalMap();
            foreach (var pair in canonical)
            {
                if (pair.Key == "training_dummy")
                {
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    pair.Value);
                Assert.That(
                    prefab,
                    Is.Not.Null,
                    $"Missing canonical prefab for {pair.Key}: {pair.Value}");
                Assert.That(
                    prefab.GetComponent<Health>(),
                    Is.Not.Null,
                    $"{pair.Key} is missing Health.");
                Assert.That(
                    prefab.GetComponent<EnemyController>(),
                    Is.Not.Null,
                    $"{pair.Key} is missing EnemyController.");
                Assert.That(
                    prefab.GetComponent<EnemyMotor>(),
                    Is.Not.Null,
                    $"{pair.Key} is missing EnemyMotor.");
                Assert.That(
                    prefab.GetComponentInChildren<NavMeshAgent>(true),
                    Is.Not.Null,
                    $"{pair.Key} is missing NavMeshAgent.");
            }
        }

        [Test]
        public void LevelDefinitions_UseCanonicalPrefabLocations()
        {
            var canonical = LoadCanonicalMap();
            var guids = AssetDatabase.FindAssets(
                "t:LevelDefinition",
                new[] { LevelFolder });
            Assert.That(guids, Is.Not.Empty);

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                Assert.That(level, Is.Not.Null, path);
                foreach (var spawn in level.EnemySpawns)
                {
                    Assert.That(
                        canonical.ContainsKey(spawn.ArchetypeId),
                        Is.True,
                        $"{path}:{spawn.SpawnId} references unknown " +
                        $"archetype '{spawn.ArchetypeId}'.");
                    Assert.That(
                        Normalize(spawn.PrefabLocation),
                        Is.EqualTo(canonical[spawn.ArchetypeId]),
                        $"{path}:{spawn.SpawnId} does not use its canonical " +
                        "prefab location.");
                }
            }
        }

        [Test]
        public void LevelDefinitions_KeepHeadSpawnCounts()
        {
            var guids = AssetDatabase.FindAssets(
                "t:LevelDefinition",
                new[] { LevelFolder });
            Assert.That(guids, Is.Not.Empty);

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                Assert.That(level, Is.Not.Null, path);
                Assert.That(
                    level.EnemySpawns,
                    Is.Not.Empty,
                    $"{path} must keep at least one enemy spawn.");

                if (HeadSpawnCounts.TryGetValue(
                        level.name,
                        out var expectedCount))
                {
                    Assert.That(
                        level.EnemySpawns.Count,
                        Is.EqualTo(expectedCount),
                        $"{path} changed its HEAD spawn count.");
                }
            }
        }

        [Test]
        public void PlayableScenes_HaveNoStaticEnemyControllerCopies()
        {
            WithPlayableScenes((scene, _) =>
            {
                var enemies = UnityEngine.Object.FindObjectsByType<EnemyController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                foreach (var enemy in enemies)
                {
                    Assert.That(
                        enemy.gameObject.scene,
                        Is.Not.EqualTo(scene),
                        $"Scene {scene.path} contains a static enemy copy: " +
                        enemy.name);
                }
            });
        }

        [Test]
        public void PlayableScenes_HaveBuiltNavMesh()
        {
            WithPlayableScenes((scene, path) =>
            {
                Assert.That(
                    NavMesh.CalculateTriangulation().vertices,
                    Is.Not.Empty,
                    $"{path} has no usable NavMesh polygons.");
            });
        }

        [Test]
        public void SceneEnemySpawnPoints_UseCanonicalArchetypes()
        {
            var canonical = LoadCanonicalMap();
            WithPlayableScenes((scene, _) =>
            {
                var points = UnityEngine.Object.FindObjectsByType<EnemySpawnPoint>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                foreach (var point in points)
                {
                    if (point.gameObject.scene != scene)
                    {
                        continue;
                    }

                    Assert.That(
                        canonical.ContainsKey(point.ArchetypeId),
                        Is.True,
                        $"{scene.path}:{point.name} references unknown " +
                        $"archetype '{point.ArchetypeId}'.");
                    Assert.That(
                        Normalize(point.PrefabLocation),
                        Is.EqualTo(canonical[point.ArchetypeId]),
                        $"{scene.path}:{point.name} has a non-canonical " +
                        "prefab location.");
                    Assert.That(
                        AssetDatabase.GetAssetPath(point.EnemyPrefab),
                        Is.EqualTo(canonical[point.ArchetypeId]),
                        $"{scene.path}:{point.name} has a non-canonical " +
                        "prefab reference.");
                }
            });
        }

        private static Dictionary<string, string> LoadCanonicalMap()
        {
            var csv = AssetDatabase.LoadAssetAtPath<TextAsset>(CsvPath);
            Assert.That(csv, Is.Not.Null, $"Missing CSV: {CsvPath}");

            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            var lines = csv.text.Split(
                new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (!line.StartsWith(",", StringComparison.Ordinal))
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

                result.Add(columns[1].Trim(), Normalize(columns[3]));
            }

            return result;
        }

        private static void WithPlayableScenes(
            Action<Scene, string> assertion)
        {
            var activePath = SceneManager.GetActiveScene().path;
            var sceneGuids = AssetDatabase.FindAssets(
                "t:Scene",
                new[] { SceneFolder });
            try
            {
                foreach (var guid in sceneGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var scene = EditorSceneManager.OpenScene(
                        path,
                        OpenSceneMode.Single);
                    assertion(scene, path);
                }
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(activePath) &&
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(activePath) != null)
                {
                    EditorSceneManager.OpenScene(
                        activePath,
                        OpenSceneMode.Single);
                }
            }
        }

        private static string Normalize(string path)
        {
            return path?.Trim().Replace('\\', '/') ?? string.Empty;
        }
    }
}
#endif
