using System;
using Train.Dialogue.Core;
using UnityEngine;

namespace Train.Dialogue.Data
{
    /// <summary>
    /// 保存可在 Inspector 中编辑的一条对话条件，并转换为纯领域配置。
    /// </summary>
    [Serializable]
    public sealed class DialogueConditionDefinition
    {
        [SerializeField] private string _typeId;
        [SerializeField] private string _key;
        [SerializeField] private string _value;
        [SerializeField] private bool _negate;

        /// <summary>将 Unity 序列化数据转换为不可变条件配置。</summary>
        public DialogueConditionSpec ToCoreSpec()
        {
            return new DialogueConditionSpec(
                _typeId,
                _key,
                _value,
                _negate);
        }
    }
}
