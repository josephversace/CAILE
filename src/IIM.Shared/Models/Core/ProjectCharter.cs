using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;
using System.Text;


namespace IIM.Shared.Models
{

public class ProjectCharter
	{
		[Required]
		public ProjectOverview ProjectOverview { get; set; }

		[Required]
		public OperationalConsiderations OperationalConsiderations { get; set; }

		[Required]
		public SystemArchitecture SystemArchitecture { get; set; }

		public ProjectBudget ProjectBudget { get; set; }

		public List<ProjectRisk> ProjectRisks { get; set; } = new List<ProjectRisk>();

		[Required]
		public ComplianceSignoff ComplianceSignoff { get; set; }
	}

	public class ProjectOverview
	{
		[Required]
		public string ProjectName { get; set; }

		[Required]
		public string Description { get; set; }

		public string ProgramArea { get; set; }

		public string BusinessFunction { get; set; }

		public string DataSensitivity { get; set; } // Enum could be used for stricter type safety

		public string SensitivityReportReference { get; set; }

		public List<string> DataTypes { get; set; } = new List<string>();

		[Required]
		public bool BusinessCritical { get; set; }

		public string BusinessCriticalImpact { get; set; }
	}

	public class OperationalConsiderations
	{
		[Required]
		public bool StakeholderConsultation { get; set; }

		[Required]
		public string AcceptanceCriteriaDefined { get; set; }

		public string TestingConducted { get; set; }

		public string ResponsibleParty { get; set; }

		public bool IncidentResponseProcedures { get; set; }
	}

	public class SystemArchitecture
	{
		[Required]
		public string ArchitectureType { get; set; }

		public bool Redundancy { get; set; }

		public string RedundancyDetails { get; set; }

		public string SinglePointOfFailureAvoided { get; set; }

		public string HostingLocation { get; set; }

		public string AvailabilityRequirements { get; set; }

		public List<ProjectTimelineEntry> ProjectTimeline { get; set; } = new List<ProjectTimelineEntry>();
	}

	public class ProjectTimelineEntry
	{
		[Required]
		public string Milestone { get; set; }

		[Required]
		[DataType(DataType.Date)]
		public DateTime Date { get; set; }
	}

	public class ProjectBudget
	{
		[Required]
		public decimal TotalBudget { get; set; }

		[Required]
		public string Currency { get; set; }

		public BudgetBreakdown Breakdown { get; set; }
	}

	public class BudgetBreakdown
	{
		[Required]
		public decimal Development { get; set; }

		[Required]
		public decimal Marketing { get; set; }

		[Required]
		public decimal Testing { get; set; }
	}

	public class ProjectRisk
	{
		[Required]
		public string Risk { get; set; }

		[Required]
		public string Mitigation { get; set; }
	}

	public class ComplianceSignoff
	{
		[Required]
		public bool AdheresToPolicies { get; set; }

		public string AssessorName { get; set; }

		[DataType(DataType.Date)]
		public DateTime? AssessmentDate { get; set; } //Nullable DateTime

		public Signatures Signatures { get; set; }
	}

	public class Signatures
	{
		public string Assessor { get; set; }

		[Required]
		public string SystemOwner { get; set; }

		[Required]
		public string ProgramManager { get; set; }
	}
}
