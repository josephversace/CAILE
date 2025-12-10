using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.AI.DataEnrichment.Services
{
    /// <summary>
    /// Service responsible for analyzing file content and extracting metadata
    /// </summary>
    public interface IContentAnalysisService
    {
        Task<ContentAnalysis> AnalyzeContentAsync(Stream content, string fileName, string mimeType, CancellationToken cancellationToken = default);
        Task<EntityExtractionResult> ExtractEntitiesAsync(Stream content, string mimeType, CancellationToken cancellationToken = default);
        Task<float[]> GenerateEmbeddingsAsync(string text, CancellationToken cancellationToken = default);
        Task<string> ExtractTextAsync(Stream content, string mimeType, CancellationToken cancellationToken = default);
        Task<Dictionary<string, object>> ExtractMetadataAsync(Stream content, string fileName, string mimeType, CancellationToken cancellationToken = default);
    }
}