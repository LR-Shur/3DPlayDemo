using Train.Architecture.Bootstrap;
using Train.Dialogue.Application;
using Train.WorldInteraction.Core;
using UnityEngine;

namespace Train.WorldInteraction.Runtime
{
    /// <summary>
    /// 场景中的对话终端。玩家靠近后 HUD 显示“对话”，
    /// 按 E 会通过 IDialogueService 启动对应对话图。
    /// 它只负责触发，不保存台词、头像或镜头等表现数据。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldDialogueTerminal :
        MonoBehaviour,
        IInteractable,
        IInteractionTargetInfo
    {
        [SerializeField] private string _interactionId;
        [SerializeField] private string _dialogueId;
        [SerializeField] private string _displayName = "数据终端";
        [SerializeField] private string _actionLabel = "对话";
        [SerializeField, Min(1)] private int _priority = 30;
        [SerializeField] private Transform _interactionPoint;

        private IDialogueService _dialogue;

        /// <inheritdoc />
        public string InteractionId => _interactionId;

        /// <inheritdoc />
        public int Priority => _priority;

        /// <inheritdoc />
        public Vector3 InteractionPosition =>
            _interactionPoint != null
                ? _interactionPoint.position
                : transform.position;

        /// <inheritdoc />
        public string DisplayName => _displayName;

        /// <inheritdoc />
        public string ActionLabel => _actionLabel;

        /// <inheritdoc />
        public bool IsAvailable =>
            _dialogue != null &&
            !_dialogue.IsActive &&
            !string.IsNullOrWhiteSpace(_dialogueId) &&
            _dialogue.TryGetDefinition(_dialogueId, out _);

        /// <summary>
        /// 启动时尝试从全局组合根取得对话服务；
        /// 独立测试场景在服务尚未安装时保持不可交互。
        /// </summary>
        private void Start()
        {
            TryResolveDialogue();
        }

        /// <summary>
        /// 对话服务在场景加载后安装时，继续等待可用。
        /// </summary>
        private void Update()
        {
            if (_dialogue == null)
            {
                TryResolveDialogue();
            }
        }

        /// <summary>
        /// 注入对话服务，供启动器、场景测试和自动化测试复用。
        /// </summary>
        public void Initialize(IDialogueService dialogue)
        {
            _dialogue = dialogue;
        }

        /// <summary>
        /// 配置对话终端的场景数据。
        /// 编辑器内容构建器使用此方法批量生成对话点。
        /// </summary>
        public void Configure(
            string interactionId,
            string dialogueId,
            string displayName,
            int priority = 30,
            Transform interactionPoint = null)
        {
            _interactionId = interactionId;
            _dialogueId = dialogueId;
            _displayName = displayName;
            _priority = Mathf.Max(1, priority);
            _interactionPoint = interactionPoint;
        }

        /// <inheritdoc />
        public InteractResult Interact()
        {
            if (!IsAvailable)
            {
                return InteractResult.Unavailable(
                    "该对话终端当前无法使用。");
            }

            var started = _dialogue.Start(_dialogueId);
            return started
                ? InteractResult.Succeeded(
                    $"开始对话：{_displayName}")
                : InteractResult.Failed(
                    "对话启动失败，请稍后再试。");
        }

        /// <summary>
        /// 尝试从全局服务注册表取得对话服务。
        /// </summary>
        private void TryResolveDialogue()
        {
            var bootstrap = GameBootstrap.EnsureExists();
            if (bootstrap.Context.Services.TryResolve<IDialogueService>(
                    out var dialogue))
            {
                _dialogue = dialogue;
            }
        }
    }
}
