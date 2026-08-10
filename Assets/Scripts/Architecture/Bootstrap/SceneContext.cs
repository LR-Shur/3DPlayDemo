using System;
using Train.Architecture.Events;

namespace Train.Architecture.Bootstrap
{
    /// <summary>
    /// 保存当前场景生命周期的服务与事件总线，场景卸载时一起释放。
    /// </summary>
    public sealed class SceneContext : IDisposable
    {
        private readonly EventBus _events;

        public SceneContext(GameContext game)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            _events = new EventBus(game.Events);
            Events = _events;
        }

        public IEventBus Events { get; }

        public void Dispose()
        {
            _events.Dispose();
        }
    }
}
