using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs
{
    /// <summary>
    /// Request DTO for creating an investigation session
    /// </summary>
    public record CreateSessionRequest(
        string CaseId,
        string Title,
        string Type,
        List<string>? EnabledTools = null,
        Dictionary<string, ModelConfiguration>? Models = null,
        Dictionary<string, object>? Metadata = null
    );

    /// <summary>
    /// Request DTO for investigation query
    /// </summary>
    public record InvestigationQueryRequest(
        string SessionId,
        string Text,
        List<string>? EnabledTools = null,
        List<AttachmentInfo>? Attachments = null,
        Dictionary<string, object>? Context = null,
        Dictionary<string, object>? Parameters = null
    );

    /// <summary>
    /// Request DTO for tool execution
    /// </summary>
    public record ExecuteToolRequest(
        string ToolName,
        Dictionary<string, object> Parameters,
        string? SessionId = null,
        Dictionary<string, object>? Context = null
    );

    /// <summary>
    /// Model configuration for investigation
    /// </summary>
    public record ModelConfiguration(
        string ModelId,
        string Provider,
        Dictionary<string, object>? Settings = null,
        bool AutoLoad = true
    );

    /// <summary>
    /// Attachment information for queries
    /// </summary>
    public record AttachmentInfo(
        string Id,
        string FileName,
        string MimeType,
        long Size,
        string? StoragePath = null
    );

    /// <summary>
    /// Request DTO for exporting investigation results
    /// </summary>
    public record ExportInvestigationRequest(
        string SessionId,
        string Format,
        bool IncludeMessages = true,
        bool IncludeToolResults = true,
        bool IncludeEvidence = false,
        Dictionary<string, object>? Options = null
    );
}