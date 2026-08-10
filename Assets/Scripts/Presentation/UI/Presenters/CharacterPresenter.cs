using System;
using System.Collections.Generic;
using Train.Architecture.Events;
using Train.Characters.Application;
using Train.Characters.Data;
using Train.Characters.Events;
using Train.Presentation.UI.Contracts;
using Train.Presentation.UI.ViewModels;

namespace Train.Presentation.UI.Presenters
{
    /// <summary>
    /// 连接角色名册 Server 与角色页面，负责选择和解锁用例。
    /// 当前选择只记录名册状态，不直接替换玩家控制器。
    /// </summary>
    public sealed class CharacterPresenter : IDisposable
    {
        private readonly ICharacterRosterService _roster;
        private readonly ICharacterView _view;
        private readonly Action _closeRequested;
        private readonly List<IDisposable> _subscriptions = new();
        private string _selectedCharacterId;
        private string _feedbackText = string.Empty;
        private bool _disposed;

        /// <summary>
        /// 创建角色 Presenter、订阅名册事件并立即渲染。
        /// </summary>
        public CharacterPresenter(
            ICharacterRosterService roster,
            IEventBus events,
            ICharacterView view,
            Action closeRequested)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _closeRequested = closeRequested ??
                throw new ArgumentNullException(nameof(closeRequested));
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            _view.EntrySelected += OnEntrySelected;
            _view.SelectRequested += OnSelectRequested;
            _view.UnlockRequested += OnUnlockRequested;
            _view.CloseRequested += OnCloseRequested;
            _subscriptions.Add(
                events.Subscribe<CharacterSelectedEvent>(
                    OnCharacterSelected));
            _subscriptions.Add(
                events.Subscribe<CharacterUnlockedEvent>(
                    OnCharacterUnlocked));

            _selectedCharacterId = _roster.Snapshot.SelectedCharacterId;
            if (_selectedCharacterId == null &&
                _roster.Catalog.Count > 0)
            {
                _selectedCharacterId = _roster.Catalog[0].CharacterId;
            }

            Render();
        }

        /// <summary>断开视图与事件订阅。</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _view.EntrySelected -= OnEntrySelected;
            _view.SelectRequested -= OnSelectRequested;
            _view.UnlockRequested -= OnUnlockRequested;
            _view.CloseRequested -= OnCloseRequested;
            for (var i = _subscriptions.Count - 1; i >= 0; i--)
            {
                _subscriptions[i].Dispose();
            }

            _subscriptions.Clear();
            _disposed = true;
        }

        private void OnEntrySelected(int index)
        {
            if (index < 0 || index >= _roster.Catalog.Count)
            {
                return;
            }

            _selectedCharacterId = _roster.Catalog[index].CharacterId;
            _feedbackText = string.Empty;
            Render();
        }

        private void OnSelectRequested()
        {
            if (string.IsNullOrWhiteSpace(_selectedCharacterId))
            {
                return;
            }

            _feedbackText = _roster.Select(_selectedCharacterId)
                ? "已切换当前代理人。"
                : "该角色当前无法选择。";
            Render();
        }

        private void OnUnlockRequested()
        {
            if (string.IsNullOrWhiteSpace(_selectedCharacterId))
            {
                return;
            }

            _feedbackText = _roster.Unlock(_selectedCharacterId)
                ? "已解锁该角色。"
                : "该角色已经解锁或不在名册中。";
            Render();
        }

        private void OnCloseRequested()
        {
            _closeRequested();
        }

        private void OnCharacterSelected(CharacterSelectedEvent message)
        {
            _selectedCharacterId = message.CurrentCharacterId;
            Render();
        }

        private void OnCharacterUnlocked(CharacterUnlockedEvent message)
        {
            Render();
        }

        /// <summary>从权威名册快照构建角色页面视图模型。</summary>
        private void Render()
        {
            if (_disposed)
            {
                return;
            }

            var snapshot = _roster.Snapshot;
            var entries = new List<CharacterEntryViewModel>(
                _roster.Catalog.Count);
            foreach (var definition in _roster.Catalog)
            {
                snapshot.TryGetEntry(
                    definition.CharacterId,
                    out var entry);
                entries.Add(
                    new CharacterEntryViewModel(
                        definition.CharacterId,
                        definition.DisplayName,
                        definition.CombatRole,
                        entry.IsUnlocked,
                        entry.IsSelected));
            }

            var selected = FindSelectedDefinition();
            snapshot.TryGetEntry(
                selected != null
                    ? selected.CharacterId
                    : string.Empty,
                out var selectedEntry);
            _view.Render(
                new CharacterScreenViewModel(
                    entries,
                    selected != null
                        ? selected.CharacterId
                        : string.Empty,
                    selected != null
                        ? selected.DisplayName
                        : "未选择角色",
                    selected != null
                        ? selected.Description
                        : string.Empty,
                    selected != null
                        ? selected.Faction
                        : string.Empty,
                    selected != null
                        ? selected.CombatRole
                        : string.Empty,
                    selectedEntry.IsUnlocked,
                    selectedEntry.IsSelected,
                    selected != null && !selectedEntry.IsUnlocked,
                    _feedbackText));
        }

        private CharacterDefinition FindSelectedDefinition()
        {
            if (string.IsNullOrWhiteSpace(_selectedCharacterId))
            {
                return null;
            }

            return _roster.TryGetDefinition(
                _selectedCharacterId,
                out var definition)
                ? definition
                : null;
        }
    }
}
