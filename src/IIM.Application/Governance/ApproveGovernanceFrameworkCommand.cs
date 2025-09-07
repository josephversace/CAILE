using IIM.Core.Mediator;
using IIM.Shared.Models.Core;
using System.Collections.Generic;

namespace IIM.Application.Governance;

/// <summary>
/// Command to save the complete, human-approved governance framework to the database.
/// This is triggered by the administrator at the end of the AI-driven setup wizard.
/// </summary>
public record ApproveGovernanceFrameworkCommand
{
    public IEnumerable<ClassificationTag> ClassificationTags { get; init; }
    public IEnumerable<StorageTier> StorageTiers { get; init; }
    public IEnumerable<DataHandlingRule> DataHandlingRules { get; init; }
    public IEnumerable<AccessRole> AccessRoles { get; init; }
    public IEnumerable<AccessControlRule> AccessControlRules { get; init; }
}
