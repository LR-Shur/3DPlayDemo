using System;

namespace Train.WorldInteraction.Core
{
    /// <summary>
    /// 一次交互的不可变返回值。
    /// 核心层只表达业务结果，不直接弹 UI、播放音效或修改背包。
    /// </summary>
    public readonly struct InteractResult
    {
        /// <summary>
        /// 创建一份交互结果。
        /// </summary>
        public InteractResult(
            InteractResultCode code,
            string message = null)
        {
            if (!Enum.IsDefined(
                    typeof(InteractResultCode),
                    code))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(code),
                    code,
                    "未知的交互结果类型。");
            }

            Code = code;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// 业务结果类型。
        /// </summary>
        public InteractResultCode Code { get; }

        /// <summary>
        /// 可选的领域提示文本；正式 UI 也可以只使用 Code 做本地化映射。
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 此次交互是否成功。
        /// </summary>
        public bool IsSuccess =>
            Code == InteractResultCode.Success;

        /// <summary>
        /// 创建成功结果。
        /// </summary>
        public static InteractResult Succeeded(
            string message = null)
        {
            return new InteractResult(
                InteractResultCode.Success,
                message);
        }

        /// <summary>
        /// 创建背包已满结果。
        /// </summary>
        public static InteractResult InventoryFull(
            string message = null)
        {
            return new InteractResult(
                InteractResultCode.InventoryFull,
                message);
        }

        /// <summary>
        /// 创建目标不可用结果。
        /// </summary>
        public static InteractResult Unavailable(
            string message = null)
        {
            return new InteractResult(
                InteractResultCode.Unavailable,
                message);
        }

        /// <summary>
        /// 创建无焦点候选结果。
        /// </summary>
        public static InteractResult NoFocusedCandidate(
            string message = null)
        {
            return new InteractResult(
                InteractResultCode.NoFocusedCandidate,
                message);
        }

        /// <summary>
        /// 创建通用失败结果。
        /// </summary>
        public static InteractResult Failed(
            string message = null)
        {
            return new InteractResult(
                InteractResultCode.Failed,
                message);
        }

        /// <summary>
        /// 创建主动取消结果。
        /// </summary>
        public static InteractResult Cancelled(
            string message = null)
        {
            return new InteractResult(
                InteractResultCode.Cancelled,
                message);
        }
    }
}
