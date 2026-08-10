using System;

namespace Train.Equipment.Core
{
    /// <summary>
    /// 描述一条不可变的属性词条，包括目标属性、运算方式与数值。
    /// </summary>
    public readonly struct StatModifier : IEquatable<StatModifier>
    {
        public StatModifier(
            StatType statType,
            StatModifierOperation operation,
            float value)
        {
            ValidateEnum(statType, nameof(statType));
            ValidateEnum(operation, nameof(operation));
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "属性词条数值必须是有限数字。");
            }

            StatType = statType;
            Operation = operation;
            Value = value;
        }

        public StatType StatType { get; }

        public StatModifierOperation Operation { get; }

        public float Value { get; }

        public bool Equals(StatModifier other)
        {
            return StatType == other.StatType &&
                   Operation == other.Operation &&
                   Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is StatModifier other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)StatType;
                hashCode = (hashCode * 397) ^ (int)Operation;
                hashCode = (hashCode * 397) ^ Value.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(
            StatModifier left,
            StatModifier right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            StatModifier left,
            StatModifier right)
        {
            return !left.Equals(right);
        }

        private static void ValidateEnum<TEnum>(
            TEnum value,
            string parameterName)
            where TEnum : struct
        {
            if (!Enum.IsDefined(typeof(TEnum), value))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "枚举值不在系统定义范围内。");
            }
        }
    }
}
