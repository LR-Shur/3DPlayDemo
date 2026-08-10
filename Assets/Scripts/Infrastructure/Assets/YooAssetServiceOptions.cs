using Train.Architecture.Assets;

namespace Train.Infrastructure.Assets
{
    /// <summary>
    /// YooAsset 服务启动参数，包含包名、加载模式和远端地址等选项。
    /// </summary>
    public sealed class YooAssetServiceOptions
    {
        public string PackageName { get; set; } = "DefaultPackage";
        public AssetPlayMode PlayMode { get; set; } = DefaultPlayMode;
        public string PrimaryHostUrl { get; set; } = "http://127.0.0.1/CDN/PC";
        public string FallbackHostUrl { get; set; } = "http://127.0.0.1/CDN/PC";

        public static AssetPlayMode DefaultPlayMode
        {
            get
            {
#if UNITY_EDITOR
                return AssetPlayMode.EditorSimulate;
#else
                return AssetPlayMode.Offline;
#endif
            }
        }
    }
}
