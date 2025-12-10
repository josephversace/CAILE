using IIM.Application.AI.DataEnrichment.Helpers;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.AI.DataEnrichment.Services
{
    /// <summary>
    /// Implementation of data query service
    /// </summary>
    public class DataQueryService : IDataQueryService
    {
        private readonly ILogger<DataQueryService> _logger;
        private readonly IWorkspaceManager _workspaceProvider;
        private readonly AIPromptBuilder _promptBuilder;

        public DataQueryService(
            ILogger<DataQueryService> logger,
            IWorkspaceManager workspaceProvider,
            AIPromptBuilder promptBuilder)
        {
            _logger = logger;
            _workspaceProvider = workspaceProvider;
            _promptBuilder = promptBuilder;
        }

        public async Task<QueryResult> ProcessQueryAsync(string query, Guid? workspaceId = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing data query: '{Query}' for workspace {WorkspaceId}", query, workspaceId);

            var result = new QueryResult
            {
                Query = query
            };

            var startTime = DateTime.UtcNow;

            try
            {
                // Parse query intent
                var queryIntent = await ParseQueryIntentAsync(query, cancellationToken);

                // Find matching files
                var matchingFiles = await FindMatchingFilesAsync(queryIntent, workspaceId, cancellationToken);
                result.MatchingFiles = matchingFiles.ToList();
                result.TotalResults = result.MatchingFiles.Count;

                // Generate response
                result.GeneratedResponse = await GenerateQueryResponseAsync(query, result.MatchingFiles, cancellationToken);

                // Generate follow-ups
                result.SuggestedFollowups = await GenerateFollowupSuggestionsAsync(query, result.MatchingFiles, cancellationToken);

                // Build facets
                result.Facets = await BuildQueryFacetsAsync(result.MatchingFiles, cancellationToken);

                result.QueryTime = DateTime.UtcNow - startTime;

                _logger.LogInformation("Query processed in {Duration}ms, found {ResultCount} results",
                    result.QueryTime.Value.TotalMilliseconds, result.TotalResults);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing data query: '{Query}'", query);
                throw;
            }
        }

        public async Task<DataInsight> GenerateInsightAsync(string question, Guid? workspaceId = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating data insight for question: '{Question}' in workspace {WorkspaceId}", question, workspaceId);

            var insight = new DataInsight
            {
                Question = question
            };

            var startTime = DateTime.UtcNow;

            try
            {
                // Analyze workspace data for insights
                var files = workspaceId.HasValue
                    ? await _workspaceProvider.GetVirtualFilesAsync(workspaceId.Value, cancellationToken)
                    : Enumerable.Empty<VirtualFile>();

                // Generate insight based on question and data
                insight.Insight = await GenerateInsightFromDataAsync(question, files, cancellationToken);

                // Extract supporting evidence
                insight.SupportingEvidence = await ExtractSupportingEvidenceAsync(question, files, cancellationToken);

                // Fix: Add missing Metrics property
                insight.Metrics = await GenerateInsightMetricsAsync(files, cancellationToken);

                // Fix: Add missing Recommendations property  
                insight.Recommendations = await GenerateInsightRecommendationsAsync(insight.Insight, files, cancellationToken);

                // Fix: Set both Confidence and ConfidenceScore
                insight.Confidence = CalculateInsightConfidence(insight);
                insight.ConfidenceScore = insight.Confidence;

                _logger.LogInformation("Generated insight for question '{Question}' with confidence {Confidence}",
                    question, insight.Confidence);

                return insight;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating insight for question: '{Question}'", question);
                throw;
            }
        }

        public async Task<SimilarityResult> FindSimilarFilesAsync(Guid virtualFileId, int maxResults = 10, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Finding similar files for {VirtualFileId}", virtualFileId);

            var startTime = DateTime.UtcNow;

            var result = new SimilarityResult
            {
                SourceFileId = virtualFileId
            };

            try
            {
                // TODO: Implement similarity search using embeddings
                // For now, return empty results
                result.SimilarFiles = new List<SimilarFile>();

                // Fix: Set missing SearchTime property
                result.SearchTime = DateTime.UtcNow - startTime;

                _logger.LogInformation("Found {Count} similar files for {VirtualFileId} in {Duration}ms",
                    result.SimilarFiles.Count, virtualFileId, result.SearchTime.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding similar files for {VirtualFileId}", virtualFileId);
                throw;
            }
        }

        private async Task<List<InsightMetric>> GenerateInsightMetricsAsync(IEnumerable<VirtualFile> files, CancellationToken cancellationToken)
        {
            var metrics = new List<InsightMetric>();

            var fileCount = files.Count();
            metrics.Add(new InsightMetric
            {
                Name = "TotalFiles",
                Value = fileCount,
                Unit = "count",
                Category = "Volume",
                Confidence = 1.0f
            });

            return metrics;
        }

        private async Task<List<string>> GenerateInsightRecommendationsAsync(string insight, IEnumerable<VirtualFile> files, CancellationToken cancellationToken)
        {
            var recommendations = new List<string>();

            // Generate basic recommendations based on data patterns
            var fileCount = files.Count();
            if (fileCount > 1000)
            {
                recommendations.Add("Consider implementing data archiving policies for large datasets");
            }

            return recommendations;
        }

        private float CalculateInsightConfidence(DataInsight insight)
        {
            var factors = new List<float>();

            if (!string.IsNullOrEmpty(insight.Insight))
            {
                factors.Add(0.8f);
            }

            if (insight.SupportingEvidence.Any())
            {
                factors.Add(0.9f);
            }

            if (insight.Metrics.Any())
            {
                factors.Add(0.7f);
            }

            return factors.Any() ? factors.Average() : 0.5f;
        }

      
        #region Private Methods

        private async Task<object> ParseQueryIntentAsync(string query, CancellationToken cancellationToken)
        {
            var keywords = query.ToLower()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToList();

            return new
            {
                Intent = "search",
                Keywords = keywords,
                IsTemporalQuery = keywords.Any(k => new[] { "recent", "today", "yesterday", "last", "this" }.Contains(k)),
                IsCountQuery = keywords.Any(k => new[] { "how", "many", "count", "number" }.Contains(k)),
                IsTypeQuery = keywords.Any(k => new[] { "type", "kind", "format" }.Contains(k))
            };
        }

        private async Task<IEnumerable<VirtualFile>> FindMatchingFilesAsync(object queryIntent, Guid? workspaceId, CancellationToken cancellationToken)
        {
            var files = workspaceId.HasValue
                ? await _workspaceProvider.GetVirtualFilesByWorkspaceAsync(workspaceId.Value, cancellationToken)
                : new List<VirtualFile>();

            // TODO: Implement sophisticated matching based on query intent
            return files;
        }

        private async Task<string> GenerateQueryResponseAsync(string query, List<VirtualFile> matchingFiles, CancellationToken cancellationToken)
        {
            if (!matchingFiles.Any())
            {
                return $"No files found matching your query: '{query}'";
            }

            var totalSize = matchingFiles.Sum(f => f.FileSize);
            return $"Found {matchingFiles.Count} files matching '{query}' with total size of {totalSize:N0} bytes.";
        }

        private async Task<List<string>> GenerateFollowupSuggestionsAsync(string query, List<VirtualFile> matchingFiles, CancellationToken cancellationToken)
        {
            var suggestions = new List<string>();

            if (matchingFiles.Any())
            {
                suggestions.Add("Show me the most recent files");
                suggestions.Add("Filter by file size");
                suggestions.Add("Group by file type");
            }
            else
            {
                suggestions.Add("Try a broader search term");
                suggestions.Add("Search in all workspaces");
            }

            return suggestions;
        }

        private async Task<Dictionary<string, List<string>>> BuildQueryFacetsAsync(List<VirtualFile> matchingFiles, CancellationToken cancellationToken)
        {
            var facets = new Dictionary<string, List<string>>();

            // Build file type facet
            var fileTypes = matchingFiles
                .GroupBy(f => Path.GetExtension(f.FileName).ToLower())
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .Select(g => g.Key)
                .ToList();
            facets["FileTypes"] = fileTypes;

            // Build date facet
            var dates = matchingFiles
                .GroupBy(f => f.CreatedAt.Date.ToString("yyyy-MM-dd"))
                .Select(g => g.Key)
                .OrderByDescending(d => d)
                .Take(10)
                .ToList();
            facets["Dates"] = dates;

            // Build size facet
            var sizes = matchingFiles
                .Select(f => GetSizeCategory(f.FileSize))
                .GroupBy(s => s)
                .Select(g => g.Key)
                .ToList();
            facets["Sizes"] = sizes;

            return facets;
        }
        private async Task<string> GetWorkspaceDataSummaryAsync(Guid? workspaceId, CancellationToken cancellationToken)
        {
            if (!workspaceId.HasValue)
                return "No workspace specified";

            var files = await _workspaceProvider.GetVirtualFilesByWorkspaceAsync(workspaceId.Value, cancellationToken);
            var filesList = files.ToList();

            if (!filesList.Any())
                return $"Workspace {workspaceId} contains no files";

            var totalSize = filesList.Sum(f => f.FileSize);
            return $"Workspace contains {filesList.Count} files with total size of {totalSize:N0} bytes";
        }

        private async Task<string> GenerateInsightFromDataAsync(string question, IEnumerable<VirtualFile> files, CancellationToken cancellationToken)
        {
            var fileCount = files.Count();
            var totalSize = files.Sum(f => f.FileSize);
            var avgSize = fileCount > 0 ? totalSize / fileCount : 0;

            // Generate basic insights based on data patterns
            var insights = new List<string>();

            if (fileCount == 0)
            {
                return "No data available to generate insights.";
            }

            insights.Add($"Dataset contains {fileCount} files with a total size of {FormatBytes(totalSize)}.");

            if (avgSize > 0)
            {
                insights.Add($"Average file size is {FormatBytes(avgSize)}.");
            }

            // Analyze file types
            var fileTypes = files
                .GroupBy(f => Path.GetExtension(f.FileName).ToLower())
                .OrderByDescending(g => g.Count())
                .Take(3)
                .ToList();

            if (fileTypes.Any())
            {
                var topType = fileTypes.First();
                insights.Add($"Most common file type is {topType.Key} ({topType.Count()} files).");
            }

            // Analyze creation dates
            var recentFiles = files.Where(f => f.CreatedAt > DateTime.UtcNow.AddDays(-30)).Count();
            if (recentFiles > 0)
            {
                insights.Add($"{recentFiles} files were created in the last 30 days.");
            }

            return string.Join(" ", insights);
        }

        private async Task<List<InsightMetric>> CalculateInsightMetricsAsync(string question, Guid? workspaceId, CancellationToken cancellationToken)
        {
            var metrics = new List<InsightMetric>();

            if (workspaceId.HasValue)
            {
                var files = await _workspaceProvider.GetVirtualFilesByWorkspaceAsync(workspaceId.Value, cancellationToken);
                var filesList = files.ToList();

                metrics.Add(new InsightMetric
                {
                    Name = "Total Files",
                    Value = filesList.Count,
                    Unit = "files",
                    Description = "Total number of files in workspace"
                });

                if (filesList.Any())
                {
                    metrics.Add(new InsightMetric
                    {
                        Name = "Total Size",
                        Value = filesList.Sum(f => f.FileSize),
                        Unit = "bytes",
                        Description = "Combined size of all files"
                    });
                }
            }

            return metrics;
        }

        private async Task<List<string>> GenerateInsightRecommendationsAsync(string question, string dataSummary, CancellationToken cancellationToken)
        {
            var recommendations = new List<string>
            {
                "Consider implementing automated classification rules",
                "Review storage tier assignments to optimize costs",
                "Establish retention policies for different data types"
            };

            if (question.ToLower().Contains("compliance"))
            {
                recommendations.Add("Conduct regular compliance audits");
            }

            return recommendations;
        }

        private async Task<List<InsightSupport>> ExtractSupportingEvidenceAsync(string question, IEnumerable<VirtualFile> files, CancellationToken cancellationToken)
        {
            var evidence = new List<InsightSupport>();

            // Extract relevant files as supporting evidence
            var relevantFiles = files
                .OrderByDescending(f => f.CreatedAt)
                .Take(5)
                .ToList();

            foreach (var file in relevantFiles)
            {
                evidence.Add(new InsightSupport
                {
                    FileId = file.Id.ToString(),
                    FileName = file.FileName,
                    Evidence = $"File created on {file.CreatedAt:yyyy-MM-dd} with size {FormatBytes(file.FileSize)}",
                    Relevance = CalculateFileRelevance(file, question)
                });
            }

            return evidence;
        }

        // Helper methods
        private string GetSizeCategory(long fileSize)
        {
            if (fileSize < 1024) return "Small (< 1KB)";
            if (fileSize < 1024 * 1024) return "Medium (< 1MB)";
            if (fileSize < 1024 * 1024 * 100) return "Large (< 100MB)";
            return "Very Large (> 100MB)";
        }

        private string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }

        private float CalculateFileRelevance(VirtualFile file, string question)
        {
            // Simple relevance calculation based on file properties
            var relevance = 0.5f; // Base relevance

            // Boost relevance for recent files
            if (file.CreatedAt > DateTime.UtcNow.AddDays(-7))
                relevance += 0.2f;

            // Boost relevance if filename contains question keywords
            var questionWords = question.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var fileNameLower = file.FileName.ToLower();

            var matchingWords = questionWords.Count(word => fileNameLower.Contains(word));
            relevance += (matchingWords / (float)questionWords.Length) * 0.3f;

            return Math.Min(relevance, 1.0f);
        }

        #endregion
    }
}