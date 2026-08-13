using System;
using Train.WorldInteraction.Core;
using Train.WorldInteraction.Data;
using UnityEngine;

namespace Train.WorldInteraction.Runtime
{
    /// <summary>场景中的传送门交互点，实际跳转由流程层注入。</summary>
    [DisallowMultipleComponent]
    public sealed class WorldPortal : MonoBehaviour, IInteractable, IInteractionTargetInfo, IInteractionRangeProvider
    {
        [SerializeField] private string _interactionId = "portal";
        [SerializeField] private string _displayName = "传送门";
        [SerializeField] private string _actionLabel = "进入";
        [SerializeField, Min(1)] private int _priority = 80;
        [SerializeField, Min(.5f)] private float _interactionRange = 2.2f;
        [SerializeField] private Transform _interactionPoint;
        private Action _onInteract;

        private void Awake()
        {
            var settings = Resources.Load<WorldInteractionSettings>(
                "Settings/WorldInteractionSettings");
            if (settings != null)
            {
                _interactionRange = settings.PortalInteractionRange;
            }
        }

        public string InteractionId => _interactionId;
        public bool IsAvailable => _onInteract != null;
        public int Priority => _priority;
        public Vector3 InteractionPosition => _interactionPoint != null ? _interactionPoint.position : transform.position;
        public string DisplayName => _displayName;
        public string ActionLabel => _actionLabel;
        public float InteractionRange => _interactionRange;

        /// <summary>配置传送门显示信息和目标动作。</summary>
        public void Configure(string interactionId, string displayName, string actionLabel, Action onInteract)
        {
            _interactionId = interactionId;
            _displayName = displayName;
            _actionLabel = actionLabel;
            _onInteract = onInteract;
            _interactionPoint ??= transform;
            EnsureVisual();
        }

        /// <summary>创建简单的青色传送门标记，便于关卡调试和玩家定位。</summary>
        private void EnsureVisual()
        {
            if (transform.Find("PortalVisual") != null)
            {
                return;
            }

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "PortalVisual";
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = new Vector3(0f, .04f, 0f);
            visual.transform.localScale = new Vector3(1.4f, .04f, 1.4f);
            var collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = new Color(0.15f, .85f, 1f, .8f);
            visual.GetComponent<Renderer>().sharedMaterial = material;
        }

        public InteractResult Interact()
        {
            if (!IsAvailable)
            {
                return InteractResult.Unavailable("传送门当前不可用。");
            }

            _onInteract.Invoke();
            return InteractResult.Succeeded($"已进入{_displayName}");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
        }
    }
}
