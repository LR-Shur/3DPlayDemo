using UnityEngine;

namespace Train.Gameplay.Enemy.Abstractions
{
    /// <summary>
    /// 敌人巡逻目的地提供接口，与具体寻路实现解耦。
    /// </summary>
    public interface IEnemyPatrol
    {
        bool TryGetNextDestination(Vector3 currentPosition, out Vector3 destination);
        void ResetPatrol();
    }
}
