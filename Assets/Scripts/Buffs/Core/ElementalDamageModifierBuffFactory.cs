namespace Train.Buffs.Core
{
    /// <summary>
    /// 创建指定元素伤害修正 Buff 的通用工厂。
    /// </summary>
    public sealed class ElementalDamageModifierBuffFactory : IBuffFactory
    {
        private readonly DamageElement _affectedElement;

        /// <summary>
        /// 创建元素 Buff 工厂。
        /// </summary>
        public ElementalDamageModifierBuffFactory(
            string buffId,
            DamageElement affectedElement)
        {
            BuffId = buffId;
            _affectedElement = affectedElement;
        }

        /// <inheritdoc />
        public string BuffId { get; }

        /// <inheritdoc />
        public IBuff Create(BuffInfo info)
        {
            return new ElementalDamageModifierBuff(
                BuffId,
                _affectedElement,
                info);
        }
    }
}
