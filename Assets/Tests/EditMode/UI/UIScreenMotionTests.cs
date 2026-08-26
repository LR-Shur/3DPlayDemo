using NUnit.Framework;
using Train.Presentation.UI.Views;
using UnityEngine;

namespace Train.Tests.UI
{
    /// <summary>覆盖主菜单页面转场和按钮反馈的纯逻辑边界。</summary>
    public sealed class UIScreenMotionTests
    {
        [Test]
        public void EaseFunctions_ClampAndReachExpectedShape()
        {
            Assert.That(UIScreenMotion.EaseOutCubic(-1f), Is.EqualTo(0f));
            Assert.That(UIScreenMotion.EaseOutCubic(.5f), Is.EqualTo(.875f).Within(.0001f));
            Assert.That(UIScreenMotion.EaseOutCubic(2f), Is.EqualTo(1f));
            Assert.That(UIScreenMotion.EaseInCubic(.5f), Is.EqualTo(.125f).Within(.0001f));
        }

        [Test]
        public void VisibilityState_HandlesShowHideAndReversal()
        {
            Assert.That(
                UIScreenMotion.ResolveTransitionState(
                    UIScreenMotion.VisibilityState.Hidden,
                    true,
                    false),
                Is.EqualTo(UIScreenMotion.VisibilityState.Showing));
            Assert.That(
                UIScreenMotion.ResolveTransitionState(
                    UIScreenMotion.VisibilityState.Showing,
                    true,
                    true),
                Is.EqualTo(UIScreenMotion.VisibilityState.Visible));
            Assert.That(
                UIScreenMotion.ResolveTransitionState(
                    UIScreenMotion.VisibilityState.Visible,
                    false,
                    false),
                Is.EqualTo(UIScreenMotion.VisibilityState.Hiding));
            Assert.That(
                UIScreenMotion.ResolveTransitionState(
                    UIScreenMotion.VisibilityState.Hiding,
                    true,
                    false),
                Is.EqualTo(UIScreenMotion.VisibilityState.Showing));
        }

        [Test]
        public void ButtonFeedback_PressWinsOverHover()
        {
            Assert.That(UIInteractionMotion.TargetScale(false, false), Is.EqualTo(1f));
            Assert.That(UIInteractionMotion.TargetScale(true, false), Is.EqualTo(1.025f));
            Assert.That(UIInteractionMotion.TargetScale(true, true), Is.EqualTo(.975f));
        }

        [Test]
        public void ButtonFeedback_DisabledButtonKeepsBaseScale()
        {
            Assert.That(
                UIInteractionMotion.TargetScale(true, true, false),
                Is.EqualTo(1f));
        }

        [Test]
        public void SetVisibleImmediate_UpdatesObjectAndInteraction()
        {
            var target = new GameObject("UIScreenMotionTest");
            try
            {
                target.SetActive(false);
                var motion = target.AddComponent<UIScreenMotion>();
                motion.Bind(target);
                var canvasGroup = target.GetComponent<CanvasGroup>();

                motion.SetVisibleImmediate(false);
                Assert.That(target.activeSelf, Is.False);
                Assert.That(canvasGroup.blocksRaycasts, Is.False);
                Assert.That(canvasGroup.interactable, Is.False);
                Assert.That(
                    motion.State,
                    Is.EqualTo(UIScreenMotion.VisibilityState.Hidden));

                motion.SetVisibleImmediate(true);
                Assert.That(target.activeSelf, Is.True);
                Assert.That(canvasGroup.blocksRaycasts, Is.True);
                Assert.That(canvasGroup.interactable, Is.True);
                Assert.That(
                    motion.State,
                    Is.EqualTo(UIScreenMotion.VisibilityState.Visible));
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
