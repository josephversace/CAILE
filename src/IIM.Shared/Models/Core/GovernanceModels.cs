using IIM.Shared.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models.Core;

    /// <summary>
    /// Represents a versioned, approvable instance of the entire data governance framework.
    /// This acts as a container for the collection of rules at a specific point in time.
    /// </summary>
    public class GovernanceFramework
    {
        public Guid Id { get; set; }
        /// <summary>
        /// The version of this framework, typically an integer that increments.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// A description of what changed in this version.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if this framework is the currently active and approved version.
        /// </summary>
        public bool IsApproved { get; set; }

        /// <summary>
        /// The ID of the user who approved this framework.
        /// </summary>
        public string? ApprovedBy { get; set; }

        /// <summary>
        /// The timestamp when this framework was approved.
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// The timestamp when this framework version was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Note: The actual rules (Tags, Tiers, etc.) are linked by convention
        // or could be explicitly linked via collections if needed in the future.
        // For now, this entity just tracks the state of the overall framework version.
    }

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


/// <summary>
/// Represents the central rule that links a data classification to its required storage tier and policies.
/// This is the output of the "Asset Classification Workshop" and drives the routing logic.
/// </summary>
public class DataHandlingRule
{
    public Guid Id { get; set; }

    /// <summary>
    /// The classification tag that triggers this rule (e.g., "LEGAL_PRIVILEGED").
    /// </summary>
    public Guid ClassificationTagId { get; set; }
    public ClassificationTag ClassificationTag { get; set; }

    /// <summary>
    /// The storage tier where data with the matching classification must be stored.
    /// </summary>
    public Guid StorageTierId { get; set; }
    public StorageTier StorageTier { get; set; }
}


// Represents a high-level data classification tag (e.g., "PII", "Financial", "Legal-Privileged").
// This is defined during the AI wizard's "Asset Classification Workshop".
public class ClassificationTag
{
    public Guid Id { get; set; }
    public string Name { get; set; } // e.g., "LEGAL_PRIVILEGED"
    public string Description { get; set; }

	public ICollection<StoredFile> StoredFiles { get; set; } = new List<StoredFile>();

}

// Represents a physical or logical storage tier.
// This defines WHERE and HOW data is stored.
public class StorageTier
{
    public Guid Id { get; set; }
    public string Name { get; set; } // e.g., "On-Premise Secure Storage"
    public StorageLocation Location { get; set; } // e.g., OnPremise, HybridCloud
    public bool EncryptionRequired { get; set; }

    // The name of the SeaweedFS collection this tier maps to.
    public string SeaweedFSCollection { get; set; }

    public int RetentionPeriodDays { get;set; } // e.g., 3650 for 10 years
}

public enum StorageLocation
{
    OnPremise,
    HybridCloud
}

// Represents a user role within the organization (e.g., "Paralegal", "Senior Partner").
// This is defined during the AI wizard's "Access Control Workshop".
public class AccessRole
{
    public Guid Id { get; set; }
    public string Name { get; set; } // e.g., "Paralegal"
    public string Description { get; set; }
}

// This is the central entity that links everything together.
// It represents a single rule in the access control matrix.
public class AccessControlRule
{
    public Guid Id { get; set; }

    // The role this rule applies to.
    public Guid AccessRoleId { get; set; }
    public AccessRole AccessRole { get; set; }

    // The data classification this rule applies to.
    public Guid ClassificationTagId { get; set; }
    public ClassificationTag ClassificationTag { get; set; }

    // The permissions granted by this rule.
    public FilePermissions Permissions { get; set; }
}

[Flags]
public enum FilePermissions
{
    None = 0,
    Read = 1,
    Write = 2,
    Delete = 4,
    Share = 8,
    All = Read | Write | Delete | Share
}


