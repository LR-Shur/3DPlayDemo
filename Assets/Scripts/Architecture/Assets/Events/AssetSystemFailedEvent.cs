namespace Train.Architecture.Assets.Events
{
    /// <summary>
    /// 表示资源系统初始化或加载失败。
    /// </summary>
    public readonly struct AssetSystemFailedEvent
    {
        public AssetSystemFailedEvent(string packageName, string error)
        {
            PackageName = packageName;
            Error = error;
        }

        public string PackageName { get; }
        public string Error { get; }
    }
}
