using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Security.Authentication;

namespace IIM.Shared.Models;

// ========================================
// CORE DOMAIN MODELS
// ========================================

#region Cases

/// <summary>
/// Investigation case - the primary aggregate root for investigations
/// </summary>
public class Case
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CaseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CaseType Type { get; set; }
    public CaseStatus Status { get; set; }
    public CasePriority Priority { get; set; }
    public string Classification { get; set; } = "Unclassified";
    public string LeadInvestigator { get; set; } = string.Empty;
    public List<string> TeamMembers { get; set; } = new();
    public List<string> AccessControlList { get; set; } = new();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? ClosedBy { get; set; }

    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object>? Statistics { get; set; }

    // Navigation properties
    public List<InvestigationSession> Sessions { get; set; } = new();
    public List<Evidence> Evidence { get; set; } = new();
    public List<Report> Reports { get; set; } = new();
    public List<Timeline> Timelines { get; set; } = new();
    public List<Finding> Findings { get; set; } = new();
}

// <summary>
/// Create case request model
/// </summary>
public class CreateCaseRequest
{
    public string CaseNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LeadInvestigator { get; set; } = string.Empty;
    public List<string>? TeamMembers { get; set; }
    public string? Classification { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Update case request model
/// </summary>
public class UpdateCaseRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? LeadInvestigator { get; set; }
    public List<string>? TeamMembers { get; set; }
    public string? Classification { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Search cases request model
/// </summary>
public class SearchCaseRequest
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
/// Get case query model
/// </summary>
public class GetCaseQuery
{
    public string CaseId { get; set; } = string.Empty;
    public bool IncludeEvidence { get; set; } = false;
    public bool IncludeSessions { get; set; } = false;
    public bool IncludeReports { get; set; } = false;
    public bool IncludeStatistics { get; set; } = true;
}

/// <summary>
/// Get case statistics query model
/// </summary>
public class GetCaseStatisticsQuery
{
    public string? CaseId { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public bool IncludeEvidenceStats { get; set; } = true;
    public bool IncludeSessionStats { get; set; } = true;
}

/// <summary>
/// Case response model
/// </summary>
public class CaseResponse
{
    public string Id { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LeadInvestigator { get; set; } = string.Empty;
    public List<string> TeamMembers { get; set; } = new();
    public string Classification { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int EvidenceCount { get; set; }
    public int SessionCount { get; set; }
    public int ReportCount { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Case list response model
/// </summary>
public class CaseListResponse
{
    public List<CaseSummary> Cases { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Case summary model
/// </summary>
public class CaseSummary
{
    public string Id { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Classification { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
    public int EvidenceCount { get; set; }
    public int ActiveSessions { get; set; }
}

/// <summary>
/// Case statistics model
/// </summary>
public class CaseStatistics
{
    public int TotalEvidence { get; set; }
    public long TotalEvidenceSize { get; set; }
    public int TotalSessions { get; set; }
    public int ActiveSessions { get; set; }
    public int TotalReports { get; set; }
    public int TotalFindings { get; set; }
    public TimeSpan TotalInvestigationTime { get; set; }
    public Dictionary<string, int> EvidenceByType { get; set; } = new();
    public Dictionary<string, int> FindingsBySeverity { get; set; } = new();
}


#endregion

// ========================================
// CONFIGURATION & SETTINGS MODELS
// ========================================
// Purpose: Manage application configuration and user preferences

#region Configuration

/// <summary>
/// Individual configuration setting
/// Purpose: Store key-value configuration with metadata
/// Used by: Settings service, configuration UI, deployment manager
/// </summary>
public class Setting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty; // JSON serialized
    public string Category { get; set; } = string.Empty; // MinIO, WSL, Models, etc.
    public string Description { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; }
    public bool IsUserConfigurable { get; set; } = true;
    public bool IsSystemSetting { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRegex { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string UpdatedBy { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Application-wide settings collection
/// Purpose: Group related settings for specific services
/// Used by: MinIO service, evidence manager, WSL manager
/// </summary>
public class Settings
{
    // MinIO Settings
    public string MinIOEndpoint { get; set; } = "localhost:9000";
    public string MinIOAccessKey { get; set; } = string.Empty;
    public string MinIOSecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = "evidence";
    public bool UseSSL { get; set; } = false;

    // Evidence Settings
    public bool VerifyHashOnUpload { get; set; } = true;
    public bool RequireAuth { get; set; } = true;
    public bool EncryptAtRest { get; set; } = true;
    public int MaxFileSizeMB { get; set; } = 5000;
    public List<string> AllowedFileTypes { get; set; } = new();

    // WSL Settings
    public string WslDistroName { get; set; } = "IIM-Ubuntu";
    public string WslBasePath { get; set; } = @"\\wsl$\IIM-Ubuntu";
    public bool AutoStartWsl { get; set; } = true;

    // Model Settings
    public string ModelsPath { get; set; } = "./models";
    public long MaxModelMemoryMB { get; set; } = 8192;
    public bool PreloadDefaultModels { get; set; } = true;

    // System Settings
    public bool EnableTelemetry { get; set; } = false;
    public bool EnableAutoUpdate { get; set; } = false;
    public string LogLevel { get; set; } = "Information";
}

#endregion

#region Evidence

/// <summary>
/// Digital evidence item with chain of custody
/// </summary>
public class Evidence
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CaseId { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // MIME type
    public long FileSize { get; set; }
    public EvidenceType Type { get; set; }
    public EvidenceStatus Status { get; set; }

    public HashType HashAlgorithm { get; set; } = HashType.SHA256;
    
    public string Hash { get; set; } = string.Empty; // Primary hash (e.g., SHA256)

    // Hashing and integrity
    public Dictionary<string, string> Hashes { get; set; } = new(); // SHA256, MD5, etc.
    public bool IntegrityValid { get; set; } = true;
    public string? Signature { get; set; }

    // Storage
    public string StoragePath { get; set; } = string.Empty;

    // Timestamps
    public DateTimeOffset IngestTimestamp { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Metadata
    public EvidenceMetadata Metadata { get; set; } = new();

    // Chain of custody
    public List<ChainOfCustodyEntry> ChainOfCustody { get; set; } = new();

    // Processing
    public List<ProcessedEvidence> ProcessedVersions { get; set; } = new();

    public string CreatedBy { get; set; } = string.Empty;

    public string CollectedBy { get; set; } = string.Empty;

    public string CollectedLocation { get; set; } = string.Empty;

    public DateTimeOffset CollectionDate { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Evidence metadata for collection context
/// </summary>
public class EvidenceMetadata
{
    public string CaseNumber { get; set; } = string.Empty;
    public string CollectedBy { get; set; } = string.Empty;
    public DateTimeOffset CollectionDate { get; set; } = DateTimeOffset.UtcNow;
    public string? CollectionLocation { get; set; }
    public string? DeviceSource { get; set; }
    public string? Description { get; set; }
    public string? SessionId { get; set; }
    public Dictionary<HashType, string> AdditionalHashes { get; set; } = new();
    public Dictionary<string, string> CustomFields { get; set; } = new();

   
}

/// <summary>
/// Chain of custody entry for evidence tracking
/// </summary>
public class ChainOfCustodyEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string Action { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public string PreviousHash { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Processed evidence version tracking
/// </summary>
public class ProcessedEvidence
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string OriginalEvidenceId { get; set; } = string.Empty;
    public string ProcessingType { get; set; } = string.Empty;
    public DateTimeOffset ProcessedTimestamp { get; set; } = DateTimeOffset.UtcNow;
    public string ProcessedBy { get; set; } = string.Empty;
    public string ProcessedHash { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public TimeSpan? ProcessingDuration { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? ProcessingResults { get; set; }
}

#region Evidence Upload Models

/// <summary>
/// Request to initiate evidence upload
/// Purpose: Start upload process with pre-flight checks
/// Used by: Evidence upload UI, API endpoints, batch importers
/// </summary>
public class InitiateEvidenceUploadRequest
{
    public string FileHash { get; set; } = string.Empty; // SHA-256 from client
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = "application/octet-stream";
    public EvidenceMetadata Metadata { get; set; } = new();
    public bool CheckDuplicates { get; set; } = true;
    public string? ChunkingStrategy { get; set; } // For deduplication
}

/// <summary>
/// Response from upload initiation
/// Purpose: Provide upload URL or duplicate info
/// Used by: Upload client to proceed with actual upload
/// </summary>
public class InitiateEvidenceUploadResponse
{
    public string EvidenceId { get; set; } = string.Empty;
    public EvidenceUploadStatus Status { get; set; }
    public string? UploadUrl { get; set; } // Pre-signed URL for MinIO
    public DateTimeOffset? UploadUrlExpires { get; set; }
    public string? DuplicateEvidenceId { get; set; }
    public DuplicateInfo? DuplicateInfo { get; set; }
    public Dictionary<string, string>? RequiredHeaders { get; set; }
    public string? UploadToken { get; set; } // For auth
}

/// <summary>
/// Information about duplicate evidence
/// Purpose: Inform user about existing evidence
/// Used by: Deduplication service, upload UI
/// </summary>
public class DuplicateInfo
{
    public string OriginalEvidenceId { get; set; } = string.Empty;
    public DateTimeOffset OriginalUploadDate { get; set; }
    public string OriginalUploadedBy { get; set; } = string.Empty;
    public string OriginalCaseNumber { get; set; } = string.Empty;
    public int DuplicateCount { get; set; }
    public bool CanLinkToCurrent { get; set; } = true;
    public string? ReasonIfCannotLink { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Confirm evidence upload completion
/// Purpose: Verify upload and update chain of custody
/// Used by: Upload completion handler
/// </summary>
public class ConfirmEvidenceUploadRequest
{
    public string EvidenceId { get; set; } = string.Empty;
    public string? ETag { get; set; } // From MinIO
    public string? ClientHash { get; set; } // Re-verify
    public long? ActualSize { get; set; }
    public Dictionary<string, string>? AdditionalHashes { get; set; } // MD5, SHA1, etc.
}

/// <summary>
/// Upload confirmation response
/// Purpose: Confirm successful upload and integrity
/// Used by: Upload UI to show completion status
/// </summary>
public class ConfirmEvidenceUploadResponse
{
    public bool Success { get; set; }
    public EvidenceStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ServerHash { get; set; }
    public bool HashesMatch { get; set; }
    public ChainOfCustodyEntry? InitialChainEntry { get; set; }
    public string? StoragePath { get; set; }
}

/// <summary>
/// Evidence context for operations
/// Purpose: Track context of evidence operations
/// Used by: Chain of custody, audit trail
/// </summary>
public class EvidenceContext
{
    public string CaseId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Operation { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public string? PreviousHash { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

#endregion


#endregion

#region Investigation Sessions

/// <summary>
/// Investigation session for interactive analysis
/// </summary>
public class InvestigationSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CaseId { get; set; } = string.Empty;
    public string Title { get; set; } = "New Investigation";
    public string Icon { get; set; } = "🔍";
    public InvestigationType Type { get; set; } = InvestigationType.GeneralInquiry;
    public InvestigationStatus Status { get; set; } = InvestigationStatus.Active;

    public List<InvestigationMessage> Messages { get; set; } = new();
    public List<string> EnabledTools { get; set; } = new();
    public Dictionary<string, ModelConfiguration> Models { get; set; } = new();
    public List<Finding> Findings { get; set; } = new();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = Environment.UserName;
}

/// <summary>
/// Request to create new investigation session
/// Purpose: Initialize new investigation with context
/// Used by: Investigation UI, session manager
/// </summary>
public class CreateSessionRequest
{
    public string CaseId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string InvestigationType { get; set; } = string.Empty;
    public Dictionary<string, ModelConfiguration>? Models { get; set; }
    public List<string>? EnabledTools { get; set; }
    public SessionContext? Context { get; set; }
    public string? TemplateId { get; set; }
    public Dictionary<string, object>? InitialParameters { get; set; }

    public CreateSessionRequest() { }

    public CreateSessionRequest(string caseId, string title, string investigationType)
    {
        CaseId = caseId;
        Title = title;
        InvestigationType = investigationType;
    }
}

    /// <summary>
    /// Session context information
    /// Purpose: Maintain context across investigation
    /// Used by: RAG service, model orchestrator
    /// </summary>
    public class SessionContext
    {
        public string CaseId { get; set; } = string.Empty;
        public List<string>? RelevantEvidenceIds { get; set; }
        public Dictionary<string, object>? Variables { get; set; }
        public TimeRange? FocusTimeRange { get; set; }
        public List<string>? FocusEntityIds { get; set; }
        public string? PreviousSessionId { get; set; }
    }


/// <summary>
/// Investigation query request
/// Purpose: Submit query to investigation engine
/// Used by: Chat interface, API endpoints
/// </summary>
public class InvestigationQuery
{
    public string SessionId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<Attachment>? Attachments { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
    public List<string>? RequestedTools { get; set; }
    public bool StreamResponse { get; set; } = false;
    public int? MaxTokens { get; set; }
    public double? Temperature { get; set; }
}


/// <summary>
/// Message in an investigation session
/// </summary>
public class InvestigationMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? SessionId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    // Optional content
    public List<Attachment>? Attachments { get; set; }
    public List<ToolResult>? ToolResults { get; set; }
    public List<Citation>? Citations { get; set; }
    public RAGSearchResult? RAGResults { get; set; }
    public List<TranscriptionResult>? Transcriptions { get; set; }
    public List<ImageAnalysisResult>? ImageAnalyses { get; set; }

    // Metadata
    public string? ModelUsed { get; set; }
    public double? Confidence { get; set; }
    public MessageStatus? Status { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    // Edit tracking
    public bool IsEdited { get; set; }
    public DateTimeOffset? EditedAt { get; set; }
    public string? EditedBy { get; set; }

    // Threading
    public string? ParentMessageId { get; set; }
    public List<string>? ChildMessageIds { get; set; }

    // References
    public List<string>? EvidenceIds { get; set; }
    public List<string>? EntityIds { get; set; }
}

/// <summary>
/// Investigation response with analysis results
/// </summary>
public class InvestigationResponse
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? SessionId { get; set; }
    public string? QueryId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    // Analysis results
    public RAGSearchResult? RAGResults { get; set; }
    public List<TranscriptionResult>? Transcriptions { get; set; }
    public List<ImageAnalysisResult>? ImageAnalyses { get; set; }
    public List<ToolResult>? ToolResults { get; set; }
    public List<Citation>? Citations { get; set; }
    public List<Visualization>? Visualizations { get; set; }

    // References
    public List<string>? EvidenceIds { get; set; }
    public List<string>? EntityIds { get; set; }

    public string Hash { get; set; } = string.Empty;

    public HashType HashType { get; set; } = HashType.SHA256;

    // Metadata
    public double? Confidence { get; set; }
    public string? ModelUsed { get; set; }
    public TimeSpan? ProcessingTime { get; set; }
    public ResponseDisplayType DisplayType { get; set; } = ResponseDisplayType.Auto;
    public Dictionary<string, object>? Metadata { get; set; }
}

#endregion

#region Analysis Results

/// Analysis result from investigation
/// Purpose: Store structured analysis results
/// Used by: Investigation service, report generator
/// </summary>
public class AnalysisResult
{
    public float ConfidenceScore;

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public AnalysisType Type { get; set; }
    public DateTimeOffset PerformedAt { get; set; } = DateTimeOffset.UtcNow;
    public string PerformedBy { get; set; } = string.Empty;
    public Dictionary<string, object> Results { get; set; } = new();
    public double Confidence { get; set; }
    public List<Finding> Findings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public TimeSpan AnalysisTime { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

#endregion

#region Processing Results

// <summary>
/// Generic process execution result
/// Purpose: Capture output from external processes
/// Used by: WSL commands, Python scripts, tools
/// </summary>
public class ProcessResult
{
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public bool Success => ExitCode == 0;
    public TimeSpan ExecutionTime { get; set; }
    public string? Command { get; set; }
    public List<string>? Arguments { get; set; }
    public Dictionary<string, object>? Environment { get; set; }
}

/// <summary>
/// Evidence processing result
/// Purpose: Track processing operations on evidence
/// Used by: Image analyzer, transcription service, OCR
/// </summary>
public class ProcessingResult
{
    public string ProcessingId { get; set; } = Guid.NewGuid().ToString();
    public string EvidenceId { get; set; } = string.Empty;
    public ProcessingStatus Status { get; set; }
    public string ProcessingType { get; set; } = string.Empty;
    public Dictionary<string, object>? Results { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public int? RetryCount { get; set; }
}

/// <summary>
/// Data chunk for deduplication
/// Purpose: Represent file chunks for dedup storage
/// Used by: Deduplication service, chunk store
/// </summary>
public class ChunkData
{
    public string Hash { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public int Size { get; set; }
    public int Offset { get; set; }
    public int Index { get; set; }
    public bool IsDuplicate { get; set; }
    public string? DuplicateChunkId { get; set; }
    public string? StorageLocation { get; set; }
    public int ReferenceCount { get; set; } = 1;
}

/// <summary>
/// Deduplication analysis result
/// Purpose: Report dedup savings and efficiency
/// Used by: Storage optimizer, admin dashboard
/// </summary>
public class DeduplicationResult
{
    public string FileHash { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public long OriginalSize { get; set; }
    public long DeduplicatedSize { get; set; }
    public List<string> ChunkHashes { get; set; } = new();
    public List<ChunkData> UniqueChunks { get; set; } = new();
    public List<ChunkData> DuplicateChunks { get; set; } = new();
    public long BytesSaved { get; set; }
    public double DeduplicationRatio { get; set; }
    public bool IsDuplicate { get; set; }
    public string? DuplicateOfId { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    public double GetSpaceSavingPercentage() =>
        OriginalSize > 0 ? (double)BytesSaved / OriginalSize * 100 : 0;
}


/// <summary>
/// Transcription result from audio analysis
/// </summary>
public class TranscriptionResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? EvidenceId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public double Confidence { get; set; }
    public TimeSpan? Duration { get; set; }
    public List<TranscriptionSegment>? Segments { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? ModelUsed { get; set; }
    public string? AudioFileId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Time-aligned transcription segment
/// </summary>
public class TranscriptionSegment
{
    public int Start { get; set; } // milliseconds
    public int End { get; set; } // milliseconds
    public string Text { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string? Speaker { get; set; }
}

/// <summary>
/// Transcribe audio command model
/// </summary>
public class TranscribeAudioCommand
{
    public string EvidenceId { get; set; } = string.Empty;
    public string? AudioPath { get; set; }
    public byte[]? AudioData { get; set; }
    public string? Language { get; set; }
    public bool EnableSpeakerDiarization { get; set; } = false;
    public bool EnableTimestamps { get; set; } = true;
    public string? ModelId { get; set; }
}

/// <summary>
/// Transcription response model
/// </summary>
public class TranscriptionResponse
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EvidenceId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public TimeSpan Duration { get; set; }
    public List<TranscriptionSegment>? Segments { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
}

/// <summary>
/// Generate image command model
/// </summary>
public class GenerateImageCommand
{
    public string Prompt { get; set; } = string.Empty;
    public string? NegativePrompt { get; set; }
    public string ModelId { get; set; } = "stable-diffusion";
    public int Width { get; set; } = 512;
    public int Height { get; set; } = 512;
    public int Steps { get; set; } = 20;
    public long? Seed { get; set; }
}



/// <summary>
/// Image analysis result from vision models
/// </summary>
public class ImageAnalysisResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EvidenceId { get; set; } = string.Empty;
    public List<DetectedObject> Objects { get; set; } = new();
    public List<DetectedFace> Faces { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<SimilarImage> SimilarImages { get; set; } = new();
    public float[]? Embedding { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Detected object in image
/// </summary>
public class DetectedObject
{
    public string Label { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public BoundingBox BoundingBox { get; set; } = new();
}

/// <summary>
/// Detected face in image
/// </summary>
public class DetectedFace
{
    public string Id { get; set; } = string.Empty;
    public BoundingBox BoundingBox { get; set; } = new();
    public Dictionary<string, double>? Emotions { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public float[]? Embedding { get; set; }
}

/// <summary>
/// Bounding box for object detection
/// </summary>
public class BoundingBox
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

/// <summary>
/// Similar image match
/// </summary>
public class SimilarImage
{
    public string EvidenceId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public double Similarity { get; set; }
}

#endregion

#region RAG and Knowledge Graph

/// <summary>
/// RAG search result with retrieved documents
/// </summary>
public class RAGSearchResult
{
    public List<RAGDocument> Documents { get; set; } = new();
    public List<Entity> Entities { get; set; } = new();
    public List<Relationship> Relationships { get; set; } = new();
    public KnowledgeGraph? KnowledgeGraph { get; set; }
    public QueryUnderstanding QueryUnderstanding { get; set; } = new();
    public List<string> SuggestedFollowUps { get; set; } = new();
    public Dictionary<string, object> CaseContext { get; set; } = new();
}

/// <summary>
/// Retrieved RAG document
/// </summary>
public class RAGDocument
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Relevance { get; set; }
    public string? SourceId { get; set; }
    public string? SourceType { get; set; }
    public List<int> ChunkIndices { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Entity extracted from analysis
/// </summary>
public class Entity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public EntityType Type { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public List<string> Aliases { get; set; } = new();
    public List<Relationship> Relationships { get; set; } = new();
    public List<string> AssociatedCaseIds { get; set; } = new();
    public double RiskScore { get; set; }
    public DateTimeOffset FirstSeen { get; set; }
    public DateTimeOffset LastSeen { get; set; }
    public Dictionary<string, object> Attributes { get; set; } = new();
}

/// <summary>
/// Relationship between entities
/// </summary>
public class Relationship
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SourceEntityId { get; set; } = string.Empty;
    public string TargetEntityId { get; set; } = string.Empty;
    public RelationshipType Type { get; set; }
    public double Strength { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Knowledge graph representation
/// </summary>
public class KnowledgeGraph
{
    public List<GraphNode> Nodes { get; set; } = new();
    public List<GraphEdge> Edges { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Graph node
/// </summary>
public class GraphNode
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Graph edge
/// </summary>
public class GraphEdge
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double Weight { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Query understanding analysis
/// </summary>
public class QueryUnderstanding
{
    public List<string> KeyTerms { get; set; } = new();
    public string Intent { get; set; } = string.Empty;
    public List<string> RequiredCapabilities { get; set; } = new();
    public double Complexity { get; set; }
}

#endregion

#region Reports and Findings

/// <summary>
/// Investigation report
/// </summary>
public class Report
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CaseId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public ReportType Type { get; set; }
    public ReportStatus Status { get; set; }
    public string Content { get; set; } = string.Empty;

    public List<ReportSection> Sections { get; set; } = new();
    public List<string> EvidenceIds { get; set; } = new();
    public List<Finding> Findings { get; set; } = new();
    public List<Recommendation> Recommendations { get; set; } = new();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset? SubmittedAt { get; set; }
    public string? SubmittedTo { get; set; }

    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Report section
/// </summary>
public class ReportSection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<string> EvidenceReferences { get; set; } = new();
    public List<Visualization> Visualizations { get; set; } = new();
}

/// <summary>
/// Investigation finding
/// </summary>
public class Finding
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FindingSeverity Severity { get; set; }
    public double Confidence { get; set; }
    public List<string> SupportingEvidenceIds { get; set; } = new();
    public List<string> RelatedEntityIds { get; set; } = new();
    public DateTimeOffset DiscoveredAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Investigation recommendation
/// </summary>
public class Recommendation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RecommendationPriority Priority { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public List<string> RelatedFindingIds { get; set; } = new();
}

#endregion

#region Timeline

/// <summary>
/// Investigation timeline
/// </summary>
public class Timeline
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CaseId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<TimelineEvent> Events { get; set; } = new();
    public List<TimelinePattern> Patterns { get; set; } = new();
    public List<TimelineAnomaly> Anomalies { get; set; } = new();
    public List<CriticalPeriod> CriticalPeriods { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Timeline event
/// </summary>
public class TimelineEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset Timestamp { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EventType Type { get; set; }
    public EventImportance Importance { get; set; }
    public string? EvidenceId { get; set; }
    public List<string> RelatedEntityIds { get; set; } = new();
    public GeoLocation? Location { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Timeline pattern detection
/// </summary>
public class TimelinePattern
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public PatternType Type { get; set; }
    public List<string> EventIds { get; set; } = new();
    public double Confidence { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Timeline anomaly
/// </summary>
public class TimelineAnomaly
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset Timestamp { get; set; }
    public AnomalyType Type { get; set; }
    public double Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> AffectedEventIds { get; set; } = new();
}

/// <summary>
/// Critical time period
/// </summary>
public class CriticalPeriod
{
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public string Description { get; set; } = string.Empty;
    public CriticalityLevel Level { get; set; }
    public List<string> EventIds { get; set; } = new();
}

#endregion

#region Tools and Visualization

/// <summary>
/// Tool execution result
/// </summary>
public class ToolResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ToolName { get; set; } = string.Empty;
    public ToolStatus Status { get; set; }
    public object? Data { get; set; }
    public List<Visualization> Visualizations { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTimeOffset ExecutedAt { get; set; } = DateTimeOffset.UtcNow;
    public TimeSpan ExecutionTime { get; set; }
    public string? ErrorMessage { get; set; }
    public ResponseDisplayType? PreferredDisplayType { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Visualization configuration
/// </summary>
public class Visualization
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public VisualizationType Type { get; set; } = VisualizationType.Auto;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public object Data { get; set; } = new { };
    public Dictionary<string, object> Options { get; set; } = new();
    public string? RenderFormat { get; set; } // html, svg, canvas
}

/// <summary>
/// Citation reference
/// </summary>
public class Citation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SourceId { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public string? Location { get; set; }
    public double Relevance { get; set; }
    public int? Index { get; set; }
    public string? Source { get; set; }
    public string? Url { get; set; }
    public DateTimeOffset? AccessedAt { get; set; }
    public string? Author { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// File attachment
/// </summary>
public class Attachment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public AttachmentType Type { get; set; }
    public string? StoragePath { get; set; }
    public string? Hash { get; set; }
    public string? ThumbnailPath { get; set; }
    public DateTimeOffset? UploadedAt { get; set; }
    public string? UploadedBy { get; set; }
    public ProcessingStatus? ProcessingStatus { get; set; }
    public Dictionary<string, object>? ExtractedMetadata { get; set; }
    public string? PreviewUrl { get; set; }
    public bool? IsProcessed { get; set; }
}

#endregion

// ========================================
// FILE SYNC MODELS
// ========================================

#region File Sync Models

/// <summary>
/// File sync request model
/// </summary>
public class FileSyncRequest
{
    public string WindowsPath { get; set; } = string.Empty;
    public string WslPath { get; set; } = string.Empty;
}

/// <summary>
/// Sync result model
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public int FilesSynced { get; set; }
    public int FilesSkipped { get; set; }
    public int FilesFailed { get; set; }
    public long BytesTransferred { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string>? Errors { get; set; }
}

#endregion


#region AI Models

/// <summary>
/// AI model configuration
/// </summary>
public class ModelConfiguration
{
    public string ModelId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public ModelType Type { get; set; }
    public ModelStatus Status { get; set; }
    public long MemoryUsage { get; set; }
    public long RequiredMemory { get; set; }
    public string? LoadedPath { get; set; }
    public string? ModelPath { get; set; }
    public DateTimeOffset? LoadedAt { get; set; }
    public ModelCapabilities Capabilities { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? SessionId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Model capabilities
/// </summary>
public class ModelCapabilities
{
    public int MaxContextLength { get; set; }
    public List<string> SupportedLanguages { get; set; } = new();
    public List<string> SpecialFeatures { get; set; } = new();
    public bool SupportsStreaming { get; set; }
    public bool SupportsFineTuning { get; set; }
    public bool SupportsMultiModal { get; set; }
    public Dictionary<string, object> CustomCapabilities { get; set; } = new();
}

/// <summary>
/// Model performance metrics
/// </summary>
public class ModelStats
{
    public string ModelId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long MemoryUsage { get; set; }
    public int AccessCount { get; set; }
    public DateTimeOffset LastAccessed { get; set; }
    public TimeSpan AverageLatency { get; set; }
    public double AverageTokensPerSecond { get; set; }
}

#endregion

#region System

/// <summary>
/// System health and status
/// Purpose: Monitor overall system health
/// Used by: Health checks, monitoring dashboard
/// </summary>
public class SystemStatus
{
    public bool IsHealthy { get; set; }
    public long MemoryUsed { get; set; }
    public long MemoryTotal { get; set; }
    public long DiskUsed { get; set; }
    public long DiskTotal { get; set; }
    public int LoadedModels { get; set; }
    public double CpuUsage { get; set; }
    public double GpuUsage { get; set; }
    public Dictionary<string, ServiceStatus> Services { get; set; } = new();
    public List<string> HealthIssues { get; set; } = new();
    public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Connection test result
/// Purpose: Verify service connectivity
/// Used by: Service health checks, diagnostics
/// </summary>
public class TestConnectionResult
{
    public bool Success { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
    public string? Error { get; set; }
    public int? StatusCode { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public DateTimeOffset TestedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// WSL subsystem status
/// Purpose: Monitor WSL2 and Linux services
/// Used by: WSL manager, platform monitor
/// </summary>
public class WslStatus
{
    public bool IsInstalled { get; set; }
    public bool IsWsl2 { get; set; }
    public string? Version { get; set; }
    public string? KernelVersion { get; set; }
    public bool VirtualMachinePlatform { get; set; }
    public bool HyperV { get; set; }
    public bool HasIimDistro { get; set; }
    public bool IsReady { get; set; }
    public string Message { get; set; } = string.Empty;

    // Runtime status
    public bool WslReady { get; set; }
    public bool DistroRunning { get; set; }
    public bool ServicesHealthy { get; set; }
    public bool NetworkConnected { get; set; }
    public List<string> Issues { get; set; } = new();
    public Dictionary<string, bool> ServiceStatuses { get; set; } = new();
}

/// <summary>
/// Device hardware information
/// Purpose: Track hardware capabilities
/// Used by: Model selector, performance optimizer
/// </summary>
public class DeviceInfo
{
    public string DeviceType { get; set; } = string.Empty; // CPU, GPU, TPU
    public string DeviceName { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
    public long MemoryAvailable { get; set; }
    public long MemoryTotal { get; set; }
    public int ComputeUnits { get; set; }
    public bool SupportsDirectML { get; set; }
    public bool SupportsROCm { get; set; }
    public bool SupportsCUDA { get; set; }
    public int? CudaVersion { get; set; }
    public Dictionary<string, object>? Capabilities { get; set; }

    public double GetMemoryUsagePercentage() =>
        MemoryTotal > 0 ? (double)(MemoryTotal - MemoryAvailable) / MemoryTotal * 100 : 0;
}

/// <summary>
/// Service operation response model
/// </summary>
/// 


public class ServiceOperationResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public Dictionary<string, object>? Data { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public ServiceOperationResponse(bool success, string messasge, string servicename, string status ) { this.Success = success; this.Message = messasge; this.Message = $"{servicename} : {status}"; }
}

    /// <summary>
    /// Service status list response model
    /// </summary>
    public class ServiceStatusListResponse
    {
        public Dictionary<string, ServiceStatus> Services { get; set; } = new();
        public int TotalServices { get; set; }
        public int HealthyServices { get; set; }
        public int UnhealthyServices { get; set; }
        public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Error response model
    /// </summary>
    public class ErrorResponse
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Status { get; set; }
        public string Detail { get; set; } = string.Empty;
        public string Instance { get; set; } = string.Empty;
        public Dictionary<string, object>? Extensions { get; set; }
    }

    /// <summary>
    /// Validation error response model
    /// </summary>
    public class ValidationErrorResponse
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = "Validation Failed";
        public int Status { get; set; } = 422;
        public Dictionary<string, List<string>> Errors { get; set; } = new();
        public string Instance { get; set; } = string.Empty;
    }

    /// <summary>
    /// Start WSL command model
    /// </summary>
    public class StartWslCommand
    {
        public string DistroName { get; set; } = "IIM-Ubuntu";
        public bool StartServices { get; set; } = true;
        public List<string>? ServicesToStart { get; set; }
        public int TimeoutSeconds { get; set; } = 60;
    }

    /// <summary>
    /// Stop WSL command model
    /// </summary>
    public class StopWslCommand
    {
        public string DistroName { get; set; } = "IIM-Ubuntu";
        public bool ForceStop { get; set; } = false;
        public bool SaveState { get; set; } = true;
        public int GracePeriodSeconds { get; set; } = 30;
    }

    /// <summary>
    /// WSL health response model
    /// </summary>
    public class WslHealthResponse
    {
        public bool IsHealthy { get; set; }
        public WslStatus Status { get; set; } = new();
        public Dictionary<string, ServiceStatus> Services { get; set; } = new();
        public List<string> Issues { get; set; } = new();
        public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// WSL status response model
    /// </summary>
    public class WslStatusResponse
    {
        public bool WslInstalled { get; set; }
        public bool DistroRunning { get; set; }
        public string? DistroName { get; set; }
        public string? WslVersion { get; set; }
        public List<string> RunningServices { get; set; } = new();
    }




#endregion

// ========================================
// EXPORT MODELS
// ========================================

#region Export Models

/// <summary>
/// Export result model
/// </summary>
public class ExportResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
    public byte[]? Data { get; set; }
    public long FileSize { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Export template model
/// </summary>
public class ExportTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public ExportFormat Format { get; set; }
    public string? HeaderTemplate { get; set; }
    public string? BodyTemplate { get; set; }
    public string? FooterTemplate { get; set; }
    public Dictionary<string, object> DefaultOptions { get; set; } = new();
    public bool IsSystemTemplate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
}

#endregion

#region Infrastructure

/// <summary>
/// Audit log entry
/// </summary>
public class AuditEvent
{
    public long Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? RequestId { get; set; }
    public string? UserId { get; set; }
    public string? ModelId { get; set; }
    public string? EntityId { get; set; }
    public string? EntityType { get; set; }
    public string? Action { get; set; }
    public string? Result { get; set; }
    public string? Details { get; set; }
    public string? ErrorType { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Priority { get; set; }
    public long? DurationMs { get; set; }
    public int? TokensGenerated { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object>? AdditionalData { get; set; }
}

public class AuditLogFilter
{
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string? EventType { get; set; }
    public string? UserId { get; set; }
    public string? EntityId { get; set; }
    public string? EntityType { get; set; }
    public string? Action { get; set; }
    public string? IpAddress { get; set; }
    public int Limit { get; set; } = 100;
    public int Offset { get; set; } = 0;
    public bool? SuccessOnly { get; set; }
    public string? SortBy { get; set; } = "Timestamp";
    public bool SortDescending { get; set; } = true;
}


/// <summary>
/// Service status information
/// </summary>
public class ServiceStatus
{
    public string Name { get; set; } = string.Empty;
    public ServiceState State { get; set; }
    public bool IsHealthy { get; set; }
    public string? Endpoint { get; set; }
    public string? Version { get; set; }
    public long MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Notification
/// </summary>
public class Notification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public NotificationCategory Category { get; set; } = NotificationCategory.System;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }
    public string? SourceId { get; set; }
    public string? SourceType { get; set; }
    public string? ImageUrl { get; set; }
    public string? Topic { get; set; }
    public NotificationAction? PrimaryAction { get; set; }
    public List<NotificationAction>? SecondaryActions { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Notification action
/// </summary>
public class NotificationAction
{
    public string Label { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty; // navigate, execute, dismiss
    public string? Target { get; set; } // URL or command
    public Dictionary<string, object>? Parameters { get; set; }
}

#endregion

#region Common Types

/// <summary>
/// Geolocation information
/// </summary>
public class GeoLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Altitude { get; set; }
    public double? Accuracy { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Time range specification
/// </summary>
public class TimeRange
{
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }

    public TimeSpan Duration => End - Start;

    public bool Contains(DateTimeOffset timestamp) =>
        timestamp >= Start && timestamp <= End;
}

/// <summary>
/// Export operation configuration
/// </summary>
public class ExportOperation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public ExportFormat Format { get; set; }
    public ExportStatus Status { get; set; } = ExportStatus.Pending;
    public string? FilePath { get; set; }
    public long? FileSize { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Export options
/// </summary>
public class ExportOptions
{
    public bool IncludeMetadata { get; set; } = true;
    public bool IncludeChainOfCustody { get; set; } = true;
    public bool IncludeHeaders { get; set; } = true;
    public bool IncludeFooters { get; set; } = true;
    public bool IncludeWatermark { get; set; } = true;
    public bool IncludeCaseInfo { get; set; } = false;
    public bool IncludeTimestamp { get; set; } = true;
    public bool IncludeSignature { get; set; } = true;
    public Dictionary<string, object>? CustomOptions { get; set; }
}

/// <summary>
/// Performance metrics
/// </summary>
public class PerformanceMetrics
{
    public string ModelId { get; set; } = string.Empty;
    public string ModelType { get; set; } = string.Empty;

    // System metrics
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double GpuUtilization { get; set; }
    public long VramUsageBytes { get; set; }
    public long RamUsageBytes { get; set; }

    // Inference metrics
    public double TokensPerSecond { get; set; }
    public double AverageLatencyMs { get; set; }
    public double P50LatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    public double P99LatencyMs { get; set; }

    // Quality metrics
    public double Accuracy { get; set; }
    public double Confidence { get; set; }

    // Counts
    public long TotalInferences { get; set; }
    public long SuccessfulInferences { get; set; }
    public long FailedInferences { get; set; }

    // Queue metrics
    public int QueueLength { get; set; }
    public int ActiveRequests { get; set; }
    public int PendingRequests { get; set; }

    // Timestamps
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public TimeSpan MeasurementPeriod { get; set; } = TimeSpan.FromMinutes(1);

    public Dictionary<string, object> CustomMetrics { get; set; } = new();

    public double GetSuccessRate() => TotalInferences > 0
        ? (double)SuccessfulInferences / TotalInferences * 100
        : 0;
}

#endregion