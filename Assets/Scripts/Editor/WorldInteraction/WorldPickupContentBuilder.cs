#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Train.Gameplay.Player.Input;
using Train.WorldInteraction.Presentation;
using Train.WorldInteraction.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Train.EditorTools.WorldInteraction
{
    /// <summary>
    /// 构建世界拾取物预制体、材质、玩家交互组件和首个战斗关卡中的拾取内容。
    /// 构建器只重建自己命名的八个拾取实例，不删除或改写场景中的其他对象。
    /// </summary>
    public static class WorldPickupContentBuilder
    {
        private const string PlayerPrefabPath =
            "Assets/Prefabs/Player/Player_Ellen.prefab";
        private const string WorldPrefabFolder =
            "Assets/Prefabs/World";
        private const string PickupPrefabPath =
            WorldPrefabFolder + "/WorldItemPickup.prefab";
        private const string MaterialFolder =
            "Assets/Arts/World/Pickups";
        private const string BaseMaterialPath =
            MaterialFolder + "/PickupBase.mat";
        private const string GlowMaterialPath =
            MaterialFolder + "/PickupGlow.mat";
        private const string CrystalMaterialPath =
            MaterialFolder + "/PickupCrystal.mat";
        private const string PlayableScenePath =
            "Assets/Scenes/Playable/Level_Combat_001.unity";
        private const string PickupsRootName = "[WorldPickups]";
        private const string ElectricIconPath =
            "Assets/Arts/UI/Icons/GameIcons/electric.png";
        private const string CrosshairIconPath =
            "Assets/Arts/UI/KenneySciFi/PNG/Blue/Default/" +
            "crosshair_color_a.png";
        private const float MarkerHeight = 1.05f;
        private const float CrosshairWorldSize = 0.28f;
        private const float ElectricIconWorldSize = 0.13f;

        // 当前物品数据库使用 thunder_blade / thunder_ring。
        // weapon.thunder_edge 是伤害来源 ID，并非 Inventory ItemId；
        // accessory.volt_ring 也没有登记为物品，因此不能直接用于世界拾取。
        private static readonly PickupSeed[] PickupSeeds =
        {
            new PickupSeed(
                "WP_Pickup_01_TrainingChip",
                "level.combat.001.pickup.01",
                "training_chip",
                3,
                10,
                new Vector3(-45.35f, 0.04f, -30.15f),
                15f),
            new PickupSeed(
                "WP_Pickup_02_HealingCanister",
                "level.combat.001.pickup.02",
                "healing_canister",
                1,
                20,
                new Vector3(-48.3f, 0.04f, -30.75f),
                -20f),
            new PickupSeed(
                "WP_Pickup_03_UpgradeModule",
                "level.combat.001.pickup.03",
                "upgrade_module",
                2,
                25,
                new Vector3(-46.75f, 0.04f, -33f),
                30f),
            new PickupSeed(
                "WP_Pickup_04_ThunderBlade",
                "level.combat.001.pickup.04",
                "thunder_blade",
                1,
                60,
                new Vector3(-43.45f, 0.04f, -28.45f),
                70f),
            new PickupSeed(
                "WP_Pickup_05_ThunderRing",
                "level.combat.001.pickup.05",
                "thunder_ring",
                1,
                55,
                new Vector3(-50.65f, 0.04f, -27.8f),
                -55f),
            new PickupSeed(
                "WP_Pickup_06_CityToken",
                "level.combat.001.pickup.06",
                "city_token",
                40,
                8,
                new Vector3(-46.8f, 0.04f, -23.9f),
                5f),
            new PickupSeed(
                "WP_Pickup_07_NeonCapacitor",
                "level.combat.001.pickup.07",
                "neon_capacitor",
                1,
                35,
                new Vector3(-40.9f, 0.04f, -25.7f),
                115f),
            new PickupSeed(
                "WP_Pickup_08_EmergencyCore",
                "level.combat.001.pickup.08",
                "emergency_core",
                1,
                35,
                new Vector3(-54f, 0.04f, -34f),
                -110f)
        };

        /// <summary>
        /// 创建或更新全部世界拾取内容。
        /// </summary>
        [MenuItem("Tools/Train/Content/Build World Pickups")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning(
                    "世界拾取内容只能在编辑模式构建；请先退出 Play Mode。");
                return;
            }

            EnsureRequiredAssetsExist();
            EnsureFolder(WorldPrefabFolder);
            EnsureFolder(MaterialFolder);

            InstallPlayerInteractionController();

            var baseMaterial = CreateOrUpdateMaterial(
                BaseMaterialPath,
                new Color32(18, 27, 43, 255),
                new Color(0.02f, 0.08f, 0.12f),
                metallic: 0.82f,
                smoothness: 0.72f);
            var glowMaterial = CreateOrUpdateMaterial(
                GlowMaterialPath,
                new Color32(32, 215, 255, 255),
                new Color(0.05f, 2.8f, 4.5f),
                metallic: 0.18f,
                smoothness: 0.9f);
            var crystalMaterial = CreateOrUpdateMaterial(
                CrystalMaterialPath,
                new Color32(118, 245, 255, 255),
                new Color(0.18f, 4.8f, 6.2f),
                metallic: 0.35f,
                smoothness: 0.94f);

            var pickupPrefab = CreateOrUpdatePickupPrefab(
                baseMaterial,
                glowMaterial,
                crystalMaterial);

            AssetDatabase.SaveAssets();
            CreateOrUpdateScenePickups(pickupPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidatePlayerPrefab();
            ValidatePickupPrefab();

            Debug.Log(
                "世界拾取内容构建完成：玩家交互组件、科幻拾取预制体、" +
                "3 个 URP 材质和 Level_Combat_001 中的 8 个拾取实例均已更新。");
        }

        /// <summary>
        /// 检查构建过程依赖的玩家预制体、关卡和图标是否存在。
        /// </summary>
        private static void EnsureRequiredAssetsExist()
        {
            RequireAsset<GameObject>(PlayerPrefabPath);
            RequireAsset<SceneAsset>(PlayableScenePath);
            RequireAsset<Sprite>(ElectricIconPath);
        }

        /// <summary>
        /// 在玩家预制体根节点幂等安装附近扫描与交互控制器。
        /// </summary>
        private static void InstallPlayerInteractionController()
        {
            var contents =
                PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var controller =
                    contents.GetComponent<PlayerInteractionController>();
                var changed = false;
                if (controller == null)
                {
                    controller =
                        contents.AddComponent<PlayerInteractionController>();
                    changed = true;
                }

                var serialized = new SerializedObject(controller);
                var input = contents.GetComponent<PlayerInputReader>();
                changed |= SetObjectReferenceIfDifferent(
                    serialized,
                    "_input",
                    input);
                changed |= SetFloatIfDifferent(
                    serialized,
                    "_interactionRange",
                    3.25f);
                changed |= SetFloatIfDifferent(
                    serialized,
                    "_scanInterval",
                    0.08f);
                changed |= SetIntegerIfDifferent(
                    serialized,
                    "_interactionLayers",
                    -1);
                changed |= SetVector3IfDifferent(
                    serialized,
                    "_scanCenterOffset",
                    new Vector3(0f, 0.9f, 0f));
                changed |=
                    serialized.ApplyModifiedPropertiesWithoutUndo();

                if (!changed)
                {
                    return;
                }

                PrefabUtility.SaveAsPrefabAsset(
                    contents,
                    PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>
        /// 创建或覆盖构建器自有的世界拾取预制体，同时保持预制体 GUID 不变。
        /// </summary>
        private static GameObject CreateOrUpdatePickupPrefab(
            Material baseMaterial,
            Material glowMaterial,
            Material crystalMaterial)
        {
            var root = new GameObject("WorldItemPickup");
            try
            {
                var pickup = root.AddComponent<WorldItemPickup>();
                var scanCollider = root.AddComponent<SphereCollider>();
                scanCollider.isTrigger = true;
                scanCollider.center = new Vector3(0f, 0.4f, 0f);
                scanCollider.radius = 0.44f;

                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "BasePlate",
                    root.transform,
                    new Vector3(0f, 0.05f, 0f),
                    new Vector3(0.48f, 0.05f, 0.48f),
                    Quaternion.identity,
                    baseMaterial);
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "EnergyPlate",
                    root.transform,
                    new Vector3(0f, 0.11f, 0f),
                    new Vector3(0.39f, 0.022f, 0.39f),
                    Quaternion.identity,
                    glowMaterial);
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "CoreSocket",
                    root.transform,
                    new Vector3(0f, 0.15f, 0f),
                    new Vector3(0.21f, 0.055f, 0.21f),
                    Quaternion.identity,
                    baseMaterial);

                CreateCornerStruts(
                    root.transform,
                    baseMaterial,
                    glowMaterial);

                var crystal = CreatePrimitive(
                    PrimitiveType.Cube,
                    "PickupCrystal",
                    root.transform,
                    new Vector3(0f, 0.47f, 0f),
                    new Vector3(0.2f, 0.32f, 0.2f),
                    Quaternion.Euler(0f, 45f, 45f),
                    crystalMaterial);

                var innerCrystal = CreatePrimitive(
                    PrimitiveType.Cube,
                    "CrystalCore",
                    crystal.transform,
                    Vector3.zero,
                    Vector3.one * 0.42f,
                    Quaternion.Euler(20f, 0f, 20f),
                    glowMaterial);
                innerCrystal.transform.localPosition = Vector3.zero;

                var lightObject = new GameObject("PickupLight");
                lightObject.transform.SetParent(root.transform, false);
                lightObject.transform.localPosition =
                    new Vector3(0f, 0.58f, 0f);
                var pickupLight = lightObject.AddComponent<Light>();
                pickupLight.type = LightType.Point;
                pickupLight.color = new Color(0.2f, 0.9f, 1f);
                pickupLight.intensity = 1.35f;
                pickupLight.range = 1.9f;
                pickupLight.shadows = LightShadows.None;

                var interactionPoint =
                    new GameObject("InteractionPoint").transform;
                interactionPoint.SetParent(root.transform, false);
                interactionPoint.localPosition =
                    new Vector3(0f, 0.58f, 0f);

                var markerRoot = CreateMarker(root.transform);
                pickup.Configure(
                    "world.pickup.prefab",
                    "training_chip",
                    1,
                    10);
                pickup.ConfigurePresentation(
                    interactionPoint,
                    markerRoot);

                var savedPrefab = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    PickupPrefabPath);
                if (savedPrefab == null)
                {
                    throw new InvalidOperationException(
                        $"无法保存世界拾取预制体：'{PickupPrefabPath}'。");
                }

                return savedPrefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 创建底座四角的机械支架与小型发光节点。
        /// </summary>
        private static void CreateCornerStruts(
            Transform parent,
            Material baseMaterial,
            Material glowMaterial)
        {
            var positions = new[]
            {
                new Vector3(0.31f, 0.15f, 0.31f),
                new Vector3(-0.31f, 0.15f, 0.31f),
                new Vector3(0.31f, 0.15f, -0.31f),
                new Vector3(-0.31f, 0.15f, -0.31f)
            };

            for (var index = 0; index < positions.Length; index++)
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    $"CornerStrut_{index + 1:00}",
                    parent,
                    positions[index],
                    new Vector3(0.08f, 0.16f, 0.08f),
                    Quaternion.Euler(0f, 45f, 0f),
                    baseMaterial);
                CreatePrimitive(
                    PrimitiveType.Cube,
                    $"CornerGlow_{index + 1:00}",
                    parent,
                    positions[index] + Vector3.up * 0.12f,
                    new Vector3(0.045f, 0.022f, 0.045f),
                    Quaternion.Euler(0f, 45f, 0f),
                    glowMaterial);
            }
        }

        /// <summary>
        /// 创建始终朝向相机、上下浮动并旋转的拾取光标。
        /// </summary>
        private static GameObject CreateMarker(Transform parent)
        {
            var markerRoot = new GameObject("MarkerRoot");
            markerRoot.transform.SetParent(parent, false);
            markerRoot.transform.localPosition =
                new Vector3(0f, MarkerHeight, 0f);
            markerRoot.AddComponent<WorldPickupMarker>();

            var crosshairSprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(CrosshairIconPath);
            if (crosshairSprite != null)
            {
                CreateSpriteVisual(
                    "Crosshair",
                    markerRoot.transform,
                    crosshairSprite,
                    new Color(0.25f, 0.92f, 1f, 0.92f),
                    CrosshairWorldSize,
                    100,
                    0f);
            }

            var electricSprite =
                RequireAsset<Sprite>(ElectricIconPath);
            CreateSpriteVisual(
                "ElectricIcon",
                markerRoot.transform,
                electricSprite,
                Color.white,
                ElectricIconWorldSize,
                101,
                -0.01f);
            return markerRoot;
        }

        /// <summary>
        /// 创建一个具有确定世界尺寸和排序层级的 SpriteRenderer 子物体。
        /// </summary>
        private static void CreateSpriteVisual(
            string name,
            Transform parent,
            Sprite sprite,
            Color color,
            float desiredWorldSize,
            int sortingOrder,
            float localZ)
        {
            var visual = new GameObject(name);
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition =
                new Vector3(0f, 0f, localZ);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            var maxSize = Mathf.Max(
                sprite.bounds.size.x,
                sprite.bounds.size.y);
            var scale = maxSize > 0f
                ? desiredWorldSize / maxSize
                : 1f;
            visual.transform.localScale =
                Vector3.one * scale;
        }

        /// <summary>
        /// 创建一个仅用于视觉表现的 Unity 基础网格，并移除自动生成的碰撞体。
        /// </summary>
        private static GameObject CreatePrimitive(
            PrimitiveType primitiveType,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            var visual = GameObject.CreatePrimitive(primitiveType);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = localRotation;
            visual.transform.localScale = localScale;

            var collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            return visual;
        }

        /// <summary>
        /// 在指定路径创建或更新一个 URP Lit 发光材质。
        /// </summary>
        private static Material CreateOrUpdateMaterial(
            string path,
            Color baseColor,
            Color emissionColor,
            float metallic,
            float smoothness)
        {
            var shader =
                Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "未找到 URP Lit 或 Standard Shader，无法创建拾取材质。");
            }

            var material =
                AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            SetMaterialColor(
                material,
                "_BaseColor",
                baseColor);
            SetMaterialColor(
                material,
                "_Color",
                baseColor);
            SetMaterialColor(
                material,
                "_EmissionColor",
                emissionColor);
            SetMaterialFloat(
                material,
                "_Metallic",
                metallic);
            SetMaterialFloat(
                material,
                "_Smoothness",
                smoothness);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags =
                MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// 仅在材质声明目标颜色属性时写入值。
        /// </summary>
        private static void SetMaterialColor(
            Material material,
            string propertyName,
            Color value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
            }
        }

        /// <summary>
        /// 仅在材质声明目标浮点属性时写入值。
        /// </summary>
        private static void SetMaterialFloat(
            Material material,
            string propertyName,
            float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        /// <summary>
        /// 在战斗场景中幂等创建构建器管理的八个拾取实例并保存场景。
        /// </summary>
        private static void CreateOrUpdateScenePickups(
            GameObject pickupPrefab)
        {
            var scene = SceneManager.GetSceneByPath(PlayableScenePath);
            var openedByBuilder = !scene.IsValid() || !scene.isLoaded;
            if (openedByBuilder)
            {
                scene = EditorSceneManager.OpenScene(
                    PlayableScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                var root = FindSceneRoot(scene, PickupsRootName);
                if (root == null)
                {
                    root = new GameObject(PickupsRootName);
                    SceneManager.MoveGameObjectToScene(root, scene);
                }

                for (var index = 0; index < PickupSeeds.Length; index++)
                {
                    RecreateManagedPickup(
                        scene,
                        root.transform,
                        pickupPrefab,
                        PickupSeeds[index]);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(
                        scene,
                        PlayableScenePath))
                {
                    throw new InvalidOperationException(
                        $"保存战斗场景失败：'{PlayableScenePath}'。");
                }

                ValidateScenePickups(scene, pickupPrefab);
                if (scene.isDirty)
                {
                    throw new InvalidOperationException(
                        "世界拾取实例验证完成后场景仍为未保存状态。");
                }
            }
            finally
            {
                if (openedByBuilder &&
                    scene.IsValid() &&
                    scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(
                        scene,
                        removeScene: true);
                }
            }
        }

        /// <summary>
        /// 只替换一个构建器专属命名的拾取实例，不触碰同根节点下其他内容。
        /// </summary>
        private static void RecreateManagedPickup(
            Scene scene,
            Transform parent,
            GameObject pickupPrefab,
            PickupSeed seed)
        {
            for (var index = parent.childCount - 1; index >= 0; index--)
            {
                var child = parent.GetChild(index);
                if (child.name == seed.ObjectName)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            var instance = PrefabUtility.InstantiatePrefab(
                pickupPrefab,
                scene) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    $"无法实例化世界拾取预制体：'{PickupPrefabPath}'。");
            }

            instance.name = seed.ObjectName;
            instance.transform.SetParent(parent, worldPositionStays: true);
            instance.transform.SetPositionAndRotation(
                seed.Position,
                Quaternion.Euler(0f, seed.YawDegrees, 0f));
            instance.transform.localScale = Vector3.one;

            var pickup = instance.GetComponent<WorldItemPickup>();
            if (pickup == null)
            {
                throw new InvalidOperationException(
                    "WorldItemPickup.prefab 根节点缺少 WorldItemPickup 组件。");
            }

            pickup.Configure(
                seed.InteractionId,
                seed.ItemId,
                seed.Quantity,
                seed.Priority);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            PrefabUtility.RecordPrefabInstancePropertyModifications(pickup);
            PrefabUtility.RecordPrefabInstancePropertyModifications(
                instance.transform);
            EditorUtility.SetDirty(instance);
            EditorUtility.SetDirty(pickup);
        }

        /// <summary>
        /// 验证玩家预制体根节点已经安装交互控制器。
        /// </summary>
        private static void ValidatePlayerPrefab()
        {
            var player = RequireAsset<GameObject>(PlayerPrefabPath);
            if (player.GetComponent<PlayerInteractionController>() == null)
            {
                throw new InvalidOperationException(
                    "Player_Ellen.prefab 根节点未安装 PlayerInteractionController。");
            }
        }

        /// <summary>
        /// 验证拾取预制体的扫描碰撞体、光标和序列化引用完整。
        /// </summary>
        private static void ValidatePickupPrefab()
        {
            var prefab = RequireAsset<GameObject>(PickupPrefabPath);
            var pickup = prefab.GetComponent<WorldItemPickup>();
            if (pickup == null)
            {
                throw new InvalidOperationException(
                    "WorldItemPickup.prefab 缺少 WorldItemPickup 组件。");
            }

            var collider = prefab.GetComponent<Collider>();
            if (collider == null || !collider.isTrigger)
            {
                throw new InvalidOperationException(
                    "WorldItemPickup.prefab 根节点需要启用 Trigger 的扫描碰撞体。");
            }

            var marker =
                prefab.GetComponentInChildren<WorldPickupMarker>(true);
            if (marker == null)
            {
                throw new InvalidOperationException(
                    "WorldItemPickup.prefab 缺少 WorldPickupMarker。");
            }

            var serialized = new SerializedObject(pickup);
            var interactionPoint =
                serialized.FindProperty("_interactionPoint")
                    .objectReferenceValue;
            var markerRoot =
                serialized.FindProperty("_markerRoot")
                    .objectReferenceValue;
            if (interactionPoint == null || markerRoot == null)
            {
                throw new InvalidOperationException(
                    "WorldItemPickup 的 InteractionPoint 或 MarkerRoot 引用为空。");
            }
        }

        /// <summary>
        /// 验证战斗场景包含八个唯一、可追踪且来自目标预制体的拾取实例。
        /// </summary>
        private static void ValidateScenePickups(
            Scene scene,
            GameObject pickupPrefab)
        {
            var root = FindSceneRoot(scene, PickupsRootName);
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"战斗场景缺少根节点 '{PickupsRootName}'。");
            }

            var interactionIds =
                new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < PickupSeeds.Length; index++)
            {
                var seed = PickupSeeds[index];
                var child = FindDirectChild(
                    root.transform,
                    seed.ObjectName);
                if (child == null)
                {
                    throw new InvalidOperationException(
                        $"战斗场景缺少拾取实例 '{seed.ObjectName}'。");
                }

                var pickup =
                    child.GetComponent<WorldItemPickup>();
                if (pickup == null ||
                    pickup.InteractionId != seed.InteractionId ||
                    pickup.ItemId != seed.ItemId ||
                    pickup.Quantity != seed.Quantity ||
                    pickup.Priority != seed.Priority)
                {
                    throw new InvalidOperationException(
                        $"拾取实例 '{seed.ObjectName}' 的配置与构建数据不一致。");
                }

                if (!interactionIds.Add(pickup.InteractionId))
                {
                    throw new InvalidOperationException(
                        $"拾取交互 ID 重复：'{pickup.InteractionId}'。");
                }

                var source =
                    PrefabUtility.GetCorrespondingObjectFromSource(
                        child.gameObject);
                if (source != pickupPrefab)
                {
                    throw new InvalidOperationException(
                        $"拾取实例 '{seed.ObjectName}' 不是目标预制体的实例。");
                }
            }
        }

        /// <summary>
        /// 在场景根节点中按完整名称查找对象。
        /// </summary>
        private static GameObject FindSceneRoot(
            Scene scene,
            string name)
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

        /// <summary>
        /// 只在直接子节点中查找对象，避免误匹配预制体内部同名节点。
        /// </summary>
        private static Transform FindDirectChild(
            Transform parent,
            string name)
        {
            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index);
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        /// <summary>
        /// 按路径读取必需资产，缺失时抛出包含路径的清晰异常。
        /// </summary>
        private static T RequireAsset<T>(string path)
            where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) ??
                   throw new InvalidOperationException(
                       $"世界拾取构建依赖资产不存在：'{path}'。");
        }

        /// <summary>
        /// 递归确保 Unity 资产文件夹存在。
        /// </summary>
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var slash = path.LastIndexOf('/');
            if (slash <= 0)
            {
                throw new ArgumentException(
                    $"无效的 Unity 资产文件夹路径：'{path}'。",
                    nameof(path));
            }

            var parent = path.Substring(0, slash);
            var folderName = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        /// <summary>
        /// 当对象引用变化时写入 SerializedObject。
        /// </summary>
        private static bool SetObjectReferenceIfDifferent(
            SerializedObject serialized,
            string propertyName,
            Object value)
        {
            var property = RequireProperty(
                serialized,
                propertyName);
            if (property.objectReferenceValue == value)
            {
                return false;
            }

            property.objectReferenceValue = value;
            return true;
        }

        /// <summary>
        /// 当浮点值变化时写入 SerializedObject。
        /// </summary>
        private static bool SetFloatIfDifferent(
            SerializedObject serialized,
            string propertyName,
            float value)
        {
            var property = RequireProperty(
                serialized,
                propertyName);
            if (Mathf.Approximately(
                    property.floatValue,
                    value))
            {
                return false;
            }

            property.floatValue = value;
            return true;
        }

        /// <summary>
        /// 当整数值变化时写入 SerializedObject。
        /// </summary>
        private static bool SetIntegerIfDifferent(
            SerializedObject serialized,
            string propertyName,
            int value)
        {
            var property = RequireProperty(
                serialized,
                propertyName);
            if (property.intValue == value)
            {
                return false;
            }

            property.intValue = value;
            return true;
        }

        /// <summary>
        /// 当三维向量变化时写入 SerializedObject。
        /// </summary>
        private static bool SetVector3IfDifferent(
            SerializedObject serialized,
            string propertyName,
            Vector3 value)
        {
            var property = RequireProperty(
                serialized,
                propertyName);
            if (property.vector3Value == value)
            {
                return false;
            }

            property.vector3Value = value;
            return true;
        }

        /// <summary>
        /// 读取序列化字段，字段不存在时抛出便于定位重构问题的异常。
        /// </summary>
        private static SerializedProperty RequireProperty(
            SerializedObject serialized,
            string propertyName)
        {
            return serialized.FindProperty(propertyName) ??
                   throw new InvalidOperationException(
                       $"{serialized.targetObject.GetType().Name} " +
                       $"缺少序列化字段 '{propertyName}'。");
        }

        /// <summary>
        /// 描述一个需要落到战斗场景中的确定性拾取实例。
        /// </summary>
        private readonly struct PickupSeed
        {
            /// <summary>
            /// 创建一个场景拾取构建记录。
            /// </summary>
            public PickupSeed(
                string objectName,
                string interactionId,
                string itemId,
                int quantity,
                int priority,
                Vector3 position,
                float yawDegrees)
            {
                ObjectName = objectName;
                InteractionId = interactionId;
                ItemId = itemId;
                Quantity = quantity;
                Priority = priority;
                Position = position;
                YawDegrees = yawDegrees;
            }

            /// <summary>
            /// 构建器专属场景对象名称。
            /// </summary>
            public string ObjectName { get; }

            /// <summary>
            /// 场景内唯一交互 ID。
            /// </summary>
            public string InteractionId { get; }

            /// <summary>
            /// InventorySettings 中实际登记的物品 ID。
            /// </summary>
            public string ItemId { get; }

            /// <summary>
            /// 拾取后写入背包的数量。
            /// </summary>
            public int Quantity { get; }

            /// <summary>
            /// 多个候选同时出现时的交互优先级。
            /// </summary>
            public int Priority { get; }

            /// <summary>
            /// 拾取实例的世界坐标。
            /// </summary>
            public Vector3 Position { get; }

            /// <summary>
            /// 拾取底座绕世界 Y 轴的旋转角。
            /// </summary>
            public float YawDegrees { get; }
        }
    }
}
#endif
