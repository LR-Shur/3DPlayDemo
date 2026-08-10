using UnityEngine;

namespace Train.Gameplay.Combat
{
    /// <summary>
    /// 所有伤害来源统一实现的接口。
    /// </summary>
    public interface IDamageSource
    {
        /// <summary>
        /// 伤害检测发生的位置，例如剑刃或弹丸。
        /// </summary>
        Transform SourceTransform { get; }

        /// <summary>
        /// 伤害来源所属角色的根节点，用于排除自伤和查询阵营。
        /// </summary>
        Transform OwnerTransform { get; }

        float Damage { get; }
        DamageType DamageType { get; }
        bool IsDamageActive { get; }
    }
}
