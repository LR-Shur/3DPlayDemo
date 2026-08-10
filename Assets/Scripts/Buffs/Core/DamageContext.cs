using System;

namespace Train.Buffs.Core
{
    /// <summary>
    /// Buff 领域层处理的一次伤害数据。
    /// 它是不可变值对象，便于多个 Buff 依次返回修正后的新结果。
    /// </summary>
    public readonly struct DamageContext
    {
        /// <summary>
        /// 创建一份伤害数据。
        /// </summary>
        /// <param name="amount">非负且有限的伤害数值。</param>
        /// <param name="element">伤害元素。</param>
        public DamageContext(float amount, DamageElement element)
        {
            if (float.IsNaN(amount) ||
                float.IsInfinity(amount) ||
                amount < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    amount,
                    "伤害数值必须是非负有限数。");
            }

            Amount = amount;
            Element = element;
        }

        /// <summary>
        /// 当前伤害数值。
        /// </summary>
        public float Amount { get; }

        /// <summary>
        /// 当前伤害元素。
        /// </summary>
        public DamageElement Element { get; }

        /// <summary>
        /// 保留元素并返回一份替换数值后的新伤害数据。
        /// </summary>
        public DamageContext WithAmount(float amount)
        {
            return new DamageContext(amount, Element);
        }
    }
}
