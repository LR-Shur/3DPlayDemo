using System.Collections.Generic;
using Train.Dialogue.Core;
using Train.Dialogue.Data;

namespace Train.Dialogue.Application
{
    /// <summary>
    /// 向表现层和玩法层提供单活动会话的对话用例接口。
    /// </summary>
    public interface IDialogueService
    {
        /// <summary>获取当前会话快照；尚未启动过对话时为空。</summary>
        DialogueSessionSnapshot Current { get; }

        /// <summary>获取当前是否存在尚未结束的活动会话。</summary>
        bool IsActive { get; }

        /// <summary>获取全部可启动对话资产的只读目录。</summary>
        IReadOnlyList<DialogueDefinition> Catalog { get; }

        /// <summary>按稳定标识查询对话资产。</summary>
        bool TryGetDefinition(
            string dialogueId,
            out DialogueDefinition definition);

        /// <summary>尝试启动指定对话；未知标识或已有活动会话时返回 false。</summary>
        bool Start(string dialogueId);

        /// <summary>尝试继续当前台词；状态不匹配时返回 false。</summary>
        bool Continue();

        /// <summary>尝试提交当前可见选项；选项无效时返回 false。</summary>
        bool Choose(string choiceId);

        /// <summary>尝试取消当前活动会话；没有活动会话时返回 false。</summary>
        bool Cancel();
    }
}
