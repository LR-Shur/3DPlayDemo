#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Train.WorldInteraction.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Train.EditorTools.Dialogue
{
    /// <summary>
    /// 以幂等方式生成世界对话终端预制体，并把三组对话点放入战斗关卡。
    /// 构建器只管理自己的根节点和实例，不触碰场景中的其他内容。
    /// </summary>
    public static class DialogueTerminalContentBuilder
    {
        private const string PrefabFolder = "Assets/Prefabs/World";
        private const string PrefabPath =
            PrefabFolder + "/WorldDialogueTerminal.prefab";
        private const string MaterialFolder =
            "Assets/Arts/World/Dialogue";
        private const string BaseMaterialPath =
            MaterialFolder + "/DialogueBase.mat";
        private const string ScreenMaterialPath =
            MaterialFolder + "/DialogueScreen.mat";
        private const string PlayableScenePath =
            "Assets/Scenes/Playable/Level_Combat_001.unity";
        private const string TerminalsRootName = "[WorldDialogueTerminals]";

        private static readonly TerminalSeed[] Seeds =
        {
            new(
                "WD_01_TrainingOperator",
                "level.combat.001.dialogue.01",
                "dialogue_training_operator",
                "训练终端",
                35,
                new Vector3(-47.2f, 0f, -25.4f),
                0f),
            new(
                "WD_02_Rusk",
                "level.combat.001.dialogue.02",
                "dialogue_rusk_first_meet",
                "Rusk",
                40,
                new Vector3(-51.6f, 0f, -31.4f),
                45f),
            new(
                "WD_03_PatrolReport",
                "level.combat.001.dialogue.03",
                "dialogue_patrol_report",
                "巡逻队联络点",
                30,
                new Vector3(-42.2f, 0f, -34.2f),
                -30f)
        };

        /// <summary>
        /// 生成或更新全部世界对话终端内容。
        /// </summary>
        [MenuItem("Tools/Train/Content/Build Dialogue Terminals")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning(
                    "世界对话终端只能在编辑模式构建；请先退出 Play Mode。");
                return;
            }

            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);

            var baseMaterial = CreateOrUpdateMaterial(
                BaseMaterialPath,
                new Color32(24, 34, 50, 255),
                new Color(0.02f, 0.04f, 0.08f),
                0.82f,
                0.74f);
            var screenMaterial = CreateOrUpdateMaterial(
                ScreenMaterialPath,
                new Color32(38, 225, 255, 255),
                new Color(0.12f, 3.4f, 4.8f),
                0.2f,
                0.9f);

            var prefab = CreateOrUpdatePrefab(
                baseMaterial,
                screenMaterial);

            AssetDatabase.SaveAssets();
            CreateOrUpdateSceneTerminals(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidatePrefab(prefab);
            Debug.Log(
                $"对话终端内容构建完成：预制体、2 个 URP 材质和 " +
                $"{Seeds.Length} 个场景对话点均已更新。");
        }

        /// <summary>
        /// 创建或覆盖构建器自有的世界对话终端预制体，保持 GUID 不变。
        /// </summary>
        private static GameObject CreateOrUpdatePrefab(
            Material baseMaterial,
            Material screenMaterial)
        {
            var root = new GameObject("WorldDialogueTerminal");
            try
            {
                var terminal = root.AddComponent<WorldDialogueTerminal>();
                var scanCollider = root.AddComponent<SphereCollider>();
                scanCollider.isTrigger = true;
                scanCollider.center = new Vector3(0f, 1f, 0f);
                scanCollider.radius = 1.35f;

                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "BasePlate",
                    root.transform,
                    new Vector3(0f, 0.05f, 0f),
                    new Vector3(0.6f, 0.05f, 0.6f),
                    Quaternion.identity,
                    baseMaterial);
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "Pole",
                    root.transform,
                    new Vector3(0f, 0.85f, 0f),
                    new Vector3(0.07f, 0.85f, 0.07f),
                    Quaternion.identity,
                    baseMaterial);

                var screen = CreatePrimitive(
                    PrimitiveType.Cube,
                    "HoloScreen",
                    root.transform,
                    new Vector3(0f, 1.32f, 0f),
                    new Vector3(0.58f, 0.42f, 0.08f),
                    Quaternion.identity,
                    screenMaterial);
                screen.transform.localRotation =
                    Quaternion.Euler(0f, 0f, 0f);

                var lightObject = new GameObject("TerminalLight");
                lightObject.transform.SetParent(root.transform, false);
                lightObject.transform.localPosition =
                    new Vector3(0f, 1.5f, 0.3f);
                var terminalLight = lightObject.AddComponent<Light>();
                terminalLight.type = LightType.Point;
                terminalLight.color = new Color(0.18f, 0.92f, 1f);
                terminalLight.intensity = 1.1f;
                terminalLight.range = 2.2f;
                terminalLight.shadows = LightShadows.None;

                var interactionPoint =
                    new GameObject("InteractionPoint").transform;
                interactionPoint.SetParent(root.transform, false);
                interactionPoint.localPosition =
                    new Vector3(0f, 1.1f, 0f);

                terminal.Configure(
                    "world.dialogue.prefab",
                    "dialogue_training_operator",
                    "训练终端",
                    35,
                    interactionPoint);

                var saved = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    PrefabPath);
                if (saved == null)
                {
                    throw new InvalidOperationException(
                        $"无法保存世界对话终端预制体：'{PrefabPath}'。");
                }

                return saved;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 在战斗场景中幂等创建三个对话点并保存场景。
        /// </summary>
        private static void CreateOrUpdateSceneTerminals(
            GameObject prefab)
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
                var root = FindSceneRoot(scene, TerminalsRootName);
                if (root == null)
                {
                    root = new GameObject(TerminalsRootName);
                    SceneManager.MoveGameObjectToScene(root, scene);
                }

                for (var index = 0; index < Seeds.Length; index++)
                {
                    RecreateManagedTerminal(
                        scene,
                        root.transform,
                        prefab,
                        Seeds[index]);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(
                        scene,
                        PlayableScenePath))
                {
                    throw new InvalidOperationException(
                        $"保存战斗场景失败：'{PlayableScenePath}'。");
                }

                ValidateSceneTerminals(scene, prefab);
                if (scene.isDirty)
                {
                    throw new InvalidOperationException(
                        "对话终端验证完成后场景仍为未保存状态。");
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
        /// 只替换一个构建器专属命名的对话终端实例。
        /// </summary>
        private static void RecreateManagedTerminal(
            Scene scene,
            Transform parent,
            GameObject prefab,
            TerminalSeed seed)
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
                prefab,
                scene) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    $"无法实例化对话终端预制体：'{PrefabPath}'。");
            }

            instance.name = seed.ObjectName;
            instance.transform.SetParent(parent, worldPositionStays: true);
            instance.transform.SetPositionAndRotation(
                seed.Position,
                Quaternion.Euler(0f, seed.YawDegrees, 0f));

            var terminal =
                instance.GetComponent<WorldDialogueTerminal>();
            if (terminal == null)
            {
                throw new InvalidOperationException(
                    "WorldDialogueTerminal.prefab 根节点缺少终端组件。");
            }

            terminal.Configure(
                seed.InteractionId,
                seed.DialogueId,
                seed.DisplayName,
                seed.Priority);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            PrefabUtility.RecordPrefabInstancePropertyModifications(terminal);
            PrefabUtility.RecordPrefabInstancePropertyModifications(
                instance.transform);
            EditorUtility.SetDirty(instance);
            EditorUtility.SetDirty(terminal);
        }

        /// <summary>
        /// 验证对话终端预制体的组件与碰撞配置完整。
        /// </summary>
        private static void ValidatePrefab(GameObject prefab)
        {
            var terminal =
                prefab.GetComponent<WorldDialogueTerminal>();
            if (terminal == null)
            {
                throw new InvalidOperationException(
                    "WorldDialogueTerminal.prefab 缺少终端组件。");
            }

            var collider = prefab.GetComponent<Collider>();
            if (collider == null || !collider.isTrigger)
            {
                throw new InvalidOperationException(
                    "WorldDialogueTerminal.prefab 需要启用 Trigger 的扫描碰撞体。");
            }

            var serialized = new SerializedObject(terminal);
            if (serialized.FindProperty("_interactionPoint")
                    .objectReferenceValue == null)
            {
                throw new InvalidOperationException(
                    "WorldDialogueTerminal 的 InteractionPoint 引用为空。");
            }
        }

        /// <summary>
        /// 验证战斗场景包含全部对话点且交互 ID 唯一。
        /// </summary>
        private static void ValidateSceneTerminals(
            Scene scene,
            GameObject prefab)
        {
            var root = FindSceneRoot(scene, TerminalsRootName);
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"战斗场景缺少根节点 '{TerminalsRootName}'。");
            }

            var interactionIds =
                new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < Seeds.Length; index++)
            {
                var seed = Seeds[index];
                var child = FindDirectChild(
                    root.transform,
                    seed.ObjectName);
                if (child == null)
                {
                    throw new InvalidOperationException(
                        $"战斗场景缺少对话终端 '{seed.ObjectName}'。");
                }

                var terminal =
                    child.GetComponent<WorldDialogueTerminal>();
                if (terminal == null ||
                    terminal.InteractionId != seed.InteractionId ||
                    terminal.DisplayName != seed.DisplayName)
                {
                    throw new InvalidOperationException(
                        $"对话终端 '{seed.ObjectName}' 配置与构建数据不一致。");
                }

                if (!interactionIds.Add(terminal.InteractionId))
                {
                    throw new InvalidOperationException(
                        $"对话交互 ID 重复：'{terminal.InteractionId}'。");
                }

                var source =
                    PrefabUtility.GetCorrespondingObjectFromSource(
                        child.gameObject);
                if (source != prefab)
                {
                    throw new InvalidOperationException(
                        $"对话终端 '{seed.ObjectName}' 不是目标预制体的实例。");
                }
            }
        }

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
                    "未找到 URP Lit 或 Standard Shader，无法创建对话材质。");
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

            SetMaterialColor(material, "_BaseColor", baseColor);
            SetMaterialColor(material, "_Color", baseColor);
            SetMaterialColor(material, "_EmissionColor", emissionColor);
            SetMaterialFloat(material, "_Metallic", metallic);
            SetMaterialFloat(material, "_Smoothness", smoothness);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags =
                MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
            return material;
        }

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

        /// <summary>保存一个世界对话终端的确定性场景构建记录。</summary>
        private readonly struct TerminalSeed
        {
            /// <summary>创建一条对话终端构建记录。</summary>
            public TerminalSeed(
                string objectName,
                string interactionId,
                string dialogueId,
                string displayName,
                int priority,
                Vector3 position,
                float yawDegrees)
            {
                ObjectName = objectName;
                InteractionId = interactionId;
                DialogueId = dialogueId;
                DisplayName = displayName;
                Priority = priority;
                Position = position;
                YawDegrees = yawDegrees;
            }

            /// <summary>获取构建器专属场景对象名称。</summary>
            public string ObjectName { get; }

            /// <summary>获取场景内唯一交互 ID。</summary>
            public string InteractionId { get; }

            /// <summary>获取对话图稳定标识。</summary>
            public string DialogueId { get; }

            /// <summary>获取 HUD 显示名称。</summary>
            public string DisplayName { get; }

            /// <summary>获取候选优先级。</summary>
            public int Priority { get; }

            /// <summary>获取世界坐标。</summary>
            public Vector3 Position { get; }

            /// <summary>获取世界 Y 轴旋转角。</summary>
            public float YawDegrees { get; }
        }
    }
}
#endif
