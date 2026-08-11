using System;

namespace Train.Buffs.Core
{
    /// <summary>
    /// 通用元素伤害修正 Buff。
    /// 同一个实现可以承载火、水、风、地以及后续元素状态，具体差异由工厂配置。
    /// </summary>
    public sealed class ElementalDamageModifierBuff : IBuff
    {
        private readonly DamageElement _affectedElement;
        private readonly string _buffId;
        private float _remainingDuration;
        private float _magnitude;
        private int _stackCount;
        private int _maxStacks;

        /// <summary>
        /// 创建一个指定元素的伤害修正 Buff。
        /// </summary>
        public ElementalDamageModifierBuff(
            string buffId,
            DamageElement affectedElement,
            BuffInfo info)
        {
            _buffId = ValidateBuffId(buffId);
            _affectedElement = affectedElement;
            ValidateInfo(info);
            SourceId = info.SourceId;
            _remainingDuration = info.Duration;
            _magnitude = info.Magnitude;
            _maxStacks = info.MaxStacks;
            _stackCount = Math.Min(info.StackAmount, info.MaxStacks);
        }

        /// <inheritdoc />
        public string BuffId => _buffId;

        /// <inheritdoc />
        public string SourceId { get; }

        /// <inheritdoc />
        public BuffStackPolicy StackPolicy => BuffStackPolicy.AddStack;

        /// <inheritdoc />
        public bool IsExpired => _remainingDuration <= 0f;

        /// <inheritdoc />
        public int StackCount => _stackCount;

        /// <summary>获取此 Buff 修正的元素。</summary>
        public DamageElement AffectedElement => _affectedElement;

        /// <inheritdoc />
        public void Reapply(BuffInfo info)
        {
            ValidateInfo(info);
            if (!string.Equals(SourceId, info.SourceId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"元素 Buff '{BuffId}' 的来源不能从 '{SourceId}' 改为 '{info.SourceId}'。");
            }

            _remainingDuration = info.Duration;
            _magnitude = info.Magnitude;
            _maxStacks = info.MaxStacks;
            _stackCount = (int)Math.Min(
                (long)_stackCount + info.StackAmount,
                _maxStacks);
        }

        /// <inheritdoc />
        public void Tick(float deltaTime)
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

            _remainingDuration = Math.Max(0f, _remainingDuration - deltaTime);
        }

        /// <inheritdoc />
        public DamageContext ModifyIncomingDamage(DamageContext context)
        {
            if (IsExpired || context.Element != _affectedElement)
            {
                return context;
            }

            var multiplier = 1d + ((double)_magnitude * _stackCount);
            return context.WithAmount((float)(context.Amount * multiplier));
        }

        /// <inheritdoc />
        public BuffSnapshot CreateSnapshot()
        {
            return new BuffSnapshot(
                BuffId,
                SourceId,
                StackPolicy,
                _remainingDuration,
                _magnitude,
                _stackCount,
                _maxStacks);
        }

        private void ValidateInfo(BuffInfo info)
        {
            info.Validate();
            if (!string.Equals(info.BuffId, BuffId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"元素 Buff 工厂期望 '{BuffId}'，实际收到 '{info.BuffId}'。",
                    nameof(info));
            }

            if (info.Magnitude < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(info),
                    info.Magnitude,
                    "元素 Buff 强度不能小于零。");
            }
        }

        private static string ValidateBuffId(string buffId)
        {
            if (string.IsNullOrWhiteSpace(buffId))
            {
                throw new ArgumentException(
                    "元素 Buff 标识不能为空。",
                    nameof(buffId));
            }

            return buffId;
        }
    }
}
