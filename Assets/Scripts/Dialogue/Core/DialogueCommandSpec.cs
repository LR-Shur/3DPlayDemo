using System;

namespace Train.Dialogue.Core
{
    /// <summary>
    /// 保存一条不可变的命令配置，由 TypeId 路由到对应命令处理器。
    /// </summary>
    public sealed class DialogueCommandSpec
    {
        /// <summary>创建一条命令配置。</summary>
        public DialogueCommandSpec(
            string typeId,
            string key,
            string value = null)
        {
            if (string.IsNullOrWhiteSpace(typeId))
            {
                throw new ArgumentException(
                    "对话命令类型标识不能为空。",
                    nameof(typeId));
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(
                    "对话命令参数键不能为空。",
                    nameof(key));
            }

            TypeId = typeId;
            Key = key;
            Value = value ?? string.Empty;
        }

        /// <summary>获取命令处理器的稳定类型标识。</summary>
        public string TypeId { get; }

        /// <summary>获取命令写入或操作使用的参数键。</summary>
        public string Key { get; }

        /// <summary>获取命令写入或操作使用的参数值。</summary>
        public string Value { get; }
    }
}
