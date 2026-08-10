namespace Train.Presentation.UI.ViewModels
{
    /// <summary>
    /// 装备页面中一行基础值与最终值的属性展示数据。
    /// </summary>
    public sealed class EquipmentStatViewModel
    {
        /// <summary>创建属性行。</summary>
        public EquipmentStatViewModel(
            string name,
            string baseValue,
            string finalValue,
            bool isIncreased)
        {
            Name = name ?? string.Empty;
            BaseValue = baseValue ?? string.Empty;
            FinalValue = finalValue ?? string.Empty;
            IsIncreased = isIncreased;
        }

        /// <summary>属性名称。</summary>
        public string Name { get; }

        /// <summary>角色基础值。</summary>
        public string BaseValue { get; }

        /// <summary>装备结算后的最终值。</summary>
        public string FinalValue { get; }

        /// <summary>最终值是否高于基础值。</summary>
        public bool IsIncreased { get; }
    }
}
