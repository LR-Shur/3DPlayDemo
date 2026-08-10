using UnityEngine;

namespace Train.Gameplay.Combat.Factions
{
    /// <summary>
    /// 可挂在角色根节点上的通用阵营组件。
    /// 没有专用战斗组件的可受击物体，也可以通过它显式声明阵营。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FactionMember : MonoBehaviour, IFactionMember
    {
        [SerializeField] private CombatFaction _faction = CombatFaction.Neutral;

        public CombatFaction Faction => _faction;

        public void Configure(CombatFaction faction)
        {
            _faction = faction;
        }
    }
}
