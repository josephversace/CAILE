using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;


namespace IIM.Shared.Models
{
    /// <summary>
    /// Domain model representing an investigation case
    /// </summary>
    public class Case
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CaseNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public CaseType Type { get; set; }
        public CaseStatus Status { get; set; } = CaseStatus.Open;
        public CasePriority Priority { get; set; } = CasePriority.Medium;
        public string Classification { get; set; } = "UNCLASSIFIED";

        // Relationships
        public string LeadInvestigator { get; set; } = string.Empty;
        public List<string> TeamMembers { get; set; } = new();
        public List<string> AccessControlList { get; set; } = new();

        // Timestamps
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ClosedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? ClosedBy { get; set; }

        // Collections (lazy-loaded in practice)
        public List<Evidence> Evidence { get; set; } = new();
        public List<InvestigationSession> Sessions { get; set; } = new();
        public List<Timeline> Timelines { get; set; } = new();
        public List<Report> Reports { get; set; } = new();
        public List<Finding> Findings { get; set; } = new();

        // Metadata
        public Dictionary<string, object> Metadata { get; set; } = new();

        // Business Methods

        /// <summary>
        /// Assigns a new lead investigator to the case
        /// </summary>
        public void AssignLeadInvestigator(string investigatorId)
        {
            if (string.IsNullOrWhiteSpace(investigatorId))
                throw new ArgumentException("Investigator ID cannot be empty");

            LeadInvestigator = investigatorId;
            if (!TeamMembers.Contains(investigatorId))
                TeamMembers.Add(investigatorId);

            UpdatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Adds a team member to the case
        /// </summary>
        public void AddTeamMember(string memberId)
        {
            if (!TeamMembers.Contains(memberId))
            {
                TeamMembers.Add(memberId);
                UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        /// <summary>
        /// Determines if the case can be closed
        /// </summary>
        public bool CanClose()
        {
            // Case can be closed if all evidence is processed and all sessions are closed
            var allEvidenceProcessed = Evidence.All(e =>
                e.Status == EvidenceStatus.Processed ||
                e.Status == EvidenceStatus.Archived);

            var allSessionsClosed = Sessions.All(s =>
                s.Status == InvestigationStatus.Completed ||
                s.Status == InvestigationStatus.Archived);

            return allEvidenceProcessed && allSessionsClosed;
        }

        /// <summary>
        /// Closes the case
        /// </summary>
        public void Close(string closedBy)
        {
            if (!CanClose())
                throw new InvalidOperationException("Case cannot be closed - pending evidence or sessions");

            Status = CaseStatus.Closed;
            ClosedAt = DateTimeOffset.UtcNow;
            ClosedBy = closedBy;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Reopens a closed case
        /// </summary>
        public void Reopen(string reopenedBy)
        {
            if (Status != CaseStatus.Closed)
                throw new InvalidOperationException("Only closed cases can be reopened");

            Status = CaseStatus.Open;
            ClosedAt = null;
            ClosedBy = null;
            UpdatedAt = DateTimeOffset.UtcNow;

            Metadata["ReopenedBy"] = reopenedBy;
            Metadata["ReopenedAt"] = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Updates the case priority
        /// </summary>
        public void UpdatePriority(CasePriority newPriority, string reason)
        {
            var oldPriority = Priority;
            Priority = newPriority;
            UpdatedAt = DateTimeOffset.UtcNow;

            Metadata[$"PriorityChange_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}"] = new
            {
                From = oldPriority.ToString(),
                To = newPriority.ToString(),
                Reason = reason
            };
        }

        /// <summary>
        /// Checks if a user has access to this case
        /// </summary>
        public bool HasAccess(string userId)
        {
            return LeadInvestigator == userId ||
                   TeamMembers.Contains(userId) ||
                   AccessControlList.Contains(userId);
        }
    }
}