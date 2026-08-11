using System.Threading;
using System.Threading.Tasks;
using Train.GameFlow.Data;

namespace Train.GameFlow.Application
{
    /// <summary>关卡配置提供者接口，GameFlow 不依赖 Luban 的具体实现。</summary>
    public interface ILevelDefinitionProvider
    {
        /// <summary>按关卡 ID 解析运行时关卡定义。</summary>
        Task<LevelDefinition> ResolveAsync(string levelId, CancellationToken cancellationToken);
    }

    /// <summary>敌人原型的运行时只读数据。</summary>
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

    /// <summary>敌人原型配置查询接口，供关卡运行时应用属性覆盖。</summary>
    public interface IEnemyArchetypeProvider
    {
        bool TryGet(string archetypeId, out EnemyArchetypeRuntimeData data);
    }
}
