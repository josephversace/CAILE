using IIM.Core.Mediator;
using IIM.Core.Services;
using IIM.Shared.Models;
using IIM.Shared.Enums;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using Mediator;
using IIM.Shared.Interfaces;

namespace IIM.Application.Governance;

/// <summary>
/// Handles the ApproveGovernanceFrameworkCommand, persisting the new framework to the database.
/// This class will need to be registered with your custom simple mediator.
/// </summary>
public class ApproveGovernanceFrameworkCommandHandler :IRequestHandler<ApproveGovernanceFrameworkCommand>
{
    private readonly IGovernanceRepository _governanceRepository;
    private readonly IAuditLogger _auditLogger;

    public ApproveGovernanceFrameworkCommandHandler(IGovernanceRepository governanceRepository, IAuditLogger auditLogger)
    {
        _governanceRepository = governanceRepository;
        _auditLogger = auditLogger;
    }

    public async Task Handle(ApproveGovernanceFrameworkCommand command)
    {
        // In a real implementation, you would add validation here to ensure the framework is consistent.
        // For example, ensuring that all Rule IDs link to existing Tags and Tiers.

        await _governanceRepository.SaveGovernanceFrameworkAsync(
            command.ClassificationTags,
            command.StorageTiers,
            command.DataHandlingRules,
            command.AccessRoles,
            command.AccessControlRules);

        // Log this critical event to the chain of custody.
        await _auditLogger.LogAsync("SYSTEM", "Governance Framework Updated", "The global data governance and access control framework has been approved and updated by an administrator.");
    }
}
