using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Train.Quest.Core
{
    /// <summary>
    /// 保存任务领域模型使用的不可变任务规格。
    /// </summary>
    public sealed class QuestSpec
    {
        private readonly ReadOnlyCollection<QuestObjectiveSpec> _objectives;
        private readonly ReadOnlyCollection<QuestRewardSpec> _rewards;

        public QuestSpec(
            string id,
            string title,
            IEnumerable<QuestObjectiveSpec> objectives,
            IEnumerable<QuestRewardSpec> rewards = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    "Quest id cannot be null, empty, or whitespace.",
                    nameof(id));
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException(
                    "Quest title cannot be null, empty, or whitespace.",
                    nameof(title));
            }

            Id = id;
            Title = title;
            _objectives = CopyAndValidateObjectives(objectives);
            _rewards = CopyAndValidateRewards(rewards);
        }

        public string Id { get; }

        public string Title { get; }

        public IReadOnlyList<QuestObjectiveSpec> Objectives => _objectives;

        public IReadOnlyList<QuestRewardSpec> Rewards => _rewards;

        private static ReadOnlyCollection<QuestObjectiveSpec>
            CopyAndValidateObjectives(
                IEnumerable<QuestObjectiveSpec> objectives)
        {
            if (objectives == null)
            {
                throw new ArgumentNullException(nameof(objectives));
            }

            var copy = new List<QuestObjectiveSpec>();
            var ids = new HashSet<string>(StringComparer.Ordinal);

            foreach (var objective in objectives)
            {
                if (objective == null)
                {
                    throw new ArgumentException(
                        "Quest objectives cannot contain null entries.",
                        nameof(objectives));
                }

                if (!ids.Add(objective.Id))
                {
                    throw new ArgumentException(
                        $"Quest objective id '{objective.Id}' is duplicated.",
                        nameof(objectives));
                }

                copy.Add(objective);
            }

            if (copy.Count == 0)
            {
                throw new ArgumentException(
                    "A quest must contain at least one objective.",
                    nameof(objectives));
            }

            return copy.AsReadOnly();
        }

        private static ReadOnlyCollection<QuestRewardSpec>
            CopyAndValidateRewards(IEnumerable<QuestRewardSpec> rewards)
        {
            if (rewards == null)
            {
                return new List<QuestRewardSpec>().AsReadOnly();
            }

            var copy = new List<QuestRewardSpec>();
            foreach (var reward in rewards)
            {
                if (reward == null)
                {
                    throw new ArgumentException(
                        "Quest rewards cannot contain null entries.",
                        nameof(rewards));
                }

                copy.Add(reward);
            }

            return copy.AsReadOnly();
        }
    }
}
