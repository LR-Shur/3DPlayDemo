#if UNITY_EDITOR
using System;
using Train.Architecture.Assets;
using Train.Architecture.Bootstrap;
using Train.GameFlow.Data;
using Train.GameFlow.Runtime;
using Train.Gameplay.Combat;
using Train.Gameplay.Combat.Factions;
using Train.Gameplay.Player.Core;
using Train.Gameplay.Player.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Train.EditorTools.GameFlow
{
    /// <summary>
    /// 幂等构建可游玩的战斗关卡、启动场景和关卡配置资源。
    /// 同时为训练场地面绑定当前 URP 管线可稳定渲染的专用材质。
    /// </summary>
    public static class PlayableCombatSliceBuilder
    {
        private const string SourceScenePath = "Assets/Scenes/New Scene.unity";
        private const string BootScenePath = "Assets/Scenes/Boot.unity";
        private const string PlayableFolder = "Assets/Scenes/Playable";
        private const string PlayableScenePath =
            PlayableFolder + "/Level_Combat_001.unity";
        private const string LevelDataFolder = "Assets/Data/Levels";
        private const string LevelDataPath =
            LevelDataFolder + "/Level_Combat_001.asset";
        private const string ArenaArtFolder =
            "Assets/Arts/World/Arena";
        private const string ArenaFloorMaterialPath =
            ArenaArtFolder + "/ArenaFloor.mat";

        private static readonly Vector3[] EnemyPositions =
        {
            new(-46.8f, 0f, -26f),
            new(-50.8f, 0f, -28f),
            new(-42.8f, 0f, -28f)
        };

        private static readonly float[] EnemyRotations =
        {
            180f,
            135f,
            225f
        };

        [MenuItem("Tools/Train/Content/Build Playable Combat Slice")]
        public static void Build()
        {
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath))
            {
                throw new InvalidOperationException(
                    $"Source scene is missing: {SourceScenePath}");
            }

            EnsureFolder(PlayableFolder);
            EnsureFolder(LevelDataFolder);
            EnsureFolder(ArenaArtFolder);

            var definition = CreateOrUpdateLevelDefinition();
            var floorMaterial = CreateOrUpdateArenaFloorMaterial();
            CreateOrUpdatePlayableScene(definition, floorMaterial);
            CreateOrUpdateBootScene();
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(
                PlayableScenePath,
                OpenSceneMode.Single);
            Debug.Log(
                "Playable combat slice built. Run the YooAsset configurator " +
                "once so the new Playable scene folder is collected.");
        }

        private static LevelDefinition CreateOrUpdateLevelDefinition()
        {
            var definition =
                AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelDataPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<LevelDefinition>();
                AssetDatabase.CreateAsset(definition, LevelDataPath);
            }

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("_levelId").stringValue =
                "level.combat.001";
            serialized.FindProperty("_displayName").stringValue =
                "第 1 训练区";
            serialized.FindProperty("_sceneLocation").stringValue =
                AssetLocations.CombatArenaScene;
            serialized.FindProperty("_introSeconds").floatValue = 1.2f;
            serialized.FindProperty("_maxPlayerDeaths").intValue = 0;

            var spawns = serialized.FindProperty("_enemySpawns");
            spawns.arraySize = EnemyPositions.Length;
            for (var index = 0; index < EnemyPositions.Length; index++)
            {
                var spawn = spawns.GetArrayElementAtIndex(index);
                spawn.FindPropertyRelative("_spawnId").stringValue =
                    $"knight_{index + 1:00}";
                spawn.FindPropertyRelative("_archetypeId").stringValue =
                    "kaykit_knight";
                spawn.FindPropertyRelative("_prefabLocation").stringValue =
                    AssetLocations.KnightEnemyPrefab;
                spawn.FindPropertyRelative("_position").vector3Value =
                    EnemyPositions[index];
                spawn.FindPropertyRelative("_eulerAngles").vector3Value =
                    new Vector3(0f, EnemyRotations[index], 0f);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void CreateOrUpdatePlayableScene(
            LevelDefinition definition,
            Material floorMaterial)
        {
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(PlayableScenePath))
            {
                if (!AssetDatabase.CopyAsset(
                        SourceScenePath,
                        PlayableScenePath))
                {
                    throw new InvalidOperationException(
                        $"Failed to copy '{SourceScenePath}' to " +
                        $"'{PlayableScenePath}'.");
                }

                AssetDatabase.ImportAsset(PlayableScenePath);
            }

            var scene = EditorSceneManager.OpenScene(
                PlayableScenePath,
                OpenSceneMode.Single);
            var player = FindPlayer(scene);
            if (player == null)
            {
                throw new InvalidOperationException(
                    "The playable scene requires the source Player object.");
            }

            player.name = "Player";
            player.tag = "Player";
            EnsureFaction(player, CombatFaction.Player);
            FixCameraTarget(player.transform);

            var floor = FindRoot(scene, "Plane");
            if (floor == null)
            {
                floor = FindRoot(scene, "ArenaFloor");
            }

            if (floor != null)
            {
                floor.name = "ArenaFloor";
                floor.transform.localScale = new Vector3(3f, 1f, 3f);
                var floorRenderer = floor.GetComponent<Renderer>();
                if (floorRenderer == null)
                {
                    throw new InvalidOperationException(
                        "ArenaFloor 缺少 Renderer，无法绑定 URP 地面材质。");
                }

                floorRenderer.sharedMaterial = floorMaterial;
                floorRenderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                floorRenderer.receiveShadows = true;
                EditorUtility.SetDirty(floorRenderer);
            }

            var levelRoot = FindRoot(scene, "[LevelRuntime]");
            if (levelRoot == null)
            {
                levelRoot = new GameObject("[LevelRuntime]");
            }

            SceneManager.MoveGameObjectToScene(levelRoot, scene);

            var sceneBootstrap =
                GetOrAddComponent<SceneBootstrap>(levelRoot);
            _ = sceneBootstrap;

            var spawnsRoot = GetOrCreateChild(
                levelRoot.transform,
                "Spawns");
            var actorsRoot = GetOrCreateChild(
                levelRoot.transform,
                "SpawnedActors");

            var playerSpawn = GetOrCreateChild(
                spawnsRoot,
                "PlayerSpawn");
            playerSpawn.SetPositionAndRotation(
                player.transform.position,
                player.transform.rotation);

            for (var index = 0; index < EnemyPositions.Length; index++)
            {
                var marker = GetOrCreateChild(
                    spawnsRoot,
                    $"EnemySpawn_{index + 1:00}");
                marker.SetPositionAndRotation(
                    EnemyPositions[index],
                    Quaternion.Euler(
                        0f,
                        EnemyRotations[index],
                        0f));
            }

            var respawn = player.GetComponent<PlayerRespawnController>();
            if (respawn != null)
            {
                SetObjectReference(respawn, "_spawnPoint", playerSpawn);
            }

            var runtime =
                GetOrAddComponent<LevelRuntimeController>(levelRoot);
            SetObjectReference(runtime, "_definition", definition);
            SetObjectReference(runtime, "_spawnedActorsRoot", actorsRoot);
            SetObjectReference(
                runtime,
                "_playerInput",
                player.GetComponent<PlayerInputReader>());
            SetObjectReference(
                runtime,
                "_playerCombat",
                player.GetComponent<PlayerCombat>());
            SetObjectReference(
                runtime,
                "_playerHealth",
                player.GetComponent<Health>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, PlayableScenePath);
        }

        /// <summary>
        /// 创建或更新训练场专用的 URP Lit 材质，并保持已有资源 GUID 不变。
        /// </summary>
        private static Material CreateOrUpdateArenaFloorMaterial()
        {
            var shader =
                Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "当前项目未找到 Universal Render Pipeline/Lit Shader。");
            }

            var material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    ArenaFloorMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(
                    material,
                    ArenaFloorMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            var floorColor = new Color32(27, 36, 49, 255);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", floorColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", floorColor);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0.18f);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.42f);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", Color.black);
            }

            material.DisableKeyword("_EMISSION");
            material.globalIlluminationFlags =
                MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateOrUpdateBootScene()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath)
                ? EditorSceneManager.OpenScene(
                    BootScenePath,
                    OpenSceneMode.Single)
                : EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);

            var launcherObject = FindRoot(scene, "[LevelSceneLauncher]");
            if (launcherObject == null)
            {
                launcherObject = new GameObject("[LevelSceneLauncher]");
            }

            SceneManager.MoveGameObjectToScene(launcherObject, scene);
            var launcher =
                GetOrAddComponent<LevelSceneLauncher>(launcherObject);
            var serialized = new SerializedObject(launcher);
            serialized.FindProperty("_levelDefinitionLocation").stringValue =
                AssetLocations.CombatArenaDefinition;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var cameraObject = FindRoot(scene, "Boot Camera");
            if (cameraObject == null)
            {
                cameraObject = new GameObject("Boot Camera");
            }

            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.tag = "MainCamera";
            var camera = GetOrAddComponent<Camera>(cameraObject);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(8, 13, 25, 255);
            if (cameraObject.GetComponent<AudioListener>() == null)
            {
                cameraObject.AddComponent<AudioListener>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, BootScenePath);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootScenePath, true)
            };
        }

        private static GameObject FindPlayer(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.CompareTag("Player") ||
                    root.name is "Player" or "Player_Ellen")
                {
                    return root;
                }
            }

            return null;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }

        private static Transform GetOrCreateChild(
            Transform parent,
            string name)
        {
            var child = parent.Find(name);
            if (child != null)
            {
                return child;
            }

            var childObject = new GameObject(name);
            child = childObject.transform;
            child.SetParent(parent, false);
            return child;
        }

        private static void EnsureFaction(
            GameObject target,
            CombatFaction faction)
        {
            var member = GetOrAddComponent<FactionMember>(target);
            member.Configure(faction);
            target.GetComponent<PlayerCombat>()?.ConfigureFaction(faction);
        }

        private static void FixCameraTarget(Transform player)
        {
            var target = player.Find("PlayerCameraTarget");
            if (target != null)
            {
                target.localPosition = Vector3.up * 1.45f;
                target.localRotation = Quaternion.identity;
            }
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
                    $"{target.GetType().Name} has no serialized property " +
                    $"'{propertyName}'.");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static T GetOrAddComponent<T>(GameObject target)
            where T : Component
        {
            var component = target.GetComponent<T>();
            if (component == null)
            {
                component = target.AddComponent<T>();
            }

            return component;
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
