using System;
using Train.Architecture.Bootstrap;
using Train.Equipment.Application;
using Train.Equipment.Core;
using Train.Equipment.Events;
using Train.Gameplay.Combat;
using UnityEngine;

namespace Train.Gameplay.Player.Application
{
    /// <summary>
    /// 把装备 Server 的最终属性快照应用到玩家生命值、防御和雷剑伤害。
    /// 它只做应用层适配，不参与装备公式计算。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(CombatStatModifierComponent))]
    public sealed class PlayerEquipmentStatBinder : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private CombatStatModifierComponent _combatStats;
        [SerializeField] private SwordHitbox _swordHitbox;

        private IEquipmentService _equipment;
        private IDisposable _equipmentChangedSubscription;
        private bool _initialized;

        /// <summary>查找玩家本地组件引用。</summary>
        private void Awake()
        {
            _health ??= GetComponent<Health>();
            _combatStats ??= GetComponent<CombatStatModifierComponent>();
            _swordHitbox ??= GetComponentInChildren<SwordHitbox>(true);
        }

        /// <summary>
        /// 等待启动类安装装备服务，支持单独打开任意测试场景。
        /// </summary>
        private void Update()
        {
            if (_equipment == null)
            {
                TryBindEquipmentService();
            }
        }

        /// <summary>组件销毁时解除事件中心订阅。</summary>
        private void OnDestroy()
        {
            _equipmentChangedSubscription?.Dispose();
            _equipmentChangedSubscription = null;
        }

        /// <summary>尝试从全局注册表取得装备服务并订阅换装事件。</summary>
        private void TryBindEquipmentService()
        {
            var bootstrap = GameBootstrap.EnsureExists();
            if (!bootstrap.Context.Services.TryResolve<IEquipmentService>(
                    out var equipment))
            {
                return;
            }

            _equipment = equipment;
            _equipmentChangedSubscription =
                bootstrap.Context.Events.Subscribe<EquipmentChangedEvent>(
                    OnEquipmentChanged);
            ApplySnapshot(_equipment.Snapshot);
        }

        /// <summary>收到成功换装事件后重新读取权威快照。</summary>
        private void OnEquipmentChanged(EquipmentChangedEvent message)
        {
            if (_equipment != null)
            {
                ApplySnapshot(_equipment.Snapshot);
            }
        }

        /// <summary>
        /// 将最终生命、防御、攻击和雷伤加成应用到玩家战斗组件。
        /// </summary>
        private void ApplySnapshot(EquipmentSnapshot snapshot)
        {
            var maxHealth = snapshot.GetFinalStat(StatType.MaxHealth);
            var defense = snapshot.GetFinalStat(StatType.Defense);
            var attack = snapshot.GetFinalStat(StatType.Attack);
            var electricBonus =
                snapshot.GetFinalStat(StatType.ElectricDamageBonus);

            _health.SetMaxHealth(maxHealth, !_initialized);
            _combatStats.SetDefense(defense);
            _swordHitbox?.SetDamage(
                Mathf.Max(0f, attack * (1f + electricBonus)));
            _initialized = true;
        }
    }
}
