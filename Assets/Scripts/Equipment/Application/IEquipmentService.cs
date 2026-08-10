using System.Collections.Generic;
using Train.Equipment.Core;
using Train.Equipment.Data;

namespace Train.Equipment.Application
{
    /// <summary>
    /// 向表现层和玩法层提供装备查询与换装用例，
    /// 隐藏可变领域模型及 Unity 配置转换细节。
    /// </summary>
    public interface IEquipmentService
    {
        /// <summary>获取当前装备栏和最终属性的不可变快照。</summary>
        EquipmentSnapshot Snapshot { get; }

        /// <summary>获取全部可穿戴装备的只读目录。</summary>
        IReadOnlyList<EquipmentItemDefinition> Catalog { get; }

        /// <summary>按稳定标识查询装备静态配置。</summary>
        bool TryGetDefinition(
            string itemId,
            out EquipmentItemDefinition definition);

        /// <summary>
        /// 尝试把指定装备穿戴到目标槽位。
        /// 未登记物品、不兼容槽位或无实际变化时返回 false。
        /// </summary>
        bool Equip(string itemId, EquipmentSlot targetSlot);

        /// <summary>
        /// 尝试卸下目标槽位中的装备；槽位为空或无效时返回 false。
        /// </summary>
        bool Unequip(EquipmentSlot slot);
    }
}
