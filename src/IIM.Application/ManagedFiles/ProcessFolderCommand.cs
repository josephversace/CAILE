using IIM.Core.Mediator;
using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static LLama.Common.ChatHistory;

namespace IIM.Application.Files
{
    // Command carrying the session ID and image attachments
    public class ProcessFolderUploadCommand : IRequest<ProcessFolderUploadResponse>
    {
        [Required]
        public string SessionId { get; set; } = string.Empty;
        [Required]
        public List<Attachment> Images { get; set; } = new();
        public string? UserId { get; set; }
        public ProcessFolderUploadCommand() { }
        public ProcessFolderUploadCommand(string sessionId, List<Attachment> images)
        {
            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            Images = images ?? throw new ArgumentNullException(nameof(images));
        }
    }

    // Response describing the extracted folder hierarchy, uploaded files and metadata
    public class ProcessFolderUploadResponse
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Message { get; set; } = string.Empty;
        public List<VirtualFolderNode> FolderStructure { get; set; } = new();
        public List<UploadedFileResult> Files { get; set; } = new();
        public List<Citation>? Citations { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class VirtualFolderNode
    {
        public string Name { get; set; } = string.Empty;
        public string Classification { get; set; } = string.Empty;
        public List<VirtualFolderNode> Children { get; set; } = new();
    }

    public class UploadedFileResult
    {
        public string OriginalFileName { get; set; } = string.Empty;
        public string StorageKey { get; set; } = string.Empty;
        public string Classification { get; set; } = string.Empty;
        public long Size { get; set; }
    }

    public class ProcessFolderUploadCommandHandler :
        IRequestHandler<ProcessFolderUploadCommand, ProcessFolderUploadResponse>
    {
        private readonly ILogger<ProcessFolderUploadCommandHandler> _logger;
        private readonly ISessionService _sessionService;
        private readonly IWorkspaceManager _workspaceManager;
        private readonly IFolderOcrService _ocrService;
        private readonly IClassificationService _classificationService;
        private readonly IDeduplicationService _dedupService;
        private readonly IFileStorageService _storageService;

        public ProcessFolderUploadCommandHandler(
            ILogger<ProcessFolderUploadCommandHandler> logger,
            ISessionService sessionService,
            IWorkspaceManager workspaceManager,
            IFolderOcrService ocrService,
            IClassificationService classificationService,
            IDeduplicationService dedupService,
            IFileStorageService storageService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
            _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
            _classificationService = classificationService ?? throw new ArgumentNullException(nameof(classificationService));
            _dedupService = dedupService ?? throw new ArgumentNullException(nameof(dedupService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        }

        public async Task<ProcessFolderUploadResponse> Handle(
            ProcessFolderUploadCommand request,
            CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            _logger.LogInformation("Processing folder upload for session {SessionId}", request.SessionId);

            // Retrieve the session and associated case
            var session = await _sessionService.GetSessionAsync(request.SessionId, cancellationToken);
            var caseEntity = await _workspaceManager.GetWorkspaceAsync(session.WorkspaceId, cancellationToken);

            // Extract folder structure from each image
            var extractedNodes = new List<VirtualFolderNode>();
            foreach (var image in request.Images)
            {
                var nodes = await _ocrService.ExtractFolderStructureAsync(image, cancellationToken);
                extractedNodes.AddRange(nodes);
            }

            // Assign security classification to each node
            var classifiedNodes = await _classificationService.AssignClassificationAsync(
                extractedNodes, cancellationToken);

            // Deduplicate and store each file
            var uploadResults = new List<UploadedFileResult>();
            foreach (var attachment in request.Images)
            {
                var hash = await _dedupService.ComputeHashAsync(attachment, cancellationToken);
                var existing = await _dedupService.LookupAsync(hash, cancellationToken);
                if (existing != null)
                {
                    // File already stored; reuse existing storage key
                    uploadResults.Add(new UploadedFileResult
                    {
                        OriginalFileName = attachment.FileName,
                        StorageKey = existing.StorageKey,
                        Classification = existing.Classification,
                        Size = attachment.Size
                    });
                    continue;
                }

                var classification = classifiedNodes.FirstOrDefault()?.Classification ?? "Unclassified";
                using var stream = attachment.Stream;

                if (stream == null || stream.Length == 0)
                {
                    _logger.LogWarning("Attachment {FileName} in message {MessageId} has no content.", attachment.FileName, message.Id);
                    continue;
                }

                var storageKey = await _storageService.StoreAsync(
                    stream, attachment.FileName, classification, cancellationToken);

                await _dedupService.AddEntryAsync(hash, storageKey, classification, cancellationToken);

                uploadResults.Add(new UploadedFileResult
                {
                    OriginalFileName = attachment.FileName,
                    StorageKey = storageKey,
                    Classification = classification,
                    Size = attachment.Size
                });
            }

            // Build response and persist assistant message
            var response = new ProcessFolderUploadResponse
            {
                Id = Guid.NewGuid().ToString(),
                Message = $"Processed {request.Images.Count} image(s) and updated folder structure.",
                FolderStructure = classifiedNodes,
                Files = uploadResults,
                Metadata = new Dictionary<string, object>
                {
                    ["SessionId"] = request.SessionId,
                    ["CaseId"] = caseEntity.Id,
                    ["ProcessedAt"] = DateTimeOffset.UtcNow
                }
            };

            var assistantMessage = new InvestigationMessage
            {
                Id = response.Id,
                Role = MessageRole.Assistant,
                Content = JsonSerializer.Serialize(response),
                Timestamp = DateTimeOffset.UtcNow
            };
            await _sessionService.AddMessageAsync(request.SessionId, assistantMessage, cancellationToken);

            return response;
        }
    }

    // Interfaces representing the OCR, classification, deduplication and storage services.
    public interface IFolderOcrService
    {
        Task<List<VirtualFolderNode>> ExtractFolderStructureAsync(Attachment image, CancellationToken ct);
    }
    public interface IClassificationService
    {
        Task<List<VirtualFolderNode>> AssignClassificationAsync(List<VirtualFolderNode> nodes, CancellationToken ct);
    }
    public interface IDeduplicationService
    {
        Task<string> ComputeHashAsync(Attachment attachment, CancellationToken ct);
        Task<DedupEntry?> LookupAsync(string hash, CancellationToken ct);
        Task AddEntryAsync(string hash, string storageKey, string classification, CancellationToken ct);
    }
    public interface IFileStorageService
    {
        Task<string> StoreAsync(System.IO.Stream stream, string fileName, string classification, CancellationToken ct);
    }
    public class DedupEntry
    {
        public string Hash { get; set; } = string.Empty;
        public string StorageKey { get; set; } = string.Empty;
        public string Classification { get; set; } = string.Empty;
    }
}
