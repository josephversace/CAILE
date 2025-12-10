using IIM.Application.AI.DataEnrichment.Helpers;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.AI.DataEnrichment.Services
{
    /// <summary>
    /// Implementation of governance suggestion service
    /// </summary>
    public class GovernanceSuggestionService : IGovernanceSuggestionService
    {
        private readonly ILogger<GovernanceSuggestionService> _logger;
        private readonly IWorkspaceManager _workspaceProvider;
        private readonly IGovernanceRepository _governanceRepository;
        private readonly ConfidenceCalculator _confidenceCalculator;

        public GovernanceSuggestionService(
            ILogger<GovernanceSuggestionService> logger,
            IWorkspaceManager workspaceProvider,
            IGovernanceRepository governanceRepository,
            ConfidenceCalculator confidenceCalculator)
        {
            _logger = logger;
            _workspaceProvider = workspaceProvider;
            _governanceRepository = governanceRepository;
            _confidenceCalculator = confidenceCalculator;
        }

        public async Task<PolicySuggestion> SuggestGovernanceRulesAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating governance rule suggestions for workspace {WorkspaceId}", workspaceId);

            var suggestion = new PolicySuggestion
            {
                GeneratedAt = DateTime.UtcNow
            };

            try
            {
                // Analyze workspace data patterns
                var files = await _workspaceProvider.GetVirtualFilesAsync(workspaceId, cancellationToken);

                // Fix: Set missing properties
                suggestion.SuggestedTags = await AnalyzeClassificationPatternsAsync(files, cancellationToken);
                suggestion.SuggestedTiers = await AnalyzeStoragePatternsAsync(files, cancellationToken);
                suggestion.SuggestedRules = await GenerateGovernanceRulesAsync(files, cancellationToken);

                // Fix: Set both Confidence and ConfidenceScore
                suggestion.ConfidenceScore = _confidenceCalculator.CalculatePolicySuggestionConfidence(suggestion);
                suggestion.Confidence = suggestion.ConfidenceScore;

                return suggestion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating governance suggestions for workspace {WorkspaceId}", workspaceId);
                throw;
            }
        }

        public async Task<ComplianceCheck> CheckComplianceAsync(VirtualFile file, GovernanceFramework rules, CancellationToken cancellationToken = default)
        {
            var check = new ComplianceCheck
            {
                FileId = file.Id.ToString(),
                CheckedAt = DateTime.UtcNow,
                FrameworkVersion = rules.Version.ToString() // Remove invalid ?? "1.0"
            };

            return check;
        }

        #region Private Methods


        private async Task<List<SuggestedClassificationTag>> AnalyzeClassificationPatternsAsync(IEnumerable<VirtualFile> files, CancellationToken cancellationToken)
        {
            var tags = new List<SuggestedClassificationTag>();

            var commonExtensions = files
                .GroupBy(f => Path.GetExtension(f.FileName).ToLower())
                .OrderByDescending(g => g.Count())
                .Take(5);

            foreach (var group in commonExtensions)
            {
                tags.Add(new SuggestedClassificationTag
                {
                    Name = $"FileType_{group.Key.TrimStart('.')}",
                    Description = $"Files with {group.Key} extension",
                    Confidence = Math.Min(group.Count() / (float)files.Count(), 1.0f),
                    Reasoning = $"Found {group.Count()} files with {group.Key} extension",
                    FileCount = group.Count() // Fix Line 224: Set missing FileCount property
                });
            }

            return tags;
        }

        private async Task<List<SuggestedStorageTier>> AnalyzeStoragePatternsAsync(IEnumerable<VirtualFile> files, CancellationToken cancellationToken)
        {
            var tiers = new List<SuggestedStorageTier>();

            var totalSize = files.Sum(f => f.FileSize);
            var fileCount = files.Count();

            if (totalSize > 1024 * 1024 * 1024) // > 1GB
            {
                tiers.Add(new SuggestedStorageTier
                {
                    Name = "Archive",
                    Description = "Long-term storage for large datasets",
                    RetentionDays = 2555, // 7 years
                    Confidence = 0.8f,
                    Reasoning = "Large dataset detected, archive storage recommended",
                    Criteria = "Total size > 1GB", // Fix Line 232: Set missing Criteria property
                    EstimatedFileCount = fileCount // Fix Line 232: Set missing EstimatedFileCount property
                });
            }

            return tiers;
        }


        private async Task<List<SuggestedDataHandlingRule>> AnalyzeDataHandlingPatternsAsync(IEnumerable<VirtualFile> files, CancellationToken cancellationToken)
        {
            var rules = new List<SuggestedDataHandlingRule>();

            // Analyze sensitive data patterns
            //var sensitiveFiles = files.Where(f => f.StoredFile.C = DataSensitivityLevel.Confidential).Count();
            //if (sensitiveFiles > 0)
            //{
            //    rules.Add(new SuggestedDataHandlingRule
            //    {
            //        RuleType = "Encryption",
            //        Description = "Require encryption for confidential data",
            //        Parameters = new Dictionary<string, object> { ["Algorithm"] = "AES-256" },
            //        Confidence = 0.9f,
            //        Reasoning = $"Found {sensitiveFiles} confidential files requiring encryption"
            //    });
            //}

            return rules;
        }

        private async Task<List<string>> GenerateGovernanceRulesAsync(IEnumerable<VirtualFile> files, CancellationToken cancellationToken)
        {
            var rules = new List<string>();

            //// Generate basic governance rules based on data patterns
            //if (files.Any(f => f.DataSensitivity >= DataSensitivityLevel.Confidential))
            //{
            //    rules.Add("Implement access controls for confidential data");
            //    rules.Add("Enable audit logging for sensitive file access");
            //}

            //if (files.Count() > 1000)
            //{
            //    rules.Add("Implement data lifecycle management policies");
            //    rules.Add("Configure automated data archiving");
            //}

            return rules;
        }

        private async Task<object> AnalyzeWorkspaceDataPatternsAsync(Guid workspaceId, CancellationToken cancellationToken)
        {
            var files = await _workspaceProvider.GetVirtualFilesByWorkspaceAsync(workspaceId, cancellationToken);
            var filesList = files.ToList();

            return new
            {
                TotalFiles = filesList.Count,
                TotalSize = filesList.Sum(f => f.FileSize),
                FileTypes = filesList.GroupBy(f => System.IO.Path.GetExtension(f.FileName).ToLower()).ToDictionary(g => g.Key, g => g.Count()),
                AverageFileSize = filesList.Any() ? filesList.Average(f => f.FileSize) : 0
            };
        }

        private async Task<List<SuggestedClassificationTag>> GenerateTagSuggestionsAsync(object dataPatterns, CancellationToken cancellationToken)
        {
            // TODO: Use AI to analyze patterns and suggest tags
            return new List<SuggestedClassificationTag>
            {
                new() { Name = "STANDARD_DOCUMENT", Description = "General business documents", Confidence = 0.8f, FileCount = 0 }
            };
        }

        private async Task<string> GenerateReasoningAsync(IEnumerable<VirtualFile> files, CancellationToken cancellationToken)
        {
            var reasons = new List<string>();

            var fileCount = files.Count();
            if (fileCount > 100)
            {
                reasons.Add($"Large dataset with {fileCount} files requires structured governance");
            }

            var sensitiveFiles = files.Count(f => GetDataSensitivity(f) >= DataSensitivityLevel.Confidential);
            if (sensitiveFiles > 0)
            {
                reasons.Add($"Found {sensitiveFiles} sensitive files requiring protection");
            }

            // Return joined string instead of List<string>
            return string.Join(". ", reasons);
        }


        private async Task<string> AnalyzeDataPatternsAsync(IEnumerable<VirtualFile> files, CancellationToken cancellationToken)
        {
            var patterns = new List<string>();

            var totalSize = files.Sum(f => f.FileSize);
            patterns.Add($"Total data volume: {totalSize:N0} bytes");

            var fileTypes = files.GroupBy(f => Path.GetExtension(f.FileName))
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => $"{g.Key}: {g.Count()} files");

            patterns.AddRange(fileTypes);

            // Return joined string instead of List<string>
            return string.Join("; ", patterns);
        }

        private async Task<List<SuggestedStorageTier>> GenerateTierSuggestionsAsync(object dataPatterns, CancellationToken cancellationToken)
        {
            return new List<SuggestedStorageTier>
            {
                new() { Name = "standard", Description = "Standard access storage", Criteria = "Regular access", EstimatedFileCount = 0 }
            };
        }

        private async Task<List<SuggestedDataHandlingRule>> GenerateRuleSuggestionsAsync(object dataPatterns, CancellationToken cancellationToken)
        {
            return new List<SuggestedDataHandlingRule>();
        }

        private async Task<string> GeneratePolicyReasoningAsync(object dataPatterns, CancellationToken cancellationToken)
        {
            return "Policy suggestions based on analysis of data patterns and industry best practices";
        }

        private async Task<IEnumerable<object>> GetApplicableRulesAsync(string classificationTag, CancellationToken cancellationToken)
        {
            var rules = await _governanceRepository.GetDataHandlingRulesAsync(cancellationToken);
            return rules.Where(r => r.ClassificationTag.Name.Equals(classificationTag, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<(bool IsCompliant, List<ComplianceIssue> Issues)> ValidateRuleComplianceAsync(VirtualFile file, ClassificationTag tag, CancellationToken cancellationToken)
        {
            var issues = new List<ComplianceIssue>();

            // TODO: Implement specific compliance validation logic

            return (issues.Count == 0, issues);
        }

        private static string DetermineRiskLevel(List<ComplianceIssue> issues)
        {
            if (!issues.Any()) return "Low";
            return issues.Any(i => i.Severity == "High") ? "High" : "Medium";
        }

        private static List<string> GenerateRecommendations(List<ComplianceIssue> issues)
        {
            return issues.Select(i => i.Recommendation).Where(r => !string.IsNullOrEmpty(r)).ToList();
        }

        private DataSensitivityLevel GetDataSensitivity(VirtualFile file)
        {
            try
            {
                var property = file.GetType().GetProperty("DataSensitivity");
                if (property != null)
                {
                    return (DataSensitivityLevel)(property.GetValue(file) ?? DataSensitivityLevel.Unknown);
                }
                return DataSensitivityLevel.Unknown; // Return Unknown instead of Internal
            }
            catch
            {
                return DataSensitivityLevel.Unknown;
            }
        }

        #endregion
    }
}