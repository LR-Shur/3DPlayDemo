namespace Train.Buffs.Core
{
    /// <summary>
    /// 具体 Buff 的领域行为接口。
    /// 句柄只依赖此接口，不需要通过类型判断识别雷易伤等具体实现。
    /// </summary>
    public interface IBuff
    {
        /// <summary>
        /// Buff 稳定标识。
        /// </summary>
        string BuffId { get; }

        /// <summary>
        /// Buff 来源稳定标识。
        /// </summary>
        string SourceId { get; }

        /// <summary>
        /// 重复施加策略。
        /// </summary>
        BuffStackPolicy StackPolicy { get; }

        /// <summary>
        /// Buff 是否已经到期。
        /// </summary>
        bool IsExpired { get; }

        /// <summary>
        /// 当前层数。
        /// </summary>
        int StackCount { get; }

        /// <summary>
        /// 再次施加同 Buff、同来源时合并新的参数。
        /// </summary>
        void Reapply(BuffInfo info);

        /// <summary>
        /// 推进 Buff 内部时间。
        /// </summary>
        void Tick(float deltaTime);

        /// <summary>
        /// 返回此 Buff 修正后的入伤数据。
        /// </summary>
        DamageContext ModifyIncomingDamage(DamageContext context);

        /// <summary>
        /// 创建当前 Buff 的只读快照。
        /// </summary>
        BuffSnapshot CreateSnapshot();
    }
}
