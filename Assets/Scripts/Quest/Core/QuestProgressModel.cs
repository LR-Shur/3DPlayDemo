using System;

namespace Train.Quest.Core
{
    /// <summary>
    /// 表示一项任务实例的纯 C# 领域进度模型。
    ///
    /// Activate 与 Claim 会拒绝非法状态转换；事实与任务无关或任务未激活时，
    /// ApplyFact 返回 false。每个成功命令只增加一次修订号并发布一次 Changed。
    /// </summary>
    public sealed class QuestProgressModel
    {
        private readonly QuestSpec _spec;
        private readonly int[] _objectiveAmounts;

        public QuestProgressModel(QuestSpec spec)
        {
            _spec = spec ?? throw new ArgumentNullException(nameof(spec));
            _objectiveAmounts = new int[spec.Objectives.Count];
            Status = QuestStatus.Inactive;
        }

        public event EventHandler<QuestProgressChangedEventArgs> Changed;

        public QuestSpec Spec => _spec;

        public QuestStatus Status { get; private set; }

        public long Revision { get; private set; }

        public bool CanClaim => Status == QuestStatus.Completed;

        public QuestProgressSnapshot Snapshot => CreateSnapshot();

        public void Activate()
        {
            if (Status != QuestStatus.Inactive)
            {
                throw new InvalidOperationException(
                    $"Quest '{_spec.Id}' cannot be activated while its status " +
                    $"is {Status}. A quest can only be activated once.");
            }

            var oldSnapshot = CreateSnapshot();
            var nextRevision = GetNextRevision();

            Status = QuestStatus.Active;
            CommitChange(oldSnapshot, nextRevision);
        }

        public bool ApplyFact(QuestFact fact)
        {
            fact.Validate();

            if (Status != QuestStatus.Active)
            {
                return false;
            }

            var hasChange = false;
            var nextAmounts = (int[])_objectiveAmounts.Clone();

            for (var i = 0; i < _spec.Objectives.Count; i++)
            {
                var objective = _spec.Objectives[i];
                if (objective.Type != fact.Type ||
                    !string.Equals(
                        objective.TargetId,
                        fact.TargetId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                var oldAmount = nextAmounts[i];
                if (oldAmount >= objective.RequiredAmount)
                {
                    continue;
                }

                var increasedAmount = (long)oldAmount + fact.Amount;
                nextAmounts[i] = increasedAmount >= objective.RequiredAmount
                    ? objective.RequiredAmount
                    : (int)increasedAmount;
                hasChange = true;
            }

            if (!hasChange)
            {
                return false;
            }

            var oldSnapshot = CreateSnapshot();
            var nextRevision = GetNextRevision();
            Array.Copy(
                nextAmounts,
                _objectiveAmounts,
                _objectiveAmounts.Length);

            if (AreAllObjectivesCompleted())
            {
                Status = QuestStatus.Completed;
            }

            CommitChange(oldSnapshot, nextRevision);
            return true;
        }

        public void Claim()
        {
            if (Status == QuestStatus.Claimed)
            {
                throw new InvalidOperationException(
                    $"Quest '{_spec.Id}' rewards have already been claimed.");
            }

            if (Status != QuestStatus.Completed)
            {
                throw new InvalidOperationException(
                    $"Quest '{_spec.Id}' cannot be claimed while its status is " +
                    $"{Status}. Complete every objective first.");
            }

            var oldSnapshot = CreateSnapshot();
            var nextRevision = GetNextRevision();

            Status = QuestStatus.Claimed;
            CommitChange(oldSnapshot, nextRevision);
        }

        public QuestProgressSnapshot CreateSnapshot()
        {
            var objectives = new QuestObjectiveProgressSnapshot[
                _spec.Objectives.Count];

            for (var i = 0; i < _spec.Objectives.Count; i++)
            {
                var definition = _spec.Objectives[i];
                objectives[i] = new QuestObjectiveProgressSnapshot(
                    definition.Id,
                    definition.Type,
                    definition.TargetId,
                    _objectiveAmounts[i],
                    definition.RequiredAmount);
            }

            return new QuestProgressSnapshot(
                _spec.Id,
                _spec.Title,
                Status,
                Revision,
                objectives);
        }

        private bool AreAllObjectivesCompleted()
        {
            for (var i = 0; i < _spec.Objectives.Count; i++)
            {
                if (_objectiveAmounts[i] <
                    _spec.Objectives[i].RequiredAmount)
                {
                    return false;
                }
            }

            return true;
        }

        private long GetNextRevision()
        {
            return checked(Revision + 1);
        }

        private void CommitChange(
            QuestProgressSnapshot oldSnapshot,
            long nextRevision)
        {
            Revision = nextRevision;
            var newSnapshot = CreateSnapshot();
            Changed?.Invoke(
                this,
                new QuestProgressChangedEventArgs(
                    oldSnapshot,
                    newSnapshot));
        }
    }
}
