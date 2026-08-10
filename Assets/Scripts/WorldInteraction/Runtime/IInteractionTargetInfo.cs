using UnityEngine;

namespace Train.WorldInteraction.Runtime
{
    /// <summary>
    /// 提供世界交互目标展示信息与扫描坐标的通用接口。
    /// 拾取物、对话终端、机关等场景组件可以实现此接口，
    /// 让交互控制器不必为每个目标类型单独写分支。
    /// </summary>
    public interface IInteractionTargetInfo
    {
        /// <summary>获取候选选择优先级，数值越大越优先。</summary>
        int Priority { get; }

        /// <summary>获取用于计算玩家距离的世界坐标。</summary>
        Vector3 InteractionPosition { get; }

        /// <summary>获取 HUD 上显示的目标名称。</summary>
        string DisplayName { get; }

        /// <summary>获取 HUD 上显示的交互动作标签。</summary>
        string ActionLabel { get; }
    }
}
