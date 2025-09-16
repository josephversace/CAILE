using IIM.Shared.Models.Core;
using System.Collections.Generic;
using System.Linq;

namespace IIM.Application.AI.DataEnrichment.Helpers
{
    /// <summary>
    /// Helper class for building AI prompts for various analysis tasks
    /// </summary>
    public class AIPromptBuilder
    {
        /// <summary>
        /// Builds classification prompt using client-defined taxonomy
        /// </summary>
        public string BuildClassificationPrompt(string textContent, IEnumerable<ClassificationTag> availableClassifications)
        {
            var tagDescriptions = availableClassifications
                .Select(t => $"- {t.Name}: {t.Description}")
                .ToList();

            var contentSample = textContent.Length > 1000
                ? textContent.Substring(0, 1000) + "..."
                : textContent;

            return $@"
Classify the following content using ONLY the provided classification tags.
Return a JSON array of applicable tag names with confidence scores.

Available Classification Tags:
{string.Join("\n", tagDescriptions)}

Content to classify:
{contentSample}

Return format: [{{""tag"": ""TAG_NAME"", ""confidence"": 0.95}}]
";
        }

        /// <summary>
        /// Builds sensitivity analysis prompt
        /// </summary>
        public string BuildSensitivityPrompt(string textContent, GovernanceFramework framework)
        {
            var contentSample = textContent.Length > 1000
                ? textContent.Substring(0, 1000) + "..."
                : textContent;

            return $@"
Determine the data sensitivity level based on governance framework version {framework.Version}.
Consider confidentiality, regulatory requirements, and business impact.

Content to analyze:
{contentSample}

Return one of: Public, Internal, Confidential, Restricted, TopSecret
";
        }

        /// <summary>
        /// Builds entity extraction prompt
        /// </summary>
        public string BuildEntityExtractionPrompt(string textContent)
        {
            var contentSample = textContent.Length > 2000
                ? textContent.Substring(0, 2000) + "..."
                : textContent;

            return $@"
Extract entities from this text. Return as structured JSON.

Content:
{contentSample}

Return format: {{""people"": [], ""organizations"": [], ""locations"": [], ""dates"": []}}
";
        }
    }
}