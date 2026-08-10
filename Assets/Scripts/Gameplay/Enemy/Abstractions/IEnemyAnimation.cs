namespace Train.Gameplay.Enemy.Abstractions
{
    /// <summary>
    /// 敌人动画表现的最小接口，供状态机播放不同动作。
    /// </summary>
    public interface IEnemyAnimation
    {
        void Play(EnemyAnimationId animationId, float fadeSeconds = 0.12f);
    }
}
