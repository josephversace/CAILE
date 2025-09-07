// src/IIM.Core/Plugins/ForensicAnalysisPlugin.cs
using IIM.Core.Models;
using IIM.Core.Services;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using IIM.Shared.Models;
using IIM.Shared.Interfaces;

namespace IIM.Application.AI
{
    /// <summary>
    /// Semantic Kernel plugin for forensic analysis operations
    /// </summary>
    public class ForensicAnalysisPlugin
    {
        private readonly ILogger<ForensicAnalysisPlugin> _logger;
        private readonly IManagedFileManager _fileManager;
        private readonly IFileService _fileService;

        public ForensicAnalysisPlugin(
            ILogger<ForensicAnalysisPlugin> logger,
            IManagedFileManager fileManager,
            IFileService fileService)
        {
            _logger = logger;
            _fileManager = fileManager;
            _fileService = fileService;
        }

        [KernelFunction("calculate_hash")]
        [Description("Calculate cryptographic hashes for files")]
        public async Task<HashResult> CalculateHashAsync(
            [Description("Path to the file")] string filePath,
            [Description("Hash algorithm (SHA256, SHA512, MD5)")] string algorithm = "SHA256")
        {
            _logger.LogInformation("Calculating {Algorithm} hash for {FilePath}", algorithm, filePath);

            try
            {
                var fileBytes = await _fileService.ReadFileAsync(filePath);

                using var hasher = algorithm.ToUpperInvariant() switch
                {
                    "SHA256" => (HashAlgorithm)SHA256.Create(),
                    "SHA512" => (HashAlgorithm)SHA512.Create(),
                    "MD5" => (HashAlgorithm)MD5.Create(),
                    _ => SHA256.Create()
                };


                var hash = hasher.ComputeHash(fileBytes);
                var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                return new HashResult
                {
                    FilePath = filePath,
                    Algorithm = algorithm,
                    Hash = hashString,
                    FileSize = fileBytes.Length,
                    CalculatedAt = DateTimeOffset.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate hash for {FilePath}", filePath);
                throw;
            }
        }

        [KernelFunction("extract_metadata")]
        [Description("Extract metadata from files")]
        public async Task<FileMetadata> ExtractMetadataAsync(
            [Description("File ID")] string fileId)
        {
            _logger.LogInformation("Extracting metadata for evidence {EvidenceId}", fileId);

            var file = await _fileManager.GetFilesAsync(fileId);

            return new FileMetadata
            {
                FilePath = file.OriginalFileName,
                Size = file.FileSize,
                CreatedAt = file.IngestTimestamp.DateTime,
                ModifiedAt = file.IngestTimestamp.DateTime,
                Hash = file.Hashes.ContainsKey("SHA256") ? file.Hashes["SHA256"] : "",
                MimeType = file.Type.ToString()
            };
        }

        [KernelFunction("build_timeline")]
        [Description("Build a timeline of events from file")]
        public async Task<Timeline> BuildTimelineAsync(
            [Description("Case ID")] string workspaceId,
            [Description("Start date (ISO format)")] string? startDate = null,
            [Description("End date (ISO format)")] string? endDate = null)
        {
            _logger.LogInformation("Building timeline for case {workspaceId}", workspaceId);

            var fileList = await _fileManager.GetFilesByWorkspaceAsync(workspaceId);

            var events = new List<TimelineEvent>();

            foreach (var file in fileList)
            {
                // Add collection event
                events.Add(new TimelineEvent
                {
                    Timestamp = file.IngestTimestamp.DateTime,
                    Type = "File Collected",
                    Description = $"Evidence '{file.OriginalFileName}' collected",
                    EvidenceId = file.Id,
                    Source = file.Metadata?.CollectionLocation ?? "Unknown"
                });

                // Add metadata collection date if available
                if (file.Metadata?.CollectionDate != null)
                {
                    events.Add(new TimelineEvent
                    {
                        Timestamp = file.Metadata.CollectionDate.DateTime,
                        Type = "Original Collection",
                        Description = $"Original collection of '{file.OriginalFileName}'",
                        EvidenceId = file.Id,
                        Source = file.Metadata.CollectionLocation ?? "Field"
                    });
                }
            }

            // Filter by date range if provided
            if (DateTime.TryParse(startDate, out var start))
            {
                events = events.Where(e => e.Timestamp >= start).ToList();
            }
            if (DateTime.TryParse(endDate, out var end))
            {
                events = events.Where(e => e.Timestamp <= end).ToList();
            }

            // Sort chronologically
            events = events.OrderBy(e => e.Timestamp).ToList();

            return new Timeline
            {
                WorkspaceId = workspaceId,
                Events = events,
                StartDate = events.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow,
                EndDate = events.LastOrDefault()?.Timestamp ?? DateTime.UtcNow,
                TotalEvents = events.Count
            };
        }

        [KernelFunction("analyze_patterns")]
        [Description("Analyze patterns in evidence data")]
        public async Task<PatternAnalysisResult> AnalyzePatternsAsync(
            [Description("Workspace ID")] string workspaceId,
            [Description("Pattern type to look for")] string patternType)
        {
            _logger.LogInformation("Analyzing {PatternType} patterns for case {CaseId}", patternType, caseId);

            // This would integrate with more sophisticated pattern analysis
            // For now, return mock results
            await Task.Delay(100);

            return new PatternAnalysisResult
            {
                WorkspaceId = workspaceId,
                PatternType = patternType,
                PatternsFound = new List<Pattern>
                {
                    new Pattern
                    {
                        Type = patternType,
                        Confidence = 0.85,
                        Description = $"Detected {patternType} pattern in evidence",
                        Occurrences = 3
                    }
                },
                AnalyzedAt = DateTimeOffset.UtcNow
            };
        }

        [KernelFunction("chain_of_custody")]
        [Description("Generate chain of custody report for evidence")]
        public async Task<ChainOfCustodyReport> GenerateChainOfCustodyAsync(
            [Description("Evidence ID")] string fileId)
        {
            _logger.LogInformation("Generating chain of custody for {fileId}", fileId);

            var file = await _fileManager.GetFilesAsync(fileId);

            // Use the chain of custody from the evidence object
            var custodyEvents = file.ChainOfCustody.Select(entry => new CustodyEvent
            {
                Timestamp = entry.Timestamp,
                Action = entry.Action,
                User = entry.Actor, // Changed from User to Actor
                Details = entry.Details ?? ""
            }).ToList();

            var primaryHash = file.Hashes.ContainsKey("SHA256")
                ? file.Hashes["SHA256"]
                : file.Hashes.Values.FirstOrDefault() ?? "";

            return new ChainOfCustodyReport
            {
                FileId = fileId,
                OriginalHash = primaryHash,
                CurrentHash = primaryHash,
                IntegrityVerified = file.IntegrityValid,
                CustodyEvents = custodyEvents,
                GeneratedAt = DateTimeOffset.UtcNow
            };
        }
    }

    // Result classes for the plugin
    public class HashResult
    {
        public string FilePath { get; set; } = string.Empty;
        public string Algorithm { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTimeOffset CalculatedAt { get; set; }
    }

    public class Timeline
    {
        public string WorkspaceId { get; set; } = string.Empty;
        public List<TimelineEvent> Events { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalEvents { get; set; }
    }

    public class TimelineEvent
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EvidenceId { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }

    public class PatternAnalysisResult
    {
        public string WorkspaceId { get; set; } = string.Empty;
        public string PatternType { get; set; } = string.Empty;
        public List<Pattern> PatternsFound { get; set; } = new();
        public DateTimeOffset AnalyzedAt { get; set; }
    }

    public class Pattern
    {
        public string Type { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Occurrences { get; set; }
    }

    public class ChainOfCustodyReport
    {
        public string FileId { get; set; } = string.Empty;
        public string OriginalHash { get; set; } = string.Empty;
        public string CurrentHash { get; set; } = string.Empty;
        public bool IntegrityVerified { get; set; }
        public List<CustodyEvent> CustodyEvents { get; set; } = new();
        public DateTimeOffset GeneratedAt { get; set; }
    }

    public class CustodyEvent
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }
}