using System;

namespace Train.WorldInteraction.Core
{
    /// <summary>
    /// 当前交互焦点身份发生变化时的事件数据。
    /// 候选距离更新但焦点仍是同一目标时不会触发此事件。
    /// </summary>
    public sealed class InteractionFocusChangedEventArgs :
        EventArgs
    {
        /// <summary>
        /// 创建焦点变化事件数据。
        /// </summary>
        public InteractionFocusChangedEventArgs(
            long revision,
            InteractionCandidate? previousCandidate,
            InteractionCandidate? currentCandidate)
        {
            Revision = revision;
            PreviousCandidate = previousCandidate;
            CurrentCandidate = currentCandidate;
        }

        /// <summary>
        /// 焦点变化提交后的候选集合版本号。
        /// </summary>
        public long Revision { get; }

        /// <summary>
        /// 变化前的焦点；此前无焦点时为 null。
        /// </summary>
        public InteractionCandidate? PreviousCandidate { get; }

        /// <summary>
        /// 变化后的焦点；当前无焦点时为 null。
        /// </summary>
        public InteractionCandidate? CurrentCandidate { get; }

        /// <summary>
        /// 变化前是否存在焦点。
        /// </summary>
        public bool HadPrevious =>
            PreviousCandidate.HasValue;

        /// <summary>
        /// 变化后是否存在焦点。
        /// </summary>
        public bool HasCurrent =>
            CurrentCandidate.HasValue;
    }
}
