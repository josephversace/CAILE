using IIM.Shared.Models.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.AI.DataEnrichment.Services
{
    /// <summary>
    /// Service responsible for processing data queries and generating insights
    /// </summary>
    public interface IDataQueryService
    {
        Task<QueryResult> ProcessQueryAsync(string query, Guid? workspaceId = null, CancellationToken cancellationToken = default);
        Task<DataInsight> GenerateInsightAsync(string question, Guid? workspaceId = null, CancellationToken cancellationToken = default);
        Task<SimilarityResult> FindSimilarFilesAsync(Guid virtualFileId, int maxResults = 10, CancellationToken cancellationToken = default);
    }
}