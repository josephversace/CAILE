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
    public class CreateCaseCommand : IRequest<IIM.Shared.Models.Case>
    {
        [Required]
        public string CaseNumber { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string LeadInvestigator { get; set; } = string.Empty;

        public List<string>? TeamMembers { get; set; }

        public string? Classification { get; set; }

        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Handler for creating a new case
    /// </summary>
    public class CreateCaseCommandHandler : IRequestHandler<CreateCaseCommand, IIM.Shared.Models.Case>
    {
        private readonly ILogger<CreateCaseCommandHandler> _logger;
        private readonly ICaseManager _caseManager;
        private readonly IMediator _mediator;

        public CreateCaseCommandHandler(
            ILogger<CreateCaseCommandHandler> logger,
            ICaseManager caseManager,
            IMediator mediator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _caseManager = caseManager ?? throw new ArgumentNullException(nameof(caseManager));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<IIM.Shared.Models.Case> Handle(CreateCaseCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating new case {CaseNumber}: {Name}",
                request.CaseNumber, request.Name);

            // Parse case type
            if (!Enum.TryParse<CaseType>(request.Type, true, out var caseType))
            {
                caseType = CaseType.Other;
            }

            // Create the case
            var caseEntity = await _caseManager.CreateCaseAsync(
                request.Name,
                request.Description,
                caseType,
                cancellationToken);

            // Update additional properties
            await _caseManager.UpdateCaseAsync(caseEntity.Id, c =>
            {
                c.CaseNumber = request.CaseNumber;
                c.LeadInvestigator = request.LeadInvestigator;
                c.TeamMembers = request.TeamMembers ?? new List<string>();
                c.Classification = request.Classification ?? "Unclassified";
                c.Metadata = request.Metadata ?? new Dictionary<string, object>();
                c.Status = CaseStatus.Open;
                c.Priority = CasePriority.Medium;
            }, cancellationToken);

            // Get updated case
            var updatedCase = await _caseManager.GetCaseAsync(caseEntity.Id, cancellationToken);

            // Publish notification
            await _mediator.Publish(new CaseCreatedNotification
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
    public class GetCaseCommand : IRequest<IIM.Shared.Models.Case?>
    {
        [Required]
        public string CaseId { get; }

        public bool IncludeEvidence { get; }
        public bool IncludeSessions { get; }
        public bool IncludeReports { get; }
        public bool IncludeStatistics { get; }

        public GetCaseCommand(
            string caseId,
            bool includeEvidence = false,
            bool includeSessions = false,
            bool includeReports = false,
            bool includeStatistics = true)
        {
            CaseId = caseId ?? throw new ArgumentNullException(nameof(caseId));
            IncludeEvidence = includeEvidence;
            IncludeSessions = includeSessions;
            IncludeReports = includeReports;
            IncludeStatistics = includeStatistics;
        }
    }

    /// <summary>
    /// Handler for retrieving a case
    /// </summary>
    public class GetCaseCommandHandler : IRequestHandler<GetCaseCommand, IIM.Shared.Models.Case?>
    {
        private readonly ILogger<GetCaseCommandHandler> _logger;
        private readonly ICaseManager _caseManager;
        private readonly ISessionService _sessionService;
        private readonly IEvidenceManager _evidenceManager;

        public GetCaseCommandHandler(
            ILogger<GetCaseCommandHandler> logger,
            ICaseManager caseManager,
            ISessionService sessionService,
            IEvidenceManager evidenceManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _caseManager = caseManager ?? throw new ArgumentNullException(nameof(caseManager));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _evidenceManager = evidenceManager ?? throw new ArgumentNullException(nameof(evidenceManager));
        }

        public async Task<IIM.Shared.Models.Case?> Handle(GetCaseCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Getting case {CaseId}", request.CaseId);

            var caseEntity = await _caseManager.GetCaseAsync(request.CaseId, cancellationToken);
            if (caseEntity == null)
            {
                _logger.LogWarning("Case {CaseId} not found", request.CaseId);
                return null;
            }

            // Include related data if requested
            if (request.IncludeSessions)
            {
                caseEntity.Sessions = await _sessionService.GetSessionsByCaseAsync(
                    request.CaseId, cancellationToken);
            }

            if (request.IncludeEvidence)
            {
                caseEntity.Evidence = await _evidenceManager.GetEvidenceByCaseAsync(
                    request.CaseId, cancellationToken);
            }

            if (request.IncludeStatistics)
            {
                caseEntity.Statistics = new Dictionary<string, object>
                {
                    ["SessionCount"] = caseEntity.Sessions?.Count ?? 0,
                    ["EvidenceCount"] = caseEntity.Evidence?.Count ?? 0,
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
    public class UpdateCaseCommand : IRequest<bool>
    {
        [Required]
        public string CaseId { get; set; } = string.Empty;

        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? LeadInvestigator { get; set; }
        public List<string>? TeamMembers { get; set; }
        public string? Classification { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Handler for updating a case
    /// </summary>
    public class UpdateCaseCommandHandler : IRequestHandler<UpdateCaseCommand, bool>
    {
        private readonly ILogger<UpdateCaseCommandHandler> _logger;
        private readonly ICaseManager _caseManager;
        private readonly IMediator _mediator;

        public UpdateCaseCommandHandler(
            ILogger<UpdateCaseCommandHandler> logger,
            ICaseManager caseManager,
            IMediator mediator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _caseManager = caseManager ?? throw new ArgumentNullException(nameof(caseManager));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<bool> Handle(UpdateCaseCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating case {CaseId}", request.CaseId);

            var result = await _caseManager.UpdateCaseAsync(request.CaseId, caseEntity =>
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

                if (!string.IsNullOrEmpty(request.LeadInvestigator))
                    caseEntity.LeadInvestigator = request.LeadInvestigator;

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
                await _mediator.Publish(new CaseUpdatedNotification
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
    public class DeleteCaseCommand : IRequest<bool>
    {
        [Required]
        public string CaseId { get; }

        public string? Reason { get; }
        public bool ArchiveOnly { get; }

        public DeleteCaseCommand(string caseId, string? reason = null, bool archiveOnly = true)
        {
            CaseId = caseId ?? throw new ArgumentNullException(nameof(caseId));
            Reason = reason;
            ArchiveOnly = archiveOnly;
        }
    }

    /// <summary>
    /// Handler for deleting a case
    /// </summary>
    public class DeleteCaseCommandHandler : IRequestHandler<DeleteCaseCommand, bool>
    {
        private readonly ILogger<DeleteCaseCommandHandler> _logger;
        private readonly ICaseManager _caseManager;
        private readonly IMediator _mediator;

        public DeleteCaseCommandHandler(
            ILogger<DeleteCaseCommandHandler> logger,
            ICaseManager caseManager,
            IMediator mediator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _caseManager = caseManager ?? throw new ArgumentNullException(nameof(caseManager));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<bool> Handle(DeleteCaseCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting case {CaseId}. Archive only: {ArchiveOnly}",
                request.CaseId, request.ArchiveOnly);

            bool result;
            if (request.ArchiveOnly)
            {
                // Archive by updating status
                result = await _caseManager.UpdateCaseAsync(request.CaseId, c =>
                {
                    c.Status = CaseStatus.Archived;
                    c.ClosedAt = DateTimeOffset.UtcNow;
                    c.ClosedBy = "System";
                }, cancellationToken);
            }
            else
            {
                // Soft delete
                result = await _caseManager.DeleteCaseAsync(request.CaseId, cancellationToken);
            }

            if (result)
            {
                await _mediator.Publish(new CaseDeletedNotification
                {
                    CaseId = request.CaseId,
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
    public class SearchCasesCommand : IRequest<CaseListResponse>
    {
        public string? SearchTerm { get; set; }
        public List<string>? CaseNumbers { get; set; }
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
    public class SearchCasesCommandHandler : IRequestHandler<SearchCasesCommand, CaseListResponse>
    {
        private readonly ILogger<SearchCasesCommandHandler> _logger;
        private readonly ICaseManager _caseManager;

        public SearchCasesCommandHandler(
            ILogger<SearchCasesCommandHandler> logger,
            ICaseManager caseManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _caseManager = caseManager ?? throw new ArgumentNullException(nameof(caseManager));
        }

        public async Task<CaseListResponse> Handle(SearchCasesCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Searching cases with term: {SearchTerm}", request.SearchTerm);

            // Get all cases (in a real implementation, this would be filtered at the database level)
            var allCases = await _caseManager.GetUserCasesAsync(null, cancellationToken);

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

            if (request.CaseNumbers?.Any() == true)
            {
                query = query.Where(c => request.CaseNumbers.Contains(c.CaseNumber));
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
            var cases = query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Map to summary DTOs
            var summaries = cases.Select(c => new CaseSummary
            {
                Id = c.Id,
                CaseNumber = c.CaseNumber,
                Name = c.Title,
                Type = c.Type.ToString(),
                Status = c.Status.ToString(),
                Classification = c.Classification,
                UpdatedAt = c.UpdatedAt,
                EvidenceCount = c.Evidence?.Count ?? 0,
                ActiveSessions = c.Sessions?.Count(s => s.Status == InvestigationStatus.Active) ?? 0
            }).ToList();

            return new CaseListResponse
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
    public class GetCaseStatisticsCommand : IRequest<CaseStatistics>
    {
        public string? CaseId { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public bool IncludeEvidenceStats { get; set; } = true;
        public bool IncludeSessionStats { get; set; } = true;
    }

    /// <summary>
    /// Handler for getting case statistics
    /// </summary>
    public class GetCaseStatisticsCommandHandler : IRequestHandler<GetCaseStatisticsCommand, CaseStatistics>
    {
        private readonly ICaseManager _caseManager;
        private readonly ISessionService _sessionService;
        private readonly IEvidenceManager _evidenceManager;

        public GetCaseStatisticsCommandHandler(
            ICaseManager caseManager,
            ISessionService sessionService,
            IEvidenceManager evidenceManager)
        {
            _caseManager = caseManager ?? throw new ArgumentNullException(nameof(caseManager));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _evidenceManager = evidenceManager ?? throw new ArgumentNullException(nameof(evidenceManager));
        }

        public async Task<CaseStatistics> Handle(GetCaseStatisticsCommand request, CancellationToken cancellationToken)
        {
            var stats = new CaseStatistics();

            if (!string.IsNullOrEmpty(request.CaseId))
            {
                // Get stats for specific case
                var caseEntity = await _caseManager.GetCaseAsync(request.CaseId, cancellationToken);
                if (caseEntity != null)
                {
                    var sessions = await _sessionService.GetSessionsByCaseAsync(request.CaseId, cancellationToken);
                    var evidence = await _evidenceManager.GetEvidenceByCaseAsync(request.CaseId, cancellationToken);

                    stats.TotalEvidence = evidence.Count;
                    stats.TotalEvidenceSize = evidence.Sum(e => e.FileSize);
                    stats.TotalSessions = sessions.Count;
                    stats.ActiveSessions = sessions.Count(s => s.Status == InvestigationStatus.Active);
                    stats.TotalReports = caseEntity.Reports?.Count ?? 0;
                    stats.TotalFindings = caseEntity.Findings?.Count ?? 0;

                    // Evidence by type
                    stats.EvidenceByType = evidence
                        .GroupBy(e => e.Type.ToString())
                        .ToDictionary(g => g.Key, g => g.Count());

                    // Findings by severity
                    if (caseEntity.Findings?.Any() == true)
                    {
                        stats.FindingsBySeverity = caseEntity.Findings
                            .GroupBy(f => f.Severity.ToString())
                            .ToDictionary(g => g.Key, g => g.Count());
                    }

                    // Calculate total investigation time
                    if (sessions.Any())
                    {
                        var earliestSession = sessions.Min(s => s.CreatedAt);
                        var latestUpdate = sessions.Max(s => s.UpdatedAt);
                        stats.TotalInvestigationTime = latestUpdate - earliestSession;
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
    public class CaseCreatedNotification : INotification
    {
        public string CaseId { get; set; } = string.Empty;
        public string CaseNumber { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }

    /// <summary>
    /// Notification when a case is updated
    /// </summary>
    public class CaseUpdatedNotification : INotification
    {
        public string CaseId { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }

    /// <summary>
    /// Notification when a case is deleted
    /// </summary>
    public class CaseDeletedNotification : INotification
    {
        public string CaseId { get; set; } = string.Empty;
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
    public class GetRecentCasesCommand : IRequest<List<IIM.Shared.Models.Case>>
    {
        public int Count { get; }
        public string? UserId { get; }

        public GetRecentCasesCommand(int count = 10, string? userId = null)
        {
            Count = count > 0 ? count : 10;
            UserId = userId;
        }
    }

    /// <summary>
    /// Handler for getting recent cases
    /// </summary>
    public class GetRecentCasesCommandHandler : IRequestHandler<GetRecentCasesCommand, List<IIM.Shared.Models.Case>>
    {
        private readonly ICaseManager _caseManager;

        public GetRecentCasesCommandHandler(ICaseManager caseManager)
        {
            _caseManager = caseManager ?? throw new ArgumentNullException(nameof(caseManager));
        }

        public async Task<List<IIM.Shared.Models.Case>> Handle(GetRecentCasesCommand request, CancellationToken cancellationToken)
        {
            return await _caseManager.GetRecentCasesAsync(request.Count, cancellationToken);
        }
    }
}