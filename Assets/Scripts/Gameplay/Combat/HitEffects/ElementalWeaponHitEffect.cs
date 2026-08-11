using Train.Buffs.Core;
using Train.Gameplay.Combat.Buffs;
using UnityEngine;

namespace Train.Gameplay.Combat.HitEffects
{
    /// <summary>
    /// 通用元素武器命中特效。
    /// 通过 Inspector 配置元素伤害对应的 Buff，不需要为每种元素复制一套命中逻辑。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ElementalWeaponHitEffect : MonoBehaviour, IWeaponHitEffect
    {
        [SerializeField] private DamageType _damageType = DamageType.Fire;
        [SerializeField] private string _buffId = ElementalBuffIds.FireVulnerability;
        [SerializeField] private string _sourceId = "weapon.elemental";
        [SerializeField, Min(0.1f)] private float _duration = 8f;
        [SerializeField, Min(0f)] private float _magnitudePerStack = 0.12f;
        [SerializeField, Min(1)] private int _stackAmount = 1;
        [SerializeField, Min(1)] private int _maxStacks = 5;

        /// <inheritdoc />
        public void OnDamageApplied(
            IDamageable target,
            DamageInfo damageInfo,
            DamageResult result)
        {
            if (result.AppliedDamage <= 0f ||
                damageInfo.DamageType != _damageType ||
                target is not Component targetComponent)
            {
                return;
            }

            var handle = targetComponent
                .GetComponentInParent<BuffHandleComponent>();
            if (handle == null || string.IsNullOrWhiteSpace(_buffId))
            {
                return;
            }

            handle.Apply(
                new BuffInfo(
                    _buffId,
                    string.IsNullOrWhiteSpace(_sourceId)
                        ? "weapon.elemental"
                        : _sourceId,
                    Mathf.Max(0.1f, _duration),
                    Mathf.Max(0f, _magnitudePerStack),
                    Mathf.Max(1, _stackAmount),
                    Mathf.Max(1, _maxStacks)));
        }
    }
}
