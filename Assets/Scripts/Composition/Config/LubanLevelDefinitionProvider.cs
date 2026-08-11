using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Train.Architecture.Bootstrap;
using Train.Architecture.Services;
using Train.GameFlow.Application;
using Train.GameFlow.Data;
using UnityEngine;

namespace Train.Composition.Config
{
    /// <summary>
    /// 将 Luban 的关卡、刷怪点和敌人原型表转换为 GameFlow 运行时数据。
    /// 场景中的点位仅用于编辑预览，实际刷怪以表格为准。
    /// </summary>
    public sealed class LubanLevelDefinitionProvider :
        ILevelDefinitionProvider,
        IEnemyArchetypeProvider,
        IDisposable
    {
        private readonly ILubanConfigService _config;
        private readonly Dictionary<string, LevelDefinition> _definitions = new();
        private bool _disposed;

        public LubanLevelDefinitionProvider(ILubanConfigService config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc />
        public async Task<LevelDefinition> ResolveAsync(
            string levelId,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            await _config.InitializeAsync(cancellationToken);
            var key = NormalizeId(levelId);
            if (_definitions.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var row = _config.Tables.TbLevel.GetOrDefault(key);
            if (row == null)
            {
                throw new InvalidOperationException($"Luban level '{levelId}' was not found.");
            }

            var spawns = new List<EnemySpawnDefinition>();
            foreach (var spawn in _config.Tables.TbLevelSpawn.DataList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (spawn == null || !string.Equals(spawn.LevelId, row.Id, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var archetype = _config.Tables.TbEnemyArchetype.GetOrDefault(spawn.ArchetypeId);
                var prefabLocation = string.IsNullOrWhiteSpace(spawn.PrefabLocation)
                    ? archetype?.PrefabLocation
                    : spawn.PrefabLocation;
                spawns.Add(EnemySpawnDefinition.CreateRuntime(
                    spawn.SpawnId,
                    spawn.ArchetypeId,
                    prefabLocation,
                    new Vector3(spawn.PosX, spawn.PosY, spawn.PosZ),
                    spawn.RotY));
            }

            var definition = LevelDefinition.CreateRuntime(
                row.Id,
                row.DisplayName,
                row.SceneLocation,
                row.IntroSeconds,
                row.MaxPlayerDeaths,
                spawns);
            _definitions[key] = definition;
            return definition;
        }

        /// <inheritdoc />
        public bool TryGet(string archetypeId, out EnemyArchetypeRuntimeData data)
        {
            data = default;
            if (_disposed || !_config.IsReady || string.IsNullOrWhiteSpace(archetypeId))
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

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (var definition in _definitions.Values)
            {
                if (definition != null)
                {
                    UnityEngine.Object.Destroy(definition);
                }
            }

            _definitions.Clear();
            _disposed = true;
        }

        private static string NormalizeId(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                return string.Empty;
            }

            var normalized = levelId.Trim().ToLowerInvariant();
            return normalized.Replace("level.", string.Empty).Replace('.', '_');
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LubanLevelDefinitionProvider));
            }
        }
    }
}
