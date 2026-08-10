using System;
using System.Collections.Generic;

namespace Train.Dialogue.Core
{
    /// <summary>
    /// 提供纯内存变量表实现，适合单机流程、测试以及被存档层包装。
    /// </summary>
    public sealed class DialogueVariableStore : IDialogueVariableStore
    {
        private readonly Dictionary<string, string> _values =
            new(StringComparer.Ordinal);

        /// <inheritdoc />
        public bool TryGetValue(string key, out string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                value = null;
                return false;
            }

            return _values.TryGetValue(key, out value);
        }

        /// <inheritdoc />
        public void SetValue(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(
                    "对话变量键不能为空。",
                    nameof(key));
            }

            _values[key] = value ?? string.Empty;
        }
    }
}
