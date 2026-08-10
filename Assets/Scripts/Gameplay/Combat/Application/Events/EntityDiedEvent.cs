namespace Train.Gameplay.Combat.Application.Events
{
    /// <summary>
    /// 表示一个战斗实体死亡，并携带最后一击信息。
    /// </summary>
    public readonly struct EntityDiedEvent
    {
        public EntityDiedEvent(Health health, DamageInfo killingBlow)
        {
            Health = health;
            KillingBlow = killingBlow;
        }

        public Health Health { get; }
        public DamageInfo KillingBlow { get; }
    }
}
