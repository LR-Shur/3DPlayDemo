using System;

namespace Train.Gameplay.Player.Animation.Data
{
    /// <summary>
    /// 保存一次动画播放需要监听的命名事件及其运行时回调。
    /// TransitionAsset 负责事件时间，状态只负责声明事件发生后要执行的玩法逻辑。
    /// </summary>
    public readonly struct PlayerAnimationEventBinding
    {
        /// <summary>
        /// 获取 TransitionAsset 中配置的事件名称。
        /// </summary>
        public string EventName { get; }

        /// <summary>
        /// 获取动画经过命名事件时执行的回调。
        /// </summary>
        public Action Callback { get; }

        /// <summary>
        /// 初始化一个动画命名事件绑定。
        /// </summary>
        /// <param name="eventName">TransitionAsset 中的稳定事件名称。</param>
        /// <param name="callback">动画经过该事件时执行的玩法回调。</param>
        public PlayerAnimationEventBinding(string eventName, Action callback)
        {
            EventName = eventName;
            Callback = callback;
        }
    }
}
