using System;
using NUnit.Framework;
using Train.Characters.Core;

namespace Train.Tests.EditMode.Characters
{
    /// <summary>
    /// 验证纯角色名册模型的默认选择、解锁、切换、修订号与快照隔离规则。
    /// </summary>
    public sealed class CharacterRosterModelTests
    {
        [Test]
        public void Constructor_SelectsConfiguredUnlockedCharacter()
        {
            var model = CreateModel();

            Assert.That(
                model.Snapshot.SelectedCharacterId,
                Is.EqualTo("character.ellen"));
            Assert.That(model.Snapshot.Entries, Has.Count.EqualTo(3));
            Assert.That(model.Snapshot.UnlockedCount, Is.EqualTo(1));
            Assert.That(model.Snapshot.Revision, Is.Zero);
            Assert.That(
                model.Snapshot.TryGetEntry(
                    "character.ellen",
                    out var ellen),
                Is.True);
            Assert.That(ellen.IsUnlocked, Is.True);
            Assert.That(ellen.IsSelected, Is.True);
        }

        [Test]
        public void Unlock_CreatesNewSnapshotWithoutMutatingOldSnapshot()
        {
            var model = CreateModel();
            var before = model.Snapshot;

            var changed = model.Unlock("character.belle");

            Assert.That(changed, Is.True);
            Assert.That(model.Snapshot, Is.Not.SameAs(before));
            Assert.That(model.Snapshot.Revision, Is.EqualTo(1));
            Assert.That(model.Snapshot.UnlockedCount, Is.EqualTo(2));
            Assert.That(
                before.TryGetEntry("character.belle", out var oldBelle),
                Is.True);
            Assert.That(oldBelle.IsUnlocked, Is.False);
            Assert.That(
                model.Snapshot.TryGetEntry(
                    "character.belle",
                    out var newBelle),
                Is.True);
            Assert.That(newBelle.IsUnlocked, Is.True);
        }

        [Test]
        public void Unlock_InvalidOrRepeatedRequest_DoesNotChangeRevision()
        {
            var model = CreateModel();

            Assert.That(model.Unlock("missing"), Is.False);
            Assert.That(model.Unlock("character.ellen"), Is.False);
            Assert.That(model.Unlock(" "), Is.False);
            Assert.That(model.Snapshot.Revision, Is.Zero);
        }

        [Test]
        public void Select_RequiresUnlockedCharacterAndRejectsNoOp()
        {
            var model = CreateModel();

            Assert.That(model.Select("character.billy"), Is.False);
            Assert.That(model.Select("character.ellen"), Is.False);
            Assert.That(model.Unlock("character.billy"), Is.True);
            Assert.That(model.Select("character.billy"), Is.True);

            Assert.That(
                model.Snapshot.SelectedCharacterId,
                Is.EqualTo("character.billy"));
            Assert.That(model.Snapshot.Revision, Is.EqualTo(2));
            Assert.That(model.Select("character.billy"), Is.False);
            Assert.That(model.Snapshot.Revision, Is.EqualTo(2));
        }

        [Test]
        public void Constructor_WithDuplicateId_Throws()
        {
            var entries = new[]
            {
                new CharacterRosterEntrySpec("duplicate", true),
                new CharacterRosterEntrySpec("duplicate", false)
            };

            Assert.Throws<InvalidOperationException>(
                () => new CharacterRosterModel(entries, "duplicate"));
        }

        [Test]
        public void Constructor_WithLockedDefaultSelection_Throws()
        {
            var entries = new[]
            {
                new CharacterRosterEntrySpec("locked", false)
            };

            Assert.Throws<InvalidOperationException>(
                () => new CharacterRosterModel(entries, "locked"));
        }

        private static CharacterRosterModel CreateModel()
        {
            return new CharacterRosterModel(
                new[]
                {
                    new CharacterRosterEntrySpec(
                        "character.ellen",
                        true),
                    new CharacterRosterEntrySpec(
                        "character.belle",
                        false),
                    new CharacterRosterEntrySpec(
                        "character.billy",
                        false)
                },
                "character.ellen");
        }
    }
}
