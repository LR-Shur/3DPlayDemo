using System.Collections.Generic;

namespace Train.Composition.Progression
{
    /// <summary>
    /// 跨关卡成长服务：金币、关卡进度和下一节点入口都由这里统一管理。
    /// </summary>
    public interface IProgressionService
    {
        RunProgressSnapshot Snapshot { get; }

        IReadOnlyList<RunNode> Nodes { get; }

        void AddCoins(int amount, string reason);

        bool TrySpendCoins(int amount, string reason);

        bool CompleteLevel(string levelId, out RunNode nextNode);

        bool TryGetNode(string levelId, out RunNode node);
    }
}
