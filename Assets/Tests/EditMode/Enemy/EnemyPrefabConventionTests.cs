using System;
using System.Linq;
using NUnit.Framework;
using Train.GameFlow.Data;
using Train.GameFlow.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Train.Tests.EditMode.Enemy
{
    public sealed class EnemyPrefabConventionTests
    {
        private static readonly string[] PlayableScenes =
        {
            "Assets/Scenes/Playable/Level_Combat_001.unity",
            "Assets/Scenes/Playable/Level_EnergyRelay.unity"
        };

        [Test]
        public void EnemyPrefabs_KeepRootScaleAndAlignAgentWithBody()
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Player/Player_Ellen.prefab");
            var playerInstance = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            var playerVisualHeight = MeasureVisualHeight(playerInstance);
            var playerBody = playerInstance.GetComponentInChildren<CapsuleCollider>(true);
            Assert.That(playerVisualHeight, Is.GreaterThan(0.1f));
            Assert.That(playerBody, Is.Not.Null);

            var guids = AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { "Assets/Prefabs/Enemies" });

            Assert.That(guids, Is.Not.Empty);
            try
            {
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("/Projectiles/", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    Assert.That(prefab, Is.Not.Null, path);
                    Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one), path);

                    var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    try
                    {
                        var body = instance.GetComponent<CapsuleCollider>();
                        var agent = instance.GetComponent<NavMeshAgent>();
                        var visualHeight = MeasureVisualHeight(instance);
                        var visualRatio = visualHeight / playerVisualHeight;
                        var band = GetVisualBand(instance.name);
                        Assert.That(body, Is.Not.Null, path);
                        Assert.That(agent, Is.Not.Null, path);
                        Assert.That(body.enabled, Is.True, path);
                        Assert.That(visualRatio, Is.InRange(band.x, band.y), path);
                        Assert.That(body.radius, Is.InRange(.25f, .65f), path);
                        Assert.That(body.height, Is.GreaterThan(1.1f), path);
                        Assert.That(body.bounds.size.y / playerBody.bounds.size.y,
                            Is.EqualTo(visualRatio).Within(.03f), path);
                        Assert.That(agent.radius, Is.EqualTo(body.radius).Within(.02f), path);
                        Assert.That(agent.height, Is.EqualTo(body.height).Within(.03f), path);
                        Assert.That(agent.baseOffset, Is.EqualTo(0f).Within(.02f), path);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(instance);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerInstance);
            }
        }

        private static float MeasureVisualHeight(GameObject root)
        {
            var hasBounds = false;
            var bounds = default(Bounds);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds ? bounds.size.y : 0f;
        }

        private static Vector2 GetVisualBand(string prefabName)
        {
            if (prefabName.Contains("Boss", StringComparison.Ordinal))
            {
                return new Vector2(1.4f, 1.8f);
            }

            if (prefabName.Contains("Brute", StringComparison.Ordinal) ||
                prefabName.Contains("Sentinel", StringComparison.Ordinal))
            {
                return new Vector2(1.3f, 1.4f);
            }

            if (prefabName.Contains("Elite", StringComparison.Ordinal))
            {
                return new Vector2(1.15f, 1.25f);
            }

            return new Vector2(.9f, 1.1f);
        }

        [Test]
        public void PlayableEnemySpawnPoints_AndDefinitionsHaveNoSceneScaleOverride()
        {
            var originalScenePath = SceneManager.GetActiveScene().path;
            try
            {
                foreach (var scenePath in PlayableScenes)
                {
                    var scene = EditorSceneManager.OpenScene(
                        scenePath,
                        OpenSceneMode.Single);
                    var points = UnityEngine.Object.FindObjectsByType<EnemySpawnPoint>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                        .Where(point => point.gameObject.scene == scene)
                        .ToArray();

                    Assert.That(points, Is.Not.Empty, scenePath);
                    foreach (var point in points)
                    {
                        Assert.That(point.transform.localScale, Is.EqualTo(Vector3.one), point.name);
                    }

                    var definition = AssetDatabase.LoadAssetAtPath<LevelDefinition>(
                        scenePath.Replace(
                            "Assets/Scenes/Playable/",
                            "Assets/Data/Levels/")
                            .Replace(".unity", ".asset"));
                    if (definition == null)
                    {
                        continue;
                    }

                    Assert.That(
                        definition.EnemySpawns.Select(spawn => spawn.SpawnId).Distinct().Count(),
                        Is.EqualTo(definition.EnemySpawns.Count),
                        scenePath);
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(originalScenePath))
                {
                    EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
                }
            }
        }
    }
}
