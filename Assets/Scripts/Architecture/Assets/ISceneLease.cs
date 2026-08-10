using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Train.Architecture.Assets
{
    /// <summary>
    /// 表示一个已加载场景的租约，释放时卸载该场景。
    /// </summary>
    public interface ISceneLease : IDisposable
    {
        string Location { get; }
        Scene Scene { get; }
        bool IsValid { get; }
        Task UnloadAsync();
    }
}
