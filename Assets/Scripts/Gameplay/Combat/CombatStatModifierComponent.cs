using UnityEngine;

namespace Train.Gameplay.Combat
{
    /// <summary>
    /// 保存由装备等长期系统计算出的战斗属性，并提供轻量伤害修正。
    /// 动态易伤仍由 BuffHandle 负责，二者职责互不混合。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatStatModifierComponent : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _defense;

        /// <summary>当前最终防御力。</summary>
        public float Defense => _defense;

        /// <summary>更新角色最终防御力。</summary>
        public void SetDefense(float defense)
        {
            _defense = Mathf.Max(0f, defense);
        }

        /// <summary>
        /// 使用平滑递减公式结算防御：伤害 × 100 / (100 + 防御)。
        /// 真实伤害有意跳过该修正。
        /// </summary>
        public float ModifyIncomingDamage(
            float amount,
            DamageType damageType)
        {
            var safeAmount = Mathf.Max(0f, amount);
            if (damageType == DamageType.True)
            {
                return safeAmount;
            }

            return safeAmount * 100f / (100f + _defense);
        }
    }
}
