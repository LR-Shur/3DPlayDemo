using Train.Buffs.Core;
using Train.Gameplay.Combat.Buffs;
using UnityEngine;

namespace Train.Gameplay.Combat.HitEffects
{
    /// <summary>
    /// 雷属性武器的命中附加效果。
    /// 每次有效命中都会给目标施加一层雷易伤，并刷新持续时间。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LightningWeaponHitEffect :
        MonoBehaviour,
        IWeaponHitEffect
    {
        private const string LightningVulnerabilityId =
            "lightning_vulnerability";

        [SerializeField] private string _sourceId = "weapon.thunder_edge";
        [SerializeField, Min(0.1f)] private float _duration = 8f;
        [SerializeField, Range(0f, 1f)] private float _magnitudePerStack = 0.12f;
        [SerializeField, Min(1)] private int _stackAmount = 1;
        [SerializeField, Min(1)] private int _maxStacks = 5;

        /// <summary>
        /// 目标受到有效雷剑伤害后，为其实体 BuffHandle 施加雷易伤。
        /// </summary>
        public void OnDamageApplied(
            IDamageable target,
            DamageInfo damageInfo,
            DamageResult result)
        {
            if (result.AppliedDamage <= 0f ||
                damageInfo.DamageType != DamageType.Electric ||
                target is not Component targetComponent)
            {
                return;
            }

            var handle = targetComponent
                .GetComponentInParent<BuffHandleComponent>();
            if (handle == null)
            {
                return;
            }

            handle.Apply(
                new BuffInfo(
                    LightningVulnerabilityId,
                    string.IsNullOrWhiteSpace(_sourceId)
                        ? "weapon.thunder_edge"
                        : _sourceId,
                    Mathf.Max(0.1f, _duration),
                    Mathf.Max(0f, _magnitudePerStack),
                    Mathf.Max(1, _stackAmount),
                    Mathf.Max(1, _maxStacks)));
        }
    }
}
