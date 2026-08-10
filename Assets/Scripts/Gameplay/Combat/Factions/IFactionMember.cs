namespace Train.Gameplay.Combat.Factions
{
    /// <summary>
    /// 为战斗对象提供阵营信息，不要求对象一定是 MonoBehaviour。
    /// </summary>
    public interface IFactionMember
    {
        CombatFaction Faction { get; }
    }
}
