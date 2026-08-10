using NUnit.Framework;
using Train.Gameplay.Enemy.Animation;
using UnityEditor;
using UnityEngine;

namespace Train.Tests.EditMode.Enemy
{
    public sealed class EnemyPrefabAnimatorTests
    {
        private const string PrefabPath =
            "Assets/Prefabs/Enemies/Enemy_KayKitKnight.prefab";

        [Test]
        public void KayKitKnightPrefab_HasBoundAnimatorController()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            Assert.That(prefab, Is.Not.Null);
            var adapter = prefab.GetComponent<EnemyAnimator>();
            var animator = prefab.GetComponentInChildren<Animator>(true);
            Assert.That(adapter, Is.Not.Null);
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
        }
    }
}
