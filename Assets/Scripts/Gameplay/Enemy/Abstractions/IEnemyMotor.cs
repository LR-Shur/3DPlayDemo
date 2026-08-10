using UnityEngine;

namespace Train.Gameplay.Enemy.Abstractions
{
    /// <summary>
    /// 敌人移动能力接口，供状态机查询位置、速度并驱动移动。
    /// </summary>
    public interface IEnemyMotor
    {
        Vector3 Position { get; }
        bool HasReachedDestination { get; }
        void MoveTo(Vector3 destination, float speed);
        void Face(Vector3 worldPosition, float turnSpeed);
        void Stop();
        void SetMovementEnabled(bool enabled);
    }
}
