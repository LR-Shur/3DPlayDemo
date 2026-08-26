using System.Linq;
using NUnit.Framework;
using Train.Gameplay.Enemy.Animation;
using UnityEditor;
using UnityEditor.Animations;
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

        [Test]
        public void KayKitKnightPrefab_IdleWalkRunClipsAreLooping()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var animator = prefab.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null);
            var controller = animator.runtimeAnimatorController as AnimatorController;
            Assert.That(controller, Is.Not.Null);

            var states = controller.layers
                .SelectMany(layer => layer.stateMachine.states)
                .Select(childState => childState.state)
                .ToArray();
            var stateNames = new[] { "Idle", "Walk", "Run" };
            var expectedClipNames = new[] { "Idle", "Walking_A", "Running_A" };
            for (var index = 0; index < stateNames.Length; index++)
            {
                var stateName = stateNames[index];
                var state = states.SingleOrDefault(candidate => candidate.name == stateName);
                Assert.That(state, Is.Not.Null, $"Missing animator state: {stateName}");
                Assert.That(state.motion, Is.Not.Null, $"Missing motion for animator state: {stateName}");

                var clip = state.motion as AnimationClip;
                Assert.That(clip, Is.Not.Null, $"Animator state is not an AnimationClip: {stateName}");
                Assert.That(clip.isLooping, Is.True, $"Animation clip is not looping: {stateName}");
                Assert.That(clip.name, Is.EqualTo(expectedClipNames[index]),
                    $"Unexpected motion for animator state: {stateName}");
            }
        }
    }
}
