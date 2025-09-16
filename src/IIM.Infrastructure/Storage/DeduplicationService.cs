// IIM.Core/Storage/IDeduplicationService.cs
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography; // Add this
using System.Threading;
using System.Threading.Tasks;



namespace IIM.Infrastructure.Storage
{
    public class DeduplicationService : IDeduplicationService
    {
        private readonly Dictionary<string, List<string>> _hashToEvidenceIds = new();
        private readonly IWorkspaceProvider _workspaceProvider;
        private readonly ILogger<DeduplicationService> _logger;
        private readonly Dictionary<string, int> _chunkRefCount = new();

        public DeduplicationService(ILogger<DeduplicationService> logger, IWorkspaceProvider workspaceProvider)
        {
            _logger = logger;
            _workspaceProvider = workspaceProvider;
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

        public async Task<bool> IsDuplicateAsync(VirtualFile file, CancellationToken cancellationToken = default)
        {
            // Check if StoredFile with this hash already exists
            return await _workspaceProvider.StoredFileExistsAsync(file.StoredFileHash, cancellationToken);
        }

        // Fix Line 106: Change ManagedFile to VirtualFile  
        public async Task<List<VirtualFile>> FindDuplicatesAsync(VirtualFile targetFile, CancellationToken cancellationToken = default)
        {
            var storedFile = await _workspaceProvider.GetStoredFileByHashAsync(targetFile.StoredFileHash, cancellationToken);
            if (storedFile?.VirtualFiles != null)
            {
                return storedFile.VirtualFiles.Where(vf => vf.Id != targetFile.Id).ToList();
            }
            return new List<VirtualFile>();
        }

        /// <summary>
        /// Register a new hash with its file ID
        /// </summary>
        public async Task RegisterHashAsync(string hash, string evidenceId, CancellationToken cancellationToken = default)
        {
            if (!_hashToEvidenceIds.ContainsKey(hash))
            {
                _hashToEvidenceIds[hash] = new List<string>();
            }

            _hashToEvidenceIds[hash].Add(evidenceId);

            _logger.LogInformation("Registered hash {Hash} for evidence {EvidenceId}", hash, evidenceId);

            await Task.CompletedTask;
        }

        public Task<StoredFile?> CheckDuplicateAsync(string hash, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetDuplicateCountAsync(string hash, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}

   
