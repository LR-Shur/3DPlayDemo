namespace Train.Architecture.Assets
{
    /// <summary>
    /// 集中保存运行时系统使用的资源定位地址。
    /// 当前未启用 YooAsset 可寻址模式，因此有意使用完整资源路径。
    /// </summary>
    public static class AssetLocations
    {
        /// <summary>可游玩战斗关卡场景的完整资源路径。</summary>
        public const string CombatArenaScene =
            "Assets/Scenes/Playable/Level_Combat_001.unity";

        /// <summary>可游玩战斗关卡配置的完整资源路径。</summary>
        public const string CombatArenaDefinition =
            "Assets/Data/Levels/Level_Combat_001.asset";

        /// <summary>玩家角色预制体的完整资源路径。</summary>
        public const string PlayerPrefab =
            "Assets/Prefabs/Player/Player_Ellen.prefab";

        /// <summary>默认第三人称 Cinemachine 虚拟相机预制体地址。</summary>
        public const string PlayerCameraPrefab =
            "Assets/Prefabs/CM_PlayerCamera.prefab";

        /// <summary>默认人形骑士敌人预制体的完整资源路径。</summary>
        public const string KnightEnemyPrefab =
            "Assets/Prefabs/Enemies/Enemy_KayKitKnight.prefab";

        /// <summary>默认人形骑士敌人配置的完整资源路径。</summary>
        public const string KnightEnemyConfig =
            "Assets/Data/Enemies/KayKitKnightEnemyConfig.asset";

        /// <summary>默认背包系统配置的完整资源路径。</summary>
        public const string InventorySettings =
            "Assets/Data/Inventory/DefaultInventorySettings.asset";

        /// <summary>默认装备与饰品系统配置的完整资源路径。</summary>
        public const string EquipmentSettings =
            "Assets/Data/Equipment/DefaultEquipmentSettings.asset";

        /// <summary>旧版游戏 HUD 预制体的完整资源路径。</summary>
        public const string GameHudPrefab =
            "Assets/Prefabs/UI/GameHUD.prefab";

        /// <summary>常驻游戏 UI 根预制体的完整资源路径。</summary>
        public const string GameUiRoot =
            "Assets/Prefabs/UI/Core/GameUIRoot.prefab";

        /// <summary>世界物品掉落预制体。</summary>
        public const string WorldItemPickupPrefab =
            "Assets/Prefabs/World/WorldItemPickup.prefab";

        /// <summary>任务系统设置的完整资源路径。</summary>
        public const string QuestSettings =
            "Assets/Data/Quests/DefaultQuestSettings.asset";

        /// <summary>角色名册设置的完整资源路径。</summary>
        public const string CharacterSettings =
            "Assets/Data/Characters/DefaultCharacterSettings.asset";

        /// <summary>对话系统设置的完整资源路径。</summary>
        public const string DialogueSettings =
            "Assets/Data/Dialogue/DefaultDialogueSettings.asset";
    }
}
