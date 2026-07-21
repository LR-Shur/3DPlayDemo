using System;
using Animancer;
using UnityEngine;

namespace Train.Gameplay.Player.Animation
{
    /// <summary>
    /// 集中管理所有直接调用 Animancer 的逻辑，并向游戏状态暴露语义化的玩家动画命令。
    /// 具体状态不需要了解动画片段、淡入时间或 Animancer 事件配置。
    /// </summary>
    public sealed class PlayerAnimation : MonoBehaviour
    {
        [SerializeField] private AnimancerComponent _animancer;
        [SerializeField, Min(0f)] private float _fadeDuration = 0.15f;
        [SerializeField] private ClipTransition _idle = new ClipTransition();
        [SerializeField] private ClipTransition _walk = new ClipTransition();
        [SerializeField] private ClipTransition _run = new ClipTransition();
        [SerializeField] private ClipTransition _dodgeForward = new ClipTransition();
        [SerializeField] private ClipTransition _dodgeBackward = new ClipTransition();
        [SerializeField] private ClipTransition _attack = new ClipTransition();

        /// <summary>
        /// 当 Inspector 中未指定时查找 Animancer 组件。
        /// </summary>
        private void Awake()
        {
            _animancer ??= GetComponent<AnimancerComponent>();
        }

        /// <summary>
        /// 播放待机循环动画。
        /// </summary>
        public void PlayIdle()
        {
            PlayLoop(_idle, "Idle");
        }

        /// <summary>
        /// 播放步行循环动画。
        /// </summary>
        public void PlayWalk()
        {
            PlayLoop(_walk, "Walk");
        }

        /// <summary>
        /// 播放奔跑循环动画。
        /// </summary>
        public void PlayRun()
        {
            PlayLoop(_run, "Run");
        }

        /// <summary>
        /// 根据锁定的翻滚方向播放对应的翻滚动画。
        /// </summary>
        /// <param name="dodgeDirection">翻滚开始时锁定的世界坐标方向。</param>
        /// <param name="onEnded">翻滚动画播放结束时调用的回调。</param>
        public void PlayDodge(Vector3 dodgeDirection, Action onEnded)
        {
            var isBackward = Vector3.Dot(transform.forward, dodgeDirection) < -0.1f;
            var transition = isBackward && _dodgeBackward.IsValid ? _dodgeBackward : _dodgeForward;
            PlayOneShot(transition, "Dodge", onEnded);
        }

        /// <summary>
        /// 播放第一段基础攻击动画，并在结束后调用回调。
        /// </summary>
        /// <param name="onEnded">攻击动画播放结束时调用的回调。</param>
        public void PlayAttack(Action onEnded)
        {
            PlayOneShot(_attack, "Attack", onEnded);
        }

        /// <summary>
        /// 当已分配动画片段时播放循环过渡。
        /// </summary>
        /// <param name="transition">需要播放的循环过渡。</param>
        /// <param name="label">用于配置错误提示的可读名称。</param>
        private void PlayLoop(ClipTransition transition, string label)
        {
            if (!ValidateTransition(transition, label))
            {
                return;
            }

            _animancer.Play(transition, _fadeDuration);
        }

        /// <summary>
        /// 播放非循环过渡，并设置其结束回调。
        /// </summary>
        /// <param name="transition">需要播放的一次性过渡。</param>
        /// <param name="label">用于配置错误提示的可读名称。</param>
        /// <param name="onEnded">动画片段播放结束后调用的回调。</param>
        private void PlayOneShot(ClipTransition transition, string label, Action onEnded)
        {
            if (!ValidateTransition(transition, label))
            {
                onEnded?.Invoke();
                return;
            }

            var state = _animancer.Play(transition, _fadeDuration);
            state.Events(this).OnEnd = onEnded;
        }

        /// <summary>
        /// 校验 Animancer 和请求的过渡是否可以播放。
        /// </summary>
        /// <param name="transition">需要校验的过渡。</param>
        /// <param name="label">错误信息中使用的可读名称。</param>
        /// <returns>过渡可播放时返回 true。</returns>
        private bool ValidateTransition(ClipTransition transition, string label)
        {
            if (_animancer != null && transition != null && transition.IsValid)
            {
                return true;
            }

            Debug.LogError($"PlayerAnimation is missing a valid {label} transition or AnimancerComponent.", this);
            return false;
        }
    }
}
