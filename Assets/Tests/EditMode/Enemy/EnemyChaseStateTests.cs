using NUnit.Framework;
using Train.Gameplay.Enemy;
using Train.Gameplay.Enemy.Abstractions;
using Train.Gameplay.Enemy.Core;
using Train.Gameplay.Enemy.Data;
using UnityEngine;

namespace Train.Tests.EditMode.Enemy
{
    public sealed class EnemyChaseStateTests
    {
        [Test]
        public void InAttackRangeWhileCooling_StopsWithoutMoving()
        {
            var fixture = CreateFixture(canStartAttack: false);
            try
            {
                fixture.Machine.ChangeState(fixture.Machine.Chase);
                fixture.Machine.Tick();

                Assert.That(fixture.Machine.CurrentState, Is.SameAs(fixture.Machine.Chase));
                Assert.That(fixture.Motor.StopCalls, Is.EqualTo(1));
                Assert.That(fixture.Motor.MoveCalls, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(fixture.Target.gameObject);
                Object.DestroyImmediate(fixture.Config);
            }
        }

        [Test]
        public void InAttackRangeWhenReady_EntersAttackWithoutMoving()
        {
            var fixture = CreateFixture(canStartAttack: true);
            try
            {
                fixture.Machine.ChangeState(fixture.Machine.Chase);
                fixture.Machine.Tick();

                Assert.That(fixture.Machine.CurrentState, Is.SameAs(fixture.Machine.Attack));
                Assert.That(fixture.Combat.BeginAttackCalls, Is.EqualTo(1));
                Assert.That(fixture.Motor.MoveCalls, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(fixture.Target.gameObject);
                Object.DestroyImmediate(fixture.Config);
            }
        }

        private static Fixture CreateFixture(bool canStartAttack)
        {
            var target = new GameObject("EnemyChaseStateTestTarget").transform;
            var sensor = new TestSensor
            {
                Target = target,
                DistanceToTarget = 1f
            };
            var motor = new TestMotor();
            var combat = new TestCombat
            {
                CanStartAttack = canStartAttack,
                AttackRange = 1.5f
            };
            var config = ScriptableObject.CreateInstance<EnemyConfig>();
            var context = new EnemyContext(
                sensor,
                motor,
                combat,
                new TestAnimation(),
                null,
                null,
                null,
                config,
                hitReaction: null);

            return new Fixture(
                new EnemyStateMachine(context),
                target,
                motor,
                combat,
                config);
        }

        private sealed class Fixture
        {
            public Fixture(
                EnemyStateMachine machine,
                Transform target,
                TestMotor motor,
                TestCombat combat,
                EnemyConfig config)
            {
                Machine = machine;
                Target = target;
                Motor = motor;
                Combat = combat;
                Config = config;
            }

            public EnemyStateMachine Machine { get; }
            public Transform Target { get; }
            public TestMotor Motor { get; }
            public TestCombat Combat { get; }
            public EnemyConfig Config { get; }
        }

        private sealed class TestSensor : IEnemySensor
        {
            public Transform Target { get; set; }
            public bool HasTarget => Target != null;
            public float DistanceToTarget { get; set; }

            public void Tick() { }
            public void ClearTarget() => Target = null;
        }

        private sealed class TestMotor : IEnemyMotor
        {
            public int MoveCalls { get; private set; }
            public int StopCalls { get; private set; }
            public Vector3 Position => Vector3.zero;
            public bool HasReachedDestination => false;

            public void MoveTo(Vector3 destination, float speed) => MoveCalls++;
            public void Face(Vector3 worldPosition, float turnSpeed) { }
            public void Stop() => StopCalls++;
            public void SetMovementEnabled(bool enabled) { }
            public void ApplyExternalPull(Vector3 displacement) { }
        }

        private sealed class TestCombat : IEnemyCombat
        {
            public bool CanStartAttack { get; set; }
            public float AttackRange { get; set; }
            public int BeginAttackCalls { get; private set; }

            public void SetAttackTarget(Transform target) { }
            public void BeginAttack() => BeginAttackCalls++;
            public void SetDamageActive(bool active) { }
            public void EndAttack() { }
        }

        private sealed class TestAnimation : IEnemyAnimation
        {
            public void Play(EnemyAnimationId animationId, float fadeSeconds = 0.12f) { }
        }
    }
}
