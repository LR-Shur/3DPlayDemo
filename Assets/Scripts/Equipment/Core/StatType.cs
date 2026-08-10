namespace Train.Equipment.Core
{
    /// <summary>
    /// 定义装备系统能够重算的角色属性。
    /// 比率类属性统一使用小数表示，例如 0.2 表示 20%。
    /// </summary>
    public enum StatType
    {
        MaxHealth = 0,
        Attack = 1,
        Defense = 2,
        CritRate = 3,
        CritDamage = 4,
        ElectricDamageBonus = 5
    }
}
