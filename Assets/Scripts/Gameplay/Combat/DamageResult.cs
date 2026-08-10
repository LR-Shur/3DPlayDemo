namespace Train.Gameplay.Combat
{
    /// <summary>
    /// 伤害结算结果，便于 UI、音效和命中特效读取。
    /// </summary>
    public readonly struct DamageResult
    {
        public DamageResult(float appliedDamage, float remainingHealth, bool killed)
        {
            AppliedDamage = appliedDamage;
            RemainingHealth = remainingHealth;
            Killed = killed;
        }

        public float AppliedDamage { get; }
        public float RemainingHealth { get; }
        public bool Killed { get; }
    }
}
