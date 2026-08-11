using Train.Architecture.Assets;
using UnityEngine;

namespace Train.Infrastructure.Assets
{
    /// <summary>
    /// 运行时使用的 YooAsset 配置资产，可切换模拟、离线或远端加载模式。
    /// </summary>
    [CreateAssetMenu(
        fileName = "YooAssetRuntimeSettings",
        menuName = "Train/Architecture/YooAsset Runtime Settings")]
    public sealed class YooAssetRuntimeSettings : ScriptableObject
    {
        public const string ResourcesLocation = "Settings/YooAssetRuntimeSettings";

        [SerializeField] private string _packageName = "DefaultPackage";
        [SerializeField] private AssetPlayMode _editorPlayMode = AssetPlayMode.EditorSimulate;
#if !UNITY_EDITOR
        [SerializeField] private AssetPlayMode _playerPlayMode = AssetPlayMode.Offline;
#endif
        [SerializeField] private string _primaryHostUrl = "http://127.0.0.1/CDN/PC";
        [SerializeField] private string _fallbackHostUrl = "http://127.0.0.1/CDN/PC";

        public YooAssetServiceOptions CreateOptions()
        {
            return new YooAssetServiceOptions
            {
                PackageName = string.IsNullOrWhiteSpace(_packageName)
                    ? "DefaultPackage"
                    : _packageName,
#if UNITY_EDITOR
                PlayMode = _editorPlayMode,
#else
                PlayMode = _playerPlayMode,
#endif
                PrimaryHostUrl = _primaryHostUrl,
                FallbackHostUrl = _fallbackHostUrl
            };
        }
    }
}
