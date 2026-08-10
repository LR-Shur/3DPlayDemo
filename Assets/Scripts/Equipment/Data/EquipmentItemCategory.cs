namespace Train.Equipment.Data
{
    /// <summary>
    /// 定义装备配置的类别。
    /// 普通装备具有唯一固定槽位，Accessory 类别可使用任意饰品槽。
    /// </summary>
    public enum EquipmentItemCategory
    {
        /// <summary>只能装备到武器槽。</summary>
        Weapon = 0,

        /// <summary>只能装备到头盔槽。</summary>
        Helmet = 1,

        /// <summary>只能装备到盔甲槽。</summary>
        Armor = 2,

        /// <summary>只能装备到手套槽。</summary>
        Gloves = 3,

        /// <summary>只能装备到鞋子槽。</summary>
        Shoes = 4,

        /// <summary>可以装备到任意一个饰品槽。</summary>
        Accessory = 5
    }
}
