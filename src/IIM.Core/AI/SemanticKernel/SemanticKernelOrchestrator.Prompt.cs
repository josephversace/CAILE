// File: SemanticKernelOrchestrator.Prompt.cs

using IIM.Core.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Core.AI
{
    public partial class SemanticKernelOrchestrator
    {
 



        /// <summary>
        /// Generates a default analysis prompt.
        /// </summary>
        /// <param name="parameters">Prompt parameters.</param>
        /// <returns>Prompt string.</returns>
        private string GenerateAnalysisPrompt(Dictionary<string, object> parameters)
        {
            return "Analyze the following evidence and provide detailed findings:\n" +
                   "1. Identify key patterns and anomalies\n" +
                   "2. Extract relevant entities and relationships\n" +
                   "3. Provide timeline of events if applicable\n" +
                   "4. Generate actionable recommendations";
        }

        private string GenerateSummaryPrompt(Dictionary<string, object> parameters)
        {
            return "Provide a comprehensive summary of the investigation findings:\n" +
                   "- Key discoveries\n" +
                   "- Critical evidence\n" +
                   "- Timeline of events\n" +
                   "- Recommendations for next steps";
        }

        private string GenerateExtractionPrompt(Dictionary<string, object> parameters)
        {
            return "Extract the following information from the provided content:\n" +
                   "- Named entities (people, places, organizations)\n" +
                   "- Dates and times\n" +
                   "- Financial transactions\n" +
                   "- Communication records\n" +
                   "- Any other relevant data points";
        }


        private string GenerateComparisonPrompt(Dictionary<string, object> parameters)
        {
            return "Compare the provided items and identify:\n" +
                   "- Similarities and differences\n" +
                   "- Common patterns\n" +
                   "- Discrepancies or contradictions\n" +
                   "- Correlation strength";
        }

        private string GenerateInvestigationPrompt(Dictionary<string, object> parameters)
        {
            return "Conduct a thorough investigation based on the available evidence:\n" +
                   "- Establish timeline of events\n" +
                   "- Identify all involved parties\n" +
                   "- Determine motive and opportunity\n" +
                   "- Assess credibility of sources\n" +
                   "- Generate investigative leads";
        }

        private string GenerateDefaultPrompt(string taskType, Dictionary<string, object> parameters)
        {
            return $"Perform {taskType} task with the following parameters:\n" +
                   string.Join("\n", parameters.Select(kvp => $"- {kvp.Key}: {kvp.Value}"));
        }

        private string FormatWithTemplate(ModelConfigurationTemplate template, object data)
        {
            // Use template configuration to format output
            var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            return $"[{template.Name}]\n{json}";
        }

        private string FormatBasic(string templateName, object data)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            return $"[{templateName}]\n{json}";
        }
    }
}
