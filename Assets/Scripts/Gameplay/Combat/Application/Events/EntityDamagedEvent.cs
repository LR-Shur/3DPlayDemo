namespace Train.Gameplay.Combat.Application.Events
{
    /// <summary>
    /// 表示一个战斗实体受到伤害。
    /// </summary>
    public readonly struct EntityDamagedEvent
    {
        public EntityDamagedEvent(
            Health health,
            DamageInfo damage,
            DamageResult result)
        {
            Health = health;
            Damage = damage;
            Result = result;
        }

        public Health Health { get; }
        public DamageInfo Damage { get; }
        public DamageResult Result { get; }
    }
}
