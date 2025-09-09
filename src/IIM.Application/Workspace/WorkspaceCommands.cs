using IIM.Core.Mediator;
using IIM.Core.Services;
using IIM.Shared.Models;
using IIM.Shared.Enums;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using Mediator;
using IIM.Shared.Interfaces;

namespace IIM.Application.Case
{
    // ========================================
    // CREATE CASE COMMAND
    // ========================================

    /// <summary>
    /// Command to create a new investigation case
    /// </summary>
    public class CreateWorkspaceCommand : IRequest<IIM.Shared.Models.Workspace>
    {
        [Required]
        public string CaseNumber { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Owner { get; set; } = string.Empty;

        public List<string>? TeamMembers { get; set; }

        public string? Classification { get; set; }

        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Handler for creating a new case
    /// </summary>
    public class CreateWorkspaceCommandHandler : IRequestHandler<CreateWorkspaceCommand, Workspace>
    {
        private readonly ILogger<CreateWorkspaceCommandHandler> _logger;
        private readonly IWorkspaceManager _workspaceManager;
        private readonly IMediator _mediator;

        public CreateWorkspaceCommandHandler(
            ILogger<CreateWorkspaceCommandHandler> logger,
            IWorkspaceManager workspaceManager,
            IMediator mediator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<Workspace> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating new case {CaseNumber}: {Name}",
                request.CaseNumber, request.Name);

            // Parse case type
            if (!Enum.TryParse<WorkspaceType>(request.Type, true, out var caseType))
            {
                caseType = WorkspaceType.Other;
            }

            // Create the case
            var workspaceEntity = await _workspaceManager.CreateWorkspaceAsync(
                request.Name,
                request.Description,
                caseType,
                cancellationToken);

            // Update additional properties
            await _workspaceManager.UpdateWorkspaceAsync(workspaceEntity.Id, c =>
            {
                c.CaseNumber = request.CaseNumber;
                c.Owner = request.Owner;
                c.TeamMembers = request.TeamMembers ?? new List<string>();
                c.Classification = request.Classification ?? "Unclassified";
                c.Metadata = request.Metadata ?? new Dictionary<string, object>();
                c.Status = WorkspaceStatus.Open;
                c.Priority = WorkspacePriority.Medium;
            }, cancellationToken);

            // Get updated case
            var updatedCase = await _workspaceManager.GetWorkspaceAsync(workspaceEntity.Id, cancellationToken);

            // Publish notification
            await _mediator.Publish(new WorkspaceCreatedNotification
            {
                WorkspaceId = updatedCase!.Id,
                WorkspaceNumber = updatedCase.CaseNumber,
                Timestamp = DateTimeOffset.UtcNow
            }, cancellationToken);

            _logger.LogInformation("Workspace {CaseId} created successfully", updatedCase.Id);
            return updatedCase;
        }
    }

    // ========================================
    // GET Workspace COMMAND
    // ========================================

    /// <summary>
    /// Command to retrieve a workspace by ID
    /// </summary>
    public class GetWorkspaceCommand : IRequest<IIM.Shared.Models.Workspace?>
    {
        [Required]
        public string WorkspaceId { get; }

        public bool IncludeEvidence { get; }
        public bool IncludeSessions { get; }
        public bool IncludeReports { get; }
        public bool IncludeStatistics { get; }

        public GetWorkspaceCommand(
            string caseId,
            bool includeEvidence = false,
            bool includeSessions = false,
            bool includeReports = false,
            bool includeStatistics = true)
        {
            WorkspaceId = caseId ?? throw new ArgumentNullException(nameof(caseId));
            IncludeEvidence = includeEvidence;
            IncludeSessions = includeSessions;
            IncludeReports = includeReports;
            IncludeStatistics = includeStatistics;
        }
    }

    /// <summary>
    /// Handler for retrieving a case
    /// </summary>
    public class GetWorkspaceCommandHandler : IRequestHandler<GetWorkspaceCommand, IIM.Shared.Models.Workspace?>
    {
        private readonly ILogger<GetWorkspaceCommandHandler> _logger;
        private readonly IWorkspaceManager _workspaceManager;
        private readonly ISessionService _sessionService;
        private readonly IManagedFileManager _fileManager;

        public GetWorkspaceCommandHandler(
            ILogger<GetWorkspaceCommandHandler> logger,
            IWorkspaceManager workspaceManager,
            ISessionService sessionService,
            IManagedFileManager fileManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
        }

        public async Task<IIM.Shared.Models.Workspace?> Handle(GetWorkspaceCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Getting workspace {WorkspaceId}", request.WorkspaceId);

            var workspaceEntity = await _workspaceManager.GetWorkspaceAsync(request.WorkspaceId, cancellationToken);
            if (workspaceEntity == null)
            {
                _logger.LogWarning("Workspace {WorkspaceId} not found", request.WorkspaceId);
                return null;
            }

            // Include related data if requested
            if (request.IncludeSessions)
            {
                workspaceEntity.Sessions = await _sessionService.GetSessionsByCaseAsync(
                    request.WorkspaceId, cancellationToken);
            }

            if (request.IncludeEvidence)
            {
                workspaceEntity.Files = await _fileManager.GetFilesByWorkspaceAsync(
                    request.WorkspaceId, cancellationToken);
            }

            if (request.IncludeStatistics)
            {
                workspaceEntity.Statistics = new Dictionary<string, object>
                {
                    ["SessionCount"] = workspaceEntity.Sessions?.Count ?? 0,
                    ["EvidenceCount"] = workspaceEntity.Files?.Count ?? 0,
                    ["ReportCount"] = workspaceEntity.Reports?.Count ?? 0,
                    ["LastActivity"] = workspaceEntity.UpdatedAt
                };
            }

            return workspaceEntity;
        }
    }

    // ========================================
    // UPDATE Workspace COMMAND
    // ========================================

    /// <summary>
    /// Command to update a case
    /// </summary>
    public class UpdateWorkspaceCommand : IRequest<bool>
    {
        [Required]
        public string WorkspaceId { get; set; } = string.Empty;

        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? Owner { get; set; }
        public List<string>? TeamMembers { get; set; }
        public string? Classification { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Handler for updating a case
    /// </summary>
    public class UpdateWorkspaceCommandHandler : IRequestHandler<UpdateWorkspaceCommand, bool>
    {
        private readonly ILogger<UpdateWorkspaceCommandHandler> _logger;
        private readonly IWorkspaceManager _workspaceManager;
        private readonly IMediator _mediator;

        public UpdateWorkspaceCommandHandler(
            ILogger<UpdateWorkspaceCommandHandler> logger,
            IWorkspaceManager workspaceManager,
            IMediator mediator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<bool> Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating case {WorkspaceId}", request.WorkspaceId);

            var result = await _workspaceManager.UpdateWorkspaceAsync(request.WorkspaceId, workspaceEntity =>
            {
                if (!string.IsNullOrEmpty(request.Name))
                    workspaceEntity.Title = request.Name;

                if (!string.IsNullOrEmpty(request.Description))
                    workspaceEntity.Description = request.Description;

                if (!string.IsNullOrEmpty(request.Status) &&
                    Enum.TryParse<WorkspaceStatus>(request.Status, true, out var status))
                {
                    workspaceEntity.Status = status;
                    if (status == WorkspaceStatus.Closed)
                    {
                        workspaceEntity.ClosedAt = DateTimeOffset.UtcNow;
                    }
                }

                if (!string.IsNullOrEmpty(request.Priority) &&
                    Enum.TryParse<WorkspacePriority>(request.Priority, true, out var priority))
                {
                    workspaceEntity.Priority = priority;
                }

                if (!string.IsNullOrEmpty(request.Owner))
                    workspaceEntity.Owner = request.Owner;

                if (request.TeamMembers != null)
                    workspaceEntity.TeamMembers = request.TeamMembers;

                if (!string.IsNullOrEmpty(request.Classification))
                    workspaceEntity.Classification = request.Classification;

                if (request.Metadata != null)
                {
                    foreach (var kvp in request.Metadata)
                    {
                        workspaceEntity.Metadata[kvp.Key] = kvp.Value;
                    }
                }

                workspaceEntity.UpdatedAt = DateTimeOffset.UtcNow;
            }, cancellationToken);

            if (result)
            {
                await _mediator.Publish(new WorkspaceUpdatedNotification
                {
                    WorkspaceId = request.WorkspaceId,
                    Timestamp = DateTimeOffset.UtcNow
                }, cancellationToken);
            }

            return result;
        }
    }

    // ========================================
    // DELETE Workspace COMMAND
    // ========================================

    /// <summary>
    /// Command to delete a case
    /// </summary>
    public class DeleteWorkspaceCommand : IRequest<bool>
    {
        [Required]
        public string WorkspaceId { get; }

        public string? Reason { get; }
        public bool ArchiveOnly { get; }

        public DeleteWorkspaceCommand(string workspaceId, string? reason = null, bool archiveOnly = true)
        {
            WorkspaceId = workspaceId ?? throw new ArgumentNullException(nameof(workspaceId));
            Reason = reason;
            ArchiveOnly = archiveOnly;
        }
    }

    /// <summary>
    /// Handler for deleting a case
    /// </summary>
    public class DeleteWorkspaceCommandHandler : IRequestHandler<DeleteWorkspaceCommand, bool>
    {
        private readonly ILogger<DeleteWorkspaceCommandHandler> _logger;
        private readonly IWorkspaceManager _workspaceManager;
        private readonly IMediator _mediator;

        public DeleteWorkspaceCommandHandler(
            ILogger<DeleteWorkspaceCommandHandler> logger,
            IWorkspaceManager workspaceManager,
            IMediator mediator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<bool> Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting workspace {WorkspaceId}. Archive only: {ArchiveOnly}",
                request.WorkspaceId, request.ArchiveOnly);

            bool result;
            if (request.ArchiveOnly)
            {
                // Archive by updating status
                result = await _workspaceManager.UpdateWorkspaceAsync(request.WorkspaceId, c =>
                {
                    c.Status = WorkspaceStatus.Archived;
                    c.ClosedAt = DateTimeOffset.UtcNow;
                    c.ClosedBy = "System";
                }, cancellationToken);
            }
            else
            {
                // Soft delete
                result = await _workspaceManager.DeleteWorkspaceAsync(request.WorkspaceId, cancellationToken);
            }

            if (result)
            {
                await _mediator.Publish(new WorkspaceDeletedNotification
                {
                    WorkspaceId = request.WorkspaceId,
                    Reason = request.Reason,
                    ArchiveOnly = request.ArchiveOnly,
                    Timestamp = DateTimeOffset.UtcNow
                }, cancellationToken);

                _logger.LogInformation("Workspace {WorkspaceId} deleted/archived. Reason: {Reason}",
                    request.WorkspaceId, request.Reason ?? "Not specified");
            }

            return result;
        }
    }

    // ========================================
    // SEARCH Workspaces COMMAND
    // ========================================

    /// <summary>
    /// Command to search cases
    /// </summary>
    public class SearchWorkspacesCommand : IRequest<WorkspaceListResponse>
    {
        public string? SearchTerm { get; set; }
        public List<string>? WorkspaceNumbers { get; set; }
        public List<string>? Statuses { get; set; }
        public DateTimeOffset? CreatedAfter { get; set; }
        public DateTimeOffset? CreatedBefore { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "UpdatedAt";
        public bool SortDescending { get; set; } = true;
    }

    /// <summary>
    /// Handler for searching cases
    /// </summary>
    public class SearchWorkspacesCommandHandler : IRequestHandler<SearchWorkspacesCommand, WorkspaceListResponse>
    {
        private readonly ILogger<SearchWorkspacesCommandHandler> _logger;
        private readonly IWorkspaceManager _workspaceManager;

        public SearchWorkspacesCommandHandler(
            ILogger<SearchWorkspacesCommandHandler> logger,
            IWorkspaceManager workspaceManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
        }

        public async Task<WorkspaceListResponse> Handle(SearchWorkspacesCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Searching workspaces with term: {SearchTerm}", request.SearchTerm);

            // Get all cases (in a real implementation, this would be filtered at the database level)
            var allCases = await _workspaceManager.GetUserWorkspacesAsync(null, cancellationToken);

            // Apply filters
            var query = allCases.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLowerInvariant();
                query = query.Where(c =>
                    c.CaseNumber.ToLowerInvariant().Contains(term) ||
                    c.Title.ToLowerInvariant().Contains(term) ||
                    c.Description.ToLowerInvariant().Contains(term));
            }

            if (request.WorkspaceNumbers?.Any() == true)
            {
                query = query.Where(c => request.WorkspaceNumbers.Contains(c.CaseNumber));
            }

            if (request.Statuses?.Any() == true)
            {
                var statusEnums = request.Statuses
                    .Select(s => Enum.TryParse<WorkspaceStatus>(s, true, out var status) ? status : (WorkspaceStatus?)null)
                    .Where(s => s.HasValue)
                    .Select(s => s!.Value)
                    .ToList();

                query = query.Where(c => statusEnums.Contains(c.Status));
            }

            if (request.CreatedAfter.HasValue)
            {
                query = query.Where(c => c.CreatedAt >= request.CreatedAfter.Value);
            }

            if (request.CreatedBefore.HasValue)
            {
                query = query.Where(c => c.CreatedAt <= request.CreatedBefore.Value);
            }

            // Sort
            query = request.SortBy.ToLowerInvariant() switch
            {
                "casenumber" => request.SortDescending
                    ? query.OrderByDescending(c => c.CaseNumber)
                    : query.OrderBy(c => c.CaseNumber),
                "title" or "name" => request.SortDescending
                    ? query.OrderByDescending(c => c.Title)
                    : query.OrderBy(c => c.Title),
                "createdat" => request.SortDescending
                    ? query.OrderByDescending(c => c.CreatedAt)
                    : query.OrderBy(c => c.CreatedAt),
                _ => request.SortDescending
                    ? query.OrderByDescending(c => c.UpdatedAt)
                    : query.OrderBy(c => c.UpdatedAt)
            };

            // Get total count
            var totalCount = query.Count();

            // Apply pagination
            var workspaces = query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Map to summary DTOs
            var summaries = workspaces.Select(c => new WorkspaceSummary
            {
                Id = c.Id,
                CaseNumber = c.CaseNumber,
                Name = c.Title,
                Type = c.Type.ToString(),
                Status = c.Status.ToString(),
                Classification = c.Classification,
                UpdatedAt = c.UpdatedAt,
                FileCount = c.Files?.Count ?? 0,
                ActiveSessions = c.Sessions?.Count(s => s.Status == InvestigationStatus.Active) ?? 0
            }).ToList();

            return new WorkspaceListResponse
            {
                Cases = summaries,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }

    // ========================================
    // GET CASE STATISTICS COMMAND
    // ========================================

    /// <summary>
    /// Command to get case statistics
    /// </summary>
    public class GetWorkspaceStatisticsCommand : IRequest<WorkspaceStatistics>
    {
        public string? WorkspaceId { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public bool IncludeEvidenceStats { get; set; } = true;
        public bool IncludeSessionStats { get; set; } = true;
    }

    /// <summary>
    /// Handler for getting case statistics
    /// </summary>
    public class GetWorkspaceStatisticsCommandHandler : IRequestHandler<GetWorkspaceStatisticsCommand, WorkspaceStatistics>
    {
        private readonly IWorkspaceManager _workspaceManager;
        private readonly ISessionService _sessionService;
        private readonly IManagedFileManager _evidenceManager;

        public GetWorkspaceStatisticsCommandHandler(
            IWorkspaceManager workspaceManager,
            ISessionService sessionService,
            IManagedFileManager evidenceManager)
        {
            _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _evidenceManager = evidenceManager ?? throw new ArgumentNullException(nameof(evidenceManager));
        }

        public async Task<WorkspaceStatistics> Handle(GetWorkspaceStatisticsCommand request, CancellationToken cancellationToken)
        {
            var stats = new WorkspaceStatistics();

            if (!string.IsNullOrEmpty(request.WorkspaceId))
            {
                // Get stats for specific case
                var workspaceEntity = await _workspaceManager.GetWorkspaceAsync(request.WorkspaceId, cancellationToken);
                if (workspaceEntity != null)
                {
                    var sessions = await _sessionService.GetSessionsByCaseAsync(request.WorkspaceId, cancellationToken);
                    var evidence = await _evidenceManager.GetFilesByWorkspaceAsync(request.WorkspaceId, cancellationToken);

                    stats.TotalFiles = evidence.Count;
                    stats.TotalFileSize = evidence.Sum(e => e.FileSize);
                    stats.TotalSessions = sessions.Count;
                    stats.ActiveSessions = sessions.Count(s => s.Status == InvestigationStatus.Active);
                    stats.TotalReports = workspaceEntity.Reports?.Count ?? 0;
                    stats.TotalFindings = workspaceEntity.Findings?.Count ?? 0;

                    // Evidence by type
                    stats.FilesByType = evidence
                        .GroupBy(e => e.Type.ToString())
                        .ToDictionary(g => g.Key, g => g.Count());

                    // Findings by severity
                    if (workspaceEntity.Findings?.Any() == true)
                    {
                        stats.FilesBySeverity = workspaceEntity.Findings
                            .GroupBy(f => f.Severity.ToString())
                            .ToDictionary(g => g.Key, g => g.Count());
                    }

                    // Calculate total investigation time
                    if (sessions.Any())
                    {
                        var earliestSession = sessions.Min(s => s.CreatedAt);
                        var latestUpdate = sessions.Max(s => s.UpdatedAt);
                        stats.TotalTime = latestUpdate - earliestSession;
                    }
                }
            }

            return stats;
        }
    }

    // ========================================
    // Workspace NOTIFICATIONS
    // ========================================

    /// <summary>
    /// Notification when a workspace is created
    /// </summary>
    public class WorkspaceCreatedNotification : INotification
    {
        public string WorkspaceId { get; set; } = string.Empty;
        public string WorkspaceNumber { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }

    /// <summary>
    /// Notification when a workspace is updated
    /// </summary>
    public class WorkspaceUpdatedNotification : INotification
    {
        public string WorkspaceId { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }

    /// <summary>
    /// Notification when a workspace is deleted
    /// </summary>
    public class WorkspaceDeletedNotification : INotification
    {
        public string WorkspaceId { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public bool ArchiveOnly { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

    // ========================================
    // GET RECENT Workspaces COMMAND
    // ========================================

    /// <summary>
    /// Command to get recent workspaces
    /// </summary>
    public class GetRecentWorspacesCommand : IRequest<List<Workspace>>
    {
        public int Count { get; }
        public string? UserId { get; }

        public GetRecentWorspacesCommand(int count = 10, string? userId = null)
        {
            Count = count > 0 ? count : 10;
            UserId = userId;
        }
    }

    /// <summary>
    /// Handler for getting recent cases
    /// </summary>
    public class GetRecentWorkspacesCommandHandler : IRequestHandler<GetRecentWorspacesCommand, List<IIM.Shared.Models.Workspace>>
    {
        private readonly IWorkspaceManager _workspaceManager;

        public GetRecentWorkspacesCommandHandler(IWorkspaceManager workspaceManager)
        {
            _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
        }

        public async Task<List<IIM.Shared.Models.Workspace>> Handle(GetRecentWorspacesCommand request, CancellationToken cancellationToken)
        {
            return await _workspaceManager.GetRecentWorkspacesAsync(request.Count, cancellationToken);
        }
    }
}