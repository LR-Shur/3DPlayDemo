namespace Train.Gameplay.Combat.Factions
{
    /// <summary>
    /// 战斗单位所属的阵营。
    /// Neutral 表示不与任何阵营结盟，因此可以与所有阵营互相造成伤害。
    /// </summary>
    public enum CombatFaction
    {
        Neutral = 0,
        Player = 1,
        Enemy = 2
    }
}
