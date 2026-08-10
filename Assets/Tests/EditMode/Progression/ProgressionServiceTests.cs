using NUnit.Framework;
using Train.Architecture.Events;
using Train.Composition.Progression;

namespace Train.Tests.EditMode.Progression
{
    /// <summary>
    /// 验证金币和关卡节点的核心契约，避免 UI 测试掩盖成长数据错误。
    /// </summary>
    public sealed class ProgressionServiceTests
    {
        [Test]
        public void CompleteLegacyLevelId_UnlocksNextNodeAndAddsClearBonus()
        {
            var service = new ProgressionService(new EventBus());

            var completed = service.CompleteLevel(
                "combat_001",
                out var nextNode);

            Assert.That(completed, Is.True);
            Assert.That(nextNode.LevelId, Is.EqualTo("level.combat.002"));
            Assert.That(service.Snapshot.Coins, Is.EqualTo(330));
            Assert.That(service.Snapshot.ClearedNodeCount, Is.EqualTo(1));
        }

        [Test]
        public void SpendCoins_RejectsOverspendAndPublishesFinalBalance()
        {
            var service = new ProgressionService(new EventBus());

            Assert.That(service.TrySpendCoins(151, "test"), Is.False);
            Assert.That(service.Snapshot.Coins, Is.EqualTo(150));
            Assert.That(service.TrySpendCoins(50, "test"), Is.True);
            Assert.That(service.Snapshot.Coins, Is.EqualTo(100));
        }

        [Test]
        public void CaptureAndRestore_RoundTripsRunProgress()
        {
            var source = new ProgressionService(new EventBus());
            source.AddCoins(245, "test");
            source.CompleteLevel("level.combat.001", out _);
            var saved = source.CaptureState();

            var restored = new ProgressionService(new EventBus());
            restored.RestoreState(saved);

            Assert.That(restored.Snapshot.Coins, Is.EqualTo(575));
            Assert.That(restored.Snapshot.CurrentNodeIndex, Is.EqualTo(0));
            Assert.That(restored.Snapshot.CurrentLevelId, Is.EqualTo("level.combat.001"));
            Assert.That(restored.Snapshot.ClearedNodeCount, Is.EqualTo(1));
        }
    }
}
