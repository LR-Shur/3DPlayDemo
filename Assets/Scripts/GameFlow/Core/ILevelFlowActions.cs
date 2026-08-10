using System.Threading;
using System.Threading.Tasks;

namespace Train.GameFlow.Core
{
    /// <summary>
    /// 纯流程核心用于请求场景具体工作的端口。
    /// MonoBehaviour 适配器可以实现此接口，而不会让 Unity 依赖反向进入状态机。
    /// </summary>
    public interface ILevelFlowActions
    {
        /// <summary>执行关卡资源与参战对象的准备工作。</summary>
        Task PrepareAsync(CancellationToken cancellationToken);

        /// <summary>播放或等待关卡开场流程。</summary>
        Task PlayIntroAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 启用或禁用本关敌人战斗逻辑。
        /// 关卡适配器可以在结果阶段保留玩家探索输入。
        /// </summary>
        Task SetCombatEnabledAsync(
            bool enabled,
            CancellationToken cancellationToken);

        /// <summary>展示通关或失败结果。</summary>
        Task PresentResultAsync(
            LevelOutcome outcome,
            CancellationToken cancellationToken);

        /// <summary>执行离开关卡所需的清理或切换工作。</summary>
        Task ExitAsync(CancellationToken cancellationToken);
    }
}
