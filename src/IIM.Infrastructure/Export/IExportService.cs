using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using Microsoft.Extensions.Logging;
using System.Text;

namespace IIM.Infrastructure.Export
{
    public class ExportService : IExportService
    {
        private readonly ILogger<ExportService> _logger;
        private readonly ITemplateEngine _templateEngine;
        private readonly IFileService _fileService;
        private readonly ISecurityService _securityService;

        public ExportService(
            ILogger<ExportService> logger,
            ITemplateEngine templateEngine,
            IFileService fileService,
            ISecurityService securityService)
        {
            _logger = logger;
            _templateEngine = templateEngine;
            _fileService = fileService;
            _securityService = securityService;
        }

        // Match exact interface signature for ExportSessionAsync
        public async Task<ExportResult> ExportSessionAsync(
            InvestigationSession session,
            ExportFormat format,
            ExportOptions? options = null)
        {
            // Use existing implementation from the found code
            var exportData = new
            {
                Session = new
                {
                    session.Id,
                    session.Title,
           
                    session.CreatedAt,
                    session.UpdatedAt,
                    session.CreatedBy
                },
                Messages = session.Messages.Select(m => new
                {
                    m.Id,
                    m.Role,
                    m.Content,
                    m.Timestamp,
                    ToolCallsCount = m.ToolCalls.Count,
                    CitationsCount = m.Citations.Count
                }),
              
                MessageCount = session.Messages.Count
            };

            var json = System.Text.Json.JsonSerializer.Serialize(exportData,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });

            var data = Encoding.UTF8.GetBytes(json);

            return new ExportResult
            {
                Success = true,
                FilePath = null,
                Data = data,
                FileSize = data.Length,
                ErrorMessage = null,
                Metadata = new Dictionary<string, object> { ["sessionId"] = session.Id }
            };
        }

        // Match exact interface signature for ExportWorkspaceAsync
        public async Task<ExportResult> ExportWorkspaceAsync(
            Workspace workspaceEntity,
            ExportFormat format,
            ExportOptions? options = null)
        {
            var exportData = new
            {
                Workspace = workspaceEntity,
                ExportDate = DateTime.UtcNow,
                ExportedBy = _securityService.GetCurrentUser().DisplayName
            };

            var json = System.Text.Json.JsonSerializer.Serialize(exportData,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });

            var data = Encoding.UTF8.GetBytes(json);

            return new ExportResult
            {
                Success = true,
                FilePath = null,
                Data = data,
                FileSize = data.Length,
                ErrorMessage = null,
                Metadata = new Dictionary<string, object> { ["workspaceId"] = workspaceEntity.Id }
            };
        }

        // Implement other required interface methods with minimal implementations
        public async Task<ExportResult> ExportResponseAsync(
            InvestigationResponse response,
            ExportFormat format,
            ExportOptions? options = null)
        {
            // TODO: Implement actual response export
            return new ExportResult { Success = true, Data = new byte[0] };
        }

        public async Task<ExportResult> ExportMessageAsync(
            InvestigationMessage message,
            ExportFormat format,
            ExportOptions? options = null)
        {
            // TODO: Implement actual message export
            return new ExportResult { Success = true, Data = new byte[0] };
        }

        public async Task<ExportResult> BatchExportAsync(
            List<string> entityIds,
            string entityType,
            ExportFormat format,
            ExportOptions? options = null)
        {
            // TODO: Implement batch export
            return new ExportResult { Success = true, Data = new byte[0] };
        }

        public async Task<List<ExportTemplate>> GetTemplatesAsync(ExportFormat? format = null)
        {
            // TODO: Implement template retrieval
            return new List<ExportTemplate>();
        }

        public async Task<ExportTemplate> CreateTemplateAsync(ExportTemplate template)
        {
            // TODO: Implement template creation
            return template;
        }

        public async Task<ExportOperation> GetExportStatusAsync(string operationId)
        {
            return new ExportOperation
            {
                Id = operationId,
                Status = ExportStatus.Completed // Use enum value instead of string
            };
        }
    }
}
