using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using IIM.Shared.Enums;

namespace IIM.Shared.Models
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
        public Guid OwnerId { get; set; }
		public ICollection<WorkspaceUser> Users { get; set; } = new List<WorkspaceUser>();
        public ICollection<WorkspaceFile> Files { get; set; } = new List<WorkspaceFile>();
        public ICollection<WorkspaceSession> Sessions { get; set; } = new List<WorkspaceSession>();
		public ICollection<TimelineEvent> TimelineEvents { get; set; } = new List<TimelineEvent>();

		public ICollection<WorkspaceArtifact> Artifacts { get; set; } = new List<WorkspaceArtifact>();

	}

	/// <summary>
	/// Represents a user's role and association with a specific workspace.
	/// </summary>
	public class WorkspaceUser
	{
		public Guid WorkspaceId { get; set; }
		public string UserId { get; set; } = null!;
		public WorkspaceRole Role { get; set; } = WorkspaceRole.Owner;
		public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;

		// Non-mapped enrichment fields
		[NotMapped]
		public string? DisplayName { get; set; }

		[NotMapped]
		public string? Email { get; set; }

		[NotMapped]
		public ApplicationUser? User { get; set; }
	}


	public enum WorkspaceRole
    {
        Owner,
        Editor,
        Viewer
	}

	public class WorkspaceArtifact
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public Guid WorkspaceId { get; set; }

		public ArtifactType Type { get; set; }

		public string Title { get; set; } = "";
		public string Summary { get; set; } = "";
		public string Content { get; set; } = "";

		public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
		public DateTime? UpdatedUtc { get; set; }

		public bool IsDeleted { get; set; } = false;

		public List<string> Tags { get; set; } = new();
	}

	public class WorkspaceFile
	{
		public Guid WorkspaceId { get; set; }
		public Workspace Workspace { get; set; }

		public Guid VirtualFileId { get; set; }
		public VirtualFile VirtualFile { get; set; }

		public DateTimeOffset LinkedAt { get; set; } = DateTimeOffset.UtcNow;
	}


	public class WorkspaceSession
	{
		public Guid WorkspaceId { get; set; }
		public Workspace Workspace { get; set; }

		public Guid SessionId { get; set; }


		public DateTimeOffset LinkedAt { get; set; } = DateTimeOffset.UtcNow;
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
