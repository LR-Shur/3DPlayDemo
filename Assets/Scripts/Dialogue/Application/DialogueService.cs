using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Train.Architecture.Assets;
using Train.Architecture.Events;
using Train.Dialogue.Core;
using Train.Dialogue.Data;
using Train.Dialogue.Events;

namespace Train.Dialogue.Application
{
    /// <summary>
    /// 负责对话资产转换、单活动会话用例、应用事件发布和资源租约生命周期。
    /// </summary>
    public sealed class DialogueService : IDialogueService, IDisposable
    {
        private readonly Dictionary<string, DialogueDefinition> _definitions =
            new(StringComparer.Ordinal);
        private readonly Dictionary<string, DialogueGraphSpec> _graphs =
            new(StringComparer.Ordinal);
        private readonly ReadOnlyCollection<DialogueDefinition> _catalog;
        private readonly IEventBus _events;
        private readonly IDialogueVariableStore _variables;
        private readonly DialogueHandlerRegistry _handlers;
        private readonly IAssetLease<DialogueSettings> _settingsLease;
        private DialogueSession _session;
        private long _sessionSequence;
        private bool _disposed;

        /// <summary>
        /// 根据设置资产创建对话目录；传入租约的所有权会转移给本服务。
        /// </summary>
        public DialogueService(
            DialogueSettings settings,
            IEventBus events,
            IDialogueVariableStore variables = null,
            IEnumerable<IDialogueCondition> conditions = null,
            IEnumerable<IDialogueCommand> commands = null,
            IAssetLease<DialogueSettings> settingsLease = null)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _events = events ?? throw new ArgumentNullException(nameof(events));
            _variables = variables ?? new DialogueVariableStore();
            _settingsLease = settingsLease;

            try
            {
                _handlers = new DialogueHandlerRegistry(
                    conditions,
                    commands);
                var catalog =
                    new DialogueDefinition[settings.Dialogues.Count];
                for (var i = 0; i < settings.Dialogues.Count; i++)
                {
                    var definition = settings.Dialogues[i] ??
                        throw new InvalidOperationException(
                            $"对话设置 '{settings.name}' 的目录包含 null。");
                    var graph = definition.ToCoreSpec();
                    if (!_definitions.TryAdd(
                            graph.DialogueId,
                            definition))
                    {
                        throw new InvalidOperationException(
                            $"对话设置 '{settings.name}' 包含重复标识 " +
                            $"'{graph.DialogueId}'。");
                    }

                    _graphs.Add(graph.DialogueId, graph);
                    catalog[i] = definition;
                }

                _catalog = Array.AsReadOnly(catalog);
            }
            catch
            {
                _settingsLease?.Dispose();
                throw;
            }
        }

        /// <inheritdoc />
        public DialogueSessionSnapshot Current
        {
            get
            {
                ThrowIfDisposed();
                return _session?.Snapshot;
            }
        }

        /// <inheritdoc />
        public bool IsActive
        {
            get
            {
                ThrowIfDisposed();
                var status = _session?.Status;
                return status == DialogueSessionStatus.AwaitingContinue ||
                       status == DialogueSessionStatus.AwaitingChoice;
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<DialogueDefinition> Catalog
        {
            get
            {
                ThrowIfDisposed();
                return _catalog;
            }
        }

        /// <inheritdoc />
        public bool TryGetDefinition(
            string dialogueId,
            out DialogueDefinition definition)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(dialogueId))
            {
                definition = null;
                return false;
            }

            return _definitions.TryGetValue(dialogueId, out definition);
        }

        /// <inheritdoc />
        public bool Start(string dialogueId)
        {
            ThrowIfDisposed();
            if (IsActive ||
                string.IsNullOrWhiteSpace(dialogueId) ||
                !_graphs.TryGetValue(dialogueId, out var graph))
            {
                return false;
            }

            var sequence = checked(_sessionSequence + 1);
            var session = new DialogueSession(
                $"{dialogueId}:{sequence}",
                graph,
                _variables,
                _handlers);
            session.Start();
            _sessionSequence = sequence;
            _session = session;

            var snapshot = session.Snapshot;
            _events.Publish(new DialogueStartedEvent(snapshot));
            PublishCurrentOrCompleted(snapshot);
            return true;
        }

        /// <inheritdoc />
        public bool Continue()
        {
            ThrowIfDisposed();
            if (_session?.Status !=
                DialogueSessionStatus.AwaitingContinue)
            {
                return false;
            }

            _session.Continue();
            PublishCurrentOrCompleted(_session.Snapshot);
            return true;
        }

        /// <inheritdoc />
        public bool Choose(string choiceId)
        {
            ThrowIfDisposed();
            if (_session?.Status != DialogueSessionStatus.AwaitingChoice ||
                string.IsNullOrWhiteSpace(choiceId))
            {
                return false;
            }

            var before = _session.Snapshot;
            var isVisible = false;
            for (var i = 0; i < before.Choices.Count; i++)
            {
                if (string.Equals(
                        before.Choices[i].ChoiceId,
                        choiceId,
                        StringComparison.Ordinal))
                {
                    isVisible = true;
                    break;
                }
            }

            if (!isVisible)
            {
                return false;
            }

            var choice = _session.Choose(choiceId);
            var after = _session.Snapshot;
            _events.Publish(
                new DialogueChoiceSelectedEvent(
                    after.SessionId,
                    after.DialogueId,
                    choice,
                    after.Revision));
            PublishCurrentOrCompleted(after);
            return true;
        }

        /// <inheritdoc />
        public bool Cancel()
        {
            ThrowIfDisposed();
            if (!IsActive)
            {
                return false;
            }

            _session.Cancel();
            _events.Publish(
                new DialogueCompletedEvent(_session.Snapshot));
            return true;
        }

        /// <summary>释放设置资产租约并拒绝后续用例调用。</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _definitions.Clear();
            _graphs.Clear();
            _session = null;
            _settingsLease?.Dispose();
            _disposed = true;
        }

        private void PublishCurrentOrCompleted(
            DialogueSessionSnapshot snapshot)
        {
            if (snapshot.IsTerminal)
            {
                _events.Publish(new DialogueCompletedEvent(snapshot));
                return;
            }

            _events.Publish(new DialogueLineChangedEvent(snapshot));
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DialogueService));
            }
        }
    }
}
