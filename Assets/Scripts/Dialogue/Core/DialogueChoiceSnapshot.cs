using System;

namespace Train.Dialogue.Core
{
    /// <summary>
    /// 表示某一时刻可供玩家选择的不可变选项快照。
    /// </summary>
    public sealed class DialogueChoiceSnapshot
    {
        /// <summary>创建一个可见选项快照。</summary>
        public DialogueChoiceSnapshot(string choiceId, string text)
        {
            if (string.IsNullOrWhiteSpace(choiceId))
            {
                throw new ArgumentException(
                    "选项快照标识不能为空。",
                    nameof(choiceId));
            }

            ChoiceId = choiceId;
            Text = text ?? string.Empty;
        }

        /// <summary>获取选项稳定标识。</summary>
        public string ChoiceId { get; }

        /// <summary>获取选项展示文本。</summary>
        public string Text { get; }
    }
}
