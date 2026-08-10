using System;

namespace Train.Architecture.Assets
{
    /// <summary>
    /// 资源服务统一使用的异常类型，便于上层按类型捕获加载错误。
    /// </summary>
    public sealed class AssetServiceException : Exception
    {
        public AssetServiceException(string message)
            : base(message)
        {
        }

        public AssetServiceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
