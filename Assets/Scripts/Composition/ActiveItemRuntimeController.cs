using System;
using System.Collections.Generic;
using Train.Architecture.Events;
using Train.Architecture.Bootstrap;
using Train.Composition.Config;
using Train.Inventory.Application;
using Train.Inventory.Data;
using Train.Inventory.Events;
using Train.Gameplay.Combat;
using Train.Gameplay.Combat.Factions;
using Train.Gameplay.Combat.Buffs;
using Train.Buffs.Core;
using Train.Gameplay.Player.Input;
using Train.Presentation.UI.Views;
using UnityEngine;

namespace Train.Composition
{
    /// <summary>
    /// 管理玩家四个主动道具快捷槽，并执行配置表中的回血和范围伤害效果。
    /// </summary>
    [DefaultExecutionOrder(-7190)]
    [DisallowMultipleComponent]
    public sealed class ActiveItemRuntimeController : MonoBehaviour
    {
        private readonly string[] _slotItemIds = new string[4];
        private readonly float[] _nextUseTimes = new float[4];
        private IInventoryService _inventory;
        private ILubanConfigService _luban;
        private PlayerInputReader _input;
        private Health _playerHealth;
        private ActiveItemQuickBarView _quickBar;
        private IDisposable _slotAssignmentSubscription;

        /// <summary>公开四个快捷槽的只读物品 ID。</summary>
        public IReadOnlyList<string> SlotItemIds => _slotItemIds;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            TryInitialize();
            BindPlayer();
            RenderQuickBar();
        }

        /// <summary>
        /// 将背包中的物品绑定到 1 到 4 号槽位；空字符串表示清空槽位。
        /// </summary>
        public bool AssignSlot(int slotIndex, string itemId)
        {
            if (slotIndex < 0 || slotIndex >= _slotItemIds.Length)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(itemId) &&
                (_inventory == null ||
                 !_inventory.TryGetDefinition(itemId, out var definition) ||
                 definition.Category != ItemCategory.Consumable))
            {
                return false;
            }

            _slotItemIds[slotIndex] = itemId ?? string.Empty;
            _nextUseTimes[slotIndex] = 0f;
            return true;
        }

        private void TryInitialize()
        {
            var bootstrap = GameBootstrap.EnsureExists();
            bootstrap.Context.Services.TryResolve(out _inventory);
            bootstrap.Context.Services.TryResolve(out _luban);
            if (_slotAssignmentSubscription == null)
            {
                _slotAssignmentSubscription = bootstrap.Context.Events.Subscribe<
                    ActiveItemSlotAssignmentRequested>(
                    message => AssignSlot(message.SlotIndex, message.ItemId));
            }
        }

        private void RenderQuickBar()
        {
            if (_quickBar == null)
            {
                _quickBar = FindFirstObjectByType<ActiveItemQuickBarView>();
                if (_quickBar == null)
                {
                    var root = FindFirstObjectByType<GameUIRootView>();
                    if (root != null && root.Hud != null)
                    {
                        var quickBarObject = new GameObject("ActiveItemQuickBar");
                        quickBarObject.transform.SetParent(root.Hud.transform, false);
                        _quickBar = quickBarObject.AddComponent<ActiveItemQuickBarView>();
                    }
                }
            }

            if (_quickBar == null || _inventory == null)
            {
                return;
            }

            var names = new string[4];
            var quantities = new int[4];
            for (var index = 0; index < 4; index++)
            {
                var itemId = _slotItemIds[index];
                quantities[index] = string.IsNullOrWhiteSpace(itemId)
                    ? 0
                    : _inventory.GetTotalQuantity(itemId);
                if (!string.IsNullOrWhiteSpace(itemId) &&
                    _inventory.TryGetDefinition(itemId, out var definition))
                {
                    names[index] = definition.DisplayName;
                }
            }

            _quickBar.Render(names, quantities);
            var equipmentView = FindFirstObjectByType<EquipmentScreenView>(
                FindObjectsInactive.Include);
            equipmentView?.RenderActiveItemSlots(names, quantities);
        }

        private void BindPlayer()
        {
            var player = FindFirstObjectByType<PlayerInputReader>();
            if (player == _input)
            {
                return;
            }

            if (_input != null)
            {
                _input.ItemUsePerformed -= OnItemUsePerformed;
            }

            _slotAssignmentSubscription?.Dispose();
            _slotAssignmentSubscription = null;

            _input = player;
            _playerHealth = player != null
                ? player.GetComponent<Health>()
                : null;
            if (_input != null)
            {
                _input.ItemUsePerformed += OnItemUsePerformed;
            }
        }

        private void OnItemUsePerformed(int slotIndex)
        {
            if (_inventory == null ||
                _luban == null ||
                !_luban.IsReady ||
                slotIndex < 0 ||
                slotIndex >= _slotItemIds.Length)
            {
                return;
            }

            var itemId = _slotItemIds[slotIndex];
            if (string.IsNullOrWhiteSpace(itemId) ||
                _inventory.GetTotalQuantity(itemId) <= 0 ||
                Time.time < _nextUseTimes[slotIndex])
            {
                return;
            }

            var config = FindActiveItem(itemId);
            if (config == null || !Apply(config))
            {
                return;
            }

            if (_inventory.TryRemove(itemId, 1))
            {
                if (_inventory.GetTotalQuantity(itemId) <= 0)
                {
                    _slotItemIds[slotIndex] = string.Empty;
                }

                _nextUseTimes[slotIndex] = Time.time + Mathf.Max(0f, config.Cooldown);
            }
        }

        private cfg.game.ActiveItem FindActiveItem(string itemId)
        {
            foreach (var row in _luban.Tables.TbActiveItem.DataList)
            {
                if (row != null && string.Equals(row.ItemId, itemId, StringComparison.Ordinal))
                {
                    return row;
                }
            }

            return null;
        }

        private bool Apply(cfg.game.ActiveItem config)
        {
            switch (config.Effect)
            {
                case cfg.game.EActiveItemEffect.HEAL:
                    if (_playerHealth == null || !_playerHealth.IsAlive)
                    {
                        return false;
                    }

                    _playerHealth.Heal(config.Value);
                    return true;
                case cfg.game.EActiveItemEffect.GRENADE:
                    return ApplyGrenade(config);
                case cfg.game.EActiveItemEffect.ELEMENTAL_GRENADE:
                    return ApplyGrenade(config);
                case cfg.game.EActiveItemEffect.SELF_BUFF:
                    return ApplySelfBuff(config);
                default:
                    return false;
            }
        }

        /// <summary>对范围内敌人造成指定元素伤害，并按配置附加 Buff。</summary>
        private bool ApplyGrenade(cfg.game.ActiveItem config)
        {
            var origin = _input != null ? _input.transform.position : transform.position;
            var applied = false;
            var targets = FindObjectsByType<Health>(FindObjectsSortMode.None);
            foreach (var target in targets)
            {
                if (target == null ||
                    !target.IsAlive ||
                    target == _playerHealth ||
                    Vector3.Distance(origin, target.transform.position) > config.Radius)
                {
                    continue;
                }

                var faction = FactionResolver.FindInParents(target.transform);
                if (faction != null && faction.Faction != CombatFaction.Enemy)
                {
                    continue;
                }

                var result = DamageHandler.Apply(
                    target,
                    new DamageInfo(
                        config.Value,
                        null,
                        target.transform.position - Vector3.up * 0.4f,
                        target.transform.position - origin,
                        0f,
                        ToDamageType(config.Element)),
                    faction);
                if (result.AppliedDamage > 0f &&
                    !string.IsNullOrWhiteSpace(config.BuffId))
                {
                    var buffHandle = target.GetComponentInParent<BuffHandleComponent>();
                    buffHandle?.Apply(new BuffInfo(
                        config.BuffId,
                        $"item.{config.ItemId}",
                        Mathf.Max(0.1f, config.Duration),
                        config.Magnitude,
                        Mathf.Max(1, config.StackAmount),
                        Mathf.Max(1, config.MaxStacks)));
                }

                applied |= result.AppliedDamage > 0f;
            }

            return applied;
        }

        /// <summary>为玩家自身附加配置的元素增益 Buff。</summary>
        private bool ApplySelfBuff(cfg.game.ActiveItem config)
        {
            if (_playerHealth == null || !_playerHealth.IsAlive ||
                string.IsNullOrWhiteSpace(config.BuffId))
            {
                return false;
            }

            var buffHandle = _playerHealth.GetComponentInParent<BuffHandleComponent>();
            if (buffHandle == null)
            {
                return false;
            }

            buffHandle.Apply(new BuffInfo(
                config.BuffId,
                $"item.{config.ItemId}",
                Mathf.Max(0.1f, config.Duration),
                config.Magnitude,
                Mathf.Max(1, config.StackAmount),
                Mathf.Max(1, config.MaxStacks)));
            return true;
        }

        /// <summary>将 Luban 元素枚举转换为战斗层伤害类型。</summary>
        private static DamageType ToDamageType(cfg.game.EElement element)
        {
            return element switch
            {
                cfg.game.EElement.FIRE => DamageType.Fire,
                cfg.game.EElement.WATER => DamageType.Water,
                cfg.game.EElement.WIND => DamageType.Wind,
                cfg.game.EElement.EARTH => DamageType.Earth,
                cfg.game.EElement.ICE => DamageType.Ice,
                cfg.game.EElement.ELECTRIC => DamageType.Electric,
                _ => DamageType.Physical
            };
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.ItemUsePerformed -= OnItemUsePerformed;
            }
        }
    }
}
