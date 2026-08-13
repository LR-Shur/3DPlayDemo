using System;

namespace Train.Buffs.Core
{
    /// <summary>电荷层数：每层提高 30% 雷属性伤害，最多五层。</summary>
    public sealed class ElectricChargeBuff : IBuff, IOutgoingDamageModifier
    {
        private const string StableId = "electric_charge";
        private float _remainingDuration;
        private float _magnitude;
        private int _stackCount;
        private int _maxStacks;

        public ElectricChargeBuff(BuffInfo info)
        {
            info.Validate();
            SourceId = info.SourceId;
            _remainingDuration = info.Duration;
            _magnitude = .3f;
            _maxStacks = Math.Min(5, Math.Max(1, info.MaxStacks));
            _stackCount = Math.Min(_maxStacks, Math.Max(1, info.StackAmount));
        }

        public string BuffId => StableId;
        public string SourceId { get; }
        public BuffStackPolicy StackPolicy => BuffStackPolicy.AddStack;
        public bool IsExpired => _remainingDuration <= 0f;
        public int StackCount => _stackCount;

        public void Reapply(BuffInfo info)
        {
            info.Validate();
            _remainingDuration = info.Duration;
            _stackCount = Math.Min(_maxStacks, _stackCount + Math.Max(1, info.StackAmount));
        }

        public void Tick(float deltaTime)
        {
            _remainingDuration = Math.Max(0f, _remainingDuration - deltaTime);
        }

        public DamageContext ModifyIncomingDamage(DamageContext context) => context;

        public DamageContext ModifyOutgoingDamage(DamageContext context)
        {
            if (IsExpired || context.Element != DamageElement.Electric)
            {
                return context;
            }

            return context.WithAmount(context.Amount * (1f + _magnitude * _stackCount));
        }

        public BuffSnapshot CreateSnapshot() => new BuffSnapshot(
            BuffId, SourceId, StackPolicy, _remainingDuration, _magnitude, _stackCount, _maxStacks);
    }

    public sealed class ElectricChargeBuffFactory : IBuffFactory
    {
        public string BuffId => "electric_charge";
        public IBuff Create(BuffInfo info) => new ElectricChargeBuff(info);
    }
}
