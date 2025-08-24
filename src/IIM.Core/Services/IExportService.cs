using DocumentFormat.OpenXml.Wordprocessing;
using IIM.Core.Models;
using IIM.Shared.Enums;
using Microsoft.Extensions.Logging;
using OpenAI.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using IIM.Shared.Models;

namespace IIM.Core.Services;

public interface IExportService
{
    Task<ExportResult> ExportResponseAsync(
        InvestigationResponse response,
        ExportFormat format,
        ExportOptions? options = null);

    Task<ExportResult> ExportMessageAsync(
        InvestigationMessage message,
        ExportFormat format,
        ExportOptions? options = null);

    Task<ExportResult> ExportSessionAsync(
        InvestigationSession session,
        ExportFormat format,
        ExportOptions? options = null);

    Task<ExportResult> ExportCaseAsync(
        Case caseEntity,
        ExportFormat format,
        ExportOptions? options = null);

    Task<ExportResult> BatchExportAsync(
        List<string> entityIds,
        string entityType,
        ExportFormat format,
        ExportOptions? options = null);

    Task<List<ExportTemplate>> GetTemplatesAsync(ExportFormat? format = null);

    Task<ExportTemplate> CreateTemplateAsync(ExportTemplate template);

    Task<ExportOperation> GetExportStatusAsync(string operationId);
}

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    //private readonly IPdfService _pdfService;
    //private readonly IWordService _wordService;
    //private readonly IExcelService _excelService;
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
        //_pdfService = pdfService;
        //_wordService = wordService;
        //_excelService = excelService;
        _templateEngine = templateEngine;
        _fileService = fileService;
        _securityService = securityService;
    }

    public async Task<ExportResult> ExportResponseAsync(
        InvestigationResponse response,
        ExportFormat format,
        ExportOptions? options = null)
    {
        options ??= new ExportOptions();

        try
        {
            _logger.LogInformation("Exporting response {ResponseId} as {Format}",
                response.Id, format);

            byte[] data = format switch
            {
                ExportFormat.Pdf => await ExportToPdfAsync(response, options),
                ExportFormat.Word => await ExportToWordAsync(response, options),
                ExportFormat.Excel => await ExportToExcelAsync(response, options),
                ExportFormat.Json => await ExportToJsonAsync(response, options),
                ExportFormat.Csv => await ExportToCsvAsync(response, options),
                ExportFormat.Html => await ExportToHtmlAsync(response, options),
                ExportFormat.Markdown => await ExportToMarkdownAsync(response, options),
                _ => throw new NotSupportedException($"Export format {format} is not supported")
            };

            return new ExportResult {
                Success= true,
                FilePath= null,
                Data= data,
                FileSize= data.Length,
                ErrorMessage= null,
                Metadata= new Dictionary<string, object>
                {
                    ["responseId"] = response.Id,
                    ["format"] = format.ToString(),
                    ["exportedAt"] = DateTime.UtcNow,
                    ["exportedBy"] = _securityService.GetCurrentUser().Id
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export response {ResponseId}", response.Id);

            return new ExportResult {
                Success= false,
                FilePath= null,
                Data= null,
                FileSize= 0,
                ErrorMessage= ex.Message,
                Metadata= null
            };
        }
    }

    private async Task<byte[]> ExportToPdfAsync(
        InvestigationResponse response,
        ExportOptions options)
    {
        var template = await _templateEngine.GetTemplateAsync("ResponsePdf");
        var html = await _templateEngine.RenderAsync(template, new
        {
            Response = response,
            Options = options,
            Metadata = new
            {
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = _securityService.GetCurrentUser().DisplayName,
                CaseNumber = response.Metadata?.GetValueOrDefault("caseNumber"),
                Watermark = options.IncludeWatermark ? "CONFIDENTIAL - LAW ENFORCEMENT ONLY" : null
            }
        });

        var pdfOptions = new PdfGenerationOptions
        {
            PageSize = PageSize.Letter,
            Margins = new Margins(0.75f, 0.75f, 0.75f, 0.75f),
            IncludeHeaders = options.IncludeHeaders,
            IncludeFooters = options.IncludeFooters,
            HeaderHtml = options.IncludeHeaders ? await GetHeaderHtmlAsync(response) : null,
            FooterHtml = options.IncludeFooters ? await GetFooterHtmlAsync(response) : null
        };

        throw new NotImplementedException("PDF export is not implemented yet");
        //return await _pdfService.GeneratePdfAsync(html, pdfOptions);
    }

    private async Task<byte[]> ExportToWordAsync(
        InvestigationResponse response,
        ExportOptions options)
    {
        // Implementation for Word export
        //return await _wordService.GenerateDocumentAsync(response, options);
        throw new NotImplementedException("Word export is not implemented yet");

    }

    private async Task<byte[]> ExportToExcelAsync(
        InvestigationResponse response,
        ExportOptions options)
    {
        // Implementation for Excel export
        // return await _excelService.GenerateSpreadsheetAsync(response, options);

        throw new NotImplementedException("Excel export is not implemented yet");
    }

    private async Task<byte[]> ExportToJsonAsync(
        InvestigationResponse response,
        ExportOptions options)
    {
        var exportData = new
        {
            response = response,
            metadata = options.IncludeMetadata ? new
            {
                exportedAt = DateTime.UtcNow,
                exportedBy = _securityService.GetCurrentUser().Id,
                version = "1.0",
                options = options
            } : null,
            chainOfCustody = options.IncludeChainOfCustody ?
                await GetChainOfCustodyAsync(response.Id) : null
        };

        var json = System.Text.Json.JsonSerializer.Serialize(exportData,
            new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    private async Task<byte[]> ExportToMarkdownAsync(
        InvestigationResponse response,
        ExportOptions options)
    {
        var markdown = new System.Text.StringBuilder();

        // Header
        markdown.AppendLine($"# Investigation Response: {response.Id}");
        markdown.AppendLine();

        // Metadata
        if (options.IncludeMetadata)
        {
            markdown.AppendLine("## Metadata");
            markdown.AppendLine($"- **Created**: {response.CreatedAt:g}");
            markdown.AppendLine($"- **Type**: {response.DisplayType}");
            if (response.Confidence.HasValue)
            {
                markdown.AppendLine($"- **Confidence**: {response.Confidence:P0}");
            }
            markdown.AppendLine();
        }

        // Content
        markdown.AppendLine("## Content");
        markdown.AppendLine(response.Message);
        markdown.AppendLine();

        // Chain of Custody
        if (options.IncludeChainOfCustody)
        {
            markdown.AppendLine("## Chain of Custody");
            markdown.AppendLine($"- **Hash**: {response.Hash}");
            markdown.AppendLine($"- **Signed By**: {response.CreatedBy}");
            markdown.AppendLine();
        }

        return System.Text.Encoding.UTF8.GetBytes(markdown.ToString());
    }

    // Helper methods
    private async Task<string> GetHeaderHtmlAsync(InvestigationResponse response)
    {
        return $@"
            <div style='text-align: center; font-size: 10pt; color: #666;'>
                Investigation Response - {response.Id} - Page <span class='page'></span> of <span class='topage'></span>
            </div>
        ";
    }

    private async Task<string> GetFooterHtmlAsync(InvestigationResponse response)
    {
        return $@"
            <div style='text-align: center; font-size: 8pt; color: #666;'>
                CONFIDENTIAL - LAW ENFORCEMENT ONLY - Generated {DateTime.UtcNow:g}
            </div>
        ";
    }

    private async Task<object?> GetChainOfCustodyAsync(string responseId)
    {
        // Implement chain of custody retrieval
        return null;
    }

    // Add these methods to your ExportService class:

    private async Task<byte[]> ExportToCsvAsync(
        InvestigationResponse response,
        ExportOptions options)
    {
        var csv = new StringBuilder();

        // Add headers
        csv.AppendLine("Field,Value");

        // Add basic fields
        csv.AppendLine($"ID,\"{response.Id}\"");
        csv.AppendLine($"Message,\"{response.Message.Replace("\"", "\"\"")}\"");
        csv.AppendLine($"Created,\"{response.Timestamp:yyyy-MM-dd HH:mm:ss}\"");
        csv.AppendLine($"Created By,\"{response.ModelUsed}\"");

        if (response.Confidence.HasValue)
        {
            csv.AppendLine($"Confidence,\"{response.Confidence.Value:P0}\"");
        }

        // Add tool results if any
        if (response.ToolResults?.Any() == true)
        {
            csv.AppendLine();
            csv.AppendLine("Tool Results");
            csv.AppendLine("Tool Name,Status,Execution Time");
            foreach (var tool in response.ToolResults)
            {
                csv.AppendLine($"\"{tool.ToolName}\",\"{tool.Status}\",\"{tool.ExecutionTime.TotalMilliseconds}ms\"");
            }
        }

        // Add citations if any
        if (response.Citations?.Any() == true)
        {
            csv.AppendLine();
            csv.AppendLine("Citations");
            csv.AppendLine("Source ID,Source Type,Text,Relevance");
            foreach (var citation in response.Citations)
            {
                csv.AppendLine($"\"{citation.SourceId}\",\"{citation.SourceType}\",\"{citation.Text.Replace("\"", "\"\"")}\",\"{citation.Relevance}\"");
            }
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private async Task<byte[]> ExportToHtmlAsync(
        InvestigationResponse response,
        ExportOptions options)
    {
        var html = new StringBuilder();

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"UTF-8\">");
        html.AppendLine($"<title>Investigation Response - {response.Id}</title>");
        html.AppendLine("<style>");
        html.AppendLine(@"
        body { font-family: Arial, sans-serif; margin: 40px; line-height: 1.6; }
        .header { border-bottom: 2px solid #333; padding-bottom: 20px; margin-bottom: 20px; }
        .metadata { background: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0; }
        .content { margin: 20px 0; }
        .tool-results { margin-top: 30px; }
        .tool-result { background: #e9ecef; padding: 10px; margin: 10px 0; border-radius: 5px; }
        .citations { margin-top: 30px; }
        .citation { border-left: 3px solid #007bff; padding-left: 10px; margin: 10px 0; }
        .confidence { color: #28a745; font-weight: bold; }
        .footer { margin-top: 40px; padding-top: 20px; border-top: 1px solid #ddd; text-align: center; color: #666; font-size: 0.9em; }
    ");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");

        // Header
        html.AppendLine("<div class=\"header\">");
        html.AppendLine($"<h1>Investigation Response</h1>");
        html.AppendLine($"<p><strong>ID:</strong> {response.Id}</p>");
        html.AppendLine("</div>");

        // Metadata
        if (options.IncludeMetadata)
        {
            html.AppendLine("<div class=\"metadata\">");
            html.AppendLine("<h2>Metadata</h2>");
            html.AppendLine($"<p><strong>Created:</strong> {response.Timestamp:g}</p>");
            html.AppendLine($"<p><strong>Created By:</strong> {response.ModelUsed}</p>");
            html.AppendLine($"<p><strong>Type:</strong> {response.DisplayType}</p>");
            if (response.Confidence.HasValue)
            {
                html.AppendLine($"<p class=\"confidence\"><strong>Confidence:</strong> {response.Confidence.Value:P0}</p>");
            }
            html.AppendLine("</div>");
        }

        // Content
        html.AppendLine("<div class=\"content\">");
        html.AppendLine("<h2>Content</h2>");
        html.AppendLine($"<p>{System.Web.HttpUtility.HtmlEncode(response.Message)}</p>");
        html.AppendLine("</div>");

        // Tool Results
        if (response.ToolResults?.Any() == true)
        {
            html.AppendLine("<div class=\"tool-results\">");
            html.AppendLine("<h2>Tool Results</h2>");
            foreach (var tool in response.ToolResults)
            {
                html.AppendLine("<div class=\"tool-result\">");
                html.AppendLine($"<strong>{tool.ToolName}</strong> - Status: {tool.Status} - Time: {tool.ExecutionTime.TotalMilliseconds}ms");
                html.AppendLine("</div>");
            }
            html.AppendLine("</div>");
        }

        // Citations
        if (response.Citations?.Any() == true)
        {
            html.AppendLine("<div class=\"citations\">");
            html.AppendLine("<h2>Citations</h2>");
            foreach (var citation in response.Citations)
            {
                html.AppendLine("<div class=\"citation\">");
                html.AppendLine($"<p><strong>Source:</strong> {citation.SourceType} ({citation.SourceId})</p>");
                html.AppendLine($"<p>{System.Web.HttpUtility.HtmlEncode(citation.Text)}</p>");
                html.AppendLine($"<p><em>Relevance: {citation.Relevance:P0}</em></p>");
                html.AppendLine("</div>");
            }
            html.AppendLine("</div>");
        }

        // Footer
        if (options.IncludeChainOfCustody)
        {
            html.AppendLine("<div class=\"footer\">");
            html.AppendLine($"<p>Hash: {response.Hash}</p>");
            html.AppendLine($"<p>CONFIDENTIAL - LAW ENFORCEMENT ONLY</p>");
            html.AppendLine($"<p>Generated: {DateTime.UtcNow:g}</p>");
            html.AppendLine("</div>");
        }

        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return Encoding.UTF8.GetBytes(html.ToString());
    }

    public async Task<ExportResult> ExportMessageAsync(
        InvestigationMessage message,
        ExportFormat format,
        ExportOptions? options = null)
    {
        // Convert message to response for export
        var response = new InvestigationResponse
        {
            Id = message.Id,
            Message = message.Content,
            ToolResults = message.ToolResults,
            Citations = message.Citations,
            Metadata = message.Metadata,
            Timestamp = message.Timestamp.DateTime,
            ModelUsed = message.ModelUsed ?? "User",
            Hash = await _securityService.GenerateHashAsync(message.Content),
            HashType = HashType.SHA256,
        };

        return await ExportResponseAsync(response, format, options);
    }

    public async Task<ExportResult> ExportSessionAsync(
        InvestigationSession session,
        ExportFormat format,
        ExportOptions? options = null)
    {
        // For now, export as JSON with session summary
        var exportData = new
        {
            Session = new
            {
                session.Id,
                session.Title,
                session.Type,
                session.Status,
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
                ToolResultsCount = m.ToolResults.Count,
                CitationsCount = m.Citations.Count
            }),
            Findings = session.Findings,
            MessageCount = session.Messages.Count
        };

        var json = System.Text.Json.JsonSerializer.Serialize(exportData,
            new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

        var data = Encoding.UTF8.GetBytes(json);

        return new ExportResult {
            Success= true,
            FilePath= null,
            Data= data,
            FileSize= data.Length,
            ErrorMessage= null,
            Metadata= new Dictionary<string, object> { ["sessionId"] = session.Id }
        };
    }

    public async Task<ExportResult> ExportCaseAsync(
        Case caseEntity,
        ExportFormat format,
        ExportOptions? options = null)
    {
        var exportData = new
        {
            Case = caseEntity,
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

        return new ExportResult {
            Success=true,
            FilePath= null,
            Data= data,
            FileSize= data.Length,
            ErrorMessage= null,
            Metadata= new Dictionary<string, object> { ["caseId"] = caseEntity.Id }
        };
    }

    public async Task<ExportResult> BatchExportAsync(
        List<string> entityIds,
        string entityType,
        ExportFormat format,
        ExportOptions? options = null)
    {
        _logger.LogInformation("Batch exporting {Count} {EntityType} items",
            entityIds.Count, entityType);

        // For now, return not implemented
        throw new NotImplementedException("Batch export not yet implemented");
    }

    public async Task<List<ExportTemplate>> GetTemplatesAsync(ExportFormat? format = null)
    {
        var templates = new List<ExportTemplate>
    {
        new ExportTemplate
        {
            Id = "default-pdf",
            Name = "Default PDF Template",
            Format = ExportFormat.Pdf,
            IsSystemTemplate = true,
            CreatedBy = "System",
            CreatedAt = DateTime.UtcNow
        },
        new ExportTemplate
        {
            Id = "investigation-report",
            Name = "Investigation Report",
            Format = ExportFormat.Word,
            IsSystemTemplate = true,
            CreatedBy = "System",
            CreatedAt = DateTime.UtcNow
        },
        new ExportTemplate
        {
            Id = "data-export",
            Name = "Data Export",
            Format = ExportFormat.Excel,
            IsSystemTemplate = true,
            CreatedBy = "System",
            CreatedAt = DateTime.UtcNow
        }
    };

        if (format.HasValue)
        {
            templates = templates.Where(t => t.Format == format.Value).ToList();
        }

        return templates;
    }

    public async Task<ExportTemplate> CreateTemplateAsync(ExportTemplate template)
    {
        template.Id = Guid.NewGuid().ToString();
        template.CreatedAt = DateTime.UtcNow;
        template.CreatedBy = _securityService.GetCurrentUser().Id;

        // In production, save to database
        _logger.LogInformation("Created template {TemplateId}", template.Id);

        return template;
    }

    public async Task<ExportOperation> GetExportStatusAsync(string operationId)
    {
        // In production, retrieve from database
        return new ExportOperation
        {
            Id = operationId,
            EntityType = "Response",
            EntityId = "unknown",
            Format = ExportFormat.Pdf,
            Status = ExportStatus.Completed,
            StartedAt = DateTime.UtcNow.AddMinutes(-1),
            CompletedAt = DateTime.UtcNow
        };
    }
}
