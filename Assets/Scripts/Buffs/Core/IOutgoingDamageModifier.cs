namespace Train.Buffs.Core
{
    /// <summary>可修改持有者造成的元素伤害的 Buff。</summary>
    public interface IOutgoingDamageModifier
    {
        DamageContext ModifyOutgoingDamage(DamageContext context);
    }
}
