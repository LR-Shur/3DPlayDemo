using System;

namespace Train.Buffs.Core
{
    /// <summary>
    /// Buff 句柄变更事件数据，同时携带变更项和变更后的完整句柄快照。
    /// </summary>
    public sealed class BuffChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 创建 Buff 句柄变更事件数据。
        /// </summary>
        public BuffChangedEventArgs(
            BuffChangeKind kind,
            BuffSnapshot changedBuff,
            BuffHandleSnapshot handleSnapshot)
        {
            HandleSnapshot = handleSnapshot ??
                throw new ArgumentNullException(nameof(handleSnapshot));
            Kind = kind;
            ChangedBuff = changedBuff;
        }

        /// <summary>
        /// 本次变更类型。
        /// </summary>
        public BuffChangeKind Kind { get; }

        /// <summary>
        /// 发生变化的 Buff；即使已移除，也保留移除前的最后状态便于表现层展示。
        /// </summary>
        public BuffSnapshot ChangedBuff { get; }

        /// <summary>
        /// 本次变更提交后的完整句柄快照。
        /// </summary>
        public BuffHandleSnapshot HandleSnapshot { get; }

        /// <summary>
        /// 本次变更提交后的版本号。
        /// </summary>
        public long Revision => HandleSnapshot.Revision;
    }
}
