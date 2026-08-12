namespace Train.Inventory.Data
{
    /// <summary>主动道具的运行时效果类型。</summary>
    public enum ActiveItemEffectType
    {
        /// <summary>无效果。</summary>
        None = 0,
        /// <summary>恢复生命。</summary>
        Heal = 1,
        /// <summary>范围伤害。</summary>
        Grenade = 2
    }
}
