using IIM.Shared.Enums;
using System;
using System.Collections.Generic;

namespace IIM.Shared.Models.Core
{
    // These models are used as Data Transfer Objects (DTOs) for API endpoints.

    public class CreateWorkspaceRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public WorkspaceType Type { get; set; }
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
}
