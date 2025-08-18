using IIM.Core.Models;
using IIM.Shared.Enums;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Core.Services;

public interface IInvestigationService
{
    // Session Management
    Task<InvestigationSession> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);
    Task<InvestigationSession> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<List<InvestigationSession>> GetSessionsByCaseAsync(string caseId, CancellationToken cancellationToken = default);
    Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    // Query Processing
    Task<InvestigationResponse> ProcessQueryAsync(string sessionId, InvestigationQuery query, CancellationToken cancellationToken = default);

    // Tool Execution
    Task<ToolResult> ExecuteToolAsync(string sessionId, string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);

    // Case Management
    Task<List<Case>> GetRecentCasesAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<List<InvestigationSession>> GetCaseSessionsAsync(string caseId, CancellationToken cancellationToken = default);

    Task<InvestigationResponse> EnrichResponseForDisplayAsync(
InvestigationResponse response,
InvestigationMessage? message = null);

    Task<byte[]> ExportResponseAsync(
        string responseId,
        ExportFormat format,
        ExportOptions? options = null);

    private async Task<ResponseDisplayType> DetermineOptimalDisplayTypeAsync(
       InvestigationResponse response,
       InvestigationMessage? message)
    {
        // Determine display type based on content
        if (message?.ToolResults?.Any() == true)
        {
            var firstTool = message.ToolResults.First();

            // Check tool name for hints
            if (firstTool.ToolName.Contains("table", StringComparison.OrdinalIgnoreCase))
                return ResponseDisplayType.Table;
            if (firstTool.ToolName.Contains("image", StringComparison.OrdinalIgnoreCase))
                return ResponseDisplayType.Image;
            if (firstTool.ToolName.Contains("timeline", StringComparison.OrdinalIgnoreCase))
                return ResponseDisplayType.Timeline;

            // Check visualizations
            if (firstTool.Visualizations?.Any() == true)
            {
                var vizType = firstTool.Visualizations.First().Type.ToLower();
                return vizType switch
                {
                    "table" => ResponseDisplayType.Table,
                    "chart" => ResponseDisplayType.Table,
                    "graph" => ResponseDisplayType.Table,
                    "timeline" => ResponseDisplayType.Timeline,
                    "map" => ResponseDisplayType.Geospatial,
                    _ => ResponseDisplayType.Structured
                };
            }
        }

        // Check response metadata
        if (response.Metadata?.ContainsKey("displayType") == true)
        {
            if (Enum.TryParse<ResponseDisplayType>(response.Metadata["displayType"].ToString(), out var type))
                return type;
        }

        return ResponseDisplayType.Text;
    }

    private async Task<ResponseVisualization?> BuildVisualizationFromToolResultsAsync(
        List<ToolResult> toolResults)
    {
        var firstVisualization = toolResults
            .Where(tr => tr.Visualizations?.Any() == true)
            .SelectMany(tr => tr.Visualizations)
            .FirstOrDefault();

        if (firstVisualization != null)
        {
            return new ResponseVisualization
            {
                Type = firstVisualization.Type,
                Title = firstVisualization.Title,
                Description = firstVisualization.Description,
                Data = firstVisualization.Data,
                Options = firstVisualization.Options
            };
        }

        return null;
    }

    public async Task<InvestigationResponse> GetResponseAsync(string responseId)
    {
        // In production, this would fetch from database
        // For now, return a placeholder
        //_logger.LogInformation("Getting response {ResponseId}", responseId);

        // You might want to implement actual retrieval logic here
        throw new NotImplementedException("GetResponseAsync needs to be implemented based on your data storage");
    }
}





