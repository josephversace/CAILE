// IIM.Core/Storage/IDeduplicationService.cs
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography; // Add this
using System.Threading;
using System.Threading.Tasks;


namespace IIM.Core.Storage
{
    public interface IDeduplicationService
    {
        Task<DeduplicationResult> DeduplicateStreamAsync(
            Stream stream,
            int chunkSize,
            CancellationToken cancellationToken = default);

        Task<string> ComputeHashAsync(
            Stream stream,
            CancellationToken cancellationToken = default);
    }


    public class FixedSizeDeduplicationService : IDeduplicationService
    {
        private readonly ILogger<FixedSizeDeduplicationService> _logger;
        private readonly Dictionary<string, int> _chunkRefCount = new();

        public FixedSizeDeduplicationService(ILogger<FixedSizeDeduplicationService> logger)
        {
            _logger = logger;
        }

        public async Task<DeduplicationResult> DeduplicateStreamAsync(
            Stream stream,
            int chunkSize,
            CancellationToken cancellationToken = default)
        {
            var result = new DeduplicationResult();
            result.FileHash = await ComputeHashAsync(stream, cancellationToken);
            stream.Position = 0;
            result.TotalSize = stream.Length;

            var buffer = new byte[chunkSize];
            var offset = 0;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, chunkSize, cancellationToken)) > 0)
            {
                var chunkData = new byte[bytesRead];
                Array.Copy(buffer, chunkData, bytesRead);

                using var sha256 = SHA256.Create();
                var hash = BitConverter.ToString(sha256.ComputeHash(chunkData))
                    .Replace("-", "").ToLowerInvariant();

                var chunk = new ChunkData
                {
                    Hash = hash,
                    Data = chunkData,
                    Size = bytesRead,
                    Offset = offset
                };

                result.ChunkHashes.Add(hash);

                if (_chunkRefCount.ContainsKey(hash))
                {
                    _chunkRefCount[hash]++;
                    result.DuplicateChunks.Add(chunk);
                    result.BytesSaved += bytesRead;
                }
                else
                {
                    _chunkRefCount[hash] = 1;
                    result.UniqueChunks.Add(chunk);
                }

                offset += bytesRead;
            }

            result.DeduplicationRatio = result.BytesSaved > 0
                ? (double)result.BytesSaved / result.TotalSize
                : 0;

            _logger.LogInformation(
                "Deduplication: {TotalSize} bytes, {Saved} saved ({Ratio:P})",
                result.TotalSize, result.BytesSaved, result.DeduplicationRatio);

            return result;
        }

        public async Task<string> ComputeHashAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            using var sha256 = SHA256.Create();
            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
            }

            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return BitConverter.ToString(sha256.Hash).Replace("-", "").ToLowerInvariant();
        }
    }
}