namespace Train.Gameplay.Player.Animation.Data
{
    /// <summary>
    /// 定义动画播放期间水平位移的唯一来源。
    /// 状态机只负责读取该策略并交给移动组件执行，不再自行写死动画位移规则。
    /// </summary>
    public enum PlayerAnimationMovementPolicy
    {
        KeepInPlace,
        ScriptedMovement,
        RootMotion,
    }
}
