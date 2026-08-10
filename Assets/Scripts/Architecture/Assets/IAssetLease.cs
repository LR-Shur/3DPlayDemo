using System;
using UnityEngine;

namespace Train.Architecture.Assets
{
    /// <summary>
    /// 表示一份已加载资源的生命周期所有权，释放时归还给资源服务。
    /// </summary>
    public interface IAssetLease<out TAsset> : IDisposable
        where TAsset : UnityEngine.Object
    {
        string Location { get; }
        TAsset Asset { get; }
        bool IsValid { get; }
    }

    /// <summary>
    /// 表示一份已实例化场景对象的生命周期所有权。
    /// </summary>
    public interface IInstanceLease : IDisposable
    {
        string Location { get; }
        GameObject Instance { get; }
        bool IsValid { get; }
    }
}
