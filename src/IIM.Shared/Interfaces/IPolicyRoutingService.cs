using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// Routes files to appropriate storage tiers based on client-defined governance policies
    /// </summary>
    public interface IPolicyRoutingService
    {
        /// <summary>
        /// Determines routing based on classification and governance rules
        /// </summary>
        Task<RoutingDecision> DetermineRoutingAsync(ClassificationResult classification, Guid? workspaceId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets available storage tiers for a client/workspace
        /// </summary>
        Task<IEnumerable<StorageTier>> GetAvailableStorageTiersAsync(Guid? workspaceId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates that a file placement complies with governance rules
        /// </summary>
        Task<ComplianceValidationResult> ValidateComplianceAsync(VirtualFile file, CancellationToken cancellationToken = default);
    }
}
