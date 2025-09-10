using IIM.Core.Mediator;
using IIM.Shared.Models.Core;
using System.Collections.Generic;

namespace IIM.Application.Governance
{
    /// <summary>
    /// Command to save the complete, human-approved governance framework to the database.
    /// This is triggered by the administrator at the end of the AI-driven setup wizard.
    /// </summary>
    public record ApproveGovernanceFrameworkCommand : ICommand
    {
        /// <summary>
        /// The new version number for this framework.
        /// </summary>
        public int Version { get; init; }

        /// <summary>
        /// The ID of the user approving this framework.
        /// </summary>
        public string UserId { get; init; } = string.Empty;

        /// <summary>
        /// A description for this new version of the framework.
        /// </summary>
        public string Description { get; init; } = "New framework version.";

        public IEnumerable<ClassificationTag> ClassificationTags { get; init; } = new List<ClassificationTag>();
        public IEnumerable<StorageTier> StorageTiers { get; init; } = new List<StorageTier>();
        public IEnumerable<DataHandlingRule> DataHandlingRules { get; init; } = new List<DataHandlingRule>();
        public IEnumerable<AccessRole> AccessRoles { get; init; } = new List<AccessRole>();
        public IEnumerable<AccessControlRule> AccessControlRules { get; init; } = new List<AccessControlRule>();
    }
}
