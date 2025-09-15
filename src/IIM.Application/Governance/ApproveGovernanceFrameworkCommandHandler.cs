using System;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Mediator;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;

namespace IIM.Application.Governance
{
    /// <summary>
    /// Handles the approval of a specific governance framework version.
    /// </summary>
    public class ApproveGovernanceFrameworkCommandHandler : IRequestHandler<ApproveGovernanceFrameworkCommand, Unit>
    {
        private readonly IGovernanceRepository _governanceRepository;
        private readonly IAuditRepository _auditRepository;

        public ApproveGovernanceFrameworkCommandHandler(IGovernanceRepository governanceRepository, IAuditRepository auditRepository)
        {
            _governanceRepository = governanceRepository;
            _auditRepository = auditRepository;
        }

        public async Task<Unit> Handle(ApproveGovernanceFrameworkCommand request, CancellationToken cancellationToken)
        {
            var framework = await _governanceRepository.GetCurrentGovernanceFrameworkAsync(cancellationToken);
            if (framework == null || framework.Version != request.Version)
            {
                // In a real application, you might throw a more specific exception
                // for better error handling on the client side.
                throw new InvalidOperationException("The governance framework to be approved does not match the current version.");
            }

            framework.IsApproved = true;
            framework.ApprovedBy = request.UserId;
            framework.ApprovedAt = DateTime.UtcNow;

            await _governanceRepository.UpdateAsync(framework, cancellationToken);

            var auditEvent = new AuditEvent
            {
                EventType = "governance.framework.approved",
                UserId = request.UserId,
                EntityType = "GovernanceFramework",
                EntityId = framework.Id.ToString(),
                Details = $"Framework version {framework.Version} was approved."
            };
            await _auditRepository.AddEventAsync(auditEvent, cancellationToken);

            return Unit.Value;
        }
    }
}

