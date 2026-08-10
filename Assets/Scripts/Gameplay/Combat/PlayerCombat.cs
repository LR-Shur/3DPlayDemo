using Train.Gameplay.Combat.Factions;
using UnityEngine;

namespace Train.Gameplay.Combat
{
    /// <summary>
    /// 玩家状态机与武器 Hitbox 之间的轻量桥接组件。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerCombat : MonoBehaviour, IFactionMember
    {
        [SerializeField] private SwordHitbox _swordHitbox;
        [SerializeField] private CombatFaction _faction = CombatFaction.Player;

        public SwordHitbox SwordHitbox => _swordHitbox;
        public CombatFaction Faction => _faction;

        private void Awake()
        {
            _swordHitbox ??= GetComponentInChildren<SwordHitbox>(true);
            _swordHitbox?.EndAttack();
        }

        private void OnDisable()
        {
            _swordHitbox?.EndAttack();
        }

        public void BeginAttack()
        {
            _swordHitbox?.BeginAttack();
        }

        public void EndAttack()
        {
            _swordHitbox?.EndAttack();
        }

        public void ConfigureFaction(CombatFaction faction)
        {
            _faction = faction;
            _swordHitbox?.ConfigureFaction(faction);
        }
    }
}
