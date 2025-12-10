using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.AI.DataEnrichment.Services
{
    /// <summary>
    /// Service responsible for governance rule suggestions and compliance checking
    /// </summary>
    public interface IGovernanceSuggestionService
    {
        Task<PolicySuggestion> SuggestGovernanceRulesAsync(Guid workspaceId, CancellationToken cancellationToken = default);
        Task<ComplianceCheck> CheckComplianceAsync(VirtualFile file, GovernanceFramework rules, CancellationToken cancellationToken = default);
    }
}