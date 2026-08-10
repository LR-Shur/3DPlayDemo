using System;
using Train.Architecture.Assets;
using Train.Architecture.Assets.Events;
using Train.Architecture.Bootstrap;
using UnityEngine;

namespace Train.Infrastructure.Assets
{
    /// <summary>
    /// 在场景加载前自动创建并初始化 YooAsset 资源服务的启动安装器。
    /// </summary>
    public static class YooAssetRuntimeInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static async void Install()
        {
#if UNITY_EDITOR
            // Editor automation and PlayMode tests frequently move focus away
            // from the Game view. Keep YooAsset's operation scheduler ticking.
            Application.runInBackground = true;
#endif
            var game = GameBootstrap.EnsureExists();
            if (game.Context.Assets == null)
            {
                var settings = Resources.Load<YooAssetRuntimeSettings>(
                    YooAssetRuntimeSettings.ResourcesLocation);
                var service = new YooAssetService(
                    settings != null
                        ? settings.CreateOptions()
                        : new YooAssetServiceOptions());
                game.Context.InstallAssetService(service);
            }

            try
            {
                await game.Context.Assets.InitializeAsync();
                game.Context.Events.Publish(
                    new AssetSystemReadyEvent(game.Context.Assets.PackageName));
            }
            catch (AssetServiceException exception)
                when (IsExpectedAbort(exception))
            {
                // 编辑器停止或应用退出时 YooAsset 会先关闭调度器，
                // 未完成的初始化会以 abort 结束，这是正常取消而不是失败。
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                game.Context.Events.Publish(
                    new AssetSystemFailedEvent(
                        game.Context.Assets.PackageName,
                        exception.Message));
            }
        }

        /// <summary>
        /// 判断资源服务异常是否由退出时调度器关闭造成。
        /// </summary>
        private static bool IsExpectedAbort(
            AssetServiceException exception)
        {
            return GameBootstrap.IsApplicationQuitting ||
                   (exception?.Message != null &&
                    exception.Message.IndexOf(
                        "aborted",
                        StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
