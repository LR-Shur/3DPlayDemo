using System;
using System.Collections.Generic;

namespace Train.WorldInteraction.Core
{
    /// <summary>
    /// 管理一个玩家附近交互候选及当前焦点的纯领域模型。
    /// 选择顺序固定为优先级高、距离近、稳定 ID 字典序靠前。
    /// </summary>
    public sealed class InteractionFocusModel
    {
        private readonly Dictionary<
            string,
            InteractionCandidate> _candidates =
            new Dictionary<
                string,
                InteractionCandidate>(
                StringComparer.Ordinal);

        private InteractionCandidate? _focusedCandidate;

        /// <summary>
        /// 当前焦点身份发生变化时触发。
        /// </summary>
        public event EventHandler<
            InteractionFocusChangedEventArgs> FocusChanged;

        /// <summary>
        /// 玩家附近已登记的候选数量，其中可以包含暂时不可用的目标。
        /// </summary>
        public int CandidateCount =>
            _candidates.Count;

        /// <summary>
        /// 候选集合或焦点语义状态每次成功改变后递增的版本号。
        /// </summary>
        public long Revision { get; private set; }

        /// <summary>
        /// 当前是否存在可交互焦点。
        /// </summary>
        public bool HasFocus =>
            _focusedCandidate.HasValue;

        /// <summary>
        /// 报告一个目标进入玩家交互范围。
        /// 相同实例的重复进入会被忽略；相同 ID 的不同实例会明确失败。
        /// </summary>
        /// <returns>候选确实新增时返回 true。</returns>
        public bool Enter(InteractionCandidate candidate)
        {
            ValidateCandidate(candidate);

            if (_candidates.TryGetValue(
                    candidate.InteractionId,
                    out var existing))
            {
                if (ReferenceEquals(
                        existing.Interactable,
                        candidate.Interactable))
                {
                    return false;
                }

                throw new InvalidOperationException(
                    $"交互标识 '{candidate.InteractionId}' 已被另一个实例占用。" +
                    "场景内每个交互目标必须使用唯一稳定 ID。");
            }

            _candidates.Add(
                candidate.InteractionId,
                candidate);
            CommitCandidateMutation();
            return true;
        }

        /// <summary>
        /// 更新范围内候选的优先级或距离。
        /// Unity 适配层可在玩家移动后提交新的纯数值候选。
        /// </summary>
        /// <returns>目标存在且数据实际改变时返回 true。</returns>
        public bool Update(InteractionCandidate candidate)
        {
            ValidateCandidate(candidate);
            if (!_candidates.TryGetValue(
                    candidate.InteractionId,
                    out var existing))
            {
                return false;
            }

            if (!ReferenceEquals(
                    existing.Interactable,
                    candidate.Interactable))
            {
                throw new InvalidOperationException(
                    $"交互标识 '{candidate.InteractionId}' 对应的实例不一致，" +
                    "不能用另一个目标覆盖已有候选。");
            }

            if (existing.Priority == candidate.Priority &&
                existing.Distance.Equals(candidate.Distance))
            {
                return false;
            }

            _candidates[candidate.InteractionId] =
                candidate;
            CommitCandidateMutation();
            return true;
        }

        /// <summary>
        /// 报告一个目标离开玩家交互范围。
        /// </summary>
        /// <returns>找到并移除候选时返回 true。</returns>
        public bool Exit(string interactionId)
        {
            ValidateInteractionId(interactionId);
            if (!_candidates.Remove(interactionId))
            {
                return false;
            }

            CommitCandidateMutation();
            return true;
        }

        /// <summary>
        /// 清空玩家附近的全部候选。
        /// </summary>
        /// <returns>清空前存在候选时返回 true。</returns>
        public bool Clear()
        {
            if (_candidates.Count == 0)
            {
                return false;
            }

            _candidates.Clear();
            CommitCandidateMutation();
            return true;
        }

        /// <summary>
        /// 在目标可用性由外部条件改变后重新计算焦点。
        /// </summary>
        /// <returns>焦点身份发生变化时返回 true。</returns>
        public bool Refresh()
        {
            var previous = _focusedCandidate;
            var next = FindBestCandidate();
            _focusedCandidate = next;

            if (HasSameIdentity(previous, next))
            {
                return false;
            }

            Revision = checked(Revision + 1);
            PublishFocusChanged(previous, next);
            return true;
        }

        /// <summary>
        /// 尝试获取当前焦点候选。
        /// 若目标可用性由外部改变，应先调用 Refresh。
        /// </summary>
        public bool TryGetFocusedCandidate(
            out InteractionCandidate candidate)
        {
            if (_focusedCandidate.HasValue)
            {
                candidate =
                    _focusedCandidate.Value;
                return true;
            }

            candidate = default;
            return false;
        }

        /// <summary>
        /// 尝试按稳定 ID 获取一个范围内候选。
        /// </summary>
        public bool TryGetCandidate(
            string interactionId,
            out InteractionCandidate candidate)
        {
            ValidateInteractionId(interactionId);
            return _candidates.TryGetValue(
                interactionId,
                out candidate);
        }

        /// <summary>
        /// 与当前最佳候选交互。
        /// 执行前后都会刷新焦点，使一次性拾取物失效后能自动切换到下一个目标。
        /// </summary>
        public InteractResult InteractFocused()
        {
            Refresh();
            if (!_focusedCandidate.HasValue)
            {
                return InteractResult.NoFocusedCandidate(
                    "附近没有可交互目标。");
            }

            var target =
                _focusedCandidate.Value.Interactable;
            var result = target.Interact();
            Refresh();
            return result;
        }

        /// <summary>
        /// 创建附近候选的时间点快照。
        /// 快照按当前选择顺序排列，不会被后续 Enter、Update 或 Exit 修改。
        /// </summary>
        public IReadOnlyList<InteractionCandidate>
            CreateCandidatesSnapshot()
        {
            var candidates =
                new InteractionCandidate[
                    _candidates.Count];
            var index = 0;
            foreach (var candidate in _candidates.Values)
            {
                candidates[index] = candidate;
                index++;
            }

            Array.Sort(
                candidates,
                CompareCandidates);
            return Array.AsReadOnly(candidates);
        }

        private void CommitCandidateMutation()
        {
            var previous = _focusedCandidate;
            Revision = checked(Revision + 1);
            var next = FindBestCandidate();
            _focusedCandidate = next;

            if (!HasSameIdentity(previous, next))
            {
                PublishFocusChanged(previous, next);
            }
        }

        private InteractionCandidate? FindBestCandidate()
        {
            InteractionCandidate? best = null;
            foreach (var candidate in _candidates.Values)
            {
                if (!candidate.IsAvailable)
                {
                    continue;
                }

                if (!best.HasValue ||
                    CompareCandidates(
                        candidate,
                        best.Value) < 0)
                {
                    best = candidate;
                }
            }

            return best;
        }

        private void PublishFocusChanged(
            InteractionCandidate? previous,
            InteractionCandidate? current)
        {
            FocusChanged?.Invoke(
                this,
                new InteractionFocusChangedEventArgs(
                    Revision,
                    previous,
                    current));
        }

        private static int CompareCandidates(
            InteractionCandidate left,
            InteractionCandidate right)
        {
            if (left.IsAvailable != right.IsAvailable)
            {
                return left.IsAvailable ? -1 : 1;
            }

            var priorityComparison =
                right.Priority.CompareTo(left.Priority);
            if (priorityComparison != 0)
            {
                return priorityComparison;
            }

            var distanceComparison =
                left.Distance.CompareTo(right.Distance);
            if (distanceComparison != 0)
            {
                return distanceComparison;
            }

            return string.CompareOrdinal(
                left.InteractionId,
                right.InteractionId);
        }

        private static bool HasSameIdentity(
            InteractionCandidate? left,
            InteractionCandidate? right)
        {
            if (!left.HasValue || !right.HasValue)
            {
                return left.HasValue == right.HasValue;
            }

            return string.Equals(
                left.Value.InteractionId,
                right.Value.InteractionId,
                StringComparison.Ordinal);
        }

        private static void ValidateCandidate(
            InteractionCandidate candidate)
        {
            if (candidate.Interactable == null)
            {
                throw new ArgumentException(
                    "不能登记默认或空交互候选。",
                    nameof(candidate));
            }

            ValidateInteractionId(
                candidate.InteractionId);
            if (float.IsNaN(candidate.Distance) ||
                float.IsInfinity(candidate.Distance) ||
                candidate.Distance < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(candidate),
                    candidate.Distance,
                    "交互距离必须是非负有限数。");
            }
        }

        private static void ValidateInteractionId(
            string interactionId)
        {
            if (string.IsNullOrWhiteSpace(interactionId))
            {
                throw new ArgumentException(
                    "交互目标稳定标识不能为空。",
                    nameof(interactionId));
            }
        }
    }
}
