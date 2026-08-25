using NUnit.Framework;
using Train.Gameplay.Combat;
using Train.Gameplay.Enemy;
using Train.Gameplay.Enemy.Abstractions;
using Train.Gameplay.Enemy.Animation;
using Train.Gameplay.Enemy.Core;
using Train.Gameplay.Enemy.Data;
using Train.Gameplay.Enemy.Presentation;
using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace Train.Tests.EditMode.Enemy
{
    public sealed class EnemyDeathPresentationTests
    {
        [Test]
        public void LethalDamage_HoldsDeathPresentationForHitConfirmation()
        {
            var enemy = new GameObject("EnemyDeathPresentationTest");
            enemy.SetActive(false);
            var config = ScriptableObject.CreateInstance<EnemyConfig>();
            var sensor = enemy.AddComponent<TestSensor>();
            var motor = enemy.AddComponent<TestMotor>();
            var combat = enemy.AddComponent<TestCombat>();
            var animation = enemy.AddComponent<TestAnimation>();
            var patrol = enemy.AddComponent<TestPatrol>();
            var lifecycle = enemy.AddComponent<TestLifecycle>();
            var health = enemy.AddComponent<Health>();
            var controller = enemy.AddComponent<EnemyController>();

            try
            {
                var serialized = new SerializedObject(controller);
                serialized.FindProperty("_config").objectReferenceValue = config;
                serialized.FindProperty("_sensorComponent").objectReferenceValue = sensor;
                serialized.FindProperty("_motorComponent").objectReferenceValue = motor;
                serialized.FindProperty("_combatComponent").objectReferenceValue = combat;
                serialized.FindProperty("_animationComponent").objectReferenceValue = animation;
                serialized.FindProperty("_patrolComponent").objectReferenceValue = patrol;
                serialized.FindProperty("_lifecycleComponent").objectReferenceValue = lifecycle;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                enemy.SetActive(true);
                InvokePrivate(controller, "Awake");
                InvokePrivate(controller, "OnEnable");
                Assert.That(controller.enabled, Is.True);
                Assert.That(controller.Config, Is.SameAs(config));
                Assert.That(lifecycle, Is.Not.Null);
                Assert.That(enemy.GetComponent<EnemyFreezeVisual>(), Is.Not.Null);
                health.SetMaxHealth(10f);
                var result = health.TakeDamage(new DamageInfo(
                    10f,
                    null,
                    enemy.transform.position,
                    Vector3.forward));

                Assert.That(result.Killed, Is.True);
                Assert.That(lifecycle.DespawnCalls, Is.EqualTo(0));
                Assert.That(enemy.GetComponent<EnemyFreezeVisual>().IsHitActive, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(enemy);
                Object.DestroyImmediate(config);
            }
        }

        private static void InvokePrivate(MonoBehaviour target, string methodName)
        {
            target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, null);
        }

        private sealed class TestSensor : MonoBehaviour, IEnemySensor
        {
            public Transform Target => null;
            public bool HasTarget => false;
            public float DistanceToTarget => float.PositiveInfinity;
            public void Tick() { }
            public void ClearTarget() { }
        }

        private sealed class TestMotor : MonoBehaviour, IEnemyMotor
        {
            public Vector3 Position => transform.position;
            public bool HasReachedDestination => true;
            public void MoveTo(Vector3 destination, float speed) { }
            public void Face(Vector3 worldPosition, float turnSpeed) { }
            public void Stop() { }
            public void SetMovementEnabled(bool enabled) { }
            public void ApplyExternalPull(Vector3 displacement) { }
        }

        private sealed class TestCombat : MonoBehaviour, IEnemyCombat
        {
            public bool CanStartAttack => false;
            public float AttackRange => 1f;
            public void SetAttackTarget(Transform target) { }
            public void BeginAttack() { }
            public void SetDamageActive(bool active) { }
            public void EndAttack() { }
        }

        private sealed class TestAnimation : MonoBehaviour, IEnemyAnimation
        {
            public void Play(EnemyAnimationId animationId, float fadeSeconds = .12f) { }
        }

        private sealed class TestPatrol : MonoBehaviour, IEnemyPatrol
        {
            public bool TryGetNextDestination(Vector3 currentPosition, out Vector3 destination)
            {
                destination = currentPosition;
                return false;
            }

            public void ResetPatrol() { }
        }

        private sealed class TestLifecycle : MonoBehaviour, IEnemyLifecycle
        {
            public int DespawnCalls { get; private set; }
            public void DespawnAfter(float delaySeconds) => DespawnCalls++;
        }
    }
}
