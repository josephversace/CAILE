using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IIM.Core.Models;

namespace IIM.Core.Services
{
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
    }



}
