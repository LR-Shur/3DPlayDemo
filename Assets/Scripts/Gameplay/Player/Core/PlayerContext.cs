using UnityEngine;

namespace Train.Gameplay.Player.Core
{
    /// <summary>
    /// 汇集所有玩家状态共用的服务与配置。
    /// 使状态类不需要自行查找 Unity 组件。
    /// </summary>
    public sealed class PlayerContext
    {
        /// <summary>
        /// 使用玩家状态所需组件初始化上下文。
        /// </summary>
        public PlayerContext(
            Input.PlayerInputReader input,
            Movement.PlayerMotor motor,
            Animation.PlayerAnimation animation,
            Data.PlayerConfig config)
        {
            Input = input;
            Motor = motor;
            Animation = animation;
            Config = config;
        }

        /// <summary>
        /// 获取读取玩家输入动作的组件。
        /// </summary>
        public Input.PlayerInputReader Input { get; }

        /// <summary>
        /// 获取执行碰撞感知移动的组件。
        /// </summary>
        public Movement.PlayerMotor Motor { get; }

        /// <summary>
        /// 获取通过 Animancer 表现游戏状态的组件。
        /// </summary>
        public Animation.PlayerAnimation Animation { get; }

        /// <summary>
        /// 获取玩家移动和状态规则所使用的可调参数。
        /// </summary>
        public Data.PlayerConfig Config { get; }
    }
}
