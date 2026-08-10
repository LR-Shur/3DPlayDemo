namespace Train.Architecture.Assets
{
    /// <summary>
    /// 资源包可用的运行模式，决定 YooAsset 使用模拟、本地离线还是远端地址。
    /// </summary>
    public enum AssetPlayMode
    {
        EditorSimulate,
        Offline,
        Host,
        Web
    }
}
