using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Train.Gameplay.Enemy.Animation;

namespace Train.EditorTools
{
    /// <summary>把 Blender 导出的机器人模型接到现有敌人战斗骨架，生成可被关卡直接刷出的敌人预制体。</summary>
    public static class RobotEnemyPrefabBaker
    {
        private const string BasePrefab = "Assets/Prefabs/Enemies/Enemy_EliteKnight.prefab";
        private const string SourceFolder = "Assets/Arts/ThirdParty/Enemies/Kenney_Robot/Converted/";
        private const string OutputFolder = "Assets/Prefabs/Enemies/";
        private const string MaterialFolder = "Assets/Arts/ThirdParty/Enemies/Kenney_Robot/Converted/Materials/";

        private readonly struct RobotSpec
        {
            public readonly string Model;
            public readonly string Prefab;
            public readonly Color Color;
            public readonly float Height;
            public readonly string DisplayName;

            public RobotSpec(string model, string prefab, Color color, float height, string displayName)
            {
                Model = model;
                Prefab = prefab;
                Color = color;
                Height = height;
                DisplayName = displayName;
            }
        }

        private static readonly RobotSpec[] Specs =
        {
            new("drone", "Enemy_ArcDroneRobot", new Color(.08f, .65f, 1f), 1.5f, "弧光无人机"),
            new("crawler", "Enemy_CrawlerBot", new Color(1f, .35f, .08f), 1.35f, "熔岩爬行机"),
            new("lobber", "Enemy_LobberBot", new Color(.8f, .18f, .9f), 1.7f, "晶簇投掷机"),
            new("roller", "Enemy_RollerBot", new Color(1f, .75f, .08f), 1.25f, "雷暴滚轮"),
        };

        [InitializeOnLoadMethod]
        private static void BuildWhenReady()
        {
            EditorApplication.update -= TryBuildWhenReady;
            EditorApplication.update += TryBuildWhenReady;
        }

        private static void TryBuildWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!Specs.Any(spec => AssetDatabase.LoadAssetAtPath<GameObject>(OutputFolder + spec.Prefab + ".prefab") == null))
            {
                EditorApplication.update -= TryBuildWhenReady;
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefab) == null ||
                Specs.Any(spec => AssetDatabase.LoadAssetAtPath<GameObject>(SourceFolder + spec.Model + ".fbx") == null))
            {
                return;
            }

            EditorApplication.update -= TryBuildWhenReady;
            BuildAll();
        }

        [MenuItem("Tools/Train/Combat/Build Robot Enemy Prefabs")]
        public static void BuildAll()
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefab);
            if (basePrefab == null)
            {
                Debug.LogError("找不到 Enemy_EliteKnight 基础敌人预制体。", basePrefab);
                return;
            }

            EnsureFolder(OutputFolder);
            EnsureFolder(MaterialFolder);
            foreach (var spec in Specs)
            {
                BuildOne(basePrefab, spec);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Robot enemy prefabs built: " + string.Join(", ", Specs.Select(spec => spec.Prefab)));
        }

        private static void BuildOne(GameObject basePrefab, RobotSpec spec)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(SourceFolder + spec.Model + ".fbx");
            if (model == null)
            {
                Debug.LogError("找不到机器人 FBX：" + SourceFolder + spec.Model + ".fbx");
                return;
            }

            var materialPath = MaterialFolder + "Robot_" + spec.Model + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    name = "Robot_" + spec.Model,
                    color = spec.Color,
                };
                AssetDatabase.CreateAsset(material, materialPath);
            }

            var root = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            root.name = spec.Prefab;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            visual.name = "RobotVisual_" + spec.DisplayName;
            DisableVisualColliders(visual);
            ApplyRobotMaterial(visual, material);
            FitVisual(visual, spec.Height);

            var attackMotion = root.GetComponent<EnemyAttackMotion>() ?? root.AddComponent<EnemyAttackMotion>();
            SetReference(attackMotion, "_visualRoot", visual.transform);
            SetValue(attackMotion, "_style", spec.Model == "crawler" ? EnemyAttackMotion.AttackStyle.HeavyStrike :
                spec.Model == "lobber" ? EnemyAttackMotion.AttackStyle.CasterBurst : EnemyAttackMotion.AttackStyle.MachineLunge);

            if (root.GetComponent<Train.Gameplay.Enemy.Combat.EnemySpecialAbilityController>() == null)
            {
                root.AddComponent<Train.Gameplay.Enemy.Combat.EnemySpecialAbilityController>();
            }

            var output = OutputFolder + spec.Prefab + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, output);
            Object.DestroyImmediate(root);
        }

        private static void DisableVisualColliders(GameObject visual)
        {
            foreach (var collider in visual.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
        }

        private static void ApplyRobotMaterial(GameObject visual, Material material)
        {
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++) materials[i] = material;
                renderer.sharedMaterials = materials.Length == 0 ? new[] { material } : materials;
            }
        }

        private static void FitVisual(GameObject visual, float targetHeight)
        {
            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            var height = Mathf.Max(.01f, bounds.size.y);
            visual.transform.localScale *= targetHeight / height;
            visual.transform.localPosition = new Vector3(0f, .05f, 0f);
        }

        private static void EnsureFolder(string folder)
        {
            var parts = folder.TrimEnd('/').Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void SetReference(Object target, string name, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetValue(Object target, string name, object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(name);
            if (property == null) return;
            if (property.propertyType == SerializedPropertyType.Enum)
            {
                property.enumValueIndex = System.Convert.ToInt32(value);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
