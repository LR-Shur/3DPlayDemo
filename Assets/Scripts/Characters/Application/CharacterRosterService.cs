using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Train.Architecture.Assets;
using Train.Architecture.Events;
using Train.Characters.Core;
using Train.Characters.Data;
using Train.Characters.Events;

namespace Train.Characters.Application
{
    /// <summary>
    /// 负责角色配置转换、名册用例、应用事件发布及设置资源租约生命周期。
    /// </summary>
    public sealed class CharacterRosterService :
        ICharacterRosterService,
        IDisposable
    {
        private readonly Dictionary<string, CharacterDefinition>
            _definitionById = new(StringComparer.Ordinal);
        private readonly IEventBus _events;
        private readonly IAssetLease<CharacterSettings> _settingsLease;
        private readonly ReadOnlyCollection<CharacterDefinition> _catalog;
        private readonly CharacterRosterModel _model;
        private bool _disposed;

        /// <summary>
        /// 根据角色设置创建名册。传入资源租约的所有权会转移给本服务。
        /// </summary>
        public CharacterRosterService(
            CharacterSettings settings,
            IEventBus events,
            IAssetLease<CharacterSettings> settingsLease = null)
        {
            if (settings == null)
            {
                settingsLease?.Dispose();
                throw new ArgumentNullException(nameof(settings));
            }

            _events = events ?? throw new ArgumentNullException(nameof(events));
            _settingsLease = settingsLease;

            try
            {
                var definitions =
                    new CharacterDefinition[settings.Characters.Count];
                var specs =
                    new CharacterRosterEntrySpec[settings.Characters.Count];
                for (var i = 0; i < settings.Characters.Count; i++)
                {
                    var definition = settings.Characters[i] ??
                        throw new InvalidOperationException(
                            $"角色设置 '{settings.name}' 的目录包含空项。");
                    var spec = definition.ToCoreSpec();
                    if (!_definitionById.TryAdd(
                            spec.CharacterId,
                            definition))
                    {
                        throw new InvalidOperationException(
                            $"角色设置 '{settings.name}' 包含重复标识 " +
                            $"'{spec.CharacterId}'。");
                    }

                    definitions[i] = definition;
                    specs[i] = spec;
                }

                _model = new CharacterRosterModel(
                    specs,
                    settings.DefaultSelectedCharacterId);
                _catalog = Array.AsReadOnly(definitions);

                var snapshot = _model.Snapshot;
                _events.Publish(
                    new CharacterRosterReadyEvent(
                        _catalog.Count,
                        snapshot.UnlockedCount,
                        snapshot.SelectedCharacterId,
                        snapshot.Revision));
            }
            catch
            {
                _definitionById.Clear();
                _settingsLease?.Dispose();
                throw;
            }
        }

        /// <inheritdoc />
        public CharacterRosterSnapshot Snapshot
        {
            get
            {
                ThrowIfDisposed();
                return _model.Snapshot;
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<CharacterDefinition> Catalog
        {
            get
            {
                ThrowIfDisposed();
                return _catalog;
            }
        }

        /// <inheritdoc />
        public bool TryGetDefinition(
            string characterId,
            out CharacterDefinition definition)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(characterId))
            {
                definition = null;
                return false;
            }

            return _definitionById.TryGetValue(
                characterId,
                out definition);
        }

        /// <inheritdoc />
        public bool Unlock(string characterId)
        {
            ThrowIfDisposed();
            if (!_model.Unlock(characterId))
            {
                return false;
            }

            var snapshot = _model.Snapshot;
            _events.Publish(
                new CharacterUnlockedEvent(
                    characterId,
                    snapshot.Revision));
            return true;
        }

        /// <inheritdoc />
        public bool Select(string characterId)
        {
            ThrowIfDisposed();
            var previousCharacterId =
                _model.Snapshot.SelectedCharacterId;
            if (!_model.Select(characterId))
            {
                return false;
            }

            var snapshot = _model.Snapshot;
            _events.Publish(
                new CharacterSelectedEvent(
                    previousCharacterId,
                    snapshot.SelectedCharacterId,
                    snapshot.Revision));
            return true;
        }

        /// <summary>释放设置资源租约并拒绝后续用例调用。</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _definitionById.Clear();
            _settingsLease?.Dispose();
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(CharacterRosterService));
            }
        }
    }
}
