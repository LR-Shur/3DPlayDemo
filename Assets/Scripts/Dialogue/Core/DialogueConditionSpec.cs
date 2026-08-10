using System;

namespace Train.Dialogue.Core
{
    /// <summary>
    /// 保存一条不可变的条件配置，由 TypeId 路由到对应条件处理器。
    /// </summary>
    public sealed class DialogueConditionSpec
    {
        /// <summary>创建一条条件配置。</summary>
        public DialogueConditionSpec(
            string typeId,
            string key,
            string value = null,
            bool negate = false)
        {
            if (string.IsNullOrWhiteSpace(typeId))
            {
                throw new ArgumentException(
                    "对话条件类型标识不能为空。",
                    nameof(typeId));
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(
                    "对话条件参数键不能为空。",
                    nameof(key));
            }

            TypeId = typeId;
            Key = key;
            Value = value ?? string.Empty;
            Negate = negate;
        }

        /// <summary>获取条件处理器的稳定类型标识。</summary>
        public string TypeId { get; }

        /// <summary>获取条件查询使用的参数键。</summary>
        public string Key { get; }

        /// <summary>获取条件比较使用的参数值。</summary>
        public string Value { get; }

        /// <summary>获取是否应反转处理器的原始判断结果。</summary>
        public bool Negate { get; }
    }
}
