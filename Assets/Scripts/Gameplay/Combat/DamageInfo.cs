using UnityEngine;

namespace Train.Gameplay.Combat
{
    /// <summary>
    /// 一次伤害所需的完整运行时上下文。
    /// 伤害来源通过 IDamageSource 抽象，不直接依赖具体 GameObject。
    /// </summary>
    public readonly struct DamageInfo
    {
        public DamageInfo(
            float amount,
            IDamageSource source,
            Vector3 hitPoint,
            Vector3 hitDirection,
            float impactForce = 0f,
            DamageType damageType = DamageType.Physical)
        {
            Amount = Mathf.Max(0f, amount);
            Source = source;
            HitPoint = hitPoint;
            HitDirection = hitDirection.sqrMagnitude > 0f
                ? hitDirection.normalized
                : Vector3.zero;
            ImpactForce = Mathf.Max(0f, impactForce);
            DamageType = damageType;
        }

        public float Amount { get; }
        public IDamageSource Source { get; }
        public Vector3 HitPoint { get; }
        public Vector3 HitDirection { get; }
        public float ImpactForce { get; }
        public DamageType DamageType { get; }

        /// <summary>
        /// 保留伤害来源、命中位置和元素类型，仅替换伤害数值。
        /// Buff、护甲与难度系统可用此方法串联计算，而不必修改原始数据。
        /// </summary>
        /// <param name="amount">替换后的非负伤害数值。</param>
        /// <returns>包含新数值的伤害信息副本。</returns>
        public DamageInfo WithAmount(float amount)
        {
            return new DamageInfo(
                amount,
                Source,
                HitPoint,
                HitDirection,
                ImpactForce,
                DamageType);
        }
    }
}
