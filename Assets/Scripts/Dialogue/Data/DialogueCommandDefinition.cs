using System;
using Train.Dialogue.Core;
using UnityEngine;

namespace Train.Dialogue.Data
{
    /// <summary>
    /// 保存可在 Inspector 中编辑的一条对话命令，并转换为纯领域配置。
    /// </summary>
    [Serializable]
    public sealed class DialogueCommandDefinition
    {
        [SerializeField] private string _typeId;
        [SerializeField] private string _key;
        [SerializeField] private string _value;

        /// <summary>将 Unity 序列化数据转换为不可变命令配置。</summary>
        public DialogueCommandSpec ToCoreSpec()
        {
            return new DialogueCommandSpec(_typeId, _key, _value);
        }
    }
}
