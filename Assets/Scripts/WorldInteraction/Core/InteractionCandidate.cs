using System;

namespace Train.WorldInteraction.Core
{
    /// <summary>
    /// 玩家附近的一个交互候选。
    /// 它组合目标、业务优先级和当前距离，但不包含 Unity Transform。
    /// </summary>
    public readonly struct InteractionCandidate
    {
        /// <summary>
        /// 创建一个交互候选。
        /// </summary>
        public InteractionCandidate(
            IInteractable interactable,
            int priority,
            float distance)
        {
            Interactable = interactable ??
                throw new ArgumentNullException(
                    nameof(interactable));

            if (string.IsNullOrWhiteSpace(
                    interactable.InteractionId))
            {
                throw new ArgumentException(
                    "交互目标的稳定标识不能为空。",
                    nameof(interactable));
            }

            if (float.IsNaN(distance) ||
                float.IsInfinity(distance) ||
                distance < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(distance),
                    distance,
                    "交互距离必须是非负有限数。");
            }

            InteractionId =
                interactable.InteractionId;
            Priority = priority;
            Distance = distance;
        }

        /// <summary>
        /// 实际可交互目标。
        /// </summary>
        public IInteractable Interactable { get; }

        /// <summary>
        /// 目标稳定唯一标识。
        /// </summary>
        public string InteractionId { get; }

        /// <summary>
        /// 业务优先级，数值越大越优先。
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// 玩家到目标的当前距离，数值越小越优先。
        /// </summary>
        public float Distance { get; }

        /// <summary>
        /// 目标当前是否可交互。
        /// </summary>
        public bool IsAvailable =>
            Interactable.IsAvailable;
    }
}
