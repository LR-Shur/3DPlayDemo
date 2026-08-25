#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Train.GameFlow.Data;
using Train.GameFlow.Runtime;
using Train.Gameplay.Combat;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Train.EditorTools.GameFlow
{
    /// <summary>
    /// 构建轨道装卸港的结构、货物岛、危险区、出生点和 NavMesh。
    /// 仅作用于 Level_OrbitalCargo，不参与运行时生成。
    /// </summary>
    public static class OrbitalCargoSceneBuilder
    {
        private const string ScenePath =
            "Assets/Scenes/Playable/Level_OrbitalCargo.unity";
        private const string DefinitionPath =
            "Assets/Data/Levels/Level_OrbitalCargo.asset";
        private const string ModelRoot = "Assets/Resources/ThemeModels/";
        private const string MaterialRoot =
            "Assets/Arts/ThirdParty/Environment/SceneMaterials/";
        private const string LocalMaterialRoot = "Assets/Arts/World/OrbitalCargo";
        private const string ScreenshotRoot =
            "Assets/Screenshots/Phase4OrbitalCargo";

        private static readonly Vector3 ArenaCenter =
            new(-47f, 0f, -30f);

        [MenuItem("Tools/Train/Content/Build Orbital Cargo Phase 4")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);
            }

            var definition = AssetDatabase.LoadAssetAtPath<LevelDefinition>(
                DefinitionPath);
            if (definition == null)
            {
                throw new InvalidOperationException(
                    $"Missing level definition: {DefinitionPath}");
            }

            EnsureFolder(LocalMaterialRoot);
            EnsureFolder(ScreenshotRoot);

            var metal = AssetDatabase.LoadAssetAtPath<Material>(
                MaterialRoot + "CargoMetal.mat");
            var accent = AssetDatabase.LoadAssetAtPath<Material>(
                MaterialRoot + "CargoAccent.mat");
            var gold = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Arts/World/Production/ProductionGold.mat");
            var warning = CreateEmissiveMaterial(
                LocalMaterialRoot + "/OrbitalCargoWarning.mat",
                "OrbitalCargoWarning",
                new Color(0.32f, 0.035f, 0.008f),
                new Color(1f, 0.08f, 0.01f));
            var danger = CreateEmissiveMaterial(
                LocalMaterialRoot + "/OrbitalCargoDanger.mat",
                "OrbitalCargoDanger",
                new Color(0.16f, 0.008f, 0.08f),
                new Color(0.75f, 0.01f, 0.28f));
            var industrial = CreateEmissiveMaterial(
                LocalMaterialRoot + "/OrbitalCargoIndustrial.mat",
                "OrbitalCargoIndustrial",
                new Color(0.92f, 0.38f, 0.015f),
                new Color(1f, 0.32f, 0.02f));

            if (metal == null || accent == null || gold == null)
            {
                throw new InvalidOperationException(
                    "Orbital Cargo requires CargoMetal, CargoAccent and ProductionGold.");
            }

            RemoveOldVisualRoots(scene);
            var stage = FindRoot(scene, "OrbitalCargo_Structure") ??
                        CreateRoot(scene, "OrbitalCargo_Structure");
            ClearChildren(stage.transform);
            stage.isStatic = true;

            CreateDeck(stage.transform, metal, industrial);
            CreateCargoIslands(stage.transform, metal, accent, industrial);
            CreateCraneAndCargoDoor(stage.transform, metal, industrial);
            CreateHazards(stage.transform, warning, danger);
            CreateExitGuide(stage.transform, gold);
            ConfigureLightingAndCamera(scene);
            ConfigureSceneCamera(scene);
            ConfigureSpawnPoints(scene, definition);
            ConfigureRuntime(scene, definition);
            BuildNavigation(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Phase4OrbitalCargo] built three cargo lanes, service walks, cargo islands, crane landmark, hazards and NavMesh.");
        }

        private static void RemoveOldVisualRoots(Scene scene)
        {
            foreach (var rootName in new[]
                     {
                         "ArenaFloor",
                         "THEME_BAKED_SCENE",
                         "OrbitalCargo_Structure",
                         "OrbitalCargoNavigation"
                     })
            {
                var root = FindRoot(scene, rootName);
                if (root != null)
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

        private static void CreateDeck(
            Transform parent,
            Material metal,
            Material gold)
        {
            CreateCube(
                parent,
                "CargoDeck_Main",
                ArenaCenter + Vector3.down * 0.25f,
                new Vector3(36f, 0.5f, 44f),
                metal,
                Quaternion.identity);

            CreateCube(
                parent,
                "CargoDeck_FrontCurb",
                ArenaCenter + new Vector3(0f, 0.16f, -21.65f),
                new Vector3(36f, 0.32f, 0.42f),
                metal,
                Quaternion.identity);

            CreateSideServiceWalk(parent, -62f, metal);
            CreateSideServiceWalk(parent, -32f, metal);
            CreateBoundaryRail(parent, -65f, -50f, -10f, metal);
            CreateBoundaryRail(parent, -29f, -50f, -10f, metal);
            CreateFrontRail(parent, metal);

            for (var lane = -1; lane <= 1; lane++)
            {
                CreateCube(
                    parent,
                    $"LaneGuide_{lane + 2}",
                    new Vector3(ArenaCenter.x + lane * 10f, 0.018f, ArenaCenter.z),
                    new Vector3(0.08f, 0.035f, 39f),
                    gold,
                    Quaternion.identity,
                    false);
            }

            foreach (var dividerX in new[] { -52f, -42f })
            {
                CreateCube(
                    parent,
                    "LaneDivider",
                    new Vector3(dividerX, 0.024f, ArenaCenter.z),
                    new Vector3(0.14f, 0.04f, 39f),
                    gold,
                    Quaternion.identity,
                    false);
            }

            for (var z = -48f; z <= -12f; z += 9f)
            {
                CreateCube(
                    parent,
                    "LaneCrossbar",
                    new Vector3(ArenaCenter.x, 0.02f, z),
                    new Vector3(34f, 0.025f, 0.08f),
                    gold,
                    Quaternion.identity,
                    false);
            }
        }

        private static void CreateSideServiceWalk(
            Transform parent,
            float x,
            Material metal)
        {
            CreateCube(
                parent,
                x < ArenaCenter.x ? "ServiceWalk_Left" : "ServiceWalk_Right",
                new Vector3(x, 0.09f, ArenaCenter.z),
                new Vector3(4.5f, 0.18f, 39f),
                metal,
                Quaternion.identity);

            var railX = x < ArenaCenter.x ? x - 2.1f : x + 2.1f;
            for (var z = -48f; z <= -12f; z += 9f)
            {
                CreateCube(
                    parent,
                    "ServiceWalk_Post",
                    new Vector3(railX, 0.9f, z),
                    new Vector3(0.16f, 1.65f, 0.16f),
                    metal,
                    Quaternion.identity);
            }

            CreateCube(
                parent,
                "ServiceWalk_TopRail",
                new Vector3(railX, 1.65f, ArenaCenter.z),
                new Vector3(0.16f, 0.16f, 39f),
                metal,
                Quaternion.identity);
        }

        private static void CreateBoundaryRail(
            Transform parent,
            float x,
            float frontZ,
            float backZ,
            Material metal)
        {
            CreateCube(
                parent,
                "DeckEdge_Curb",
                new Vector3(x, 0.17f, (frontZ + backZ) * 0.5f),
                new Vector3(0.38f, 0.34f, backZ - frontZ),
                metal,
                Quaternion.identity);

            for (var z = frontZ; z <= backZ; z += 9f)
            {
                CreateCube(
                    parent,
                    "DeckEdge_Post",
                    new Vector3(x, 0.9f, z),
                    new Vector3(0.18f, 1.65f, 0.18f),
                    metal,
                    Quaternion.identity);
            }

            CreateCube(
                parent,
                "DeckEdge_TopRail",
                new Vector3(x, 1.65f, (frontZ + backZ) * 0.5f),
                new Vector3(0.18f, 0.18f, backZ - frontZ),
                metal,
                Quaternion.identity);
        }

        private static void CreateFrontRail(
            Transform parent,
            Material metal)
        {
            for (var x = -63f; x <= -31f; x += 8f)
            {
                CreateCube(
                    parent,
                    "FrontEdge_Post",
                    new Vector3(x, 0.9f, -52f),
                    new Vector3(0.18f, 1.65f, 0.18f),
                    metal,
                    Quaternion.identity);
            }

            CreateCube(
                parent,
                "FrontEdge_TopRail",
                new Vector3(ArenaCenter.x, 1.65f, -52f),
                new Vector3(34f, 0.18f, 0.18f),
                metal,
                Quaternion.identity);
        }

        private static void CreateCargoIslands(
            Transform parent,
            Material metal,
            Material accent,
            Material gold)
        {
            CreateCargoModule(
                parent,
                "CargoIsland_Left",
                new Vector3(-56f, 0f, -31f),
                Quaternion.Euler(0f, 2f, 0f),
                metal,
                accent,
                gold,
                true);
            CreateCargoModule(
                parent,
                "CargoIsland_Center",
                new Vector3(-47f, 0f, -37f),
                Quaternion.Euler(0f, 90f, 0f),
                metal,
                accent,
                gold,
                false);
            CreateCargoModule(
                parent,
                "CargoIsland_Right",
                new Vector3(-38f, 0f, -27f),
                Quaternion.Euler(0f, -4f, 0f),
                metal,
                accent,
                gold,
                true);
        }

        private static void CreateCargoModule(
            Transform parent,
            string name,
            Vector3 position,
            Quaternion rotation,
            Material metal,
            Material accent,
            Material gold,
            bool offsetStack)
        {
            var module = new GameObject(name);
            module.transform.SetParent(parent, false);
            module.transform.SetPositionAndRotation(position, rotation);
            module.isStatic = true;

            CreateLocalCube(
                module.transform,
                "PalletDeck",
                new Vector3(0f, 0.08f, 0f),
                new Vector3(4.8f, 0.16f, 3.8f),
                metal,
                Quaternion.identity);
            CreateLocalCube(
                module.transform,
                "ContainerLower",
                new Vector3(0f, 1.35f, 0f),
                new Vector3(4.2f, 2.5f, 3.1f),
                metal,
                Quaternion.identity);
            CreateLocalCube(
                module.transform,
                "ContainerAccentBand",
                new Vector3(0f, 2.48f, -1.58f),
                new Vector3(4.05f, 0.12f, 0.12f),
                gold,
                Quaternion.identity,
                false);
            CreateLocalCube(
                module.transform,
                "ContainerDoorPanel",
                new Vector3(0f, 1.36f, -1.61f),
                new Vector3(1.55f, 1.55f, 0.08f),
                accent,
                Quaternion.identity,
                false);

            if (offsetStack)
            {
                CreateLocalCube(
                    module.transform,
                    "ContainerUpper",
                    new Vector3(0.75f, 3.15f, 0.3f),
                    new Vector3(2.45f, 1.05f, 2.25f),
                    metal,
                    Quaternion.Euler(0f, -7f, 0f));
                CreateLocalCube(
                    module.transform,
                    "ContainerUpperBand",
                    new Vector3(0.75f, 3.15f, 0.3f),
                    new Vector3(2.25f, 0.1f, 2.1f),
                    gold,
                    Quaternion.Euler(0f, -7f, 0f),
                    false);
            }

            PlaceLocalModel(
                module.transform,
                ModelRoot + "conveyor-long.fbx",
                "CargoConveyor",
                new Vector3(0f, 0.23f, -2.55f),
                Quaternion.identity,
                Vector3.one * 0.72f);
            CreateLocalCollider(
                module.transform,
                "ConveyorCollider",
                new Vector3(0f, 0.21f, -2.55f),
                new Vector3(4.1f, 0.32f, 2.25f),
                Quaternion.identity);
            CreateLocalCube(
                module.transform,
                "ConveyorBeltAccent",
                new Vector3(0f, 0.44f, -2.55f),
                new Vector3(2.6f, 0.05f, 0.14f),
                accent,
                Quaternion.identity,
                false);
            CreateLocalCube(
                module.transform,
                "PalletAccent",
                new Vector3(-1.55f, 0.22f, -1.55f),
                new Vector3(0.85f, 0.16f, 0.85f),
                gold,
                Quaternion.identity,
                false);
        }

        private static void CreateCraneAndCargoDoor(
            Transform parent,
            Material metal,
            Material gold)
        {
            var cranePosition = ArenaCenter + new Vector3(0f, 0f, 15f);
            var craneModel = PlaceModel(
                parent,
                ModelRoot + "crane.fbx",
                "MainCargoCrane",
                cranePosition,
                Quaternion.Euler(0f, 180f, 0f),
                Vector3.one * 2.7f);
            OverrideModelMaterials(craneModel, gold);

            CreateCube(
                parent,
                "CraneColumn_Left",
                cranePosition + new Vector3(-5.8f, 2.8f, 0f),
                new Vector3(0.75f, 5.6f, 0.75f),
                gold,
                Quaternion.identity);
            CreateCube(
                parent,
                "CraneColumn_Right",
                cranePosition + new Vector3(5.8f, 2.8f, 0f),
                new Vector3(0.75f, 5.6f, 0.75f),
                gold,
                Quaternion.identity);
            CreateCube(
                parent,
                "CraneTopBeam",
                cranePosition + new Vector3(0f, 5.55f, 0f),
                new Vector3(12.5f, 0.75f, 0.75f),
                gold,
                Quaternion.identity);
            CreateCube(
                parent,
                "CraneBoom_Longitudinal",
                cranePosition + new Vector3(0f, 5.95f, 2.2f),
                new Vector3(0.6f, 0.6f, 9.6f),
                gold,
                Quaternion.identity);
            CreateCube(
                parent,
                "CraneBoom_Crossbar",
                cranePosition + new Vector3(0f, 5.95f, -2.8f),
                new Vector3(10.4f, 0.45f, 0.45f),
                gold,
                Quaternion.identity);

            CreateCube(
                parent,
                "CraneHoist",
                cranePosition + new Vector3(0f, 5.4f, 1.5f),
                new Vector3(0.7f, 0.7f, 0.7f),
                gold,
                Quaternion.identity,
                false);

            var suspendedPosition = ArenaCenter + new Vector3(0f, 2.45f, 10.8f);
            CreateCube(
                parent,
                "SuspendedCargo",
                suspendedPosition,
                new Vector3(3.6f, 1.8f, 2.7f),
                metal,
                Quaternion.Euler(0f, -8f, 0f));
            CreateCube(
                parent,
                "SuspendedCargo_AccentPanel",
                suspendedPosition + new Vector3(0f, 0f, -1.38f),
                new Vector3(1.4f, 1.2f, 0.08f),
                gold,
                Quaternion.Euler(0f, -8f, 0f),
                false);
            for (var x = -1.35f; x <= 1.35f; x += 2.7f)
            {
                CreateCube(
                    parent,
                    "SuspendedCargo_Cable",
                    suspendedPosition + new Vector3(x, 1.85f, 0f),
                    new Vector3(0.08f, 3.1f, 0.08f),
                    gold,
                    Quaternion.identity,
                    false);
            }

            var doorPosition = ArenaCenter + new Vector3(0f, 0f, 21.1f);
            var doorModel = PlaceModel(
                parent,
                ModelRoot + "door-wide-closed.fbx",
                "FreightBayDoor",
                doorPosition,
                Quaternion.Euler(0f, 180f, 0f),
                Vector3.one * 2.1f);
            OverrideModelMaterials(doorModel, metal);
            CreateCube(
                parent,
                "FreightBayDoor_LeftPillar",
                doorPosition + new Vector3(-5.2f, 2f, 0.1f),
                new Vector3(0.7f, 4f, 0.8f),
                metal,
                Quaternion.identity);
            CreateCube(
                parent,
                "FreightBayDoor_RightPillar",
                doorPosition + new Vector3(5.2f, 2f, 0.1f),
                new Vector3(0.7f, 4f, 0.8f),
                metal,
                Quaternion.identity);
            CreateCube(
                parent,
                "FreightBayDoor_Header",
                doorPosition + new Vector3(0f, 4.1f, 0.1f),
                new Vector3(11.2f, 0.32f, 0.8f),
                gold,
                Quaternion.identity);
            CreateCube(
                parent,
                "FreightBayThreshold",
                doorPosition + new Vector3(0f, 0.16f, 0.7f),
                new Vector3(34f, 0.32f, 0.5f),
                metal,
                Quaternion.identity);
            CreatePointLight(
                parent,
                "FreightBayDoorLight",
                doorPosition + Vector3.up * 3.7f,
                new Color(1f, 0.48f, 0.08f),
                4f,
                11f);
        }

        private static void CreateHazards(
            Transform parent,
            Material warning,
            Material danger)
        {
            var positions = new[]
            {
                new Vector3(-57f, 0f, -40f),
                new Vector3(-47f, 0f, -24f),
                new Vector3(-37f, 0f, -35f)
            };

            for (var index = 0; index < positions.Length; index++)
            {
                var hazard = new GameObject(
                    $"FieldHazard_EM_{index + 1:00}");
                hazard.transform.SetParent(parent, false);
                hazard.transform.position = positions[index];
                hazard.AddComponent<FieldHazardController>();

                var serialized = new SerializedObject(
                    hazard.GetComponent<FieldHazardController>());
                serialized.FindProperty("_interval").floatValue = 7f;
                serialized.FindProperty("_telegraphSeconds").floatValue = 1.6f;
                serialized.FindProperty("_activeSeconds").floatValue = 0.45f;
                serialized.FindProperty("_radius").floatValue = 2.75f;
                serialized.FindProperty("_damage").floatValue = 16f;
                serialized.FindProperty("_impactForce").floatValue = 0f;
                serialized.FindProperty("_damageType").enumValueIndex =
                    (int)DamageType.Electric;
                serialized.FindProperty("_effectColor").colorValue =
                    new Color(0.9f, 0.035f, 0.12f, 0.95f);
                serialized.FindProperty("_verticalBeam").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                CreateCylinder(
                    hazard.transform,
                    "HazardWarningPad",
                    new Vector3(0f, 0.035f, 0f),
                    new Vector3(5.7f, 0.08f, 5.7f),
                    warning,
                    Quaternion.identity,
                    false);
                CreateCylinder(
                    hazard.transform,
                    "HazardDangerCore",
                    new Vector3(0f, 0.14f, 0f),
                    new Vector3(0.75f, 0.16f, 0.75f),
                    danger,
                    Quaternion.identity,
                    false);
                CreateLocalCube(
                    hazard.transform,
                    "HazardWarningStripe",
                    new Vector3(0f, 0.12f, -2.1f),
                    new Vector3(3.6f, 0.05f, 0.12f),
                    warning,
                    Quaternion.identity,
                    false);
                CreatePointLight(
                    hazard.transform,
                    "HazardStandbyLight",
                    Vector3.up * 0.85f,
                    new Color(0.95f, 0.05f, 0.02f),
                    0.65f,
                    4.5f);
            }
        }

        private static void CreateExitGuide(
            Transform parent,
            Material gold)
        {
            var guideZ = -12.4f;
            CreateCube(
                parent,
                "ExitGuide_Main",
                new Vector3(ArenaCenter.x, 0.04f, guideZ),
                new Vector3(5.8f, 0.08f, 0.32f),
                gold,
                Quaternion.identity,
                false);
            for (var x = -2.2f; x <= 2.2f; x += 1.1f)
            {
                CreateCube(
                    parent,
                    "ExitGuide_Chevron",
                    new Vector3(ArenaCenter.x + x, 0.08f, guideZ - 0.72f),
                    new Vector3(0.7f, 0.08f, 0.18f),
                    gold,
                    Quaternion.Euler(0f, 35f, 0f),
                    false);
            }
            CreatePointLight(
                parent,
                "ExitGuide_Light",
                new Vector3(ArenaCenter.x, 0.9f, guideZ),
                new Color(1f, 0.55f, 0.08f),
                1.6f,
                6f);
        }

        private static void ConfigureLightingAndCamera(Scene scene)
        {
            var directional = FindRoot(scene, "Directional Light")?.GetComponent<Light>();
            if (directional != null)
            {
                directional.color = new Color(0.68f, 0.76f, 0.9f);
                directional.intensity = 1.15f;
                directional.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            }

            RenderSettings.ambientSkyColor = new Color(0.17f, 0.21f, 0.29f);
            RenderSettings.ambientEquatorColor = new Color(0.08f, 0.11f, 0.16f);
            RenderSettings.ambientGroundColor = new Color(0.025f, 0.032f, 0.05f);
            RenderSettings.ambientIntensity = 0.88f;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.055f, 0.075f, 0.11f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 42f;
            RenderSettings.fogEndDistance = 105f;

            var camera = FindRoot(scene, "Main Camera")?.GetComponent<Camera>();
            if (camera == null)
            {
                camera = UnityEngine.Object.FindObjectsByType<Camera>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .FirstOrDefault(item => item.gameObject.scene == scene);
            }

            if (camera != null)
            {
                camera.transform.position = new Vector3(-47f, 12.5f, -58f);
                camera.transform.LookAt(new Vector3(-47f, 1.5f, -20f));
                camera.fieldOfView = 58f;
                camera.farClipPlane = 150f;
            }
        }

        private static void ConfigureSceneCamera(Scene scene)
        {
            var cameraRoot = FindRoot(scene, "CM_PlayerCamera");
            if (cameraRoot == null)
            {
                return;
            }

            var virtualCamera = cameraRoot.GetComponent<CinemachineCamera>();
            if (virtualCamera != null)
            {
                var lens = virtualCamera.Lens;
                lens.FieldOfView = 62f;
                lens.FarClipPlane = 180f;
                virtualCamera.Lens = lens;
                EditorUtility.SetDirty(virtualCamera);
            }

            var orbitalFollow = cameraRoot.GetComponent<CinemachineOrbitalFollow>();
            if (orbitalFollow == null)
            {
                return;
            }

            orbitalFollow.Radius = 5.8f;
            orbitalFollow.TargetOffset = new Vector3(0.35f, 0.45f, 0f);
            var orbits = orbitalFollow.Orbits;
            orbits.Top.Radius = 5.8f;
            orbits.Top.Height = 5.5f;
            orbits.Center.Radius = 5.8f;
            orbits.Center.Height = 2.25f;
            orbits.Bottom.Radius = 5.3f;
            orbits.Bottom.Height = 0.25f;
            orbitalFollow.Orbits = orbits;
            var verticalAxis = orbitalFollow.VerticalAxis;
            verticalAxis.Value = 16f;
            orbitalFollow.VerticalAxis = verticalAxis;
            EditorUtility.SetDirty(orbitalFollow);
        }

        private static void ConfigureSpawnPoints(
            Scene scene,
            LevelDefinition definition)
        {
            var runtime = FindSceneComponent<LevelRuntimeController>(scene);
            if (runtime == null)
            {
                throw new InvalidOperationException(
                    "Orbital Cargo scene is missing LevelRuntimeController.");
            }

            var spawns = runtime.transform.Find("Spawns") ??
                         CreateChild(runtime.transform, "Spawns").transform;
            ClearChildren(spawns);

            var playerPosition = new Vector3(-47f, 0.984f, -46f);
            var playerSpawn = CreateChild(spawns, "PlayerSpawn");
            playerSpawn.transform.SetPositionAndRotation(
                playerPosition,
                Quaternion.Euler(0f, 0f, 0f));
            playerSpawn.AddComponent<PlayerSpawnPoint>();

            var enemyData = new[]
            {
                ("sentinel_01", "elite_knight", "Assets/Prefabs/Enemies/Enemy_OverloadSentinel.prefab", new Vector3(-47f, 0f, -13f), 180f, "Enemies_Enemy_OverloadSentinel"),
                ("duelist_01", "human_duelist", "Assets/Prefabs/Enemies/Enemy_HumanDuelist.prefab", new Vector3(-57f, 0f, -25f), 150f, "Enemies_Enemy_HumanDuelist"),
                ("caster_01", "human_caster", "Assets/Prefabs/Enemies/Enemy_HumanCaster.prefab", new Vector3(-61f, 0f, -14f), 150f, "Enemies_Enemy_HumanCaster"),
                ("drone_01", "arc_drone", "Assets/Prefabs/Enemies/Enemy_ArcDroneRobot.prefab", new Vector3(-37f, 1.4f, -23f), 90f, "Enemies_Enemy_ArcDroneRobot"),
                ("lobber_01", "lobber_bot", "Assets/Prefabs/Enemies/Enemy_LobberBot.prefab", new Vector3(-33f, 1.4f, -12f), -90f, "Enemies_Enemy_LobberBot"),
                ("crawler_01", "crawler_bot", "Assets/Prefabs/Enemies/Enemy_CrawlerBot.prefab", new Vector3(-47f, 0f, -20f), 180f, "Enemies_Enemy_CrawlerBot")
            };

            foreach (var data in enemyData)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(data.Item3);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Missing enemy prefab for {data.Item1}: {data.Item3}");
                }

                var point = CreateChild(spawns, $"EnemySpawn_{data.Item1}");
                point.transform.SetPositionAndRotation(
                    data.Item4,
                    Quaternion.Euler(0f, data.Item5, 0f));
                var marker = point.AddComponent<EnemySpawnPoint>();
                marker.Configure(
                    data.Item1,
                    data.Item2,
                    prefab,
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

        private static void BuildNavigation(Scene scene)
        {
            var navigation = CreateRoot(scene, "OrbitalCargoNavigation");
            var surface = navigation.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~0;
            surface.agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID;
            surface.BuildNavMesh();
            if (surface.navMeshData == null)
            {
                throw new InvalidOperationException(
                    "Orbital Cargo NavMeshSurface did not produce NavMeshData.");
            }

            navigation.isStatic = true;
            EditorSceneManager.MarkSceneDirty(scene);
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
                throw new InvalidOperationException($"Missing model: {path}");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = scale;
            instance.isStatic = true;
            return instance;
        }

        private static GameObject PlaceLocalModel(
            Transform parent,
            string path,
            string name,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Missing model: {path}");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = localScale;
            instance.isStatic = true;
            return instance;
        }

        private static void OverrideModelMaterials(
            GameObject model,
            Material material)
        {
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var index = 0; index < materials.Length; index++)
                {
                    materials[index] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static GameObject CreateColliderCube(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 size,
            Quaternion rotation)
        {
            var colliderObject = new GameObject(name);
            colliderObject.transform.SetParent(parent, false);
            colliderObject.transform.SetPositionAndRotation(position, rotation);
            colliderObject.transform.localScale = size;
            colliderObject.AddComponent<BoxCollider>();
            colliderObject.isStatic = true;
            return colliderObject;
        }

        private static GameObject CreateLocalCollider(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 size,
            Quaternion localRotation)
        {
            var colliderObject = new GameObject(name);
            colliderObject.transform.SetParent(parent, false);
            colliderObject.transform.localPosition = localPosition;
            colliderObject.transform.localRotation = localRotation;
            colliderObject.transform.localScale = size;
            colliderObject.AddComponent<BoxCollider>();
            colliderObject.isStatic = true;
            return colliderObject;
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

        private static GameObject CreateLocalCube(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 size,
            Material material,
            Quaternion localRotation,
            bool collider = true)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localRotation = localRotation;
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
            Vector3 localPosition,
            Vector3 size,
            Material material,
            Quaternion localRotation,
            bool collider = true)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = localPosition;
            cylinder.transform.localRotation = localRotation;
            cylinder.transform.localScale = size;
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            cylinder.isStatic = true;
            if (!collider)
            {
                UnityEngine.Object.DestroyImmediate(cylinder.GetComponent<Collider>());
            }

            return cylinder;
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

        private static Material CreateEmissiveMaterial(
            string path,
            string name,
            Color baseColor,
            Color emission)
        {
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
                    name = name
                };
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_Color", baseColor);
            material.SetColor("_EmissionColor", emission);
            material.SetFloat("_Metallic", 0.35f);
            material.SetFloat("_Smoothness", 0.5f);
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
