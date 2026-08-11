using System;
using Train.Composition.Config;
using Train.GameFlow.Application;

namespace Train.Composition.Config
{
    /// <summary>只负责把 Luban 敌人原型表暴露给 GameFlow，不参与关卡刷怪点解析。</summary>
    public sealed class LubanEnemyArchetypeProvider : IEnemyArchetypeProvider
    {
        private readonly ILubanConfigService _config;

        public LubanEnemyArchetypeProvider(ILubanConfigService config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc />
        public bool TryGet(string archetypeId, out EnemyArchetypeRuntimeData data)
        {
            data = default;
            if (!_config.IsReady || string.IsNullOrWhiteSpace(archetypeId))
            {
                return false;
            }

            var row = _config.Tables.TbEnemyArchetype.GetOrDefault(archetypeId);
            if (row == null)
            {
                return false;
            }

            data = new EnemyArchetypeRuntimeData(
                row.Id,
                row.PrefabLocation,
                row.MaxHealth,
                row.Attack,
                row.Defense,
                row.MoveSpeed);
            return true;
        }
    }
}
