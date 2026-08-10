using NUnit.Framework;
using Train.Architecture.Events;
using Train.GameFlow.Application;
using Train.GameFlow.Application.Events;
using Train.GameFlow.Core;
using Train.Gameplay.Player.Application.Events;
using Train.Presentation.UI.Contracts;
using Train.Presentation.UI.Presenters;
using Train.Presentation.UI.ViewModels;
using UnityEngine;

namespace Train.Tests.UI
{
    public sealed class HudPresenterTests
    {
        private EventBus _events;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
        }

        [TearDown]
        public void TearDown()
        {
            _events.Dispose();
        }

        [Test]
        public void Constructor_ReadsCurrentSessionAndRendersCombatHud()
        {
            var readModel = new FakeLevelReadModel
            {
                Snapshot = CreateSnapshot(
                    LevelPhase.Combat,
                    LevelOutcome.None,
                    75f,
                    100f,
                    1,
                    3)
            };
            var registry = new FakeLevelSessionRegistry(readModel);
            var view = new FakeHudView();

            using var presenter =
                new HudPresenter(registry, _events, view);

            Assert.That(view.RenderCount, Is.EqualTo(1));
            Assert.That(view.LastModel.HasLevel, Is.True);
            Assert.That(view.LastModel.Health01, Is.EqualTo(0.75f));
            Assert.That(view.LastModel.RemainingEnemyCount, Is.EqualTo(2));
            Assert.That(
                view.LastModel.ObjectiveText,
                Is.EqualTo("击败敌人  1/3"));
            Assert.That(
                view.LastModel.Banner.Kind,
                Is.EqualTo(HudBannerKind.Phase));
            Assert.That(
                view.LastModel.Banner.Title,
                Is.EqualTo("战斗开始"));
        }

        [Test]
        public void GameplayEvent_RereadsAuthoritativeSnapshot()
        {
            var readModel = new FakeLevelReadModel
            {
                Snapshot = CreateSnapshot(
                    LevelPhase.Combat,
                    LevelOutcome.None,
                    100f,
                    100f,
                    0,
                    3)
            };
            var view = new FakeHudView();
            using var presenter =
                new HudPresenter(
                    new FakeLevelSessionRegistry(readModel),
                    _events,
                    view);

            readModel.Snapshot = CreateSnapshot(
                LevelPhase.Combat,
                LevelOutcome.None,
                62f,
                100f,
                1,
                3);
            _events.Publish(
                new EnemyDefeatedEvent(
                    "level_001",
                    "knight",
                    "enemy_01",
                    "player"));

            Assert.That(view.RenderCount, Is.EqualTo(2));
            Assert.That(view.LastModel.PlayerHealth, Is.EqualTo(62f));
            Assert.That(view.LastModel.DefeatedEnemyCount, Is.EqualTo(1));
            Assert.That(
                view.LastModel.Banner.Kind,
                Is.EqualTo(HudBannerKind.None));
        }

        [Test]
        public void RespawnAndResultEvents_RenderExpectedAnnouncements()
        {
            var readModel = new FakeLevelReadModel
            {
                Snapshot = CreateSnapshot(
                    LevelPhase.Combat,
                    LevelOutcome.None,
                    0f,
                    100f,
                    0,
                    3)
            };
            var view = new FakeHudView();
            using var presenter =
                new HudPresenter(
                    new FakeLevelSessionRegistry(readModel),
                    _events,
                    view);
            var player = new GameObject("Player");

            try
            {
                _events.Publish(
                    new PlayerRespawnStartedEvent(player, 2.5f));
                Assert.That(
                    view.LastModel.Banner.Kind,
                    Is.EqualTo(HudBannerKind.Respawning));
                Assert.That(
                    view.LastModel.Banner.Subtitle,
                    Does.Contain("2.5"));

                _events.Publish(
                    new PlayerRespawnCompletedEvent(player));
                Assert.That(
                    view.LastModel.Banner.Kind,
                    Is.EqualTo(HudBannerKind.Revived));

                readModel.Snapshot = CreateSnapshot(
                    LevelPhase.Cleared,
                    LevelOutcome.Cleared,
                    100f,
                    100f,
                    3,
                    3);
                _events.Publish(
                    new LevelCompletedEvent("level_001", 42f));
                Assert.That(
                    view.LastModel.Banner.Kind,
                    Is.EqualTo(HudBannerKind.Cleared));
                Assert.That(
                    view.LastModel.Banner.Title,
                    Is.EqualTo("作战完成"));

                _events.Publish(
                    new LevelFailedEvent(
                        "level_001",
                        "代理人全部失去战斗能力"));
                Assert.That(
                    view.LastModel.Banner.Kind,
                    Is.EqualTo(HudBannerKind.Failed));
                Assert.That(
                    view.LastModel.Banner.Subtitle,
                    Is.EqualTo("代理人全部失去战斗能力"));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void Dispose_StopsEventDrivenRendering()
        {
            var readModel = new FakeLevelReadModel
            {
                Snapshot = CreateSnapshot(
                    LevelPhase.Intro,
                    LevelOutcome.None,
                    100f,
                    100f,
                    0,
                    3)
            };
            var view = new FakeHudView();
            var presenter =
                new HudPresenter(
                    new FakeLevelSessionRegistry(readModel),
                    _events,
                    view);

            presenter.Dispose();
            _events.Publish(
                new LevelPhaseChangedEvent(
                    "level_001",
                    LevelPhase.Intro,
                    LevelPhase.Combat,
                    LevelOutcome.None));

            Assert.That(view.RenderCount, Is.EqualTo(1));
        }

        private static LevelReadSnapshot CreateSnapshot(
            LevelPhase phase,
            LevelOutcome outcome,
            float health,
            float maxHealth,
            int defeated,
            int enemyCount)
        {
            return new LevelReadSnapshot(
                "level_001",
                "旧城训练区",
                phase,
                outcome,
                enemyCount,
                defeated,
                health,
                maxHealth,
                0);
        }

        private sealed class FakeHudView : IHudView
        {
            public int RenderCount { get; private set; }
            public HudViewModel LastModel { get; private set; }

            public void Render(HudViewModel viewModel)
            {
                RenderCount++;
                LastModel = viewModel;
            }

            /// <inheritdoc />
            public void RenderInteractionPrompt(
                InteractionPromptViewModel viewModel)
            {
            }

            /// <inheritdoc />
            public void RenderPickupToast(
                PickupToastViewModel viewModel)
            {
            }
        }

        private sealed class FakeLevelReadModel : ILevelReadModel
        {
            public LevelReadSnapshot Snapshot { get; set; }
        }

        private sealed class FakeLevelSessionRegistry :
            ILevelSessionRegistry
        {
            public FakeLevelSessionRegistry(ILevelReadModel current)
            {
                Current = current;
            }

            public ILevelReadModel Current { get; private set; }

            public void Attach(ILevelReadModel readModel)
            {
                Current = readModel;
            }

            public void Detach(ILevelReadModel readModel)
            {
                if (ReferenceEquals(Current, readModel))
                {
                    Current = null;
                }
            }
        }
    }
}
