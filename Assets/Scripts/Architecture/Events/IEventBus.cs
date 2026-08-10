using System;

namespace Train.Architecture.Events
{
    /// <summary>
    /// 跨模块事件通信的最小接口，支持订阅和发布强类型事件。
    /// </summary>
    public interface IEventBus
    {
        IDisposable Subscribe<TEvent>(Action<TEvent> handler);
        void Publish<TEvent>(TEvent message);
    }
}
