using IIM.Shared.Enums;
using System;
using System.Collections.Generic;

namespace IIM.Shared.Models.Core
{
    /// <summary>
    /// Represents a workspace, which is the primary container for investigations, files, and collaboration.
    /// Replaces the legacy "Case" concept.
    /// </summary>
    public class Workspace
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public WorkspaceType Type { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }

        public bool IsPublic { get; set; } = false;

        public ICollection<WorkspaceUser> Users { get; set; } = new List<WorkspaceUser>();
        public ICollection<VirtualFile> Files { get; set; } = new List<VirtualFile>();
        public ICollection<InvestigationSession> Sessions { get; set; } = new List<InvestigationSession>();
    }

    /// <summary>
    /// Represents a user's role and association with a specific workspace.
    /// </summary>
    public class WorkspaceUser
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = "Member"; // e.g., "Owner", "Member", "Viewer"
    }

    public class WorkspaceSummary
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTimeOffset UpdatedAt { get; set; }
        public int FileCount { get; set; }
        public int ActiveSessions { get; set; }

       
    }

    /// <summary>
    /// Case list response model
    /// </summary>
    public class WorkspaceListResponse
    {
        public List<WorkspaceSummary> Cases { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }



    /// <summary>
    /// Case statistics model
    /// </summary>
    public class WorkspaceStatistics
    {
        public int TotalFiles { get; set; }
        public long TotalFileSize { get; set; }
        public int TotalSessions { get; set; }
        public int ActiveSessions { get; set; }
        public int TotalReports { get; set; }
        public int TotalFindings { get; set; }
        public TimeSpan TotalTime { get; set; }
        public Dictionary<string, int> FilesByType { get; set; } = new();
        public Dictionary<string, int> FilesBySeverity { get; set; } = new();
    }



    /// <summary>
    /// Represents a single, auditable event in the timeline of a workspace.
    /// </summary>
    public class TimelineEvent
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string EventType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public Guid? AssociatedEntityId { get; set; }
        public string? AssociatedEntityType { get; set; }
    }
}
