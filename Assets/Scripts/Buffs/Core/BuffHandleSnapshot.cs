using System;
using System.Collections.Generic;

namespace Train.Buffs.Core
{
    /// <summary>
    /// 一个实体的完整 Buff 只读快照。
    /// 快照会复制集合，因此后续 Tick 或施加操作不会反向修改旧快照。
    /// </summary>
    public sealed class BuffHandleSnapshot
    {
        private readonly IReadOnlyList<BuffSnapshot> _buffs;

        /// <summary>
        /// 创建句柄快照。
        /// </summary>
        public BuffHandleSnapshot(
            long revision,
            BuffSnapshot[] buffs)
        {
            if (buffs == null)
            {
                throw new ArgumentNullException(nameof(buffs));
            }

            Revision = revision;
            _buffs = Array.AsReadOnly(
                (BuffSnapshot[])buffs.Clone());
        }

        /// <summary>
        /// 发生语义变更时递增的版本号。
        /// </summary>
        public long Revision { get; }

        /// <summary>
        /// 快照包含的 Buff 数量。
        /// </summary>
        public int Count => _buffs.Count;

        /// <summary>
        /// 按 Buff 标识、来源标识排序的只读 Buff 列表。
        /// </summary>
        public IReadOnlyList<BuffSnapshot> Buffs => _buffs;

        /// <summary>
        /// 按 Buff 与来源标识查找一个快照。
        /// </summary>
        public bool TryGet(
            string buffId,
            string sourceId,
            out BuffSnapshot snapshot)
        {
            for (var i = 0; i < _buffs.Count; i++)
            {
                var candidate = _buffs[i];
                if (string.Equals(
                        candidate.BuffId,
                        buffId,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        candidate.SourceId,
                        sourceId,
                        StringComparison.Ordinal))
                {
                    snapshot = candidate;
                    return true;
                }
            }

            snapshot = default;
            return false;
        }
    }
}
