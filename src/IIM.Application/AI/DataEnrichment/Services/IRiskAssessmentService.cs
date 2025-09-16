using IIM.Shared.Models.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.AI.DataEnrichment.Services
{
    /// <summary>
    /// Service responsible for assessing data risks across workspaces
    /// </summary>
    public interface IRiskAssessmentService
    {
        Task<RiskAssessment> AssessWorkspaceRiskAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    }
}