#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Train.Gameplay.Combat;
using Train.Gameplay.Combat.Buffs;
using Train.Gameplay.Combat.Presentation;
using Train.Gameplay.Enemy;
using Train.Gameplay.Enemy.Animation;
using Train.Gameplay.Enemy.Combat;
using Train.Gameplay.Enemy.Core;
using Train.Gameplay.Enemy.Data;
using Train.Gameplay.Enemy.Movement;
using Train.Gameplay.Enemy.Sensing;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Train.EditorTools.Combat
{
    /// <summary>
    /// 生成带状态机、动画、生命值与独立 BuffHandle 的 KayKit 人形敌人预制体。
    /// </summary>
    [InitializeOnLoad]
    internal static class KayKitEnemyPrefabBuilder
    {
        private const string ModelPath =
            "Assets/Arts/KayKit/Adventurers/Characters/Knight.fbx";
        private const string SwordPath =
            "Assets/Arts/KayKit/Adventurers/Weapons/sword_1handed.fbx";
        private const string AnimatorFolder =
            "Assets/Arts/KayKit/Adventurers/Animations";
        private const string AnimatorPath =
            AnimatorFolder + "/KayKitKnightEnemy.controller";
        private const string ConfigFolder = "Assets/Data/Enemies";
        private const string ConfigPath = ConfigFolder + "/KayKitKnightEnemyConfig.asset";
        private const string PrefabFolder = "Assets/Prefabs/Enemies";
        private const string PrefabPath = PrefabFolder + "/Enemy_KayKitKnight.prefab";

        static KayKitEnemyPrefabBuilder()
        {
            EditorApplication.delayCall += BuildIfOutdated;
        }

        [MenuItem("Tools/Train/Combat/Rebuild KayKit Knight Enemy")]
        private static void Rebuild()
        {
            Build();
        }

        private static void BuildIfOutdated()
        {
            CleanupInterruptedBuild();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var expectedController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorPath);
            var animator = prefab != null
                ? prefab.GetComponentInChildren<Animator>(true)
                : null;
            if (prefab == null ||
                prefab.GetComponent<EnemyController>() == null ||
                prefab.GetComponent<HealthEventRelay>() == null ||
                prefab.GetComponent<BuffHandleComponent>() == null ||
                animator == null ||
                animator.runtimeAnimatorController != expectedController)
            {
                Build();
            }
        }

        private static void CleanupInterruptedBuild()
        {
            var interruptedInstance = GameObject.Find("Enemy_KayKitKnight_TEMP");
            if (interruptedInstance != null)
            {
                UnityEngine.Object.DestroyImmediate(interruptedInstance);
                Debug.Log("Removed interrupted KayKit enemy build instance from the open scene.");
            }
        }

        private static void Build()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var sword = AssetDatabase.LoadAssetAtPath<GameObject>(SwordPath);
            if (model == null || sword == null)
            {
                Debug.LogWarning("KayKit enemy build skipped: required FBX assets are not imported yet.");
                return;
            }

            EnsureFolder(AnimatorFolder);
            EnsureFolder(ConfigFolder);
            EnsureFolder(PrefabFolder);

            var config = EnsureConfig();
            var animatorController = EnsureAnimatorController();
            if (config == null || animatorController == null)
            {
                Debug.LogError("KayKit enemy build failed: config or animator controller is missing.");
                return;
            }

            var enemy = UnityEngine.Object.Instantiate(model);
            enemy.name = "Enemy_KayKitKnight";
            enemy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            var animator = EnsureAnimator(enemy);
            animator.runtimeAnimatorController = animatorController;
            animator.applyRootMotion = false;

            var hitVolume = CreateWeapon(enemy.transform, sword);

            var capsule = enemy.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.75f, 0f);
            capsule.height = 1.5f;
            capsule.radius = 0.35f;

            var body = enemy.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            var health = enemy.AddComponent<Health>();
            health.SetMaxHealth(150f);
            enemy.AddComponent<HealthEventRelay>();
            enemy.AddComponent<BuffHandleComponent>();

            var sensor = enemy.AddComponent<EnemyTargetSensor>();
            var motor = enemy.AddComponent<EnemyMotor>();
            var enemyAnimator = enemy.AddComponent<EnemyAnimator>();
            var combat = enemy.AddComponent<EnemyMeleeCombat>();
            var patrol = enemy.AddComponent<EnemyPatrolArea>();
            var lifecycle = enemy.AddComponent<EnemyLifecycle>();
            var controller = enemy.AddComponent<EnemyController>();

            SetObjectReference(enemyAnimator, "_animator", animator);
            SetObjectReference(combat, "_hitVolume", hitVolume);
            SetObjectReference(controller, "_config", config);
            SetObjectReference(controller, "_sensorComponent", sensor);
            SetObjectReference(controller, "_motorComponent", motor);
            SetObjectReference(controller, "_combatComponent", combat);
            SetObjectReference(controller, "_animationComponent", enemyAnimator);
            SetObjectReference(controller, "_patrolComponent", patrol);
            SetObjectReference(controller, "_lifecycleComponent", lifecycle);

            PrefabUtility.SaveAsPrefabAsset(enemy, PrefabPath);
            UnityEngine.Object.DestroyImmediate(enemy);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created state-machine enemy prefab at {PrefabPath}");
        }

        private static Animator EnsureAnimator(GameObject enemy)
        {
            var animator = enemy.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                return animator;
            }

            animator = enemy.AddComponent<Animator>();
            animator.avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<Avatar>()
                .FirstOrDefault();
            return animator;
        }

        private static SphereCollider CreateWeapon(Transform enemy, GameObject sword)
        {
            var handSlot = FindChild(enemy, "handslot.r");
            if (handSlot == null)
            {
                Debug.LogWarning("KayKit knight hand slot 'handslot.r' was not found.");
                return null;
            }

            var weapon = UnityEngine.Object.Instantiate(sword, handSlot);
            weapon.name = "EnemySword";
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
            weapon.transform.localScale = Vector3.one;

            var sphere = weapon.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.center = Vector3.zero;
            sphere.radius = 1.35f;

            sphere.enabled = false;
            return sphere;
        }

        private static EnemyConfig EnsureConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(ConfigPath);
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<EnemyConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static AnimatorController EnsureAnimatorController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorPath) ??
                             AnimatorController.CreateAnimatorControllerAtPath(AnimatorPath);
            var stateMachine = controller.layers[0].stateMachine;
            foreach (var childState in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(childState.state);
            }

            AddAnimationState(stateMachine, EnemyAnimationId.Idle, "Idle");
            AddAnimationState(stateMachine, EnemyAnimationId.Walk, "Walking_A");
            AddAnimationState(stateMachine, EnemyAnimationId.Run, "Running_A");
            AddAnimationState(stateMachine, EnemyAnimationId.Attack, "Attack_Slice");
            AddAnimationState(stateMachine, EnemyAnimationId.Hit, "Hit_A");
            AddAnimationState(stateMachine, EnemyAnimationId.Death, "Death_A");

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AddAnimationState(
            AnimatorStateMachine stateMachine,
            EnemyAnimationId stateId,
            string clipName)
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(candidate =>
                    !candidate.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase) &&
                    (string.Equals(candidate.name, clipName, StringComparison.OrdinalIgnoreCase) ||
                     candidate.name.EndsWith(clipName, StringComparison.OrdinalIgnoreCase) ||
                     candidate.name.Contains(clipName, StringComparison.OrdinalIgnoreCase)));

            var state = stateMachine.AddState(stateId.ToString());
            state.motion = clip;
            if (stateId == EnemyAnimationId.Idle)
            {
                stateMachine.defaultState = state;
            }

            if (clip == null)
            {
                Debug.LogWarning($"KayKit animation clip '{clipName}' was not found.");
            }
        }

        private static void SetObjectReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"Serialized property '{propertyName}' was not found on {target.GetType().Name}.");
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            Directory.CreateDirectory(assetPath);
            AssetDatabase.Refresh();
        }

        private static Transform FindChild(Transform root, string targetName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == targetName)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
#endif
