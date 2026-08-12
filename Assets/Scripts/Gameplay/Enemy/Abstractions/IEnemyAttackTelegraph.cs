namespace Train.Gameplay.Enemy.Abstractions
{
    /// <summary>敌人攻击前摇的地面预警能力。</summary>
    public interface IEnemyAttackTelegraph
    {
        void BeginTelegraph(float duration, float attackRange);
        void EndTelegraph();
    }
}
