using System.ComponentModel.DataAnnotations;

namespace IIM.Shared.Enums;

public enum WorkspaceStatus
{
    Active,
    Open,
    InProgress,
    AssignedTo,
    Pending,
    UnderReview,
    Suspended,
    Closed,
    Cold,
    Archived
}



public enum WorkspaceType
{
	[Display(Name = "")]
	Undefined,
    Investigation,
    Intelligence,
    Surveillance,
    Forensics,
    CyberCrime,
	[Display(Name = "Financial Crime")]
	FinancialCrime,
	[Display(Name = "Counter Intelligence")]
	CounterIntelligence,
	[Display(Name = "Missing Persons")]
	MissingPerson,
    Homicide,
    Fraud,
    Narcotics,
    [Display(Name = "Organized Crime")]
    OrganizedCrime,
    Terrorism,
    Other
}




public enum WorkspacePriority
{
    Low,
    Medium,
    High,
    Critical,
    Emergency
}

