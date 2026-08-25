using System.Reflection;
using NUnit.Framework;
using Train.Gameplay.Enemy.Movement;
using UnityEngine;

namespace Train.Tests.EditMode.Enemy
{
    public sealed class EnemyMotorFallbackTests
    {
        [Test]
        public void MissingNavMeshAgent_RejectsDirectTransformFallback()
        {
            var enemy = new GameObject("EnemyMotorFallbackTest");
            try
            {
                var motor = enemy.AddComponent<EnemyMotor>();
                var start = enemy.transform.position;

                motor.MoveTo(start + Vector3.forward * 10f, 5f);
                motor.ApplyExternalPull(Vector3.right * 3f);
                typeof(EnemyMotor)
                    .GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(motor, null);

                Assert.That(enemy.transform.position, Is.EqualTo(start));
            }
            finally
            {
                Object.DestroyImmediate(enemy);
            }
        }
    }
}
