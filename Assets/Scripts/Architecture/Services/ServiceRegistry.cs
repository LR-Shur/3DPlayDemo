using System;
using System.Collections.Generic;

namespace Train.Architecture.Services
{
    /// <summary>
    /// 按接口注册和解析服务的注册表，并统一释放已安装的可释放服务。
    /// </summary>
    public sealed class ServiceRegistry : IServiceRegistry, IDisposable
    {
        private readonly Dictionary<Type, object> _services = new();
        private readonly List<IDisposable> _ownedDisposables = new();
        private bool _disposed;

        public void Install<TService>(TService service)
            where TService : class
        {
            ThrowIfDisposed();
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            var serviceType = typeof(TService);
            if (_services.TryGetValue(serviceType, out var existing))
            {
                if (ReferenceEquals(existing, service))
                {
                    return;
                }

                throw new InvalidOperationException(
                    $"Service '{serviceType.FullName}' is already installed.");
            }

            _services.Add(serviceType, service);
            if (service is IDisposable disposable &&
                !ContainsReference(_ownedDisposables, disposable))
            {
                _ownedDisposables.Add(disposable);
            }
        }

        public bool TryResolve<TService>(out TService service)
            where TService : class
        {
            ThrowIfDisposed();
            if (_services.TryGetValue(typeof(TService), out var stored))
            {
                service = (TService)stored;
                return true;
            }

            service = null;
            return false;
        }

        public TService Resolve<TService>()
            where TService : class
        {
            if (TryResolve<TService>(out var service))
            {
                return service;
            }

            throw new InvalidOperationException(
                $"Service '{typeof(TService).FullName}' is not installed.");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            for (var index = _ownedDisposables.Count - 1; index >= 0; index--)
            {
                _ownedDisposables[index].Dispose();
            }

            _ownedDisposables.Clear();
            _services.Clear();
            _disposed = true;
        }

        private static bool ContainsReference(
            List<IDisposable> values,
            IDisposable candidate)
        {
            foreach (var value in values)
            {
                if (ReferenceEquals(value, candidate))
                {
                    return true;
                }
            }

            return false;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ServiceRegistry));
            }
        }
    }
}
