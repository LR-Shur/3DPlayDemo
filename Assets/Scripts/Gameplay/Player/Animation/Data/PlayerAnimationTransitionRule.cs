using Animancer;
using UnityEngine;

namespace Train.Gameplay.Player.Animation.Data
{
    /// <summary>
    /// 定义从一个动画进入另一个动画时使用的专属 TransitionAsset。
    /// 用于保存只有特定来源才需要的起始相位、淡入时间和事件，不影响目标动画的普通播放配置。
    /// </summary>
    [System.Serializable]
    public sealed class PlayerAnimationTransitionRule
    {
        [SerializeField] private PlayerAnimationId _from;
        [SerializeField] private PlayerAnimationId _to;
        [SerializeField] private TransitionAssetBase _transition;

        /// <summary>
        /// 获取该规则允许的来源动画。
        /// </summary>
        public PlayerAnimationId From => _from;

        /// <summary>
        /// 获取该规则允许的目标动画。
        /// </summary>
        public PlayerAnimationId To => _to;

        /// <summary>
        /// 获取该来源到目标组合使用的专属 TransitionAsset。
        /// </summary>
        public TransitionAssetBase Transition => _transition;

        /// <summary>
        /// 初始化一条来源相关的动画过渡规则。
        /// </summary>
        /// <param name="from">过渡开始前正在播放的动画标识。</param>
        /// <param name="to">需要进入的目标动画标识。</param>
        /// <param name="transition">负责播放配置的专属 TransitionAsset。</param>
        public PlayerAnimationTransitionRule(
            PlayerAnimationId from,
            PlayerAnimationId to,
            TransitionAssetBase transition)
        {
            _from = from;
            _to = to;
            _transition = transition;
        }
    }
}
