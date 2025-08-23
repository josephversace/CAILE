using IIM.Application.Interfaces;
using IIM.Core.Mediator;
using IIM.Core.Services;
using IIM.Shared.DTOs;
using IIM.Shared.Enums;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace IIM.Application.Commands.Investigation
{
    /// <summary>
    /// Handles exporting responses - adapted to actual export service interface.
    /// </summary>
    public class ExportResponseCommandHandler : IRequestHandler<ExportResponseCommand, byte[]>
    {
        private readonly ILogger<ExportResponseCommandHandler> _logger;
        private readonly ISessionService _sessionService;
        private readonly IExportService _exportService;

        public ExportResponseCommandHandler(
            ILogger<ExportResponseCommandHandler> logger,
            ISessionService sessionService,
            IExportService exportService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        }

        public async Task<byte[]> Handle(
            ExportResponseCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Exporting response {ResponseId} as {Format}",
                request.ResponseId, request.Format);

            // Find the response
            var response = await FindResponseAsync(request.ResponseId, cancellationToken);
            if (response == null)
            {
                throw new KeyNotFoundException($"Response {request.ResponseId} not found");
            }

            // Convert ExportOptions to the DTO version
            var options = new ExportOptions(
                IncludeMetadata: request.Options?.IncludeMetadata ?? true,
                IncludeChainOfCustody: request.Options?.IncludeChainOfCustody ?? true);

            // Use the actual IExportService interface
            var exportResult = await _exportService.ExportResponseAsync(
                response,
                request.Format,
                options);

            // Return the data bytes
            if (exportResult.Data != null)
            {
                return exportResult.Data;
            }

            // If data is null but file path exists, read from file
            if (!string.IsNullOrEmpty(exportResult.FilePath))
            {
                return await File.ReadAllBytesAsync(exportResult.FilePath, cancellationToken);
            }

            throw new InvalidOperationException("Export failed - no data or file path returned");
        }

        private async Task<InvestigationResponse> FindResponseAsync(
            string responseId,
            CancellationToken cancellationToken)
        {
            var sessions = await _sessionService.GetAllSessionsAsync(cancellationToken);

            foreach (var session in sessions)
            {
                var message = session.Messages.FirstOrDefault(m =>
                    m.Id == responseId && m.Role == MessageRole.Assistant);

                if (message != null)
                {
                    return new InvestigationResponse
                    {
                        Id = message.Id,
                        Message = message.Content,
                        ToolResults = message.ToolResults,
                        Citations = message.Citations,
                        Metadata = new Dictionary<string, object>
                        {
                            ["SessionTitle"] = session.Title,
                            ["CaseId"] = session.CaseId,
                            ["ModelUsed"] = message.ModelUsed ?? "unknown"
                        }
                    };
                }
            }

            return null;
        }
    }
}
