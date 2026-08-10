using System.Threading;
using System.Threading.Tasks;

namespace Train.Composition.Config
{
    /// <summary>
    /// Luban 配置服务接口；上层只依赖只读表访问，不关心文件来源和生成工具。
    /// </summary>
    public interface ILubanConfigService
    {
        /// <summary>配置是否已经完成加载。</summary>
        bool IsReady { get; }

        /// <summary>生成的 Luban 表集合。</summary>
        cfg.Tables Tables { get; }

        /// <summary>异步加载并校验所有客户端配置表。</summary>
        Task InitializeAsync(CancellationToken cancellationToken);
    }
}

