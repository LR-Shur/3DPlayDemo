namespace Train.Architecture.Assets.Events
{
    /// <summary>
    /// 表示资源系统已经初始化完成并可以开始加载资产。
    /// </summary>
    public readonly struct AssetSystemReadyEvent
    {
        public AssetSystemReadyEvent(string packageName)
        {
            PackageName = packageName;
        }

        public string PackageName { get; }
    }
}
