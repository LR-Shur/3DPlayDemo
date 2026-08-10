namespace Train.Gameplay.Combat.Factions
{
    /// <summary>
    /// 统一决定两个阵营之间是否允许造成伤害。
    /// 缺少阵营信息时保持旧行为（允许伤害），避免旧场景静默失效。
    /// </summary>
    public static class DamagePolicy
    {
        public static bool CanDamage(IFactionMember source, IFactionMember target)
        {
            return source == null ||
                   target == null ||
                   CanDamage(source.Faction, target.Faction);
        }

        public static bool CanDamage(CombatFaction source, CombatFaction target)
        {
            if (source == CombatFaction.Neutral || target == CombatFaction.Neutral)
            {
                return true;
            }

            return source != target;
        }
    }
}
