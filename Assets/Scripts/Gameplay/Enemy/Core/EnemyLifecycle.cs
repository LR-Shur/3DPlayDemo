using System.Collections;
using Train.Gameplay.Enemy.Abstractions;
using UnityEngine;

namespace Train.Gameplay.Enemy.Core
{
    /// <summary>
    /// 负责敌人死亡后的延迟销毁生命周期。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyLifecycle : MonoBehaviour, IEnemyLifecycle
    {
        private Coroutine _despawnRoutine;

        public void DespawnAfter(float delaySeconds)
        {
            if (_despawnRoutine == null)
            {
                _despawnRoutine = StartCoroutine(DespawnRoutine(delaySeconds));
            }
        }

        private IEnumerator DespawnRoutine(float delaySeconds)
        {
            foreach (var collider in GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            yield return new WaitForSeconds(Mathf.Max(0f, delaySeconds));
            Destroy(gameObject);
        }
    }
}
