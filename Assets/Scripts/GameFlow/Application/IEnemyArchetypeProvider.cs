namespace Train.GameFlow.Application
{
    /// <summary>敌人原型配置查询接口，供关卡运行时应用属性覆盖。</summary>
    public interface IEnemyArchetypeProvider
    {
        bool TryGet(string archetypeId, out EnemyArchetypeRuntimeData data);
    }

    /// <summary>Luban 敌人原型转换后的运行时只读数据。</summary>
    public readonly struct EnemyArchetypeRuntimeData
    {
        public EnemyArchetypeRuntimeData(
            string id,
            string prefabLocation,
            float maxHealth,
            float attack,
            float defense,
            float moveSpeed)
        {
            Id = id;
            PrefabLocation = prefabLocation;
            MaxHealth = maxHealth;
            Attack = attack;
            Defense = defense;
            MoveSpeed = moveSpeed;
        }

        public string Id { get; }
        public string PrefabLocation { get; }
        public float MaxHealth { get; }
        public float Attack { get; }
        public float Defense { get; }
        public float MoveSpeed { get; }
    }
}
