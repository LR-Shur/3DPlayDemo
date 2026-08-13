using System;
using System.Collections.Generic;

namespace Train.Buffs.Core
{
    /// <summary>
    /// 单个战斗实体拥有的 Buff 句柄。
    /// 它负责创建、合并、计时、移除和入伤修正，外部始终只提交 BuffInfo。
    /// </summary>
    public sealed class BuffHandle
    {
        private readonly BuffFactoryRegistry _factoryRegistry;
        private readonly Dictionary<BuffKey, IBuff> _buffs =
            new Dictionary<BuffKey, IBuff>();

        /// <summary>
        /// 创建一个实体专属 Buff 句柄。
        /// </summary>
        public BuffHandle(
            BuffFactoryRegistry factoryRegistry)
        {
            _factoryRegistry = factoryRegistry ??
                throw new ArgumentNullException(
                    nameof(factoryRegistry));
        }

        /// <summary>
        /// Buff 发生新增、叠层、刷新、移除或到期时触发。
        /// 普通逐帧倒计时不触发，避免表现层每帧收到大量事件。
        /// </summary>
        public event EventHandler<BuffChangedEventArgs> Changed;

        /// <summary>
        /// 当前 Buff 实例数量。
        /// 同一 Buff 的不同来源分别占一个实例。
        /// </summary>
        public int Count => _buffs.Count;

        /// <summary>
        /// 每次语义变更成功提交后递增的版本号。
        /// </summary>
        public long Revision { get; private set; }

        /// <summary>
        /// 当前句柄的只读快照。
        /// </summary>
        public BuffHandleSnapshot Snapshot => CreateSnapshot();

        /// <summary>
        /// 施加 Buff。
        /// 同 Buff、同来源会按具体 Buff 策略合并，否则通过注册工厂创建新实例。
        /// </summary>
        /// <returns>施加或合并后的 Buff 快照。</returns>
        public BuffSnapshot Apply(BuffInfo info)
        {
            info.Validate();
            var key = new BuffKey(
                info.BuffId,
                info.SourceId);

            if (_buffs.TryGetValue(key, out var existing))
            {
                if (existing.StackPolicy ==
                    BuffStackPolicy.Independent)
                {
                    throw new NotSupportedException(
                        $"Buff '{info.BuffId}' 使用 Independent 策略。" +
                        "当前版本尚未加入实例序号，请为该 Buff 扩展独立实例键。");
                }

                var oldStackCount = existing.StackCount;
                existing.Reapply(info);
                var changedSnapshot =
                    existing.CreateSnapshot();
                var kind = existing.StackCount > oldStackCount
                    ? BuffChangeKind.Stacked
                    : BuffChangeKind.Refreshed;

                CommitChange(kind, changedSnapshot);
                return changedSnapshot;
            }

            var created = _factoryRegistry.Create(info);
            if (created.IsExpired)
            {
                throw new InvalidOperationException(
                    $"Buff 工厂 '{info.BuffId}' 创建了已经到期的实例。");
            }

            _buffs.Add(key, created);
            var appliedSnapshot = created.CreateSnapshot();
            CommitChange(
                BuffChangeKind.Applied,
                appliedSnapshot);
            return appliedSnapshot;
        }

        /// <summary>
        /// 推进所有 Buff 的时间，并自动移除已经到期的 Buff。
        /// </summary>
        public void Tick(float deltaTime)
        {
            ValidateDeltaTime(deltaTime);
            if (deltaTime == 0f || _buffs.Count == 0)
            {
                return;
            }

            var expiredKeys = new List<BuffKey>();
            foreach (var pair in _buffs)
            {
                pair.Value.Tick(deltaTime);
                if (pair.Value.IsExpired)
                {
                    expiredKeys.Add(pair.Key);
                }
            }

            expiredKeys.Sort(CompareKeys);
            for (var i = 0; i < expiredKeys.Count; i++)
            {
                var key = expiredKeys[i];
                var expiredBuff = _buffs[key];
                var expiredSnapshot =
                    expiredBuff.CreateSnapshot();
                _buffs.Remove(key);
                CommitChange(
                    BuffChangeKind.Expired,
                    expiredSnapshot);
            }
        }

        /// <summary>
        /// 让当前全部 Buff 依稳定顺序修正一份入伤数据。
        /// </summary>
        public DamageContext ModifyIncomingDamage(
            DamageContext context)
        {
            if (_buffs.Count == 0)
            {
                return context;
            }

            var orderedBuffs =
                new List<IBuff>(_buffs.Values);
            orderedBuffs.Sort(CompareBuffs);

            var result = context;
            for (var i = 0; i < orderedBuffs.Count; i++)
            {
                result = orderedBuffs[i]
                    .ModifyIncomingDamage(result);
            }

            return result;
        }

        public DamageContext ModifyOutgoingDamage(DamageContext context)
        {
            if (_buffs.Count == 0)
            {
                return context;
            }

            var result = context;
            foreach (var buff in _buffs.Values)
            {
                if (buff is IOutgoingDamageModifier modifier)
                {
                    result = modifier.ModifyOutgoingDamage(result);
                }
            }

            return result;
        }

        /// <summary>
        /// 主动移除指定 Buff、指定来源的实例。
        /// </summary>
        /// <returns>找到并移除时返回 true，否则返回 false。</returns>
        public bool Remove(
            string buffId,
            string sourceId)
        {
            ValidateIdentity(buffId, sourceId);
            var key = new BuffKey(buffId, sourceId);
            if (!_buffs.TryGetValue(key, out var buff))
            {
                return false;
            }

            var removedSnapshot = buff.CreateSnapshot();
            _buffs.Remove(key);
            CommitChange(
                BuffChangeKind.Removed,
                removedSnapshot);
            return true;
        }

        /// <summary>
        /// 主动移除某个来源施加的全部 Buff。
        /// 适用于来源角色离场、武器卸下或关卡清理。
        /// </summary>
        /// <returns>实际移除的 Buff 数量。</returns>
        public int RemoveAllFromSource(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException(
                    "Buff 来源标识不能为空。",
                    nameof(sourceId));
            }

            var keys = new List<BuffKey>();
            foreach (var key in _buffs.Keys)
            {
                if (string.Equals(
                        key.SourceId,
                        sourceId,
                        StringComparison.Ordinal))
                {
                    keys.Add(key);
                }
            }

            keys.Sort(CompareKeys);
            RemoveKeys(keys);
            return keys.Count;
        }

        /// <summary>
        /// 查询指定 Buff、指定来源的当前快照。
        /// </summary>
        public bool TryGetSnapshot(
            string buffId,
            string sourceId,
            out BuffSnapshot snapshot)
        {
            ValidateIdentity(buffId, sourceId);
            if (_buffs.TryGetValue(
                    new BuffKey(buffId, sourceId),
                    out var buff))
            {
                snapshot = buff.CreateSnapshot();
                return true;
            }

            snapshot = default;
            return false;
        }

        /// <summary>
        /// 创建当前句柄的时间点快照。
        /// </summary>
        public BuffHandleSnapshot CreateSnapshot()
        {
            var snapshots =
                new BuffSnapshot[_buffs.Count];
            var index = 0;
            foreach (var buff in _buffs.Values)
            {
                snapshots[index] =
                    buff.CreateSnapshot();
                index++;
            }

            Array.Sort(
                snapshots,
                CompareSnapshots);
            return new BuffHandleSnapshot(
                Revision,
                snapshots);
        }

        private void RemoveKeys(List<BuffKey> keys)
        {
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                var buff = _buffs[key];
                var removedSnapshot =
                    buff.CreateSnapshot();
                _buffs.Remove(key);
                CommitChange(
                    BuffChangeKind.Removed,
                    removedSnapshot);
            }
        }

        private void CommitChange(
            BuffChangeKind kind,
            BuffSnapshot changedBuff)
        {
            Revision = checked(Revision + 1);
            var handleSnapshot = CreateSnapshot();
            Changed?.Invoke(
                this,
                new BuffChangedEventArgs(
                    kind,
                    changedBuff,
                    handleSnapshot));
        }

        private static int CompareBuffs(
            IBuff left,
            IBuff right)
        {
            var idComparison = string.CompareOrdinal(
                left.BuffId,
                right.BuffId);
            return idComparison != 0
                ? idComparison
                : string.CompareOrdinal(
                    left.SourceId,
                    right.SourceId);
        }

        private static int CompareSnapshots(
            BuffSnapshot left,
            BuffSnapshot right)
        {
            var idComparison = string.CompareOrdinal(
                left.BuffId,
                right.BuffId);
            return idComparison != 0
                ? idComparison
                : string.CompareOrdinal(
                    left.SourceId,
                    right.SourceId);
        }

        private static int CompareKeys(
            BuffKey left,
            BuffKey right)
        {
            var idComparison = string.CompareOrdinal(
                left.BuffId,
                right.BuffId);
            return idComparison != 0
                ? idComparison
                : string.CompareOrdinal(
                    left.SourceId,
                    right.SourceId);
        }

        private static void ValidateIdentity(
            string buffId,
            string sourceId)
        {
            if (string.IsNullOrWhiteSpace(buffId))
            {
                throw new ArgumentException(
                    "Buff 标识不能为空。",
                    nameof(buffId));
            }

            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException(
                    "Buff 来源标识不能为空。",
                    nameof(sourceId));
            }
        }

        private static void ValidateDeltaTime(float deltaTime)
        {
            if (float.IsNaN(deltaTime) ||
                float.IsInfinity(deltaTime) ||
                deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaTime),
                    deltaTime,
                    "Buff 推进时间必须是非负有限数。");
            }
        }

        /// <summary>
        /// 字典内部使用的 Buff 与来源复合键，保证不同来源互不覆盖。
        /// </summary>
        private readonly struct BuffKey :
            IEquatable<BuffKey>
        {
            /// <summary>
            /// 创建一个复合键。
            /// </summary>
            public BuffKey(
                string buffId,
                string sourceId)
            {
                BuffId = buffId;
                SourceId = sourceId;
            }

            /// <summary>
            /// Buff 稳定标识。
            /// </summary>
            public string BuffId { get; }

            /// <summary>
            /// 来源稳定标识。
            /// </summary>
            public string SourceId { get; }

            /// <inheritdoc />
            public bool Equals(BuffKey other)
            {
                return string.Equals(
                        BuffId,
                        other.BuffId,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        SourceId,
                        other.SourceId,
                        StringComparison.Ordinal);
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                return obj is BuffKey other &&
                    Equals(other);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    return ((BuffId != null
                                ? StringComparer.Ordinal
                                    .GetHashCode(BuffId)
                                : 0) * 397) ^
                        (SourceId != null
                            ? StringComparer.Ordinal
                                .GetHashCode(SourceId)
                            : 0);
                }
            }
        }
    }
}
