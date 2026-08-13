using System;
using Train.Architecture.Bootstrap;
using Train.Architecture.Events;
using Train.Equipment.Application;
using Train.Equipment.Core;
using Train.Equipment.Events;
using Train.Gameplay.Combat;
using Train.Gameplay.Combat.Buffs;
using Train.Gameplay.Player.Input;
using UnityEngine;

namespace Train.Composition
{
    /// <summary>
    /// 将装备服务中的武器元素和命中 Buff 同步到玩家 SwordHitbox。
    /// 组合层负责连接应用服务与 Unity 战斗组件，Gameplay 本身不依赖装备服务。
    /// </summary>
    [DefaultExecutionOrder(-7200)]
    [DisallowMultipleComponent]
    public sealed class EquipmentCombatRuntimeController : MonoBehaviour
    {
        private IEquipmentService _equipment;
        private IDisposable _equipmentSubscription;
        private long _lastRevision = -1;
        private int _lastPlayerInstanceId;
        private PlayerInputReader _input;
        private Health _health;
        private BuffHandleComponent _buffs;
        private ParticleSystem _chargeParticles;
        private bool _applyingEffect;
        private readonly System.Collections.Generic.Dictionary<string, float> _nextEffectTimes =
            new System.Collections.Generic.Dictionary<string, float>(StringComparer.Ordinal);

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            TryInitialize();
            SyncWeapon();
            UpdateSelfEffects();
        }

        private void TryInitialize()
        {
            if (_equipment != null)
            {
                return;
            }

            var bootstrap = GameBootstrap.EnsureExists();
            if (!bootstrap.Context.Services.TryResolve<IEquipmentService>(
                    out _equipment))
            {
                return;
            }

            _equipmentSubscription = bootstrap.Context.Events.Subscribe<
                EquipmentChangedEvent>(OnEquipmentChanged);
        }

        private void OnEquipmentChanged(EquipmentChangedEvent message)
        {
            _lastRevision = -1;
        }

        private void SyncWeapon()
        {
            if (_equipment == null)
            {
                return;
            }

            var player = UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();
            if (player == null)
            {
                return;
            }

            BindPlayerEvents(player);

            var snapshot = _equipment.Snapshot;
            if (snapshot.Revision == _lastRevision &&
                player.GetInstanceID() == _lastPlayerInstanceId)
            {
                return;
            }

            _lastRevision = snapshot.Revision;
            _lastPlayerInstanceId = player.GetInstanceID();

            var weapon = snapshot.GetEquippedItem(EquipmentSlot.Weapon);
            if (weapon == null ||
                !_equipment.TryGetDefinition(weapon.ItemId, out var definition))
            {
                player.ConfigureEquippedWeapon(
                    DamageType.Physical,
                    string.Empty,
                    0f,
                    0f,
                    1,
                    1,
                    0f,
                    "weapon.none");
                return;
            }

            player.ConfigureEquippedWeapon(
                ToDamageType(definition.ElementId),
                definition.OnHitBuffId,
                definition.EffectDuration,
                definition.EffectMagnitude,
                definition.EffectStackAmount,
                definition.EffectMaxStacks,
                definition.EffectCooldown,
                $"weapon.{definition.ItemId}");
        }

        private void BindPlayerEvents(PlayerCombat player)
        {
            if (player.GetInstanceID() == _lastPlayerInstanceId)
            {
                return;
            }

            UnbindPlayerEvents();
            _input = player.GetComponent<PlayerInputReader>();
            _health = player.GetComponent<Health>();
            _buffs = player.GetComponent<BuffHandleComponent>();
            EnsureChargeParticles(player.transform);
            if (player.SwordHitbox != null)
            {
                player.SwordHitbox.DamageApplied += OnPlayerDamageApplied;
            }
            if (_input != null)
            {
                _input.DodgePerformed += OnDodgePerformed;
            }
            if (_health != null)
            {
                _health.Damaged += OnPlayerDamaged;
            }

            if (_buffs != null)
            {
                _buffs.Changed += OnPlayerBuffChanged;
            }
        }

        private void UpdateSelfEffects()
        {
            if (_health == null || !_health.IsAlive)
            {
                return;
            }

            if (_health.CurrentHealth <= _health.MaxHealth * 0.3f)
            {
                ApplyTrigger("ON_LOW_HEALTH");
            }
        }

        private void OnDodgePerformed()
        {
            ApplyTrigger("ON_DODGE");
        }

        private void OnPlayerDamaged(DamageInfo damage, DamageResult result)
        {
            if (result.AppliedDamage > 0f)
            {
                ApplyTrigger("ON_TAKE_DAMAGE");
            }
        }

        private void OnPlayerDamageApplied(DamageInfo damage, DamageResult result)
        {
            if (result.AppliedDamage <= 0f || damage.DamageType != DamageType.Electric || _buffs == null)
            {
                return;
            }

            _buffs.Apply(new Train.Buffs.Core.BuffInfo(
                "electric_charge",
                "equipment.electric_charge",
                8f,
                .3f,
                1,
                5));
        }

        private void OnPlayerBuffChanged(object sender, Train.Buffs.Core.BuffChangedEventArgs args)
        {
            if (_applyingEffect)
            {
                return;
            }

            if (args.ChangedBuff.StackCount >= args.ChangedBuff.MaxStacks)
            {
                ApplyTrigger("ON_MARK_REACHED");
            }

            UpdateChargeParticles();
        }

        private void EnsureChargeParticles(Transform parent)
        {
            if (_chargeParticles != null || parent == null) return;
            var go = new GameObject("ElectricChargeParticles");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.up * .8f;
            _chargeParticles = go.AddComponent<ParticleSystem>();
            var main = _chargeParticles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.2f, .8f);
            main.startSize = new ParticleSystem.MinMaxCurve(.025f, .08f);
            main.startColor = new Color(0.65f, 0.12f, 1f, 1f);
            main.maxParticles = 32;
            var shape = _chargeParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .45f;
            _chargeParticles.GetComponent<ParticleSystemRenderer>().material =
                new Material(Shader.Find("Sprites/Default"));
            UpdateChargeParticles();
        }

        private void UpdateChargeParticles()
        {
            if (_chargeParticles == null || _buffs == null) return;
            var stacks = 0;
            foreach (var buff in _buffs.Snapshot.Buffs)
            {
                if (buff.BuffId == "electric_charge") stacks = buff.StackCount;
            }
            var emission = _chargeParticles.emission;
            emission.rateOverTime = stacks > 0 ? 8f + stacks * 8f : 0f;
            if (stacks > 0 && !_chargeParticles.isPlaying) _chargeParticles.Play();
            if (stacks == 0 && _chargeParticles.isPlaying) _chargeParticles.Stop();
        }

        private void ApplyTrigger(string trigger)
        {
            if (_equipment == null || _buffs == null)
            {
                return;
            }

            var snapshot = _equipment.Snapshot;
            for (var i = 0; i < snapshot.Slots.Count; i++)
            {
                var item = snapshot.Slots[i].Item;
                if (item == null ||
                    !_equipment.TryGetDefinition(item.ItemId, out var definition) ||
                    !string.Equals(definition.EffectTriggerId, trigger, StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(definition.OnHitBuffId))
                {
                    continue;
                }

                var key = $"{trigger}:{definition.ItemId}";
                if (_nextEffectTimes.TryGetValue(key, out var nextTime) &&
                    Time.time < nextTime)
                {
                    continue;
                }

                _nextEffectTimes[key] = Time.time + definition.EffectCooldown;
                _applyingEffect = true;
                try
                {
                    _buffs.Apply(new Train.Buffs.Core.BuffInfo(
                        definition.OnHitBuffId,
                        $"equipment.{definition.ItemId}",
                        definition.EffectDuration,
                        definition.EffectMagnitude,
                        definition.EffectStackAmount,
                        definition.EffectMaxStacks));
                }
                finally
                {
                    _applyingEffect = false;
                }
            }
        }

        private static DamageType ToDamageType(string elementId)
        {
            return elementId?.ToUpperInvariant() switch
            {
                "FIRE" => DamageType.Fire,
                "WATER" => DamageType.Water,
                "WIND" => DamageType.Wind,
                "EARTH" => DamageType.Earth,
                "ICE" => DamageType.Ice,
                "ELECTRIC" => DamageType.Electric,
                _ => DamageType.Physical
            };
        }

        private void OnDestroy()
        {
            UnbindPlayerEvents();
            _equipmentSubscription?.Dispose();
        }

        private void UnbindPlayerEvents()
        {
            if (_input != null)
            {
                _input.DodgePerformed -= OnDodgePerformed;
            }
            if (_health != null)
            {
                _health.Damaged -= OnPlayerDamaged;
            }
            var player = _input != null ? _input.GetComponent<PlayerCombat>() : null;
            if (player != null && player.SwordHitbox != null)
            {
                player.SwordHitbox.DamageApplied -= OnPlayerDamageApplied;
            }

            if (_buffs != null)
            {
                _buffs.Changed -= OnPlayerBuffChanged;
            }

            _input = null;
            _health = null;
            _buffs = null;
        }
    }
}
