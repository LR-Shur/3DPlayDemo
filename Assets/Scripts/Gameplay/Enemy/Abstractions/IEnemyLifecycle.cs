namespace Train.Gameplay.Enemy.Abstractions
{
    /// <summary>
    /// 敌人生命周期控制接口，用于延迟销毁等表现收尾。
    /// </summary>
    public interface IEnemyLifecycle
    {
        void DespawnAfter(float delaySeconds);
    }
}
