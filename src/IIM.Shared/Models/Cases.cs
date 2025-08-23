using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Represents an investigation case with extended properties.
    /// </summary>
    public class Case
    {
        // Existing core properties
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CaseNumber { get; set; } = string.Empty;
        public CaseType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public CaseStatus Status { get; set; }
        public CasePriority Priority { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string Classification { get; set; }
        public List<string> AccessControlList { get; set; }

        // New optional properties
        public string? Name { get; set; }  // Case name (alias for Title)
        public string? ModelTemplateId { get; set; }  // Default model configuration
        public List<InvestigationSession>? Sessions { get; set; }  // All sessions for this case
        public List<Timeline> Timelines { get; set; }
        public List<Evidence>? Evidence { get; set; }  // All evidence
        public List<Finding>? Findings { get; set; }  // Key findings
        public List<string>? AssignedTo { get; set; }  // Assigned investigators
        public List<Report> Reports { get; set; }
        public string? LeadInvestigator { get; set; }  // Lead investigator
        public List<string>? TeamMembers { get; set; }  // Investigation team
        public DateTimeOffset? ClosedAt { get; set; }  // When case was closed
        public string? ClosedBy { get; set; }  // Who closed it
        public Dictionary<string, object>? Statistics { get; set; }  // Case metrics

        // Computed properties for convenience
        public string GetName() => Name ?? Title;
    }

}
