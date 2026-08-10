using UnityEngine;

namespace Train.Gameplay.Enemy.Abstractions
{
    /// <summary>
    /// 敌人感知接口，提供当前锁定的目标。
    /// </summary>
    public interface IEnemySensor
    {
        Transform Target { get; }
        bool HasTarget { get; }
        float DistanceToTarget { get; }
        void Tick();
        void ClearTarget();
    }
}
