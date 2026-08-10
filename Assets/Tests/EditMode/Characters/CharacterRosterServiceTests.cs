using System;
using System.Collections.Generic;
using NUnit.Framework;
using Train.Architecture.Assets;
using Train.Architecture.Events;
using Train.Characters.Application;
using Train.Characters.Data;
using Train.Characters.Events;
using UnityEditor;
using UnityEngine;

namespace Train.Tests.EditMode.Characters
{
    /// <summary>
    /// 验证角色名册应用服务的配置转换、事件发布、无效用例及资源租约生命周期。
    /// </summary>
    public sealed class CharacterRosterServiceTests
    {
        private readonly List<UnityEngine.Object> _createdObjects = new();
        private EventBus _events;
        private CharacterRosterService _service;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
            _events?.Dispose();
            for (var i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        _createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void Constructor_BuildsCatalogAndPublishesReady()
        {
            CharacterRosterReadyEvent? ready = null;
            using var subscription =
                _events.Subscribe<CharacterRosterReadyEvent>(
                    value => ready = value);

            CreateService(CreateDefaultSettings());

            Assert.That(_service.Catalog, Has.Count.EqualTo(3));
            Assert.That(
                _service.TryGetDefinition(
                    "character.belle",
                    out var belle),
                Is.True);
            Assert.That(belle.DisplayName, Is.EqualTo("铃"));
            Assert.That(ready.HasValue, Is.True);
            Assert.That(ready.Value.CatalogCount, Is.EqualTo(3));
            Assert.That(ready.Value.UnlockedCount, Is.EqualTo(1));
            Assert.That(
                ready.Value.SelectedCharacterId,
                Is.EqualTo("character.ellen"));
        }

        [Test]
        public void UnlockAndSelect_PublishDecoupledApplicationEvents()
        {
            CreateService(CreateDefaultSettings());
            CharacterUnlockedEvent? unlocked = null;
            CharacterSelectedEvent? selected = null;
            using var unlockSubscription =
                _events.Subscribe<CharacterUnlockedEvent>(
                    value => unlocked = value);
            using var selectSubscription =
                _events.Subscribe<CharacterSelectedEvent>(
                    value => selected = value);

            Assert.That(_service.Select("character.billy"), Is.False);
            Assert.That(_service.Unlock("character.billy"), Is.True);
            Assert.That(_service.Select("character.billy"), Is.True);

            Assert.That(unlocked.HasValue, Is.True);
            Assert.That(
                unlocked.Value.CharacterId,
                Is.EqualTo("character.billy"));
            Assert.That(selected.HasValue, Is.True);
            Assert.That(
                selected.Value.PreviousCharacterId,
                Is.EqualTo("character.ellen"));
            Assert.That(
                selected.Value.CurrentCharacterId,
                Is.EqualTo("character.billy"));
            Assert.That(
                _service.Snapshot.SelectedCharacterId,
                Is.EqualTo("character.billy"));
        }

        [Test]
        public void InvalidRequests_ReturnFalseWithoutEvents()
        {
            CreateService(CreateDefaultSettings());
            var unlockedCount = 0;
            var selectedCount = 0;
            using var unlockSubscription =
                _events.Subscribe<CharacterUnlockedEvent>(
                    _ => unlockedCount++);
            using var selectSubscription =
                _events.Subscribe<CharacterSelectedEvent>(
                    _ => selectedCount++);

            Assert.That(_service.Unlock("missing"), Is.False);
            Assert.That(_service.Unlock("character.ellen"), Is.False);
            Assert.That(_service.Select("character.belle"), Is.False);
            Assert.That(_service.Select("character.ellen"), Is.False);

            Assert.That(unlockedCount, Is.Zero);
            Assert.That(selectedCount, Is.Zero);
            Assert.That(_service.Snapshot.Revision, Is.Zero);
        }

        [Test]
        public void DuplicateDefinition_ThrowsAndReleasesLease()
        {
            var duplicateA = CreateDefinition(
                "character.duplicate",
                "重复甲",
                true);
            var duplicateB = CreateDefinition(
                "character.duplicate",
                "重复乙",
                false);
            var settings = CreateSettings(
                "character.duplicate",
                duplicateA,
                duplicateB);
            var lease = new FakeSettingsLease(settings);

            Assert.Throws<InvalidOperationException>(
                () => new CharacterRosterService(
                    settings,
                    _events,
                    lease));
            Assert.That(lease.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_ReleasesLeaseAndRejectsFurtherUse()
        {
            var settings = CreateDefaultSettings();
            var lease = new FakeSettingsLease(settings);
            _service = new CharacterRosterService(
                settings,
                _events,
                lease);

            _service.Dispose();

            Assert.That(lease.DisposeCount, Is.EqualTo(1));
            Assert.Throws<ObjectDisposedException>(
                () => _ = _service.Snapshot);
            Assert.Throws<ObjectDisposedException>(
                () => _service.Unlock("character.belle"));
        }

        private void CreateService(CharacterSettings settings)
        {
            _service = new CharacterRosterService(settings, _events);
        }

        private CharacterSettings CreateDefaultSettings()
        {
            return CreateSettings(
                "character.ellen",
                CreateDefinition(
                    "character.ellen",
                    "艾莲·乔",
                    true),
                CreateDefinition(
                    "character.belle",
                    "铃",
                    false),
                CreateDefinition(
                    "character.billy",
                    "比利·奇德",
                    false));
        }

        private CharacterDefinition CreateDefinition(
            string characterId,
            string displayName,
            bool unlockedByDefault)
        {
            var definition =
                ScriptableObject.CreateInstance<CharacterDefinition>();
            _createdObjects.Add(definition);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("_characterId").stringValue =
                characterId;
            serialized.FindProperty("_displayName").stringValue =
                displayName;
            serialized.FindProperty("_unlockedByDefault").boolValue =
                unlockedByDefault;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private CharacterSettings CreateSettings(
            string defaultCharacterId,
            params CharacterDefinition[] definitions)
        {
            var settings =
                ScriptableObject.CreateInstance<CharacterSettings>();
            _createdObjects.Add(settings);
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("_defaultSelectedCharacterId")
                .stringValue = defaultCharacterId;
            var characters = serialized.FindProperty("_characters");
            characters.arraySize = definitions.Length;
            for (var i = 0; i < definitions.Length; i++)
            {
                characters.GetArrayElementAtIndex(i)
                    .objectReferenceValue = definitions[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }

        /// <summary>记录角色设置资源租约是否按所有权规则释放。</summary>
        private sealed class FakeSettingsLease :
            IAssetLease<CharacterSettings>
        {
            /// <summary>创建测试角色设置租约。</summary>
            public FakeSettingsLease(CharacterSettings asset)
            {
                Asset = asset;
            }

            /// <inheritdoc />
            public string Location => "test://character-settings";

            /// <inheritdoc />
            public CharacterSettings Asset { get; }

            /// <inheritdoc />
            public bool IsValid => DisposeCount == 0 && Asset != null;

            /// <summary>获取租约被释放的次数。</summary>
            public int DisposeCount { get; private set; }

            /// <summary>记录一次租约释放。</summary>
            public void Dispose()
            {
                DisposeCount++;
            }
        }
    }
}
