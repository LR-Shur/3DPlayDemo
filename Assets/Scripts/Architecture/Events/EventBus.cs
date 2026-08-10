using System;
using System.Collections.Generic;

namespace Train.Architecture.Events
{
    /// <summary>
    /// 轻量同步事件中心，支持事件向上层总线冒泡传播。
    /// </summary>
    public sealed class EventBus : IEventBus, IDisposable
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();
        private readonly IEventBus _parent;
        private bool _disposed;

        public EventBus(IEventBus parent = null)
        {
            _parent = parent;
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            ThrowIfDisposed();

            var eventType = typeof(TEvent);
            if (!_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Delegate>();
                _handlers.Add(eventType, handlers);
            }

            handlers.Add(handler);
            return new EventSubscription(() => Unsubscribe(handler));
        }

        public void Publish<TEvent>(TEvent message)
        {
            ThrowIfDisposed();

            if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
            {
                // A subscriber may unsubscribe while handling the event.
                foreach (var handler in handlers.ToArray())
                {
                    ((Action<TEvent>)handler).Invoke(message);
                }
            }

            _parent?.Publish(message);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _handlers.Clear();
            _disposed = true;
        }

        private void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            if (_disposed || !_handlers.TryGetValue(typeof(TEvent), out var handlers))
            {
                return;
            }

            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                _handlers.Remove(typeof(TEvent));
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(EventBus));
            }
        }

        /// <summary>
        /// 保存一次订阅的反向移除操作，重复释放不会产生副作用。
        /// </summary>
        private sealed class EventSubscription : IDisposable
        {
            private Action _unsubscribe;

            public EventSubscription(Action unsubscribe)
            {
                _unsubscribe = unsubscribe;
            }

            public void Dispose()
            {
                _unsubscribe?.Invoke();
                _unsubscribe = null;
            }
        }
    }
}
