using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Train.Architecture.Assets;
using Train.Architecture.Events;
using Train.Dialogue.Events;
using Train.GameFlow.Application.Events;
using Train.Inventory.Application;
using Train.Inventory.Core;
using Train.Inventory.Events;
using Train.Quest.Core;
using Train.Quest.Data;
using Train.Quest.Events;

namespace Train.Quest.Application
{
    /// <summary>
    /// 协调任务配置、纯领域进度模型、游戏事实事件和背包奖励事务。
    /// 这是任务模块供表现层使用的 Server/Service 应用层实现。
    /// </summary>
    public sealed class QuestService : IQuestService, IDisposable
    {
        private readonly Dictionary<string, QuestDefinition> _definitions =
            new(StringComparer.Ordinal);
        private readonly Dictionary<string, QuestProgressModel> _models =
            new(StringComparer.Ordinal);
        private readonly List<IDisposable> _eventSubscriptions = new();
        private readonly IEventBus _events;
        private readonly IInventoryService _inventory;
        private readonly IAssetLease<QuestSettings> _settingsLease;
        private readonly ReadOnlyCollection<QuestDefinition> _catalog;
        private string _trackedQuestId;
        private bool _disposed;

        /// <summary>
        /// 根据任务设置创建目录和任务实例，并取得设置资源租约的所有权。
        /// </summary>
        public QuestService(
            QuestSettings settings,
            IInventoryService inventory,
            IEventBus events,
            IAssetLease<QuestSettings> settingsLease = null)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _inventory = inventory ??
                throw new ArgumentNullException(nameof(inventory));
            _settingsLease = settingsLease;

            if (settings == null)
            {
                PublishSystemFailure("任务设置资源不能为空。");
                _settingsLease?.Dispose();
                throw new ArgumentNullException(nameof(settings));
            }

            try
            {
                var catalog = BuildCatalog(settings);
                _catalog = Array.AsReadOnly(catalog);
                SubscribeToGameFacts();
                ActivateConfiguredQuests();

                _events.Publish(
                    new QuestSystemReadyEvent(
                        _catalog.Count,
                        CountAcceptedQuests(),
                        _trackedQuestId));
            }
            catch (Exception exception)
            {
                ReleaseSubscriptionsAndModels();
                _definitions.Clear();
                _models.Clear();
                _settingsLease?.Dispose();
                PublishSystemFailure(exception.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<QuestDefinition> Catalog
        {
            get
            {
                ThrowIfDisposed();
                return _catalog;
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<QuestProgressSnapshot> Snapshots
        {
            get
            {
                ThrowIfDisposed();
                var snapshots =
                    new QuestProgressSnapshot[_catalog.Count];
                for (var i = 0; i < _catalog.Count; i++)
                {
                    snapshots[i] =
                        _models[_catalog[i].QuestId].Snapshot;
                }

                return Array.AsReadOnly(snapshots);
            }
        }

        /// <inheritdoc />
        public string TrackedQuestId
        {
            get
            {
                ThrowIfDisposed();
                return _trackedQuestId;
            }
        }

        /// <inheritdoc />
        public bool AcceptQuest(string questId)
        {
            ThrowIfDisposed();
            if (!TryGetModel(questId, out var model) ||
                model.Status != QuestStatus.Inactive)
            {
                return false;
            }

            model.Activate();
            if (string.IsNullOrEmpty(_trackedQuestId))
            {
                SetTrackedQuestId(questId);
            }

            return true;
        }

        /// <inheritdoc />
        public bool TrackQuest(string questId)
        {
            ThrowIfDisposed();
            if (!TryGetModel(questId, out var model) ||
                model.Status == QuestStatus.Inactive ||
                model.Status == QuestStatus.Claimed)
            {
                return false;
            }

            if (string.Equals(
                    _trackedQuestId,
                    questId,
                    StringComparison.Ordinal))
            {
                return true;
            }

            SetTrackedQuestId(questId);
            return true;
        }

        /// <inheritdoc />
        public void ClearTrackedQuest()
        {
            ThrowIfDisposed();
            SetTrackedQuestId(null);
        }

        /// <inheritdoc />
        public bool ApplyFact(QuestFact fact)
        {
            ThrowIfDisposed();
            var changed = false;

            for (var i = 0; i < _catalog.Count; i++)
            {
                if (_models[_catalog[i].QuestId].ApplyFact(fact))
                {
                    changed = true;
                }
            }

            return changed;
        }

        /// <inheritdoc />
        public QuestRewardClaimResult ClaimRewards(string questId)
        {
            ThrowIfDisposed();
            if (!TryGetModel(questId, out var model))
            {
                return PublishClaimFailure(
                    questId,
                    QuestRewardClaimFailureReason.QuestNotFound);
            }

            if (model.Status != QuestStatus.Completed)
            {
                return PublishClaimFailure(
                    questId,
                    QuestRewardClaimFailureReason.QuestNotCompleted);
            }

            var planningFailure = CreateRewardGrantPlan(
                model.Spec.Rewards,
                out var grantPlan);
            if (planningFailure != QuestRewardClaimFailureReason.None)
            {
                return PublishClaimFailure(questId, planningFailure);
            }

            var granted = new List<KeyValuePair<string, int>>();
            try
            {
                foreach (var reward in grantPlan)
                {
                    if (!_inventory.TryAdd(reward.Key, reward.Value))
                    {
                        RollbackGrantedRewards(granted);
                        return PublishClaimFailure(
                            questId,
                            QuestRewardClaimFailureReason.InventoryChanged);
                    }

                    granted.Add(reward);
                }

                model.Claim();
            }
            catch
            {
                RollbackGrantedRewards(granted);
                return PublishClaimFailure(
                    questId,
                    QuestRewardClaimFailureReason.InventoryChanged);
            }

            if (string.Equals(
                    _trackedQuestId,
                    questId,
                    StringComparison.Ordinal))
            {
                SetTrackedQuestId(FindNextTrackableQuestId());
            }

            _events.Publish(
                new QuestRewardsClaimedEvent(
                    questId,
                    CopyRewards(model.Spec.Rewards)));
            return QuestRewardClaimResult.Success(questId);
        }

        /// <inheritdoc />
        public bool TryGetSnapshot(
            string questId,
            out QuestProgressSnapshot snapshot)
        {
            ThrowIfDisposed();
            if (TryGetModel(questId, out var model))
            {
                snapshot = model.Snapshot;
                return true;
            }

            snapshot = null;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetDefinition(
            string questId,
            out QuestDefinition definition)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(questId))
            {
                definition = null;
                return false;
            }

            return _definitions.TryGetValue(questId, out definition);
        }

        /// <summary>
        /// 停止监听游戏事实、释放任务设置资源租约，并使服务不可再使用。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            ReleaseSubscriptionsAndModels();
            _definitions.Clear();
            _models.Clear();
            _settingsLease?.Dispose();
            _disposed = true;
        }

        private QuestDefinition[] BuildCatalog(QuestSettings settings)
        {
            var catalog = settings.Quests
                .Select(
                    definition => definition ??
                        throw new InvalidOperationException(
                            $"任务设置 '{settings.name}' 的目录包含空项。"))
                .OrderBy(definition => definition.SortOrder)
                .ThenBy(
                    definition => definition.QuestId,
                    StringComparer.Ordinal)
                .ToArray();

            for (var i = 0; i < catalog.Length; i++)
            {
                var definition = catalog[i];
                var spec = definition.ToCoreSpec();

                if (!_definitions.TryAdd(spec.Id, definition))
                {
                    throw new InvalidOperationException(
                        $"任务设置 '{settings.name}' 包含重复任务标识 " +
                        $"'{spec.Id}'。");
                }

                var model = new QuestProgressModel(spec);
                model.Changed += OnModelChanged;
                _models.Add(spec.Id, model);
            }

            return catalog;
        }

        private void SubscribeToGameFacts()
        {
            _eventSubscriptions.Add(
                _events.Subscribe<EnemyDefeatedEvent>(
                    OnEnemyDefeated));
            _eventSubscriptions.Add(
                _events.Subscribe<ItemAcquiredEvent>(
                    OnItemAcquired));
            _eventSubscriptions.Add(
                _events.Subscribe<LevelCompletedEvent>(
                    OnLevelCompleted));
            _eventSubscriptions.Add(
                _events.Subscribe<DialogueCompletedEvent>(
                    OnDialogueCompleted));
        }

        private void ActivateConfiguredQuests()
        {
            for (var i = 0; i < _catalog.Count; i++)
            {
                var definition = _catalog[i];
                if (definition.AutoAccept)
                {
                    AcceptQuest(definition.QuestId);
                }
            }
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent message)
        {
            if (!string.IsNullOrWhiteSpace(message.EnemyArchetypeId))
            {
                ApplyFact(
                    new QuestFact(
                        QuestFactType.EnemyDefeated,
                        message.EnemyArchetypeId));
            }
        }

        private void OnItemAcquired(ItemAcquiredEvent message)
        {
            if (!string.IsNullOrWhiteSpace(message.ItemId) &&
                message.Quantity > 0)
            {
                ApplyFact(
                    new QuestFact(
                        QuestFactType.ItemAcquired,
                        message.ItemId,
                        message.Quantity));
            }
        }

        private void OnLevelCompleted(LevelCompletedEvent message)
        {
            if (!string.IsNullOrWhiteSpace(message.LevelId))
            {
                ApplyFact(
                    new QuestFact(
                        QuestFactType.LevelCompleted,
                        message.LevelId));
            }
        }

        private void OnDialogueCompleted(DialogueCompletedEvent message)
        {
            if (!message.WasCompleted ||
                string.IsNullOrWhiteSpace(message.Snapshot.DialogueId))
            {
                return;
            }

            ApplyFact(
                new QuestFact(
                    QuestFactType.DialogueCompleted,
                    message.Snapshot.DialogueId));
        }

        private void OnModelChanged(
            object sender,
            QuestProgressChangedEventArgs change)
        {
            var kind = ResolveChangeKind(
                change.OldSnapshot,
                change.NewSnapshot);
            _events.Publish(
                new QuestChangedEvent(kind, change.NewSnapshot));
        }

        private static QuestChangeKind ResolveChangeKind(
            QuestProgressSnapshot oldSnapshot,
            QuestProgressSnapshot newSnapshot)
        {
            if (oldSnapshot.Status == QuestStatus.Inactive &&
                newSnapshot.Status == QuestStatus.Active)
            {
                return QuestChangeKind.Accepted;
            }

            if (newSnapshot.Status == QuestStatus.Completed &&
                oldSnapshot.Status != QuestStatus.Completed)
            {
                return QuestChangeKind.Completed;
            }

            if (newSnapshot.Status == QuestStatus.Claimed)
            {
                return QuestChangeKind.Claimed;
            }

            return QuestChangeKind.Progressed;
        }

        private QuestRewardClaimFailureReason CreateRewardGrantPlan(
            IReadOnlyList<QuestRewardSpec> rewards,
            out Dictionary<string, int> grantPlan)
        {
            grantPlan = new Dictionary<string, int>(StringComparer.Ordinal);
            try
            {
                for (var i = 0; i < rewards.Count; i++)
                {
                    var reward = rewards[i];
                    if (!_inventory.TryGetDefinition(
                            reward.ItemId,
                            out _))
                    {
                        return QuestRewardClaimFailureReason.InvalidReward;
                    }

                    grantPlan.TryGetValue(
                        reward.ItemId,
                        out var currentAmount);
                    grantPlan[reward.ItemId] =
                        checked(currentAmount + reward.Amount);
                }
            }
            catch (OverflowException)
            {
                return QuestRewardClaimFailureReason.InvalidReward;
            }

            return CanFitGrantPlan(grantPlan)
                ? QuestRewardClaimFailureReason.None
                : QuestRewardClaimFailureReason.InventoryFull;
        }

        private bool CanFitGrantPlan(
            IReadOnlyDictionary<string, int> grantPlan)
        {
            var snapshot = _inventory.Snapshot;
            var itemIds = new string[snapshot.Capacity];
            var quantities = new int[snapshot.Capacity];

            for (var i = 0; i < snapshot.Slots.Count; i++)
            {
                var slot = snapshot.Slots[i];
                itemIds[i] = slot.ItemId;
                quantities[i] = slot.Quantity;
            }

            foreach (var reward in grantPlan)
            {
                if (!_inventory.TryGetDefinition(
                        reward.Key,
                        out var definition))
                {
                    return false;
                }

                var remaining = reward.Value;
                var maxStack = definition.MaxStack;

                for (var i = 0; i < itemIds.Length && remaining > 0; i++)
                {
                    if (!string.Equals(
                            itemIds[i],
                            reward.Key,
                            StringComparison.Ordinal) ||
                        quantities[i] >= maxStack)
                    {
                        continue;
                    }

                    var moved = Math.Min(
                        remaining,
                        maxStack - quantities[i]);
                    quantities[i] += moved;
                    remaining -= moved;
                }

                for (var i = 0; i < itemIds.Length && remaining > 0; i++)
                {
                    if (quantities[i] != 0)
                    {
                        continue;
                    }

                    var moved = Math.Min(remaining, maxStack);
                    itemIds[i] = reward.Key;
                    quantities[i] = moved;
                    remaining -= moved;
                }

                if (remaining > 0)
                {
                    return false;
                }
            }

            return true;
        }

        private void RollbackGrantedRewards(
            IReadOnlyList<KeyValuePair<string, int>> granted)
        {
            for (var i = granted.Count - 1; i >= 0; i--)
            {
                _inventory.TryRemove(
                    granted[i].Key,
                    granted[i].Value);
            }
        }

        private QuestRewardClaimResult PublishClaimFailure(
            string questId,
            QuestRewardClaimFailureReason reason)
        {
            _events.Publish(
                new QuestRewardClaimFailedEvent(questId, reason));
            return QuestRewardClaimResult.Failure(questId, reason);
        }

        private static QuestRewardSpec[] CopyRewards(
            IReadOnlyList<QuestRewardSpec> rewards)
        {
            var copy = new QuestRewardSpec[rewards.Count];
            for (var i = 0; i < rewards.Count; i++)
            {
                copy[i] = rewards[i];
            }

            return copy;
        }

        private bool TryGetModel(
            string questId,
            out QuestProgressModel model)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                model = null;
                return false;
            }

            return _models.TryGetValue(questId, out model);
        }

        private void SetTrackedQuestId(string questId)
        {
            if (string.Equals(
                    _trackedQuestId,
                    questId,
                    StringComparison.Ordinal))
            {
                return;
            }

            var previous = _trackedQuestId;
            _trackedQuestId = questId;
            _events.Publish(
                new TrackedQuestChangedEvent(previous, questId));
        }

        private string FindNextTrackableQuestId()
        {
            for (var i = 0; i < _catalog.Count; i++)
            {
                var model = _models[_catalog[i].QuestId];
                if (model.Status == QuestStatus.Active)
                {
                    return model.Spec.Id;
                }
            }

            for (var i = 0; i < _catalog.Count; i++)
            {
                var model = _models[_catalog[i].QuestId];
                if (model.Status == QuestStatus.Completed)
                {
                    return model.Spec.Id;
                }
            }

            return null;
        }

        private int CountAcceptedQuests()
        {
            var count = 0;
            foreach (var model in _models.Values)
            {
                if (model.Status != QuestStatus.Inactive)
                {
                    count++;
                }
            }

            return count;
        }

        private void ReleaseSubscriptionsAndModels()
        {
            for (var i = _eventSubscriptions.Count - 1; i >= 0; i--)
            {
                _eventSubscriptions[i]?.Dispose();
            }

            _eventSubscriptions.Clear();
            foreach (var model in _models.Values)
            {
                model.Changed -= OnModelChanged;
            }
        }

        private void PublishSystemFailure(string reason)
        {
            try
            {
                _events.Publish(new QuestSystemFailedEvent(reason));
            }
            catch
            {
                // 初始化失败信息不应覆盖最初的配置异常。
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(QuestService));
            }
        }
    }
}
