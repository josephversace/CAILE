
using IIM.Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
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
        


        /// <summary>
        /// Check if a hash already exists and return the evidence
        /// </summary>
        Task<Evidence?> CheckDuplicateAsync(string hash, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get count of how many times this hash has been seen
        /// </summary>
        Task<int> GetDuplicateCountAsync(string hash, CancellationToken cancellationToken = default);

        /// <summary>
        /// Register a new hash with its evidence ID
        /// </summary>
        Task RegisterHashAsync(string hash, string evidenceId, CancellationToken cancellationToken = default);
    }
}
