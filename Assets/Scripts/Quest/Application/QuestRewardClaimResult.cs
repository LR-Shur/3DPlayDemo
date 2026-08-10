namespace Train.Quest.Application
{
    /// <summary>
    /// 返回一次任务奖励领取操作的结果，不依赖异常表达正常失败。
    /// </summary>
    public readonly struct QuestRewardClaimResult
    {
        private QuestRewardClaimResult(
            bool succeeded,
            string questId,
            QuestRewardClaimFailureReason failureReason)
        {
            Succeeded = succeeded;
            QuestId = questId ?? string.Empty;
            FailureReason = failureReason;
        }

        /// <summary>获取奖励是否已经完整发放并完成领取。</summary>
        public bool Succeeded { get; }

        /// <summary>获取本次操作对应的任务标识。</summary>
        public string QuestId { get; }

        /// <summary>获取失败原因；成功时为 None。</summary>
        public QuestRewardClaimFailureReason FailureReason { get; }

        /// <summary>创建一条成功结果。</summary>
        public static QuestRewardClaimResult Success(string questId)
        {
            return new QuestRewardClaimResult(
                true,
                questId,
                QuestRewardClaimFailureReason.None);
        }

        /// <summary>创建一条失败结果。</summary>
        public static QuestRewardClaimResult Failure(
            string questId,
            QuestRewardClaimFailureReason failureReason)
        {
            return new QuestRewardClaimResult(
                false,
                questId,
                failureReason);
        }
    }
}
