namespace Train.Gameplay.Combat.HitEffects
{
    /// <summary>
    /// 定义武器命中并造成有效伤害后的可插拔附加效果。
    /// SwordHitbox 只依赖此接口，因此元素武器不需要继承或复制命中盒。
    /// </summary>
    public interface IWeaponHitEffect
    {
        /// <summary>
        /// 在目标实际扣除生命后执行附加效果。
        /// </summary>
        /// <param name="target">本次受击目标。</param>
        /// <param name="damageInfo">进入结算前的伤害信息。</param>
        /// <param name="result">伤害结算结果。</param>
        void OnDamageApplied(
            IDamageable target,
            DamageInfo damageInfo,
            DamageResult result);
    }
}
