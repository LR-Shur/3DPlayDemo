using System;
using Train.Architecture.Events;
using Train.GameFlow.Application.Events;

namespace Train.GameFlow.Application
{
    /// <summary>
    /// 关卡会话注册表的默认实现，并通过事件中心广播会话挂载变化。
    /// </summary>
    public sealed class LevelSessionRegistry : ILevelSessionRegistry
    {
        private readonly IEventBus _events;

        /// <summary>
        /// 创建关卡会话注册表。
        /// </summary>
        /// <param name="events">用于发布会话变化消息的事件中心。</param>
        public LevelSessionRegistry(IEventBus events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <summary>
        /// 获取当前激活的关卡只读模型。
        /// </summary>
        public ILevelReadModel Current { get; private set; }

        /// <summary>
        /// 挂载一个关卡只读模型，并将其设为当前会话。
        /// </summary>
        /// <param name="readModel">要挂载的关卡只读模型。</param>
        public void Attach(ILevelReadModel readModel)
        {
            if (readModel == null)
            {
                throw new ArgumentNullException(nameof(readModel));
            }

            if (ReferenceEquals(Current, readModel))
            {
                return;
            }

            Current = readModel;
            _events.Publish(new LevelSessionChangedEvent(true));
        }

        /// <summary>
        /// 当指定模型仍为当前会话时将其卸载。
        /// </summary>
        /// <param name="readModel">要卸载的关卡只读模型。</param>
        public void Detach(ILevelReadModel readModel)
        {
            if (!ReferenceEquals(Current, readModel))
            {
                return;
            }

            Current = null;
            _events.Publish(new LevelSessionChangedEvent(false));
        }
    }
}
