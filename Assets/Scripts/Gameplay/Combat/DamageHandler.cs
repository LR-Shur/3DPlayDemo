using Train.Gameplay.Combat.Factions;
using Train.Gameplay.Combat.Buffs;

namespace Train.Gameplay.Combat
{
    /// <summary>
    /// 伤害结算的统一入口。
    /// 后续的护甲、暴击、阵营、难度倍率和 Buff 修正都可以集中加在这里。
    /// </summary>
    public static class DamageHandler
    {
        public static DamageResult Apply(
            IDamageable target,
            DamageInfo damageInfo,
            IFactionMember targetFaction = null)
        {
            var sourceFaction = damageInfo.Source as IFactionMember;
            targetFaction ??= target as IFactionMember;

            if (target == null ||
                !target.IsAlive ||
                damageInfo.Amount <= 0f ||
                !DamagePolicy.CanDamage(sourceFaction, targetFaction))
            {
                return default;
            }

            var finalDamageInfo = ApplyIncomingBuffModifiers(
                target,
                damageInfo);
            return target.TakeDamage(finalDamageInfo);
        }

        /// <summary>
        /// 将目标身上的动态 Buff 依次应用到入伤数据。
        /// 这里仅负责适配 Unity 伤害类型与纯 C# Buff 领域类型。
        /// </summary>
        private static DamageInfo ApplyIncomingBuffModifiers(
            IDamageable target,
            DamageInfo damageInfo)
        {
            if (target is not UnityEngine.Component targetComponent)
            {
                return damageInfo;
            }

            var result = damageInfo;
            var combatStats = targetComponent
                .GetComponentInParent<CombatStatModifierComponent>();
            if (combatStats != null)
            {
                result = result.WithAmount(
                    combatStats.ModifyIncomingDamage(
                        result.Amount,
                        result.DamageType));
            }

            var buffHandle = targetComponent
                .GetComponentInParent<BuffHandleComponent>();
            if (buffHandle == null)
            {
                return result;
            }

            var modified = buffHandle.ModifyIncomingDamage(
                result.Amount,
                ConvertElement(result.DamageType));
            return result.WithAmount(modified);
        }

        /// <summary>
        /// 将战斗层伤害类型转换为 Buff 领域层元素类型。
        /// </summary>
        private static Train.Buffs.Core.DamageElement ConvertElement(
            DamageType damageType)
        {
            return damageType switch
            {
                DamageType.Fire => Train.Buffs.Core.DamageElement.Fire,
                DamageType.Ice => Train.Buffs.Core.DamageElement.Ice,
                DamageType.Electric => Train.Buffs.Core.DamageElement.Electric,
                DamageType.Water => Train.Buffs.Core.DamageElement.Water,
                DamageType.Wind => Train.Buffs.Core.DamageElement.Wind,
                DamageType.Earth => Train.Buffs.Core.DamageElement.Earth,
                DamageType.True => Train.Buffs.Core.DamageElement.True,
                _ => Train.Buffs.Core.DamageElement.Physical
            };
        }
    }
}
