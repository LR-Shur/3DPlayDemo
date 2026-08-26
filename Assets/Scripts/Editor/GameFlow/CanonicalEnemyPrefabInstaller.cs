#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Train.Gameplay.Combat;
using Train.Gameplay.Combat.Buffs;
using Train.Gameplay.Combat.Presentation;
using Train.Gameplay.Enemy.Animation;
using Train.Gameplay.Enemy.Combat;
using Train.Gameplay.Enemy.Core;
using Train.Gameplay.Enemy.Data;
using Train.Gameplay.Enemy.Movement;
using Train.Gameplay.Enemy.Sensing;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Train.EditorTools.GameFlow
{
    /// <summary>
    /// 为 CSV 中的每个真实战斗敌人安装同一套运行时骨架。
    /// 外观、特殊技能和已有近/远程战斗组件保留在各自 canonical prefab 上。
    /// </summary>
    public static class CanonicalEnemyPrefabInstaller
    {
        private const string DefaultConfigPath =
            "Assets/Data/Enemies/KayKitKnightEnemyConfig.asset";
        private const string AnimatorControllerPath =
            "Assets/Arts/KayKit/Adventurers/Animations/KayKitKnightEnemy.controller";

        private static readonly Dictionary<string, string> ConfigByArchetype =
            new(StringComparer.Ordinal)
            {
                ["kaykit_knight"] = "KayKitKnightEnemyConfig",
                ["scout_knight"] = "ScoutKnightConfig",
                ["elite_knight"] = "EliteKnightConfig",
                ["boss_rusk"] = "BossRuskPrototypeConfig",
                ["arc_drone"] = "ArcDroneConfig",
                ["human_duelist"] = "HumanDuelistConfig",
                ["human_brute"] = "HumanBruteConfig",
                ["human_caster"] = "HumanCasterConfig",
            };

        [MenuItem("Tools/Train/Combat/Install Canonical Enemy Components")]
        public static void InstallAll()
        {
            var changed = 0;
            foreach (var pair in EnemyArchetypeEditorCatalog.Entries)
            {
                if (string.Equals(
                        pair.Key,
                        "training_dummy",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (Install(pair.Key, pair.Value))
                {
                    changed++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Canonical enemy components installed: {changed} prefabs.");
        }

        public static void InstallOne(
            string archetypeId,
            string prefabPath)
        {
            Install(archetypeId, prefabPath);
            AssetDatabase.SaveAssets();
        }

        private static bool Install(
            string archetypeId,
            string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Canonical enemy prefab is missing: {prefabPath}");
            }

            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var changed = false;
                var health = EnsureComponent<Health>(contents, ref changed);
                EnsureComponent<HealthEventRelay>(contents, ref changed);
                EnsureComponent<BuffHandleComponent>(contents, ref changed);
                EnsureComponent<EnemyTargetSensor>(contents, ref changed);
                var motor = EnsureComponent<EnemyMotor>(contents, ref changed);
                var animator = EnsureComponent<EnemyAnimator>(contents, ref changed);
                EnsureComponent<EnemyPatrolArea>(contents, ref changed);
                var lifecycle = EnsureComponent<EnemyLifecycle>(contents, ref changed);
                var combat = ResolveCombat(contents, ref changed);
                var controller = EnsureComponent<EnemyController>(contents, ref changed);
                var agent = EnsureComponent<NavMeshAgent>(contents, ref changed);

                changed |= ConfigureAgent(agent);
                changed |= ConfigureSensor(contents.GetComponent<EnemyTargetSensor>());
                EnsureBodyCollider(contents, ref changed);
                var unityAnimator = EnsureAnimator(contents, ref changed);
                changed |= SetReference(motor, "_agent", agent);
                changed |= SetReference(animator, "_animator", unityAnimator);
                changed |= SetReference(
                    controller,
                    "_config",
                    ResolveConfig(archetypeId));
                changed |= SetReference(
                    controller,
                    "_sensorComponent",
                    contents.GetComponent<EnemyTargetSensor>());
                changed |= SetReference(controller, "_motorComponent", motor);
                changed |= SetReference(controller, "_combatComponent", combat);
                changed |= SetReference(controller, "_animationComponent", animator);
                changed |= SetReference(
                    controller,
                    "_patrolComponent",
                    contents.GetComponent<EnemyPatrolArea>());
                changed |= SetReference(controller, "_lifecycleComponent", lifecycle);

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static Component ResolveCombat(
            GameObject root,
            ref bool changed)
        {
            var ranged = root.GetComponent<EnemyRangedCombat>();
            if (ranged != null)
            {
                return ranged;
            }

            var melee = root.GetComponent<EnemyMeleeCombat>();
            if (melee != null)
            {
                return melee;
            }

            changed = true;
            return root.AddComponent<EnemyMeleeCombat>();
        }

        private static T EnsureComponent<T>(
            GameObject root,
            ref bool changed)
            where T : Component
        {
            var component = root.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            changed = true;
            return root.AddComponent<T>();
        }

        private static bool ConfigureAgent(NavMeshAgent agent)
        {
            var changed = false;
            if (agent.updateRotation)
            {
                changed = true;
            }

            agent.updateRotation = false;
            var speed = Mathf.Max(0.1f, agent.speed);
            if (!Mathf.Approximately(agent.speed, speed)) changed = true;
            agent.speed = speed;
            var acceleration = Mathf.Max(8f, agent.acceleration);
            if (!Mathf.Approximately(agent.acceleration, acceleration)) changed = true;
            agent.acceleration = acceleration;
            if (!Mathf.Approximately(agent.angularSpeed, 0f)) changed = true;
            agent.angularSpeed = 0f;
            var stoppingDistance = Mathf.Min(agent.stoppingDistance, .1f);
            if (!Mathf.Approximately(agent.stoppingDistance, stoppingDistance)) changed = true;
            agent.stoppingDistance = stoppingDistance;
            if (!agent.autoRepath) changed = true;
            agent.autoRepath = true;
            return changed;
        }

        private static bool ConfigureSensor(EnemyTargetSensor sensor)
        {
            var serialized = new SerializedObject(sensor);
            var detection = serialized.FindProperty("_detectionRadius");
            var loseTarget = serialized.FindProperty("_loseTargetRadius");
            var changed = !Mathf.Approximately(detection.floatValue, 30f) ||
                          !Mathf.Approximately(loseTarget.floatValue, 36f);
            detection.floatValue = 30f;
            loseTarget.floatValue = 36f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return changed;
        }

        private static void EnsureBodyCollider(
            GameObject root,
            ref bool changed)
        {
            if (root.GetComponent<Collider>() != null)
            {
                return;
            }

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, .75f, 0f);
            capsule.height = 1.5f;
            capsule.radius = .35f;
            changed = true;
        }

        private static Animator EnsureAnimator(
            GameObject root,
            ref bool changed)
        {
            var animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = root.AddComponent<Animator>();
                changed = true;
            }

            if (animator.runtimeAnimatorController == null)
            {
                animator.runtimeAnimatorController =
                    AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                        AnimatorControllerPath);
                changed = true;
            }

            animator.applyRootMotion = false;
            return animator;
        }

        private static EnemyConfig ResolveConfig(string archetypeId)
        {
            if (ConfigByArchetype.TryGetValue(archetypeId, out var configName))
            {
                var config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(
                    "Assets/Data/Enemies/" + configName + ".asset");
                if (config != null)
                {
                    return config;
                }
            }

            return AssetDatabase.LoadAssetAtPath<EnemyConfig>(DefaultConfigPath);
        }

        private static bool SetReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name} has no property '{propertyName}'.");
            }

            var changed = property.objectReferenceValue != value;
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return changed;
        }
    }
}
#endif
