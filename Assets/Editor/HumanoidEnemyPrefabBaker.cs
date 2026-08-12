using System.Collections.Generic;
using System.Linq;
using Train.Gameplay.Combat;
using Train.Gameplay.Enemy.Combat;
using Train.Gameplay.Enemy.Data;
using Train.Gameplay.Enemy.Core;
using Train.Gameplay.Enemy.Animation;
using UnityEditor;
using UnityEngine;

namespace Train.EditorTools
{
    /// <summary>用 Blender 人形模型生成三种外观与攻击节奏不同的敌人预制体。</summary>
    public static class HumanoidEnemyPrefabBaker
    {
        private const string BasePrefab = "Assets/Prefabs/Enemies/Enemy_EliteKnight.prefab";
        private const string ModelPath = "Assets/Arts/ThirdParty/Enemies/Stylized_Humanoid/Imported/baseMesh_A1_(human_YW)_by_Girush.fbx";
        private const string OutputFolder = "Assets/Prefabs/Enemies/";
        private const string ConfigFolder = "Assets/Data/Enemies/";
        private const string MaterialFolder = "Assets/Arts/ThirdParty/Enemies/Stylized_Humanoid/Materials/";

        private readonly struct Spec
        {
            public readonly string Prefab;
            public readonly string Config;
            public readonly string Archetype;
            public readonly string DisplayName;
            public readonly Color Color;
            public readonly float Height;
            public readonly bool Telegraph;
            public readonly float AttackDuration;
            public readonly float HitStart;
            public readonly float HitEnd;
            public readonly float Range;
            public readonly bool Special;

            public Spec(string prefab, string config, string archetype, string displayName, Color color, float height,
                bool telegraph, float attackDuration, float hitStart, float hitEnd, float range, bool special)
            {
                Prefab = prefab; Config = config; Archetype = archetype; DisplayName = displayName; Color = color;
                Height = height; Telegraph = telegraph; AttackDuration = attackDuration; HitStart = hitStart;
                HitEnd = hitEnd; Range = range; Special = special;
            }
        }

        private static readonly Spec[] Specs =
        {
            new("Enemy_HumanDuelist", "HumanDuelistConfig", "human_duelist", "人形·疾刃决斗者", new Color(.12f, .55f, .95f), 1.75f, false, .9f, .52f, .72f, 1.1f, false),
            new("Enemy_HumanBrute", "HumanBruteConfig", "human_brute", "人形·赤铠重击者", new Color(.82f, .1f, .08f), 1.95f, true, 1.6f, .96f, 1.28f, 1.9f, false),
            new("Enemy_HumanCaster", "HumanCasterConfig", "human_caster", "人形·晶核术士", new Color(.63f, .16f, .95f), 1.8f, true, 1.8f, 1.2f, 1.5f, 7f, true),
        };

        [MenuItem("Tools/Train/Combat/Build Humanoid Enemy Prefabs")]
        public static void BuildAll()
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefab);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (basePrefab == null || model == null)
            {
                Debug.LogError($"人形敌人生成失败：基础预制体或 Blender FBX 未导入。{ModelPath}");
                return;
            }

            EnsureFolder(OutputFolder);
            EnsureFolder(ConfigFolder);
            EnsureFolder(MaterialFolder);
            foreach (var spec in Specs) BuildOne(basePrefab, model, spec);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Humanoid enemy prefabs built: " + string.Join(", ", Specs.Select(x => x.Prefab)));
        }

        private static void BuildOne(GameObject basePrefab, GameObject model, Spec spec)
        {
            var config = EnsureConfig(spec);
            var material = EnsureMaterial(spec);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            root.name = spec.Prefab;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            visual.name = spec.DisplayName;
            DisableColliders(visual);
            ApplyMaterial(visual, material);
            FitVisual(visual, spec.Height);
            PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var weapon = AddWeaponSilhouette(root.transform, spec);

            var attackMotion = root.GetComponent<EnemyAttackMotion>() ?? root.AddComponent<EnemyAttackMotion>();
            SetReference(attackMotion, "_visualRoot", visual.transform);
            SetReference(attackMotion, "_weaponRoot", weapon);
            SetValue(attackMotion, "_style", spec.Special ? EnemyAttackMotion.AttackStyle.CasterBurst :
                spec.Telegraph ? EnemyAttackMotion.AttackStyle.HeavyStrike : EnemyAttackMotion.AttackStyle.QuickSlash);

            if (spec.Special)
            {
                var rangedMelee = root.GetComponent<EnemyMeleeCombat>();
                if (rangedMelee != null) rangedMelee.enabled = false;

                var ranged = root.GetComponent<EnemyRangedCombat>() ?? root.AddComponent<EnemyRangedCombat>();
                SetReference(ranged, "_projectilePrefab", EnsureProjectilePrefab());
                SetReference(ranged, "_muzzle", weapon);
                SetValue(ranged, "_attackRange", 7f);
                SetValue(ranged, "_damage", 34f);
                SetValue(ranged, "_attackCooldown", 1.6f);
                SetValue(ranged, "_projectileSpeed", 9f);
                SetValue(ranged, "_projectileLifetime", 4f);
                SetValue(ranged, "_projectileRadius", .16f);
                SetValue(ranged, "_damageType", DamageType.Electric);

                var controllerForRanged = root.GetComponent<EnemyController>();
                SetReference(controllerForRanged, "_combatComponent", ranged);
            }

            var controller = root.GetComponent<EnemyController>();
            SetReference(controller, "_config", config);
            var melee = root.GetComponent<EnemyMeleeCombat>();
            SetValue(melee, "_telegraphEnabled", spec.Telegraph);
            SetValue(melee, "_telegraphRadiusMultiplier", spec.Range / 1.1f);
            SetValue(melee, "_telegraphColor", new Color(.95f, .025f, .02f, .92f));

            if (spec.Special)
            {
                var special = root.GetComponent<EnemySpecialAbilityController>() ?? root.AddComponent<EnemySpecialAbilityController>();
                SetValue(special, "_damageType", DamageType.Earth);
                SetValue(special, "_color", new Color(.75f, .16f, 1f, .95f));
                SetValue(special, "_interval", 5.8f);
                SetValue(special, "_radius", 2.7f);
                SetValue(special, "_damage", 26f);
                SetValue(special, "_telegraph", 1.15f);
            }

            PrefabUtility.SaveAsPrefabAsset(root, OutputFolder + spec.Prefab + ".prefab");
            Object.DestroyImmediate(root);
        }

        private static EnemyConfig EnsureConfig(Spec spec)
        {
            var path = ConfigFolder + spec.Config + ".asset";
            var config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<EnemyConfig>();
                config.name = spec.Config;
                AssetDatabase.CreateAsset(config, path);
            }
            SetValue(config, "_patrolSpeed", spec.Range > 2f ? 1.7f : 2.2f);
            SetValue(config, "_chaseSpeed", spec.Range > 2f ? 2.8f : 3.8f);
            SetValue(config, "_turnSpeed", 12f);
            SetValue(config, "_idleSeconds", spec.Telegraph ? 1.1f : .55f);
            SetValue(config, "_attackRange", spec.Range);
            SetValue(config, "_attackDuration", spec.AttackDuration);
            SetValue(config, "_hitboxStartTime", spec.HitStart);
            SetValue(config, "_hitboxEndTime", spec.HitEnd);
            SetValue(config, "_hitReactionDuration", .38f);
            SetValue(config, "_deathDespawnDelay", 2f);
            EditorUtility.SetDirty(config);
            return config;
        }

        private static Material EnsureMaterial(Spec spec)
        {
            var path = MaterialFolder + spec.Prefab + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = spec.Prefab };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = spec.Color;
            material.SetFloat("_Metallic", .15f);
            material.SetFloat("_Smoothness", .48f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform AddWeaponSilhouette(Transform root, Spec spec)
        {
            var weapon = new GameObject("WeaponSilhouette_" + spec.DisplayName);
            weapon.transform.SetParent(root, false);
            weapon.transform.localPosition = new Vector3(.34f, .9f, .12f);
            weapon.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.transform.SetParent(weapon.transform, false);
            blade.transform.localPosition = new Vector3(0f, .55f, 0f);
            blade.transform.localScale = spec.Special ? new Vector3(.16f, 1.1f, .16f) : new Vector3(.12f, .9f, .12f);
            blade.GetComponent<Renderer>().sharedMaterial = EnsureWeaponMaterial(spec);
            DisableColliders(weapon);
            if (spec.Special)
            {
                var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.transform.SetParent(weapon.transform, false);
                orb.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                orb.transform.localScale = Vector3.one * .28f;
                orb.GetComponent<Renderer>().sharedMaterial = EnsureWeaponMaterial(spec);
                DisableColliders(orb);
            }

            return weapon.transform;
        }

        private static Material EnsureWeaponMaterial(Spec spec)
        {
            var path = MaterialFolder + spec.Prefab + "_Weapon.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = spec.Prefab + "_Weapon" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = spec.Special ? new Color(1f, .35f, 1f) : new Color(1f, .72f, .15f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject EnsureProjectilePrefab()
        {
            const string folder = "Assets/Prefabs/Enemies/Projectiles/";
            const string path = folder + "Enemy_CrystalBolt.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            EnsureFolder(folder);
            var root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "Enemy_CrystalBolt";
            root.transform.localScale = Vector3.one * .22f;
            var renderer = root.GetComponent<Renderer>();
            renderer.sharedMaterial = EnsureProjectileMaterial();
            var collider = root.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            root.AddComponent<EnemyProjectile>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Material EnsureProjectileMaterial()
        {
            const string path = "Assets/Arts/ThirdParty/Enemies/Stylized_Humanoid/Materials/Enemy_CrystalBolt.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Enemy_CrystalBolt" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = new Color(.65f, .12f, 1f, 1f);
            material.SetFloat("_EmissionIntensity", 2.2f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(.55f, .08f, 1f, 1f));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyMaterial(GameObject root, Material material)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var slots = renderer.sharedMaterials;
                for (var i = 0; i < slots.Length; i++) slots[i] = material;
                renderer.sharedMaterials = slots.Length == 0 ? new[] { material } : slots;
            }
        }

        private static void DisableColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        }

        private static void FitVisual(GameObject visual, float targetHeight)
        {
            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            visual.transform.localScale *= targetHeight / Mathf.Max(.01f, bounds.size.y);
            visual.transform.localPosition = new Vector3(0f, .04f, 0f);
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
            switch (property.propertyType)
            {
                case SerializedPropertyType.Float: property.floatValue = (float)value; break;
                case SerializedPropertyType.Boolean: property.boolValue = (bool)value; break;
                case SerializedPropertyType.Color: property.colorValue = (Color)value; break;
                case SerializedPropertyType.Enum: property.enumValueIndex = System.Convert.ToInt32(value); break;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
    }
}
