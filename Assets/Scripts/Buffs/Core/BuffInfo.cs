using System;

namespace Train.Buffs.Core
{
    /// <summary>
    /// 外部系统向 Buff 句柄提交的不可变施加参数。
    /// 调用者只传数据，不需要知道具体 Buff 类。
    /// </summary>
    public readonly struct BuffInfo
    {
        /// <summary>
        /// 创建一次 Buff 施加信息。
        /// </summary>
        public BuffInfo(
            string buffId,
            string sourceId,
            float duration,
            float magnitude,
            int stackAmount,
            int maxStacks)
        {
            BuffId = buffId;
            SourceId = sourceId;
            Duration = duration;
            Magnitude = magnitude;
            StackAmount = stackAmount;
            MaxStacks = maxStacks;

            Validate();
        }

        /// <summary>
        /// Buff 的稳定标识，用于工厂注册和存档。
        /// </summary>
        public string BuffId { get; }

        /// <summary>
        /// Buff 来源的稳定标识，例如角色实例 ID 或武器实例 ID。
        /// </summary>
        public string SourceId { get; }

        /// <summary>
        /// 本次施加后的持续时间，单位为秒。
        /// </summary>
        public float Duration { get; }

        /// <summary>
        /// 每层效果强度。具体含义由具体 Buff 决定。
        /// </summary>
        public float Magnitude { get; }

        /// <summary>
        /// 本次施加增加的层数。
        /// </summary>
        public int StackAmount { get; }

        /// <summary>
        /// 允许达到的最大层数。
        /// </summary>
        public int MaxStacks { get; }

        /// <summary>
        /// 验证信息是否可用于创建或刷新 Buff。
        /// 公开此方法是为了拦截 default(BuffInfo) 绕过构造函数的情况。
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(BuffId))
            {
                throw new ArgumentException(
                    "Buff 标识不能为空。",
                    nameof(BuffId));
            }

            if (string.IsNullOrWhiteSpace(SourceId))
            {
                throw new ArgumentException(
                    "Buff 来源标识不能为空。",
                    nameof(SourceId));
            }

            if (float.IsNaN(Duration) ||
                float.IsInfinity(Duration) ||
                Duration <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Duration),
                    Duration,
                    "Buff 持续时间必须是大于零的有限数。");
            }

            if (float.IsNaN(Magnitude) ||
                float.IsInfinity(Magnitude))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(Magnitude),
                    Magnitude,
                    "Buff 强度必须是有限数。");
            }

            if (StackAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(StackAmount),
                    StackAmount,
                    "Buff 施加层数必须大于零。");
            }

            if (MaxStacks <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(MaxStacks),
                    MaxStacks,
                    "Buff 最大层数必须大于零。");
            }
        }
    }
}
