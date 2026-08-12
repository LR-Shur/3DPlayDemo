namespace Train.Architecture.Assets
{
    /// <summary>
    /// 运行时使用的 YooAsset 地址。
    /// 编辑器脚本仍使用 AssetLocations 中的工程路径，避免把 AssetDatabase 路径和运行时地址混用。
    /// </summary>
    public static class AssetAddresses
    {
        /// <summary>第一关关卡 SO 地址。</summary>
        public const string CombatArenaDefinition = "Levels_Level_Combat_001";

        /// <summary>第一关场景地址。</summary>
        public const string CombatArenaScene = "Playable_Level_Combat_001";

        /// <summary>玩家预制体地址。</summary>
        public const string PlayerPrefab = "Player_Player_Ellen";

        /// <summary>玩家跟随相机预制体地址。</summary>
        public const string PlayerCameraPrefab = "Prefabs_CM_PlayerCamera";

        /// <summary>默认掉落预制体地址。</summary>
        public const string WorldItemPickupPrefab = "World_WorldItemPickup";

        /// <summary>装备配置 SO 地址。</summary>
        public const string EquipmentSettings = "Equipment_DefaultEquipmentSettings";

        /// <summary>背包配置 SO 地址。</summary>
        public const string InventorySettings = "Inventory_DefaultInventorySettings";

        /// <summary>任务配置 SO 地址。</summary>
        public const string QuestSettings = "Quests_DefaultQuestSettings";

        /// <summary>角色配置 SO 地址。</summary>
        public const string CharacterSettings = "Characters_DefaultCharacterSettings";

        /// <summary>对话配置 SO 地址。</summary>
        public const string DialogueSettings = "Dialogue_DefaultDialogueSettings";

        /// <summary>常驻 UI 根预制体地址。</summary>
        public const string GameUiRoot = "Core_GameUIRoot";
    }
}
