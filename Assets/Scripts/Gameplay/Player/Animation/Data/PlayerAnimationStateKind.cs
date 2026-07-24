namespace Train.Gameplay.Player.Animation.Data
{
    /// <summary>
    /// 标识动画应由哪一类玩家状态处理。
    /// 目录生成器依据动画命名自动填写，玩法代码据此选择通用状态而无需为每个剪辑复制脚本。
    /// </summary>
    public enum PlayerAnimationStateKind
    {
        LocomotionIdle,
        LocomotionStart,
        LocomotionLoop,
        LocomotionStop,
        LocomotionTurn,
        Dodge,
        PrimaryAttack,
        CombatAction,
        Reaction,
        Death,
        Transition,
        Interaction,
        Cinematic,
    }
}
