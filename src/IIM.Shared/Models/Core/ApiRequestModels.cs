using IIM.Shared.Enums;
using System;
using System.Collections.Generic;

namespace IIM.Shared.Models
{
    // These models are used as Data Transfer Objects (DTOs) for API endpoints.

    public class InitiateFileUploadResponse
    {
        public bool IsDuplicate { get; set; }
        public string? TransactionId { get; set; } // A unique ID for this upload transaction
        public string? UploadUrl { get; set; }
        public VirtualFile? VirtualFile { get; set; } // The created VirtualFile if it's a duplicate
    }

    public class CreateWorkspaceRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public WorkspaceType Type { get; set; } = WorkspaceType.Undefined;

		public string? OwnerId { get; set; }
    }

    public class SearchWorkspacesRequest
    {
        public string? Query { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class BatchUpdateWorkspaceRequest
    {
        public List<Guid> WorkspaceIds { get; set; } = new();
        public string? Status { get; set; }
        public string? Assignee { get; set; }
    }


    public record UploadRequest(
        Guid WorkspaceId,
        string FileName,
        bool RequiresQuarantine = true
    );

    public record UploadResponse(
        string UploadUrl,
        string Bucket,
        string ObjectKey,
        DateTimeOffset ExpiresAt
    );
}


