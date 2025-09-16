using IIM.Application.AI.DataEnrichment.Helpers;
using IIM.Shared.Interfaces;
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
    /// Implementation of file classification service
    /// </summary>
    public class FileClassificationService : IFileClassificationService
    {
        private readonly ILogger<FileClassificationService> _logger;
        private readonly IGovernanceRepository _governanceRepository;
        private readonly ITextExtractionService _textExtraction;
        private readonly AIPromptBuilder _promptBuilder;
        private readonly ConfidenceCalculator _confidenceCalculator;

        public FileClassificationService(
            ILogger<FileClassificationService> logger,
            IGovernanceRepository governanceRepository,
            ITextExtractionService textExtraction,
            AIPromptBuilder promptBuilder,
            ConfidenceCalculator confidenceCalculator)
        {
            _logger = logger;
            _governanceRepository = governanceRepository;
            _textExtraction = textExtraction;
            _promptBuilder = promptBuilder;
            _confidenceCalculator = confidenceCalculator;
        }

        public async Task<ClassificationSuggestion> SuggestClassificationAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating classification suggestion for file {FileName}", fileName);

            var suggestion = new ClassificationSuggestion();

            // Get client-defined classification tags from governance framework
            var availableClassifications = await _governanceRepository.GetClassificationTagsAsync(cancellationToken);
            if (!availableClassifications.Any())
            {
                _logger.LogWarning("No classification tags defined in governance framework");
                suggestion.ConfidenceScore = 0.0f;
                suggestion.Reasoning = "No classification framework configured";
                return suggestion;
            }

            // Analyze file name patterns
            var fileNameTags = await AnalyzeFileNameAsync(fileName, availableClassifications, cancellationToken);
            // Fix: Convert List<string> to List<TagSuggestion>
            suggestion.SuggestedTags.AddRange(fileNameTags.Select(tag => new TagSuggestion
            {
                Name = tag,
                Confidence = 0.7f,
                Reason = "File name pattern match"
            }));

            // Analyze content if available
            var mimeType = GetMimeTypeFromFileName(fileName);
            var textContent = await _textExtraction.ExtractTextAsync(content, mimeType, cancellationToken);

            if (!string.IsNullOrEmpty(textContent))
            {
                var contentTags = await ClassifyContentAsync(textContent, availableClassifications, cancellationToken);
                // Fix: Convert List<string> to List<TagSuggestion>
                suggestion.SuggestedTags.AddRange(contentTags.Select(tag => new TagSuggestion
                {
                    Name = tag,
                    Confidence = 0.8f,
                    Reason = "Content analysis match"
                }));

                // Fix: Use SensitivityLevel property instead of SuggestedSensitivity
                suggestion.SensitivityLevel = await DetermineSensitivityAsync(textContent, cancellationToken);
                suggestion.SuggestedSensitivity = suggestion.SensitivityLevel; // Set both for compatibility
            }

            // Generate reasoning
            suggestion.Reasoning = await GenerateReasoningAsync(fileName, textContent,
                suggestion.SuggestedTags.Select(t => t.Name).ToList(), cancellationToken);

            // Determine storage requirements
            var storageTier = await DetermineStorageTierAsync(
                suggestion.SuggestedTags.Select(t => t.Name).ToList(),
                suggestion.SensitivityLevel,
                cancellationToken);

            // Fix: Set missing properties
            suggestion.RecommendedStorageTier = storageTier?.Name ?? "default";
            suggestion.StorageTier = suggestion.RecommendedStorageTier;
            suggestion.RetentionDays = storageTier?.RetentionPeriodDays ?? 365;
            suggestion.RequiresHumanReview = DetermineIfHumanReviewRequired(suggestion);

            // Calculate confidence
            suggestion.ConfidenceScore = _confidenceCalculator.CalculateClassificationConfidence(suggestion);

            return suggestion;
        }

        public async Task<List<string>> ClassifyContentAsync(string textContent, IEnumerable<ClassificationTag> availableClassifications, CancellationToken cancellationToken = default)
        {
            // Implementation here
            throw new NotImplementedException("Content classification not yet implemented");
        }

        public async Task<DataSensitivityLevel> DetermineSensitivityAsync(string textContent, CancellationToken cancellationToken = default)
        {
            // Implementation here
            return DataSensitivityLevel.Internal;
        }

        #region Private Methods

        private async Task<List<string>> AnalyzeFileNameAsync(string fileName, IEnumerable<ClassificationTag> availableClassifications, CancellationToken cancellationToken)
        {
            // Implementation here
            return new List<string>();
        }

        private async Task<string> GenerateReasoningAsync(string fileName, string textContent, List<string> tags, CancellationToken cancellationToken)
        {
            // Implementation here
            return "Classification reasoning would be generated here";
        }

        private async Task<StorageTier?> DetermineStorageTierAsync(List<string> tags, DataSensitivityLevel sensitivityLevel, CancellationToken cancellationToken)
        {
            // Implementation here
            return null;
        }

        private static bool DetermineIfHumanReviewRequired(ClassificationSuggestion suggestion)
        {
            return suggestion.SensitivityLevel >= DataSensitivityLevel.Confidential ||
                   suggestion.ConfidenceScore < 0.75f;
        }

        private static string GetMimeTypeFromFileName(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".txt" => "text/plain",
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".json" => "application/json",
                _ => "application/octet-stream"
            };
        }

        #endregion
    }
}