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
        private readonly IWorkspaceManager _caseManager;
        private readonly IMediator _mediator;

        public CreateWorkspaceCommandHandler(
            ILogger<CreateWorkspaceCommandHandler> logger,
            IWorkspaceManager caseManager,
            IMediator mediator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _caseManager = caseManager ?? throw new ArgumentNullException(nameof(caseManager));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<Workspace> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating new case {CaseNumber}: {Name}",
                request.CaseNumber, request.Name);

            // Parse case type
            if (!Enum.TryParse<CaseType>(request.Type, true, out var caseType))
            {
                caseType = CaseType.Other;
            }

            // Create the case
            var caseEntity = await _caseManager.CreateWorkspceAsync(
                request.Name,
                request.Description,
                caseType,
                cancellationToken);

            // Update additional properties
            await _caseManager.UpdateWorkspaceAsync(caseEntity.Id, c =>
            {
                c.CaseNumber = request.CaseNumber;
                c.Owner = request.Owner;
                c.TeamMembers = request.TeamMembers ?? new List<string>();
                c.Classification = request.Classification ?? "Unclassified";
                c.Metadata = request.Metadata ?? new Dictionary<string, object>();
                c.Status = CaseStatus.Open;
                c.Priority = CasePriority.Medium;
            }, cancellationToken);

            // Get updated case
            var updatedCase = await _caseManager.GetWorkspaceAsync(caseEntity.Id, cancellationToken);

            // Publish notification
            await _mediator.Publish(new WorkspaceCreatedNotification
            {
                CaseId = updatedCase!.Id,
                CaseNumber = updatedCase.CaseNumber,
                Timestamp = DateTimeOffset.UtcNow
            }, cancellationToken);

            _logger.LogInformation("Case {CaseId} created successfully", updatedCase.Id);
            return updatedCase;
        }
    }

    // ========================================
    // GET CASE COMMAND
    // ========================================

    /// <summary>
    /// Command to retrieve a case by ID
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
        private readonly IWorkspaceManager _caseManager;
        private readonly ISessionService _sessionService;
        private readonly IManagedFileManager _evidenceManager;

        public GetWorkspaceCommandHandler(
            ILogger<GetWorkspaceCommandHandler> logger,
            IWorkspaceManager caseManager,
            ISessionService sessionService,
            IManagedFileManager evidenceManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _caseManager = caseManager ?? throw new ArgumentNullException(nameof(caseManager));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _evidenceManager = evidenceManager ?? throw new ArgumentNullException(nameof(evidenceManager));
        }

        public async Task<IIM.Shared.Models.Workspace?> Handle(GetWorkspaceCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Getting workspace {CaseId}", request.WorkspaceId);

            var caseEntity = await _caseManager.GetWorkspaceAsync(request.WorkspaceId, cancellationToken);
            if (caseEntity == null)
            {
                _logger.LogWarning("Workspace {CaseId} not found", request.WorkspaceId);
                return null;
            }

            // Include related data if requested
            if (request.IncludeSessions)
            {
                caseEntity.Sessions = await _sessionService.GetSessionsByCaseAsync(
                    request.WorkspaceId, cancellationToken);
            }

            if (request.IncludeEvidence)
            {
                caseEntity.Files = await _evidenceManager.GetFilesByWorkspaceAsync(
                    request.WorkspaceId, cancellationToken);
            }

            if (request.IncludeStatistics)
            {
                caseEntity.Statistics = new Dictionary<string, object>
                {
                    ["SessionCount"] = caseEntity.Sessions?.Count ?? 0,
                    ["EvidenceCount"] = caseEntity.Files?.Count ?? 0,
                    ["ReportCount"] = caseEntity.Reports?.Count ?? 0,
                    ["LastActivity"] = caseEntity.UpdatedAt
                };
            }

            return caseEntity;
        }
    }

    // ========================================
    // UPDATE CASE COMMAND
    // ========================================

    /// <summary>
    /// Command to update a case
    /// </summary>
    public class UpdateWorkspaceCommand : IRequest<bool>
    {
        [Required]
        public string CaseId { get; set; } = string.Empty;

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
        private readonly IWorkspaceManager _caseManager;
        private readonly IMediator _mediator;

        public UpdateWorkspaceCommandHandler(
            ILogger<UpdateWorkspaceCommandHandler> logger,
            IWorkspaceManager caseManager,
            IMediator mediator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _caseManager = caseManager ?? throw new ArgumentNullException(nameof(caseManager));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<bool> Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating case {CaseId}", request.CaseId);

            var result = await _caseManager.UpdateWorkspaceAsync(request.CaseId, caseEntity =>
            {
                if (!string.IsNullOrEmpty(request.Name))
                    caseEntity.Title = request.Name;

                if (!string.IsNullOrEmpty(request.Description))
                    caseEntity.Description = request.Description;

                if (!string.IsNullOrEmpty(request.Status) &&
                    Enum.TryParse<CaseStatus>(request.Status, true, out var status))
                {
                    caseEntity.Status = status;
                    if (status == CaseStatus.Closed)
                    {
                        caseEntity.ClosedAt = DateTimeOffset.UtcNow;
                    }
                }

                if (!string.IsNullOrEmpty(request.Priority) &&
                    Enum.TryParse<CasePriority>(request.Priority, true, out var priority))
                {
                    caseEntity.Priority = priority;
                }

                if (!string.IsNullOrEmpty(request.Owner))
                    caseEntity.Owner = request.Owner;

                if (request.TeamMembers != null)
                    caseEntity.TeamMembers = request.TeamMembers;

                if (!string.IsNullOrEmpty(request.Classification))
                    caseEntity.Classification = request.Classification;

                if (request.Metadata != null)
                {
                    foreach (var kvp in request.Metadata)
                    {
                        caseEntity.Metadata[kvp.Key] = kvp.Value;
                    }
                }

                caseEntity.UpdatedAt = DateTimeOffset.UtcNow;
            }, cancellationToken);

            if (result)
            {
                await _mediator.Publish(new WorkspaceUpdatedNotification
                {
                    CaseId = request.CaseId,
                    Timestamp = DateTimeOffset.UtcNow
                }, cancellationToken);
            }

            return result;
        }
    }

    // ========================================
    // DELETE CASE COMMAND
    // ========================================

    /// <summary>
    /// Command to delete a case
    /// </summary>
    public class DeleteWorkspaceCommand : IRequest<bool>
    {
        [Required]
        public string CaseId { get; }

        public string? Reason { get; }
        public bool ArchiveOnly { get; }

        public DeleteWorkspaceCommand(string caseId, string? reason = null, bool archiveOnly = true)
        {
            CaseId = caseId ?? throw new ArgumentNullException(nameof(caseId));
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
        private readonly IWorkspaceManager _caseManager;
        private readonly IMediator _mediator;

        public DeleteWorkspaceCommandHandler(
            ILogger<DeleteWorkspaceCommandHandler> logger,
            IWorkspaceManager caseManager,
            IMediator mediator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _caseManager = caseManager ?? throw new ArgumentNullException(nameof(caseManager));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<bool> Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting case {CaseId}. Archive only: {ArchiveOnly}",
                request.CaseId, request.ArchiveOnly);

            bool result;
            if (request.ArchiveOnly)
            {
                // Archive by updating status
                result = await _caseManager.UpdateWorkspaceAsync(request.CaseId, c =>
                {
                    c.Status = CaseStatus.Archived;
                    c.ClosedAt = DateTimeOffset.UtcNow;
                    c.ClosedBy = "System";
                }, cancellationToken);
            }
            else
            {
                // Soft delete
                result = await _caseManager.DeleteWorkspaceAsync(request.CaseId, cancellationToken);
            }

            if (result)
            {
                await _mediator.Publish(new WorkspaceDeletedNotification
                {
                    WorkspaceId = request.CaseId,
                    Reason = request.Reason,
                    ArchiveOnly = request.ArchiveOnly,
                    Timestamp = DateTimeOffset.UtcNow
                }, cancellationToken);

                _logger.LogInformation("Case {CaseId} deleted/archived. Reason: {Reason}",
                    request.CaseId, request.Reason ?? "Not specified");
            }

            return result;
        }
    }

    // ========================================
    // SEARCH CASES COMMAND
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
        private readonly IWorkspaceManager _caseManager;

        public SearchWorkspacesCommandHandler(
            ILogger<SearchWorkspacesCommandHandler> logger,
            IWorkspaceManager caseManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _caseManager = caseManager ?? throw new ArgumentNullException(nameof(caseManager));
        }

        public async Task<WorkspaceListResponse> Handle(SearchWorkspacesCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Searching workspaces with term: {SearchTerm}", request.SearchTerm);

            // Get all cases (in a real implementation, this would be filtered at the database level)
            var allCases = await _caseManager.GetUserWorkspacesAsync(null, cancellationToken);

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
                    .Select(s => Enum.TryParse<CaseStatus>(s, true, out var status) ? status : (CaseStatus?)null)
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
                EvidenceCount = c.Files?.Count ?? 0,
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
        private readonly IWorkspaceManager _caseManager;
        private readonly ISessionService _sessionService;
        private readonly IManagedFileManager _evidenceManager;

        public GetWorkspaceStatisticsCommandHandler(
            IWorkspaceManager caseManager,
            ISessionService sessionService,
            IManagedFileManager evidenceManager)
        {
            _caseManager = caseManager ?? throw new ArgumentNullException(nameof(caseManager));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _evidenceManager = evidenceManager ?? throw new ArgumentNullException(nameof(evidenceManager));
        }

        public async Task<WorkspaceStatistics> Handle(GetWorkspaceStatisticsCommand request, CancellationToken cancellationToken)
        {
            var stats = new WorkspaceStatistics();

            if (!string.IsNullOrEmpty(request.WorkspaceId))
            {
                // Get stats for specific case
                var caseEntity = await _caseManager.GetWorkspaceAsync(request.WorkspaceId, cancellationToken);
                if (caseEntity != null)
                {
                    var sessions = await _sessionService.GetSessionsByCaseAsync(request.WorkspaceId, cancellationToken);
                    var evidence = await _evidenceManager.GetFilesByWorkspaceAsync(request.WorkspaceId, cancellationToken);

                    stats.TotalFiles = evidence.Count;
                    stats.TotalFileSize = evidence.Sum(e => e.FileSize);
                    stats.TotalSessions = sessions.Count;
                    stats.ActiveSessions = sessions.Count(s => s.Status == InvestigationStatus.Active);
                    stats.TotalReports = caseEntity.Reports?.Count ?? 0;
                    stats.TotalFindings = caseEntity.Findings?.Count ?? 0;

                    // Evidence by type
                    stats.FilesByType = evidence
                        .GroupBy(e => e.Type.ToString())
                        .ToDictionary(g => g.Key, g => g.Count());

                    // Findings by severity
                    if (caseEntity.Findings?.Any() == true)
                    {
                        stats.FilesBySeverity = caseEntity.Findings
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
    // CASE NOTIFICATIONS
    // ========================================

    /// <summary>
    /// Notification when a case is created
    /// </summary>
    public class WorkspaceCreatedNotification : INotification
    {
        public string CaseId { get; set; } = string.Empty;
        public string CaseNumber { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }

    /// <summary>
    /// Notification when a case is updated
    /// </summary>
    public class WorkspaceUpdatedNotification : INotification
    {
        public string CaseId { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }

    /// <summary>
    /// Notification when a case is deleted
    /// </summary>
    public class WorkspaceDeletedNotification : INotification
    {
        public string WorkspaceId { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public bool ArchiveOnly { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

    // ========================================
    // GET RECENT CASES COMMAND
    // ========================================

    /// <summary>
    /// Command to get recent cases
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
        private readonly IWorkspaceManager _caseManager;

        public GetRecentWorkspacesCommandHandler(IWorkspaceManager caseManager)
        {
            _caseManager = caseManager ?? throw new ArgumentNullException(nameof(caseManager));
        }

        public async Task<List<IIM.Shared.Models.Workspace>> Handle(GetRecentWorspacesCommand request, CancellationToken cancellationToken)
        {
            return await _caseManager.GetRecentWorkspacesAsync(request.Count, cancellationToken);
        }
    }
}