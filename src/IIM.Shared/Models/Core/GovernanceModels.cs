using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models.Core;

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


