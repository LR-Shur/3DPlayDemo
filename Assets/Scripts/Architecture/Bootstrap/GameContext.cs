using System;
using Train.Architecture.Assets;
using Train.Architecture.Events;
using Train.Architecture.Services;

namespace Train.Architecture.Bootstrap
{
    /// <summary>
    /// 游戏进程级上下文，持有全局事件中心、服务注册表和资源服务。
    /// </summary>
    public sealed class GameContext : IDisposable
    {
        private readonly EventBus _events;
        private readonly ServiceRegistry _services;
        private IAssetService _assets;

        public GameContext()
        {
            _events = new EventBus();
            _services = new ServiceRegistry();
            Events = _events;
            Services = _services;
        }

        public IEventBus Events { get; }
        public IServiceRegistry Services { get; }
        public IAssetService Assets => _assets;

        public void InstallAssetService(IAssetService assets)
        {
            if (assets == null)
            {
                throw new ArgumentNullException(nameof(assets));
            }

            if (_assets != null && !ReferenceEquals(_assets, assets))
            {
                throw new InvalidOperationException(
                    "An asset service is already installed in this GameContext.");
            }

            _assets = assets;
        }

        public void Dispose()
        {
            // Application services may own asset leases, so release them
            // before shutting down the lower-level asset server.
            _services.Dispose();
            _assets?.Dispose();
            _assets = null;
            _events.Dispose();
        }
    }
}
