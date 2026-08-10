using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Train.Quest.Core
{
    /// <summary>
    /// 保存整项任务在某一时刻的不可变只读视图。
    /// </summary>
    public sealed class QuestProgressSnapshot
    {
        private readonly ReadOnlyCollection<QuestObjectiveProgressSnapshot>
            _objectives;

        internal QuestProgressSnapshot(
            string questId,
            string title,
            QuestStatus status,
            long revision,
            QuestObjectiveProgressSnapshot[] objectives)
        {
            QuestId = questId ??
                throw new ArgumentNullException(nameof(questId));
            Title = title ??
                throw new ArgumentNullException(nameof(title));
            Status = status;
            Revision = revision;

            if (objectives == null)
            {
                throw new ArgumentNullException(nameof(objectives));
            }

            _objectives = Array.AsReadOnly(
                (QuestObjectiveProgressSnapshot[])objectives.Clone());
        }

        public string QuestId { get; }

        public string Title { get; }

        public QuestStatus Status { get; }

        public long Revision { get; }

        public IReadOnlyList<QuestObjectiveProgressSnapshot> Objectives =>
            _objectives;

        public QuestObjectiveProgressSnapshot GetObjective(string objectiveId)
        {
            if (string.IsNullOrWhiteSpace(objectiveId))
            {
                throw new ArgumentException(
                    "Objective id cannot be null, empty, or whitespace.",
                    nameof(objectiveId));
            }

            for (var i = 0; i < _objectives.Count; i++)
            {
                var objective = _objectives[i];
                if (string.Equals(
                        objective.ObjectiveId,
                        objectiveId,
                        StringComparison.Ordinal))
                {
                    return objective;
                }
            }

            throw new KeyNotFoundException(
                $"Quest '{QuestId}' does not contain objective '{objectiveId}'.");
        }
    }
}
