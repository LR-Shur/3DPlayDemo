using UnityEngine;

namespace Train.WorldInteraction.Runtime
{
    /// <summary>
    /// 对话终端的 NPC 外观加载器。
    /// 保留原终端的交互组件，只在运行时把终端外壳替换为 Resources 中的人形模型。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DialogueNpcVisualLoader : MonoBehaviour
    {
        [SerializeField] private string _resourcePath = "NPC/Casual_Female";
        [SerializeField] private Vector3 _localPosition = new(0f, 0f, 0f);
        [SerializeField] private Vector3 _localEulerAngles = new(0f, 180f, 0f);
        [SerializeField, Min(0.01f)] private float _localScale = 1f;

        private GameObject _visualInstance;

        private void Awake()
        {
            HideTerminalVisuals();

            if (string.IsNullOrWhiteSpace(_resourcePath))
            {
                return;
            }

            var visualPrefab = Resources.Load<GameObject>(_resourcePath);
            if (visualPrefab == null)
            {
                Debug.LogError(
                    $"对话 NPC 模型不存在，请检查 Resources 路径：{_resourcePath}",
                    this);
                return;
            }

            _visualInstance = Instantiate(visualPrefab, transform, false);
            _visualInstance.name = "DialogueNpcVisual";
            _visualInstance.transform.localPosition = _localPosition;
            _visualInstance.transform.localRotation =
                Quaternion.Euler(_localEulerAngles);
            _visualInstance.transform.localScale =
                Vector3.one * _localScale;
        }

        private void OnDestroy()
        {
            if (_visualInstance != null)
            {
                Destroy(_visualInstance);
            }
        }

        /// <summary>隐藏旧的全息终端外观，但保留交互点和触发器。</summary>
        private void HideTerminalVisuals()
        {
            SetChildActive("BasePlate", false);
            SetChildActive("Pole", false);
            SetChildActive("HoloScreen", false);
            SetChildActive("TerminalLight", false);
        }

        private void SetChildActive(string childName, bool active)
        {
            var child = transform.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(active);
            }
        }
    }
}
