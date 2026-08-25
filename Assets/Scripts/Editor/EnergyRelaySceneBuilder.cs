#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Train.GameFlow.Data;
using Train.GameFlow.Runtime;
using Train.Gameplay.Combat;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Train.EditorTools.GameFlow
{
    /// <summary>
    /// 一次性重建能源中继站的场景结构；完成后由 EditorPrefs 防止重复执行。
    /// </summary>
    [InitializeOnLoad]
    public static class EnergyRelaySceneBuilder
    {
        private const string ScenePath =
            "Assets/Scenes/Playable/Level_EnergyRelay.unity";
        private const string DefinitionPath =
            "Assets/Data/Levels/Level_EnergyRelay.asset";
        private const string BuildKey =
            "Train.EnergyRelaySceneBuilder.v5";
        private const string FactoryRoot =
            "Assets/Arts/ThirdParty/Environment/Kenney_Factory/Imported/Models/FBX format/";
        private const string MaterialFolder =
            "Assets/Arts/World/EnergyRelay";

        private static readonly Vector3 ArenaCenter =
            new(-47f, 0f, -30f);

        static EnergyRelaySceneBuilder()
        {
            EditorApplication.delayCall += TryBuild;
        }

        private static void TryBuild()
        {
            if (EditorPrefs.GetBool(BuildKey, false) ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Build();
        }

        [MenuItem("Tools/Train/Content/Build Energy Relay Phase 2")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            var definition =
                AssetDatabase.LoadAssetAtPath<LevelDefinition>(DefinitionPath);
            if (definition == null)
            {
                throw new InvalidOperationException(
                    $"Missing level definition: {DefinitionPath}");
            }

            EnsureFolder(MaterialFolder);
            CreateWarningMaterial();
            var coreMaterial = CreateCoreMaterial();
            var floorMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/Arts/World/Arena/ArenaFloor.mat");
            var metalMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/Arts/World/Production/ProductionMetal.mat");
            var cyanMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/Arts/World/Production/ProductionCyan.mat");
            var goldMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/Arts/World/Production/ProductionGold.mat");

            if (floorMaterial == null || metalMaterial == null ||
                cyanMaterial == null || goldMaterial == null)
            {
                throw new InvalidOperationException(
                    "Energy Relay requires the existing Production materials.");
            }

            RemoveOldVisualRoots(scene);
            var stage = FindRoot(scene, "StageDressing") ??
                        CreateRoot(scene, "StageDressing");
            ClearChildren(stage.transform);

            CreateBoundary(stage.transform, floorMaterial);
            CreateCore(stage.transform, coreMaterial);
            CreateTowers(stage.transform, metalMaterial, goldMaterial);
            CreateCoverIslands(stage.transform, metalMaterial, goldMaterial);
            CreateFactoryBackdrop(stage.transform, goldMaterial);
            CreateSafetyGuide(stage.transform, goldMaterial);
            ConfigureLighting(scene);
            ConfigureSpawnPoints(scene, definition);
            ConfigureRuntime(scene, definition);
            BuildNavigation(scene, stage.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorPrefs.SetBool(BuildKey, true);
            Debug.Log(
                "[Phase2EnergyRelay] built ring layout, three towers, cover islands and NavMesh.");
        }

        private static void RemoveOldVisualRoots(Scene scene)
        {
            var names = new HashSet<string>(StringComparer.Ordinal)
            {
                "ArenaFloor"
            };
            foreach (var root in scene.GetRootGameObjects())
            {
                if (names.Contains(root.name))
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            foreach (var rootName in new[]
                     {
                         "[WorldPickups]",
                         "[WorldDialogueTerminals]"
                     })
            {
                var root = FindRoot(scene, rootName);
                if (root != null)
                {
                    ClearChildren(root.transform);
                }
            }
        }

        private static void CreateBoundary(
            Transform parent,
            Material floorMaterial)
        {
            CreateCube(
                parent,
                "RelayFloor",
                ArenaCenter + Vector3.down * 0.22f,
                new Vector3(36f, 0.44f, 32f),
                floorMaterial,
                Quaternion.identity);

            CreateCube(
                parent,
                "BoundaryLeft",
                ArenaCenter + new Vector3(-18f, 2.8f, 0f),
                new Vector3(0.7f, 5.6f, 32f),
                floorMaterial,
                Quaternion.identity);
            CreateCube(
                parent,
                "BoundaryRight",
                ArenaCenter + new Vector3(18f, 2.8f, 0f),
                new Vector3(0.7f, 5.6f, 32f),
                floorMaterial,
                Quaternion.identity);
            CreateCube(
                parent,
                "BoundaryBack",
                ArenaCenter + new Vector3(0f, 2.8f, 16f),
                new Vector3(36f, 5.6f, 0.7f),
                floorMaterial,
                Quaternion.identity);
            CreateCube(
                parent,
                "BoundaryFront",
                ArenaCenter + new Vector3(0f, 2.8f, -16f),
                new Vector3(36f, 5.6f, 0.7f),
                floorMaterial,
                Quaternion.identity);

            for (var x = -15f; x <= 15f; x += 6f)
            {
                PlaceModel(
                    parent,
                    FactoryRoot + "structure-medium.fbx",
                    $"BackStructure_{x:0}",
                    ArenaCenter + new Vector3(x, 0.2f, 15.2f),
                    Quaternion.identity,
                    Vector3.one * 1.45f);
            }

            for (var z = -10f; z <= 10f; z += 10f)
            {
                PlaceModel(
                    parent,
                    FactoryRoot + "structure-tall.fbx",
                    $"SideStructure_L_{z:0}",
                    ArenaCenter + new Vector3(-17.2f, 0.25f, z),
                    Quaternion.Euler(0f, 90f, 0f),
                    Vector3.one * 1.2f);
                PlaceModel(
                    parent,
                    FactoryRoot + "structure-tall.fbx",
                    $"SideStructure_R_{z:0}",
                    ArenaCenter + new Vector3(17.2f, 0.25f, z),
                    Quaternion.Euler(0f, -90f, 0f),
                    Vector3.one * 1.2f);
            }
        }

        private static void CreateCore(
            Transform parent,
            Material coreMaterial)
        {
            var core = new GameObject("EnergyRelayCore");
            core.transform.SetParent(parent, false);
            core.transform.position = ArenaCenter + Vector3.up * 0.5f;
            core.isStatic = false;

            var collider = core.AddComponent<CapsuleCollider>();
            collider.radius = 0.9f;
            collider.height = 2.8f;
            collider.center = new Vector3(0f, 0.9f, 0f);

            var pulse = core.AddComponent<RelayPulseController>();
            var serialized = new SerializedObject(pulse);
            serialized.FindProperty("_pulseInterval").floatValue = 7f;
            serialized.FindProperty("_telegraphSeconds").floatValue = 1.4f;
            serialized.FindProperty("_activeSeconds").floatValue = 0.35f;
            serialized.FindProperty("_radius").floatValue = 6f;
            serialized.FindProperty("_damage").floatValue = 22f;
            serialized.FindProperty("_pulseColor").colorValue =
                new Color(0.2f, 0.8f, 1f, 0.85f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            CreateCylinder(
                core.transform,
                "CoreBase",
                ArenaCenter + Vector3.up * 0.42f,
                new Vector3(2.8f, 0.42f, 2.8f),
                coreMaterial,
                Quaternion.identity);
            CreateCylinder(
                core.transform,
                "CoreSocket",
                ArenaCenter + Vector3.up * 0.95f,
                new Vector3(1.75f, 0.16f, 1.75f),
                coreMaterial,
                Quaternion.identity);
            CreateSphere(
                core.transform,
                "CoreOrb",
                ArenaCenter + new Vector3(0f, 2.2f, 0f),
                Vector3.one * 0.78f,
                coreMaterial);
            CreateCube(
                core.transform,
                "CoreSpine",
                ArenaCenter + new Vector3(0f, 1.55f, 0f),
                new Vector3(0.28f, 1.15f, 0.28f),
                coreMaterial,
                Quaternion.Euler(0f, 45f, 0f));

            CreatePointLight(
                core.transform,
                "CoreLight",
                new Vector3(0f, 2.2f, 0f),
                new Color(0.12f, 0.75f, 1f),
                5.5f,
                8f);
        }

        private static void CreateTowers(
            Transform parent,
            Material metalMaterial,
            Material goldMaterial)
        {
            var towerPositions = new[]
            {
                ArenaCenter + new Vector3(0f, 0f, 9f),
                ArenaCenter + new Vector3(-7.8f, 0f, -4.5f),
                ArenaCenter + new Vector3(7.8f, 0f, -4.5f)
            };

            for (var index = 0; index < towerPositions.Length; index++)
            {
                var position = towerPositions[index];
                var tower = new GameObject($"PowerTower_{index + 1:00}");
                tower.transform.SetParent(parent, false);
                tower.transform.position = position;
                tower.isStatic = true;

                CreateCube(
                    tower.transform,
                    "TowerBase",
                    position + Vector3.up * 0.22f,
                    new Vector3(2.8f, 0.44f, 2.8f),
                    metalMaterial,
                    Quaternion.identity);
                PlaceModel(
                    tower.transform,
                    FactoryRoot + "structure-high.fbx",
                    "TowerStructure",
                    position + Vector3.up * 0.3f,
                    Quaternion.Euler(0f, index * 120f, 0f),
                    Vector3.one * 1.55f);
                PlaceModel(
                    tower.transform,
                    FactoryRoot + "machine.fbx",
                    "TowerMachine",
                    position + Vector3.up * 2.3f,
                    Quaternion.Euler(0f, index * 120f, 0f),
                    Vector3.one * 1.1f);
                PlaceModel(
                    tower.transform,
                    FactoryRoot + "warning-traffic.fbx",
                    "TowerWarning",
                    position + Vector3.up * 4.1f,
                    Quaternion.identity,
                    Vector3.one * 0.85f);
                CreateCube(
                    tower.transform,
                    "TowerGuideBand",
                    position + Vector3.up * 1.35f,
                    new Vector3(2.9f, 0.16f, 2.9f),
                    goldMaterial,
                    Quaternion.identity,
                    false);

                var blocker = tower.AddComponent<BoxCollider>();
                blocker.center = new Vector3(0f, 2f, 0f);
                blocker.size = new Vector3(2.7f, 4f, 2.7f);
                CreatePointLight(
                    tower.transform,
                    "TowerWarningLight",
                    new Vector3(0f, 3.65f, 0f),
                    new Color(1f, 0.12f, 0.03f),
                    1.8f,
                    5f);
            }
        }

        private static void CreateCoverIslands(
            Transform parent,
            Material metalMaterial,
            Material goldMaterial)
        {
            var islands = new[]
            {
                new Vector3(7.6f, 0f, 4.5f),
                new Vector3(-7.6f, 0f, 4.5f),
                new Vector3(0f, 0f, -8.6f)
            };

            for (var index = 0; index < islands.Length; index++)
            {
                var position = ArenaCenter + islands[index];
                var island = new GameObject($"CoverIsland_{index + 1:00}");
                island.transform.SetParent(parent, false);
                island.transform.position = position;
                island.isStatic = true;

                CreateCube(
                    island.transform,
                    "IslandBase",
                    position + Vector3.up * 0.4f,
                    new Vector3(4.4f, 0.8f, 3.1f),
                    metalMaterial,
                    Quaternion.Euler(0f, index == 2 ? 90f : 0f, 0f));
                PlaceModel(
                    island.transform,
                    FactoryRoot + "box-large.fbx",
                    "IslandBox",
                    position + Vector3.up * 0.85f,
                    Quaternion.Euler(0f, index * 30f, 0f),
                    Vector3.one * 1.3f);
                PlaceModel(
                    island.transform,
                    FactoryRoot + "machine-bed.fbx",
                    "IslandMachine",
                    position + new Vector3(0f, 0.95f, 0.9f),
                    Quaternion.Euler(0f, index * 120f, 0f),
                    Vector3.one * 0.95f);
                PlaceModel(
                    island.transform,
                    FactoryRoot + "screen-panel-wide.fbx",
                    "IslandScreen",
                    position + new Vector3(0f, 2.05f, 1.15f),
                    Quaternion.LookRotation(ArenaCenter - position),
                    Vector3.one * 0.7f);
                PlaceModel(
                    island.transform,
                    FactoryRoot + "conveyor-long.fbx",
                    "IslandConveyor",
                    position + new Vector3(0f, 0.18f, -1.15f),
                    Quaternion.Euler(0f, index == 2 ? 90f : 0f, 0f),
                    Vector3.one * 0.72f);
                PlaceModel(
                    island.transform,
                    FactoryRoot + "catwalk-straight.fbx",
                    "IslandCatwalk",
                    position + new Vector3(0f, 0.16f, 1.55f),
                    Quaternion.Euler(0f, index == 2 ? 90f : 0f, 0f),
                    Vector3.one * 0.9f);

                CreateCube(
                    island.transform,
                    "IslandSafetyEdge",
                    position + new Vector3(0f, 0.83f, -1.52f),
                    new Vector3(3.5f, 0.12f, 0.12f),
                    goldMaterial,
                    Quaternion.identity,
                    false);

                var blocker = island.AddComponent<BoxCollider>();
                blocker.center = new Vector3(0f, 0.55f, 0f);
                blocker.size = new Vector3(4.4f, 1.1f, 3.1f);
            }

            var corridorAngles = new[] { 90f, 210f, 330f };
            foreach (var angle in corridorAngles)
            {
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                var start = ArenaCenter + direction * 2.8f + Vector3.up * 0.03f;
                CreateCube(
                    parent,
                    $"SafeCorridor_{angle:0}",
                    start + direction * 2.5f,
                    new Vector3(0.18f, 0.06f, 5f),
                    goldMaterial,
                    Quaternion.Euler(0f, angle, 0f),
                    false);
            }
        }

        private static void CreateFactoryBackdrop(
            Transform parent,
            Material goldMaterial)
        {
            PlaceModel(
                parent,
                FactoryRoot + "pipe-large-long.fbx",
                "BackPipeHeader",
                ArenaCenter + new Vector3(0f, 5.1f, 14.1f),
                Quaternion.Euler(0f, 90f, 0f),
                Vector3.one * 1.2f);
            PlaceModel(
                parent,
                FactoryRoot + "structure-doorway-wide.fbx",
                "BackGate",
                ArenaCenter + new Vector3(0f, 0.2f, 15.1f),
                Quaternion.Euler(0f, 180f, 0f),
                Vector3.one * 1.7f);
            CreateCube(
                parent,
                "BackGateGoldHeader",
                ArenaCenter + new Vector3(0f, 3.7f, 14.55f),
                new Vector3(7.5f, 0.22f, 0.18f),
                goldMaterial,
                Quaternion.identity,
                false);
        }

        private static void CreateSafetyGuide(
            Transform parent,
            Material goldMaterial)
        {
            var position = ArenaCenter + new Vector3(0f, 0f, -13.6f);
            CreateCube(
                parent,
                "ExitSafetyGuide",
                position + Vector3.up * 0.04f,
                new Vector3(4.5f, 0.08f, 0.35f),
                goldMaterial,
                Quaternion.identity,
                false);
            for (var offset = -1.2f; offset <= 1.2f; offset += 1.2f)
            {
                CreateCube(
                    parent,
                    "ExitGuideChevron",
                    position + new Vector3(offset, 0.08f, -0.65f),
                    new Vector3(0.65f, 0.06f, 0.18f),
                    goldMaterial,
                    Quaternion.Euler(0f, 35f, 0f),
                    false);
            }

            CreatePointLight(
                parent,
                "ExitGuideLight",
                position + Vector3.up * 0.9f,
                new Color(1f, 0.55f, 0.08f),
                1.4f,
                4f);
        }

        private static void ConfigureLighting(Scene scene)
        {
            var directional = FindRoot(scene, "Directional Light")?.GetComponent<Light>();
            if (directional != null)
            {
                directional.color = new Color(0.7f, 0.78f, 0.9f);
                directional.intensity = 1.15f;
                directional.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            }

            RenderSettings.ambientSkyColor = new Color(0.18f, 0.22f, 0.3f);
            RenderSettings.ambientEquatorColor = new Color(0.1f, 0.13f, 0.18f);
            RenderSettings.ambientGroundColor = new Color(0.035f, 0.04f, 0.06f);
            RenderSettings.ambientIntensity = 0.9f;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.07f, 0.09f, 0.13f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 38f;
            RenderSettings.fogEndDistance = 90f;
        }

        private static void ConfigureSpawnPoints(
            Scene scene,
            LevelDefinition definition)
        {
            var runtime = FindSceneComponent<LevelRuntimeController>(scene);
            if (runtime == null)
            {
                throw new InvalidOperationException(
                    "Energy Relay scene is missing LevelRuntimeController.");
            }

            var spawns = runtime.transform.Find("Spawns") ??
                         CreateChild(runtime.transform, "Spawns").transform;
            ClearChildren(spawns);

            var playerPosition = ArenaCenter + new Vector3(-5f, 0.984f, -13f);
            var playerSpawn = CreateChild(spawns, "PlayerSpawn");
            playerSpawn.transform.SetPositionAndRotation(
                playerPosition,
                Quaternion.Euler(0f, 20f, 0f));
            playerSpawn.AddComponent<PlayerSpawnPoint>();

            var sentinelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Enemies/Enemy_OverloadSentinel.prefab");
            var dronePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Enemies/Enemy_ArcDroneRobot.prefab");
            var crawlerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Enemies/Enemy_CrawlerBot.prefab");
            var rollerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Enemies/Enemy_RollerBot.prefab");
            var enemyData = new[]
            {
                ("sentinel_01", "elite_knight", sentinelPrefab,
                    ArenaCenter + new Vector3(0f, 0f, 6.5f), 180f,
                    "Enemies_Enemy_OverloadSentinel"),
                ("scout_01", "crawler_bot", crawlerPrefab,
                    ArenaCenter + new Vector3(11f, 0f, 5.5f), 210f,
                    "Enemies_Enemy_CrawlerBot"),
                ("scout_02", "roller_bot", rollerPrefab,
                    ArenaCenter + new Vector3(-11f, 0f, 5.5f), 150f,
                    "Enemies_Enemy_RollerBot"),
                ("elite_01", "arc_drone", dronePrefab,
                    ArenaCenter + new Vector3(0f, 0f, 13f), 180f,
                    "Enemies_Enemy_ArcDroneRobot")
            };

            foreach (var data in enemyData)
            {
                if (data.Item3 == null)
                {
                    throw new InvalidOperationException(
                        $"Missing enemy prefab for {data.Item1}.");
                }

                var point = CreateChild(spawns, $"EnemySpawn_{data.Item1}");
                point.transform.SetPositionAndRotation(
                    data.Item4,
                    Quaternion.Euler(0f, data.Item5, 0f));
                var marker = point.AddComponent<EnemySpawnPoint>();
                marker.Configure(
                    data.Item1,
                    data.Item2,
                    data.Item3,
                    data.Item6);
            }

            var serializedRuntime = new SerializedObject(runtime);
            serializedRuntime.FindProperty("_spawnedActorsRoot").objectReferenceValue =
                runtime.transform.Find("SpawnedActors");
            serializedRuntime.FindProperty("_playerSpawnPoint").objectReferenceValue =
                playerSpawn.transform;
            serializedRuntime.FindProperty("_definition").objectReferenceValue =
                definition;
            serializedRuntime.FindProperty("_autoStart").boolValue = true;
            serializedRuntime.ApplyModifiedPropertiesWithoutUndo();

            var serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("_playerSpawnPosition").vector3Value =
                playerPosition;
            serializedDefinition.FindProperty("_playerSpawnEulerAngles").vector3Value =
                Vector3.zero;
            serializedDefinition.FindProperty("_introSeconds").floatValue = 1.2f;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            LevelPointSyncBuilder.SyncScene(scene, definition);
        }

        private static void ConfigureRuntime(
            Scene scene,
            LevelDefinition definition)
        {
            var runtime = FindSceneComponent<LevelRuntimeController>(scene);
            var serialized = new SerializedObject(runtime);
            serialized.FindProperty("_definition").objectReferenceValue = definition;
            serialized.FindProperty("_autoStart").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildNavigation(
            Scene scene,
            Transform stage)
        {
            var navigation = FindRoot(scene, "EnergyRelayNavigation");
            if (navigation == null)
            {
                navigation = CreateRoot(scene, "EnergyRelayNavigation");
            }

            var surface = navigation.GetComponent<NavMeshSurface>() ??
                          navigation.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~0;
            surface.agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID;
            surface.BuildNavMesh();
            if (surface.navMeshData == null)
            {
                throw new InvalidOperationException(
                    "Energy Relay NavMeshSurface did not produce NavMeshData.");
            }

            navigation.isStatic = true;
            _ = stage;
        }

        private static GameObject PlaceModel(
            Transform parent,
            string path,
            string name,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Missing Factory model: {path}");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = scale;
            instance.isStatic = true;
            return instance;
        }

        private static GameObject CreateCube(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 size,
            Material material,
            Quaternion rotation,
            bool collider = true)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.SetPositionAndRotation(position, rotation);
            cube.transform.localScale = size;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            cube.isStatic = true;
            if (!collider)
            {
                UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
            }

            return cube;
        }

        private static GameObject CreateCylinder(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 size,
            Material material,
            Quaternion rotation)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.SetPositionAndRotation(position, rotation);
            cylinder.transform.localScale = size;
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            cylinder.isStatic = true;
            return cylinder;
        }

        private static GameObject CreateSphere(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 size,
            Material material)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent, false);
            sphere.transform.position = position;
            sphere.transform.localScale = size;
            sphere.GetComponent<Renderer>().sharedMaterial = material;
            sphere.isStatic = true;
            UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
            return sphere;
        }

        private static Light CreatePointLight(
            Transform parent,
            string name,
            Vector3 localPosition,
            Color color,
            float intensity,
            float range)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = localPosition;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        private static Material CreateWarningMaterial()
        {
            const string path = MaterialFolder + "/EnergyRelayWarning.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("URP Lit shader is required.");
            }

            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "EnergyRelayWarning"
                };
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.5f, 0.035f, 0.01f));
            material.SetColor("_Color", new Color(0.5f, 0.035f, 0.01f));
            material.SetColor("_EmissionColor", new Color(0.75f, 0.04f, 0.005f));
            material.SetFloat("_Metallic", 0.35f);
            material.SetFloat("_Smoothness", 0.55f);
            material.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateCoreMaterial()
        {
            const string path = MaterialFolder + "/EnergyRelayCore.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("URP Lit shader is required.");
            }

            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "EnergyRelayCore"
                };
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.02f, 0.32f, 0.4f));
            material.SetColor("_Color", new Color(0.02f, 0.32f, 0.4f));
            material.SetColor("_EmissionColor", new Color(0.02f, 0.95f, 1f));
            material.SetFloat("_Metallic", 0.18f);
            material.SetFloat("_Smoothness", 0.58f);
            material.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateRoot(Scene scene, string name)
        {
            var root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void ClearChildren(Transform parent)
        {
            var children = new List<GameObject>();
            for (var index = 0; index < parent.childCount; index++)
            {
                children.Add(parent.GetChild(index).gameObject);
            }

            foreach (var child in children)
            {
                UnityEngine.Object.DestroyImmediate(child);
            }
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == name);
        }

        private static T FindSceneComponent<T>(Scene scene)
            where T : Component
        {
            return UnityEngine.Object.FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(component => component.gameObject.scene == scene);
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
