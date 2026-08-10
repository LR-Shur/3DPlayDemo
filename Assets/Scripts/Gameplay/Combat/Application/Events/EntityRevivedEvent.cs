namespace Train.Gameplay.Combat.Application.Events
{
    /// <summary>
    /// 表示一个战斗实体已经复活。
    /// </summary>
    public readonly struct EntityRevivedEvent
    {
        public EntityRevivedEvent(Health health)
        {
            Health = health;
        }

        public Health Health { get; }
    }
}
