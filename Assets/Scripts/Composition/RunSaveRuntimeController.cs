using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Train.Architecture.Bootstrap;
using Train.Architecture.Events;
using Train.Equipment.Application;
using Train.Equipment.Core;
using Train.Equipment.Events;
using Train.Inventory.Application;
using Train.Inventory.Core;
using Train.Inventory.Events;
using Train.Composition.Progression;
using UnityEngine;

namespace Train.Composition
{
    /// <summary>
    /// 管理一次 Run 的本地存档：金币、关卡、背包和装备槽位。
    /// 使用 JSON 文件便于调试和后续迁移，不依赖 Unity 全局设置。
    /// </summary>
    [DefaultExecutionOrder(-8300)]
    [DisallowMultipleComponent]
    public sealed class RunSaveRuntimeController : MonoBehaviour
    {
        private const int CurrentSaveVersion = 1;
        private const float SaveDebounceSeconds = 0.25f;

        private readonly List<IDisposable> _subscriptions = new();
        private IProgressionService _progressionInterface;
        private ProgressionService _progression;
        private IInventoryService _inventory;
        private IEquipmentService _equipment;
        private bool _initialized;
        private bool _restoring;
        private bool _savePending;
        private float _saveAt;

        private string SavePath =>
            Path.Combine(Application.persistentDataPath, "3DPlay", "run-save.json");

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            TryInitialize();
            if (_savePending && Time.unscaledTime >= _saveAt)
            {
                _savePending = false;
                SaveNow();
            }
        }

        private void TryInitialize()
        {
            if (_initialized)
            {
                return;
            }

            var bootstrap = GameBootstrap.EnsureExists();
            if (!bootstrap.Context.Services.TryResolve<IProgressionService>(out _progressionInterface) ||
                !bootstrap.Context.Services.TryResolve<IInventoryService>(out _inventory) ||
                !bootstrap.Context.Services.TryResolve<IEquipmentService>(out _equipment))
            {
                return;
            }

            _progression = _progressionInterface as ProgressionService;
            if (_progression == null)
            {
                Debug.LogError("RunSaveRuntimeController requires ProgressionService.");
                enabled = false;
                return;
            }

            LoadIfPresent();
            var events = bootstrap.Context.Events;
            _subscriptions.Add(events.Subscribe<CurrencyChangedEvent>(_ => RequestSave()));
            _subscriptions.Add(events.Subscribe<ProgressionChangedEvent>(_ => RequestSave()));
            _subscriptions.Add(events.Subscribe<InventoryChangedEvent>(_ => RequestSave()));
            _subscriptions.Add(events.Subscribe<EquipmentChangedEvent>(_ => RequestSave()));
            _initialized = true;
            SaveNow();
        }

        private void RequestSave()
        {
            if (!_initialized || _restoring)
            {
                return;
            }

            _savePending = true;
            _saveAt = Time.unscaledTime + SaveDebounceSeconds;
        }

        private void LoadIfPresent()
        {
            if (!File.Exists(SavePath))
            {
                return;
            }

            try
            {
                var json = File.ReadAllText(SavePath, Encoding.UTF8);
                var data = JsonUtility.FromJson<RunSaveData>(json);
                if (data == null || data.Version != CurrentSaveVersion)
                {
                    Debug.LogWarning("Run 存档版本不匹配，将使用当前默认进度。", this);
                    return;
                }

                _restoring = true;
                _progression.RestoreState(data.Progression);
                RestoreInventory(data.Inventory);
                RestoreEquipment(data.Equipment);
                _restoring = false;
            }
            catch (Exception exception)
            {
                _restoring = false;
                Debug.LogException(exception, this);
            }
        }

        private void RestoreInventory(List<InventorySaveEntry> entries)
        {
            var current = _inventory.Snapshot;
            for (var i = 0; i < current.Slots.Count; i++)
            {
                var slot = current.Slots[i];
                if (!slot.IsEmpty)
                {
                    _inventory.TryRemove(slot.ItemId, slot.Quantity);
                }
            }

            if (entries == null)
            {
                return;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry != null && entry.Quantity > 0)
                {
                    _inventory.TryAdd(entry.ItemId, entry.Quantity);
                }
            }
        }

        private void RestoreEquipment(List<EquipmentSaveEntry> entries)
        {
            var current = _equipment.Snapshot;
            for (var i = 0; i < current.Slots.Count; i++)
            {
                if (current.Slots[i].Item != null)
                {
                    _equipment.Unequip(current.Slots[i].Slot);
                }
            }

            if (entries == null)
            {
                return;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId))
                {
                    continue;
                }

                var slot = (EquipmentSlot)entry.Slot;
                if (Enum.IsDefined(typeof(EquipmentSlot), slot))
                {
                    _equipment.Equip(entry.ItemId, slot);
                }
            }
        }

        private void SaveNow()
        {
            if (!_initialized && _progression == null)
            {
                return;
            }

            try
            {
                var data = new RunSaveData
                {
                    Version = CurrentSaveVersion,
                    Progression = _progression.CaptureState()
                };

                var inventory = _inventory.Snapshot;
                for (var i = 0; i < inventory.Slots.Count; i++)
                {
                    var slot = inventory.Slots[i];
                    if (!slot.IsEmpty)
                    {
                        data.Inventory.Add(new InventorySaveEntry
                        {
                            ItemId = slot.ItemId,
                            Quantity = slot.Quantity
                        });
                    }
                }

                var equipment = _equipment.Snapshot;
                for (var i = 0; i < equipment.Slots.Count; i++)
                {
                    var slot = equipment.Slots[i];
                    if (slot.Item != null)
                    {
                        data.Equipment.Add(new EquipmentSaveEntry
                        {
                            ItemId = slot.Item.ItemId,
                            Slot = (int)slot.Slot
                        });
                    }
                }

                var directory = Path.GetDirectoryName(SavePath);
                Directory.CreateDirectory(directory);
                File.WriteAllText(
                    SavePath,
                    JsonUtility.ToJson(data, true),
                    new UTF8Encoding(false));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void OnDestroy()
        {
            if (_initialized)
            {
                SaveNow();
            }

            for (var i = _subscriptions.Count - 1; i >= 0; i--)
            {
                _subscriptions[i]?.Dispose();
            }
        }
    }

    [Serializable]
    internal sealed class RunSaveData
    {
        public int Version;
        public ProgressionSaveData Progression = new();
        public List<InventorySaveEntry> Inventory = new();
        public List<EquipmentSaveEntry> Equipment = new();
    }

    [Serializable]
    internal sealed class InventorySaveEntry
    {
        public string ItemId;
        public int Quantity;
    }

    [Serializable]
    internal sealed class EquipmentSaveEntry
    {
        public string ItemId;
        public int Slot;
    }
}
