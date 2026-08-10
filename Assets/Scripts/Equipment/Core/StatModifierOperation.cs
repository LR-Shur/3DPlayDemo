namespace Train.Equipment.Core
{
    /// <summary>
    /// 定义属性词条参与数值计算的方式。
    /// 最终公式为：(基础值 + 固定值) × (1 + 加算百分比总和) × 每个乘算百分比。
    /// </summary>
    public enum StatModifierOperation
    {
        Flat = 0,
        AdditivePercent = 1,
        MultiplicativePercent = 2
    }
}
