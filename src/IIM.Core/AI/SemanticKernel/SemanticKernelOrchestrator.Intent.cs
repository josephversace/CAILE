// File: SemanticKernelOrchestrator.Intent.cs
using IIM.Shared.Enums;
using IIM.Shared.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Core.AI
{
    public partial class SemanticKernelOrchestrator
    {


        /// <summary>
        /// Determines the primary intent from a query.
        /// </summary>
        /// <param name="query">User query.</param>
        /// <returns>Primary intent string.</returns>
        private string DetermineIntent(string query)
        {
            var lowerQuery = query.ToLowerInvariant();

            if (lowerQuery.Contains("analyze") || lowerQuery.Contains("examine"))
                return "analyze";
            if (lowerQuery.Contains("search") || lowerQuery.Contains("find"))
                return "search";
            if (lowerQuery.Contains("investigate") || lowerQuery.Contains("explore"))
                return "investigate";
            if (lowerQuery.Contains("compare") || lowerQuery.Contains("match"))
                return "compare";
            if (lowerQuery.Contains("summarize") || lowerQuery.Contains("summary"))
                return "summarize";

            return "general";
        }

        /// <summary>
        /// Calculates intent scores for possible intents.
        /// </summary>
        /// <param name="query">User query.</param>
        /// <returns>Dictionary of intent scores.</returns>
        private Dictionary<string, float> CalculateIntentScores(string query)
        {
            // Simplified scoring
            return new Dictionary<string, float>
            {
                ["analyze"] = query.Contains("analyze", StringComparison.OrdinalIgnoreCase) ? 0.9f : 0.1f,
                ["search"] = query.Contains("search", StringComparison.OrdinalIgnoreCase) ? 0.9f : 0.1f,
                ["investigate"] = query.Contains("investigate", StringComparison.OrdinalIgnoreCase) ? 0.9f : 0.1f,
                ["general"] = 0.5f
            };
        }

        /// <summary>
        /// Extracts basic entities from a query string.
        /// </summary>
        /// <param name="query">User query.</param>
        /// <returns>List of entities.</returns>
        private List<string> ExtractBasicEntities(string query)
        {
            var entities = new List<string>();

            // Extract quoted strings
            var quotedPattern = "\"([^\"]+)\"";
            var matches = System.Text.RegularExpressions.Regex.Matches(query, quotedPattern);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                entities.Add(match.Groups[1].Value);
            }

            return entities;
        }

        /// <summary>
        /// Extracts structured entities from a user query.
        /// </summary>
        /// <param name="query">User query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Dictionary of extracted entities.</returns>
        private async Task<Dictionary<string, object>> ExtractEntitiesAsync(
           string query,
           CancellationToken cancellationToken)
        {
            var entities = new Dictionary<string, object>();

            // Simple entity extraction (would use NER model in production)
            if (query.Contains("email", StringComparison.OrdinalIgnoreCase))
            {
                entities["entityType"] = EntityType.Account;
            }

            if (query.Contains("person", StringComparison.OrdinalIgnoreCase) ||
                query.Contains("suspect", StringComparison.OrdinalIgnoreCase))
            {
                entities["entityType"] = EntityType.Person;
            }

            return entities;
        }


        /// <summary>
        /// Summarizes conversation messages for a session.
        /// </summary>
        /// <param name="messages">List of investigation messages.</param>
        /// <returns>Summary string.</returns>
        private string SummarizeConversation(List<InvestigationMessage> messages)
        {
            if (!messages.Any())
                return "No messages";

            var userMessages = messages.Count(m => m.Role == MessageRole.User);
            var assistantMessages = messages.Count(m => m.Role == MessageRole.Assistant);

            return $"Conversation with {userMessages} user messages and {assistantMessages} assistant responses";
        }
    }
}
