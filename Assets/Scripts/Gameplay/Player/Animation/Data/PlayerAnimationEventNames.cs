namespace Train.Gameplay.Player.Animation.Data
{
    /// <summary>
    /// 集中保存 TransitionAsset 中由玩法状态监听的命名事件。
    /// 动画资源与状态代码共用这些名称，避免手写字符串产生拼写不一致。
    /// </summary>
    public static class PlayerAnimationEventNames
    {
        /// <summary>
        /// 表示转身动画已经完成落脚，可以开始向新方向产生真实位移。
        /// </summary>
        public const string TurnMovementStart = nameof(TurnMovementStart);

        /// <summary>
        /// 表示转身动画与移动循环的姿势已经可以安全交接。
        /// </summary>
        public const string TurnComplete = nameof(TurnComplete);
    }
}
