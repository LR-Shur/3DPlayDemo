using UnityEngine;

namespace Train.Gameplay.Enemy.Abstractions
{
    /// <summary>
    /// 敌人近战战斗能力接口，供状态机询问是否可以发起攻击。
    /// </summary>
    public interface IEnemyCombat
    {
        bool CanStartAttack { get; }
        float AttackRange { get; }
        void SetAttackTarget(Transform target);
        void BeginAttack();
        void SetDamageActive(bool active);
        void EndAttack();
    }
}
