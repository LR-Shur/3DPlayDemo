namespace Train.Inventory.Data
{
    /// <summary>
    /// 表示物品在背包界面和玩法中的用途分类。
    /// </summary>
    public enum ItemCategory
    {
        /// <summary>角色、装备等系统使用的养成材料。</summary>
        Material = 0,

        /// <summary>使用后会被消耗的物品。</summary>
        Consumable = 1,

        /// <summary>用于购买或兑换的货币。</summary>
        Currency = 2,

        /// <summary>与任务流程关联的关键物品。</summary>
        Quest = 3,

        /// <summary>可在装备系统中穿戴并改变角色属性的物品。</summary>
        Equipment = 4
    }
}
