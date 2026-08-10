namespace Train.Equipment.Core
{
    /// <summary>
    /// 定义角色可装备物品的互斥槽位。
    /// 同一槽位装备新物品时会自动替换旧物品。
    /// </summary>
    public enum EquipmentSlot
    {
        Weapon = 0,
        Helmet = 1,
        Armor = 2,
        Gloves = 3,
        Shoes = 4,
        Accessory1 = 5,
        Accessory2 = 6,
        Accessory3 = 7,
        Accessory4 = 8,
        Accessory5 = 9
    }
}
