
using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using IIM.Shared.Models;
using IIM.Shared.Interfaces;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces;

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


