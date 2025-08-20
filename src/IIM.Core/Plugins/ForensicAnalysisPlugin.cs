// src/IIM.Core/Plugins/ForensicAnalysisPlugin.cs
using IIM.Core.Models;
using IIM.Core.Services;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace IIM.Core.AI
{
    /// <summary>
    /// Semantic Kernel plugin for forensic analysis operations
    /// </summary>
    public class ForensicAnalysisPlugin
    {
        private readonly ILogger<ForensicAnalysisPlugin> _logger;
        private readonly IEvidenceManager _evidenceManager;
        private readonly IFileService _fileService;

        public ForensicAnalysisPlugin(
            ILogger<ForensicAnalysisPlugin> logger,
            IEvidenceManager evidenceManager,
            IFileService fileService)
        {
            _logger = logger;
            _evidenceManager = evidenceManager;
            _fileService = fileService;
        }

        [KernelFunction("calculate_hash")]
        [Description("Calculate cryptographic hashes for evidence files")]
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
        [Description("Extract metadata from evidence files")]
        public async Task<FileMetadata> ExtractMetadataAsync(
            [Description("Evidence ID")] string evidenceId)
        {
            _logger.LogInformation("Extracting metadata for evidence {EvidenceId}", evidenceId);

            var evidence = await _evidenceManager.GetEvidenceAsync(evidenceId);

            return new FileMetadata
            {
                FilePath = evidence.OriginalFileName,
                Size = evidence.FileSize,
                CreatedAt = evidence.IngestTimestamp.DateTime,
                ModifiedAt = evidence.IngestTimestamp.DateTime,
                Hash = evidence.Hashes.ContainsKey("SHA256") ? evidence.Hashes["SHA256"] : "",
                MimeType = evidence.Type.ToString()
            };
        }

        [KernelFunction("build_timeline")]
        [Description("Build a timeline of events from evidence")]
        public async Task<Timeline> BuildTimelineAsync(
            [Description("Case ID")] string caseId,
            [Description("Start date (ISO format)")] string? startDate = null,
            [Description("End date (ISO format)")] string? endDate = null)
        {
            _logger.LogInformation("Building timeline for case {CaseId}", caseId);

            var evidenceList = await _evidenceManager.GetEvidenceByCaseAsync(caseId);

            var events = new List<TimelineEvent>();

            foreach (var evidence in evidenceList)
            {
                // Add collection event
                events.Add(new TimelineEvent
                {
                    Timestamp = evidence.IngestTimestamp.DateTime,
                    Type = "Evidence Collected",
                    Description = $"Evidence '{evidence.OriginalFileName}' collected",
                    EvidenceId = evidence.Id,
                    Source = evidence.Metadata?.CollectionLocation ?? "Unknown"
                });

                // Add metadata collection date if available
                if (evidence.Metadata?.CollectionDate != null)
                {
                    events.Add(new TimelineEvent
                    {
                        Timestamp = evidence.Metadata.CollectionDate.DateTime,
                        Type = "Original Collection",
                        Description = $"Original collection of '{evidence.OriginalFileName}'",
                        EvidenceId = evidence.Id,
                        Source = evidence.Metadata.CollectionLocation ?? "Field"
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
                CaseId = caseId,
                Events = events,
                StartDate = events.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow,
                EndDate = events.LastOrDefault()?.Timestamp ?? DateTime.UtcNow,
                TotalEvents = events.Count
            };
        }

        [KernelFunction("analyze_patterns")]
        [Description("Analyze patterns in evidence data")]
        public async Task<PatternAnalysisResult> AnalyzePatternsAsync(
            [Description("Case ID")] string caseId,
            [Description("Pattern type to look for")] string patternType)
        {
            _logger.LogInformation("Analyzing {PatternType} patterns for case {CaseId}", patternType, caseId);

            // This would integrate with more sophisticated pattern analysis
            // For now, return mock results
            await Task.Delay(100);

            return new PatternAnalysisResult
            {
                CaseId = caseId,
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
            [Description("Evidence ID")] string evidenceId)
        {
            _logger.LogInformation("Generating chain of custody for {EvidenceId}", evidenceId);

            var evidence = await _evidenceManager.GetEvidenceAsync(evidenceId);

            // Use the chain of custody from the evidence object
            var custodyEvents = evidence.ChainOfCustody.Select(entry => new CustodyEvent
            {
                Timestamp = entry.Timestamp,
                Action = entry.Action,
                User = entry.Actor, // Changed from User to Actor
                Details = entry.Details ?? ""
            }).ToList();

            var primaryHash = evidence.Hashes.ContainsKey("SHA256")
                ? evidence.Hashes["SHA256"]
                : evidence.Hashes.Values.FirstOrDefault() ?? "";

            return new ChainOfCustodyReport
            {
                EvidenceId = evidenceId,
                OriginalHash = primaryHash,
                CurrentHash = primaryHash,
                IntegrityVerified = evidence.IntegrityValid,
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
        public string CaseId { get; set; } = string.Empty;
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
        public string CaseId { get; set; } = string.Empty;
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
        public string EvidenceId { get; set; } = string.Empty;
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