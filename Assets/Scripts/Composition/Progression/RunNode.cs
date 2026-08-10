namespace Train.Composition.Progression
{
    /// <summary>
    /// 一条战斗、商店和 Boss 串联起来的关卡节点。
    /// </summary>
    public readonly struct RunNode
    {
        public RunNode(
            string levelId,
            string displayName,
            string scenePath,
            bool isBoss,
            int clearBonus)
        {
            LevelId = levelId;
            DisplayName = displayName;
            ScenePath = scenePath;
            IsBoss = isBoss;
            ClearBonus = clearBonus;
        }

        public string LevelId { get; }
        public string DisplayName { get; }
        public string ScenePath { get; }
        public bool IsBoss { get; }
        public int ClearBonus { get; }
    }
}
