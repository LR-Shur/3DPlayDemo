using System;

namespace Train.Gameplay.Player.Animation.Data
{
    /// <summary>
    /// 标识动画在取消窗口内允许切换到的玩法行为。
    /// 使用位标记可以让同一动画同时允许移动、翻滚或攻击接管。
    /// </summary>
    [Flags]
    public enum PlayerAnimationCancelTarget
    {
        None = 0,
        Movement = 1 << 0,
        Dodge = 1 << 1,
        Attack = 1 << 2,
        Skill = 1 << 3,
    }
}
