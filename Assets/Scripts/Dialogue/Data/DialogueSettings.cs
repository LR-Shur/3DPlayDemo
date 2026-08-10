using System.Collections.Generic;
using UnityEngine;

namespace Train.Dialogue.Data
{
    /// <summary>
    /// 汇总运行时允许启动的对话资产目录。
    /// </summary>
    [CreateAssetMenu(
        menuName = "Train/Dialogue/Dialogue Settings",
        fileName = "DialogueSettings")]
    public sealed class DialogueSettings : ScriptableObject
    {
        [SerializeField]
        private List<DialogueDefinition> _dialogues = new();

        /// <summary>获取只读对话资产目录。</summary>
        public IReadOnlyList<DialogueDefinition> Dialogues => _dialogues;
    }
}
