using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Luban;
using UnityEngine;
using UnityEngine.Networking;

namespace Train.Composition.Config
{
    /// <summary>
    /// 读取 Luban 生成的二进制表，并把生成的 cfg.Tables 安装到游戏服务层。
    /// </summary>
    public sealed class LubanConfigService : ILubanConfigService, IDisposable
    {
        private static readonly string[] TableNames =
        {
            "game_tbitem",
            "game_tbequipment",
            "game_tbenemyarchetype",
            "game_tblevel",
            "game_tblevelspawn",
            "game_tbreward"
        };

        private readonly Dictionary<string, byte[]> _buffers = new();
        private bool _disposed;

        /// <inheritdoc />
        public bool IsReady => Tables != null && !_disposed;

        /// <inheritdoc />
        public cfg.Tables Tables { get; private set; }

        /// <inheritdoc />
        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (IsReady)
            {
                return;
            }

            foreach (var tableName in TableNames)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _buffers[tableName] = await LoadTableBytesAsync(
                    tableName,
                    cancellationToken);
            }

            Tables = new cfg.Tables(
                tableName =>
                {
                    if (!_buffers.TryGetValue(tableName, out var bytes))
                    {
                        throw new InvalidOperationException(
                            $"Luban table '{tableName}' was not loaded.");
                    }

                    return ByteBuf.Wrap(bytes);
                });
        }

        /// <summary>按平台读取 StreamingAssets 中的二进制表。</summary>
        private static async Task<byte[]> LoadTableBytesAsync(
            string tableName,
            CancellationToken cancellationToken)
        {
            var path = Path.Combine(
                Application.streamingAssetsPath,
                "Config",
                "LubanBytes",
                tableName + ".bytes");

            if (Application.platform != RuntimePlatform.Android &&
                !path.StartsWith("jar:", StringComparison.OrdinalIgnoreCase))
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException(
                        $"Luban table file was not found: {path}",
                        path);
                }

                return await Task.Run(
                    () => File.ReadAllBytes(path),
                    cancellationToken);
            }

            using var request = UnityWebRequest.Get(path);
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException(
                    $"Failed to load Luban table '{tableName}': {request.error}");
            }

            return request.downloadHandler.data;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _buffers.Clear();
            Tables = null;
            _disposed = true;
        }

        /// <summary>拒绝释放后继续使用配置服务。</summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LubanConfigService));
            }
        }
    }
}

