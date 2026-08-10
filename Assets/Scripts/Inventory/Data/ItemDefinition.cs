using UnityEngine;

namespace Train.Inventory.Data
{
    /// <summary>
    /// 描述一种物品的稳定标识、展示信息、分类、稀有度和堆叠规则。
    /// </summary>
    [CreateAssetMenu(
        menuName = "Train/Inventory/Item Definition",
        fileName = "ItemDefinition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string _itemId;
        [SerializeField] private string _displayName;
        [TextArea(2, 5)]
        [SerializeField] private string _description;
        [SerializeField] private ItemCategory _category;
        [SerializeField] private ItemRarity _rarity = ItemRarity.Common;
        [SerializeField, Min(1)] private int _maxStack = 99;
        [Tooltip("YooAsset location. Empty means the UI uses a generated fallback icon.")]
        [SerializeField] private string _iconLocation;

        /// <summary>获取物品的稳定标识。</summary>
        public string ItemId => _itemId;

        /// <summary>获取物品的展示名称。</summary>
        public string DisplayName => _displayName;

        /// <summary>获取物品的展示说明。</summary>
        public string Description => _description;

        /// <summary>获取物品用途分类。</summary>
        public ItemCategory Category => _category;

        /// <summary>获取物品稀有度。</summary>
        public ItemRarity Rarity => _rarity;

        /// <summary>获取经过安全限制后的单槽堆叠上限。</summary>
        public int MaxStack => Mathf.Max(1, _maxStack);

        /// <summary>获取 YooAsset 图标定位地址。</summary>
        public string IconLocation => _iconLocation;
    }
}
