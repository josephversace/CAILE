
using IIM.Shared.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;


namespace IIM.Shared.Interfaces
{
    public interface IInvestigationService
    {
        // Session Management
        Task<InvestigationSession> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);

        Task<InvestigationSession> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);
        Task<List<InvestigationSession>> GetSessionsByWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default);
        Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);

        // Query Processing
        Task<InvestigationResponse> ProcessQueryAsync(string sessionId, InvestigationQuery query, CancellationToken cancellationToken = default);

        // Add SendQueryAsync for UI compatibility (alias for ProcessQueryAsync)
        Task<InvestigationResponse> SendQueryAsync(string sessionId, InvestigationQuery query, CancellationToken cancellationToken = default);

        // Tool Execution
        Task<ToolResult> ExecuteToolAsync(string sessionId, string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);

        // Workspace Management
        Task<List<Workspace>> GetRecentCasesAsync(int count = 10, CancellationToken cancellationToken = default);
        Task<List<InvestigationSession>> GetCaseSessionsAsync(string caseId, CancellationToken cancellationToken = default);
        Task<Workspace> GetCaseAsync(string workspaceId, CancellationToken cancellationToken = default);

        // Response Management
        Task<InvestigationResponse> EnrichResponseForDisplayAsync(InvestigationResponse response, InvestigationMessage? message = null);
        Task<InvestigationResponse> GetResponseAsync(string responseId);
        Task<byte[]> ExportResponseAsync(string responseId, ExportFormat format, ExportOptions? options = null);
    }
}