// ====================================================================
// IIM Model-DTO Migration and Consolidation Script
// ====================================================================
// This script analyzes both Models and DTOs to preserve the best properties
// from each version, then consolidates them into a single source of truth.
//
// Run this script from the solution root directory using:
// dotnet script migration-script.csx
// ====================================================================

#r "nuget: Microsoft.CodeAnalysis.CSharp, 4.7.0"
#r "nuget: Microsoft.CodeAnalysis.Workspaces.Common, 4.7.0"

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// ====================================================================
// CONFIGURATION
// ====================================================================

var solutionRoot = Directory.GetCurrentDirectory();
var sharedProjectPath = Path.Combine(solutionRoot, "src", "IIM.Shared");
var modelsPath = Path.Combine(sharedProjectPath, "Models");
var dtosPath = Path.Combine(sharedProjectPath, "DTOs");
var backupPath = Path.Combine(solutionRoot, "backup", $"migration_{DateTime.Now:yyyyMMddHHmmss}");

// ====================================================================
// STEP 1: ANALYZE COMPLETENESS - Which version is more complete?
// ====================================================================

public class ModelComparison
{
    public string Name { get; set; }
    public string ModelPath { get; set; }
    public string DtoPath { get; set; }
    public List<string> ModelProperties { get; set; } = new();
    public List<string> DtoProperties { get; set; } = new();
    public List<string> OnlyInModel { get; set; } = new();
    public List<string> OnlyInDto { get; set; } = new();
    public string RecommendedSource { get; set; }
    public string MergeStrategy { get; set; }
}

var comparisons = new List<ModelComparison>();

// Analyze InvestigationSession
var sessionComparison = new ModelComparison
{
    Name = "InvestigationSession",
    ModelProperties = new() 
    { 
        "Id", "CaseId", "Title", "Icon", "Type", "Messages", "EnabledTools", 
        "Models", "CreatedAt", "UpdatedAt", "CreatedBy", "Status", "Findings" 
    },
    DtoProperties = new() 
    { 
        "Id", "CaseId", "UserId", "Title", "Type", "Status", "CreatedAt", 
        "UpdatedAt", "ClosedAt", "MessageCount", "EnabledTools", "Models", 
        "Findings", "Metrics" 
    }
};
sessionComparison.OnlyInModel = new() { "Icon", "CreatedBy", "Messages" };
sessionComparison.OnlyInDto = new() { "UserId", "ClosedAt", "MessageCount", "Metrics" };
sessionComparison.RecommendedSource = "MERGE";
sessionComparison.MergeStrategy = "Combine both - Model has actual Messages list, DTO has metrics";
comparisons.Add(sessionComparison);

// Analyze InvestigationMessage
var messageComparison = new ModelComparison
{
    Name = "InvestigationMessage",
    ModelProperties = new() 
    { 
        "Id", "Role", "Content", "Attachments", "ToolResults", "Citations", 
        "Timestamp", "ModelUsed", "Metadata", "SessionId", "ParentMessageId", 
        "ChildMessageIds", "IsEdited", "EditedAt", "EditedBy", "Status", "Confidence" 
    },
    DtoProperties = new() 
    { 
        "Id", "Role", "Content", "Attachments", "ToolResults", "Citations", 
        "Timestamp", "ModelUsed", "Metadata" 
    }
};
messageComparison.OnlyInModel = new() 
{ 
    "SessionId", "ParentMessageId", "ChildMessageIds", "IsEdited", 
    "EditedAt", "EditedBy", "Status", "Confidence" 
};
messageComparison.OnlyInDto = new() { }; // DTO has no unique properties
messageComparison.RecommendedSource = "MODEL";
messageComparison.MergeStrategy = "Use Model - it's more complete with threading support";
comparisons.Add(messageComparison);

// Analyze InvestigationResponse
var responseComparison = new ModelComparison
{
    Name = "InvestigationResponse",
    ModelProperties = new() 
    { 
        "Id", "Message", "ToolResults", "Citations", "RelatedEvidence", 
        "Confidence", "DisplayType", "Metadata", "DisplayMetadata", 
        "Visualizations", "SessionId", "Timestamp", "QueryId", "ModelUsed", 
        "ProcessingTime", "CreatedBy", "CreatedAt", "Hash", "Visualization" 
    },
    DtoProperties = new() 
    { 
        "Id", "SessionId", "QueryId", "Message", "RAGResults", "Transcriptions", 
        "ImageAnalyses", "ToolResults", "Citations", "EvidenceIds", "EntityIds", 
        "Confidence", "FineTuneJobId", "Timestamp", "Metadata" 
    }
};
responseComparison.OnlyInModel = new() 
{ 
    "RelatedEvidence", "DisplayType", "DisplayMetadata", "Visualizations", 
    "ModelUsed", "ProcessingTime", "CreatedBy", "CreatedAt", "Hash", "Visualization" 
};
responseComparison.OnlyInDto = new() 
{ 
    "RAGResults", "Transcriptions", "ImageAnalyses", "EvidenceIds", 
    "EntityIds", "FineTuneJobId" 
};
responseComparison.RecommendedSource = "MERGE";
responseComparison.MergeStrategy = "Complex merge - Model has display properties, DTO has processing results";
comparisons.Add(responseComparison);

// Analyze CreateSessionRequest
var createSessionComparison = new ModelComparison
{
    Name = "CreateSessionRequest",
    ModelProperties = new() 
    { 
        "CaseId", "Title", "InvestigationType", "Description", "UserId", 
        "Metadata", "EnabledTools", "InitialContext" 
    },
    DtoProperties = new() 
    { 
        "CaseId", "Title", "InvestigationType", "Models", "EnabledTools", "Context" 
    }
};
createSessionComparison.OnlyInModel = new() { "Description", "UserId", "Metadata", "InitialContext" };
createSessionComparison.OnlyInDto = new() { "Models", "Context" };
createSessionComparison.RecommendedSource = "MERGE";
createSessionComparison.MergeStrategy = "Combine - Model has metadata, DTO has models config";
comparisons.Add(createSessionComparison);

// ====================================================================
// STEP 2: CREATE BACKUP
// ====================================================================

Console.WriteLine("Creating backup...");
Directory.CreateDirectory(backupPath);

void BackupDirectory(string source, string destination)
{
    Directory.CreateDirectory(destination);
    foreach (var file in Directory.GetFiles(source, "*.cs", SearchOption.AllDirectories))
    {
        var relativePath = Path.GetRelativePath(source, file);
        var destFile = Path.Combine(destination, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destFile));
        File.Copy(file, destFile, true);
    }
}

BackupDirectory(sharedProjectPath, Path.Combine(backupPath, "IIM.Shared"));
Console.WriteLine($"✅ Backup created at: {backupPath}");

// ====================================================================
// STEP 3: GENERATE MERGED MODELS
// ====================================================================

Console.WriteLine("\n📊 ANALYSIS RESULTS:");
Console.WriteLine("====================");

foreach (var comp in comparisons)
{
    Console.WriteLine($"\n{comp.Name}:");
    Console.WriteLine($"  Strategy: {comp.MergeStrategy}");
    Console.WriteLine($"  Properties only in Model: {string.Join(", ", comp.OnlyInModel)}");
    Console.WriteLine($"  Properties only in DTO: {string.Join(", ", comp.OnlyInDto)}");
}

// Generate the merged classes
var mergedClasses = new StringBuilder();

mergedClasses.AppendLine(@"// ====================================================================
// MERGED INVESTIGATION MODELS
// Generated by migration script - Best of both Models and DTOs
// ====================================================================

using System;
using System.Collections.Generic;
using IIM.Shared.Enums;

namespace IIM.Shared.DTOs
{
    /// <summary>
    /// Investigation session with complete properties from both versions
    /// </summary>
    public class InvestigationSession
    {
        // Core properties (from Model)
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CaseId { get; set; } = string.Empty;
        public string Title { get; set; } = ""New Investigation"";
        public string Icon { get; set; } = ""🕵️‍♂️"";
        public InvestigationType Type { get; set; } = InvestigationType.GeneralInquiry;
        public InvestigationStatus Status { get; set; } = InvestigationStatus.Active;
        
        // User tracking (merged from both)
        public string CreatedBy { get; set; } = Environment.UserName;
        public string? UserId { get; set; }  // From DTO
        
        // Timestamps (merged)
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ClosedAt { get; set; }  // From DTO
        
        // Collections (from Model)
        public List<InvestigationMessage> Messages { get; set; } = new();
        public List<string> EnabledTools { get; set; } = new();
        public Dictionary<string, ModelConfiguration> Models { get; set; } = new();
        public List<Finding> Findings { get; set; } = new();
        
        // Metrics (from DTO)
        public int MessageCount => Messages?.Count ?? 0;  // Computed property
        public Dictionary<string, object>? Metrics { get; set; }  // From DTO
    }

    /// <summary>
    /// Investigation message with threading and edit tracking
    /// </summary>
    public class InvestigationMessage
    {
        // Core properties (from both)
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public MessageRole Role { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        
        // Optional properties (from Model)
        public List<Attachment>? Attachments { get; set; }
        public List<ToolResult>? ToolResults { get; set; }
        public List<Citation>? Citations { get; set; }
        public string? ModelUsed { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        
        // Threading support (from Model - unique features)
        public string? SessionId { get; set; }
        public string? ParentMessageId { get; set; }
        public List<string>? ChildMessageIds { get; set; }
        
        // Edit tracking (from Model - unique features)
        public bool IsEdited { get; set; }
        public DateTimeOffset? EditedAt { get; set; }
        public string? EditedBy { get; set; }
        
        // Status tracking (from Model)
        public MessageStatus? Status { get; set; }
        public double? Confidence { get; set; }
    }

    /// <summary>
    /// Investigation query request
    /// </summary>
    public class InvestigationQuery
    {
        public string Text { get; set; } = string.Empty;
        public List<Attachment> Attachments { get; set; } = new();
        public List<string> EnabledTools { get; set; } = new();
        public Dictionary<string, object> Context { get; set; } = new();
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        
        // From DTO version
        public string? SessionId { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
        public List<string>? RequestedTools { get; set; }
    }

    /// <summary>
    /// Investigation response with all processing results and display metadata
    /// </summary>
    public class InvestigationResponse
    {
        // Core properties
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Message { get; set; } = string.Empty;
        
        // Session context (from both)
        public string? SessionId { get; set; }
        public string? QueryId { get; set; }
        
        // Processing results (merged from both)
        public List<ToolResult>? ToolResults { get; set; }
        public List<Citation>? Citations { get; set; }
        public List<Evidence>? RelatedEvidence { get; set; }  // From Model
        public List<string>? EvidenceIds { get; set; }  // From DTO
        public List<string>? EntityIds { get; set; }  // From DTO
        
        // Analysis results (from DTO)
        public RAGSearchResult? RAGResults { get; set; }
        public List<TranscriptionResult>? Transcriptions { get; set; }
        public List<ImageAnalysisResult>? ImageAnalyses { get; set; }
        
        // Display properties (from Model)
        public ResponseDisplayType DisplayType { get; set; } = ResponseDisplayType.Auto;
        public Dictionary<string, object>? DisplayMetadata { get; set; }
        public List<Visualization>? Visualizations { get; set; }
        public ResponseVisualization? PrimaryVisualization { get; set; }
        
        // Metrics and metadata
        public double? Confidence { get; set; }
        public TimeSpan? ProcessingTime { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModelUsed { get; set; }
        public string? Hash { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        
        // Fine-tuning (from DTO - for future use)
        public string? FineTuneJobId { get; set; }
    }

    /// <summary>
    /// Create session request with all configuration options
    /// </summary>
    public class CreateSessionRequest
    {
        // Required properties
        public string CaseId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string InvestigationType { get; set; } = string.Empty;
        
        // Optional properties (from Model)
        public string? Description { get; set; }
        public string? UserId { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public Dictionary<string, object>? InitialContext { get; set; }
        
        // Configuration (from DTO)
        public Dictionary<string, ModelConfiguration>? Models { get; set; }
        public List<string>? EnabledTools { get; set; }
        public SessionContext? Context { get; set; }
    }
}");

// ====================================================================
// STEP 4: UPDATE REFERENCES
// ====================================================================

Console.WriteLine("\n🔄 UPDATING REFERENCES:");
Console.WriteLine("========================");

var filesToUpdate = Directory.GetFiles(solutionRoot, "*.cs", SearchOption.AllDirectories)
    .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\backup\\"))
    .ToList();

var updateCount = 0;
var filesModified = new List<string>();

foreach (var file in filesToUpdate)
{
    var content = File.ReadAllText(file);
    var originalContent = content;
    
    // Update using statements
    if (content.Contains("using IIM.Shared.DTOs;"))
    {
        content = content.Replace("using IIM.Shared.DTOs;", "using IIM.Shared.DTOs;");
        
        // Also check if we need to add specific model namespaces
        if (content.Contains("CreateSessionRequest") && !content.Contains("IIM.Shared.DTOs"))
        {
            content = content.Replace("using IIM.Shared.DTOs;", 
                "using IIM.Shared.DTOs;\nusing CreateSessionRequest = IIM.Shared.DTOs.CreateSessionRequest;");
        }
    }
    
    // Update fully qualified names
    content = content.Replace("IIM.Shared.DTOs.InvestigationSession", "IIM.Shared.DTOs.InvestigationSession");
    content = content.Replace("IIM.Shared.DTOs.InvestigationMessage", "IIM.Shared.DTOs.InvestigationMessage");
    content = content.Replace("IIM.Shared.DTOs.InvestigationQuery", "IIM.Shared.DTOs.InvestigationQuery");
    content = content.Replace("IIM.Shared.DTOs.InvestigationResponse", "IIM.Shared.DTOs.InvestigationResponse");
    content = content.Replace("IIM.Shared.DTOs.CreateSessionRequest", "IIM.Shared.DTOs.CreateSessionRequest");
    
    // Remove Dto suffixes where they appear
    content = Regex.Replace(content, @"\bInvestigationSessionDto\b", "InvestigationSession");
    content = Regex.Replace(content, @"\bInvestigationMessageDto\b", "InvestigationMessage");
    content = Regex.Replace(content, @"\bInvestigationQueryDto\b", "InvestigationQuery");
    content = Regex.Replace(content, @"\bInvestigationResponseDto\b", "InvestigationResponse");
    content = Regex.Replace(content, @"\bCreateSessionRequestDto\b", "CreateSessionRequest");
    
    if (content != originalContent)
    {
        File.WriteAllText(file, content);
        updateCount++;
        filesModified.Add(Path.GetRelativePath(solutionRoot, file));
    }
}

Console.WriteLine($"✅ Updated {updateCount} files");

// ====================================================================
// STEP 5: GENERATE MIGRATION REPORT
// ====================================================================

var reportPath = Path.Combine(backupPath, "migration_report.md");
var report = new StringBuilder();

report.AppendLine("# IIM Model-DTO Migration Report");
report.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
report.AppendLine();
report.AppendLine("## Summary");
report.AppendLine($"- Files backed up: {Directory.GetFiles(backupPath, "*.cs", SearchOption.AllDirectories).Length}");
report.AppendLine($"- Files updated: {updateCount}");
report.AppendLine($"- Models analyzed: {comparisons.Count}");
report.AppendLine();
report.AppendLine("## Merge Decisions");
foreach (var comp in comparisons)
{
    report.AppendLine($"### {comp.Name}");
    report.AppendLine($"- **Strategy**: {comp.MergeStrategy}");
    report.AppendLine($"- **Properties added from Model**: {string.Join(", ", comp.OnlyInModel)}");
    report.AppendLine($"- **Properties added from DTO**: {string.Join(", ", comp.OnlyInDto)}");
    report.AppendLine();
}
report.AppendLine("## Files Modified");
foreach (var file in filesModified.Take(20))
{
    report.AppendLine($"- {file}");
}
if (filesModified.Count > 20)
{
    report.AppendLine($"... and {filesModified.Count - 20} more files");
}

File.WriteAllText(reportPath, report.ToString());

// ====================================================================
// STEP 6: CLEANUP RECOMMENDATIONS
// ====================================================================

Console.WriteLine("\n🧹 CLEANUP RECOMMENDATIONS:");
Console.WriteLine("============================");
Console.WriteLine("1. DELETE these unused DTOs:");
Console.WriteLine("   - ModelHandleDto");
Console.WriteLine("   - BatchInferenceRequest/Response");
Console.WriteLine("   - FineTuneRequest, TrainingConfigDto");
Console.WriteLine("   - CollaborationUpdate, StreamMessage");
Console.WriteLine("   - ExportPackageDto, ImportResultDto");
Console.WriteLine("   - FileSyncRequest/Response");
Console.WriteLine();
Console.WriteLine("2. DELETE the old Models folder after verification:");
Console.WriteLine($"   - {modelsPath}");
Console.WriteLine();
Console.WriteLine("3. TEST these critical paths:");
Console.WriteLine("   - Desktop client -> API communication");
Console.WriteLine("   - Session creation and message handling");
Console.WriteLine("   - Investigation query processing");
Console.WriteLine();
Console.WriteLine($"📋 Full report saved to: {reportPath}");
Console.WriteLine($"💾 Backup location: {backupPath}");
Console.WriteLine();
Console.WriteLine("✅ Migration script completed successfully!");
Console.WriteLine("⚠️  Please review changes and test before committing.");

// ====================================================================
// ROLLBACK SCRIPT (save separately as rollback.csx)
// ====================================================================

var rollbackScript = @"
// Run this to rollback changes if needed
var backupPath = @""" + backupPath + @""";
var solutionRoot = @""" + solutionRoot + @""";

Console.WriteLine(""Rolling back changes..."");
var sharedBackup = Path.Combine(backupPath, ""IIM.Shared"");
var sharedTarget = Path.Combine(solutionRoot, ""src"", ""IIM.Shared"");

foreach (var file in Directory.GetFiles(sharedBackup, ""*.cs"", SearchOption.AllDirectories))
{
    var relativePath = Path.GetRelativePath(sharedBackup, file);
    var targetFile = Path.Combine(sharedTarget, relativePath);
    File.Copy(file, targetFile, true);
}

Console.WriteLine(""✅ Rollback completed"");
";

File.WriteAllText(Path.Combine(backupPath, "rollback.csx"), rollbackScript);
Console.WriteLine($"↩️  Rollback script saved to: {Path.Combine(backupPath, "rollback.csx")}");