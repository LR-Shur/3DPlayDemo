namespace Train.Gameplay.Combat
{
    /// <summary>
    /// 所有可受伤对象统一实现的接口。
    /// </summary>
    public interface IDamageable
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsAlive { get; }
        DamageResult TakeDamage(DamageInfo damageInfo);
    }
}
