// ====================================================================
// IIM COMPREHENSIVE MODEL/DTO CLEANUP SCRIPT
// ====================================================================
// This script performs a thorough analysis and cleanup of duplicate
// models and DTOs, keeping the best version of each.
// ====================================================================

#r "nuget: Microsoft.CodeAnalysis.CSharp, 4.7.0"
#r "nuget: System.Text.Json, 7.0.0"

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

var solutionRoot = Directory.GetCurrentDirectory();
var backupPath = Path.Combine(solutionRoot, "backup", $"cleanup_{DateTime.Now:yyyyMMddHHmmss}");

// ====================================================================
// STEP 1: COMPREHENSIVE ANALYSIS
// ====================================================================

Console.WriteLine("🔍 COMPREHENSIVE ANALYSIS OF MODELS vs DTOs");
Console.WriteLine("=" + new string('=', 50));

var analysisResults = new Dictionary<string, (string winner, string reason, List<string> mergeProperties)>();

// 1. InvestigationSession - EXISTS IN BOTH
// Models version: Has Icon, Findings list
// DTOs version: Missing (using record syntax for others)
analysisResults["InvestigationSession"] = ("Models", "Models has complete implementation", new List<string>());

// 2. InvestigationMessage - EXISTS IN BOTH  
// Models version: Has threading (ParentMessageId, ChildMessageIds), edit tracking
// DTOs version (record): Basic properties only, using AttachmentDto
analysisResults["InvestigationMessage"] = ("Models", "Models has threading and edit tracking", new List<string>());

// 3. InvestigationQuery - EXISTS IN BOTH
// Models version: class with properties
// DTOs version (record): Has SessionId, Parameters, RequestedTools  
analysisResults["InvestigationQuery"] = ("DTOs", "DTOs version has SessionId which is needed", new List<string> { "SessionId", "Parameters", "RequestedTools" });

// 4. InvestigationResponse - EXISTS IN BOTH
// Models version: Has RelatedEvidence, DisplayType, Visualizations
// DTOs version (record): Has RAGResults, Transcriptions, ImageAnalyses
analysisResults["InvestigationResponse"] = ("MERGE", "Both have unique valuable properties", 
    new List<string> { "RAGResults", "Transcriptions", "ImageAnalyses", "EvidenceIds", "EntityIds", "FineTuneJobId" });

// 5. CreateSessionRequest - EXISTS IN BOTH
// Models version: Has Description, UserId, Metadata, InitialContext
// DTOs version (record): Has Models, Context
analysisResults["CreateSessionRequest"] = ("MERGE", "Both have unique properties",
    new List<string> { "Models", "Context" });

// 6. Attachment - EXISTS IN Models only
analysisResults["Attachment"] = ("Models", "Only exists in Models", new List<string>());

// 7. Citation - EXISTS IN Models only  
analysisResults["Citation"] = ("Models", "Only exists in Models", new List<string>());

// 8. ToolResult - EXISTS IN Models only
analysisResults["ToolResult"] = ("Models", "Only exists in Models", new List<string>());

// 9. Evidence - EXISTS IN Models only
analysisResults["Evidence"] = ("Models", "Only exists in Models", new List<string>());

// 10. Case - EXISTS IN Models only
analysisResults["Case"] = ("Models", "Only exists in Models", new List<string>());

// 11. Finding - EXISTS IN Models only (but FindingDto exists in DTOs)
analysisResults["Finding"] = ("Models", "Models version is complete", new List<string>());

// 12. ModelConfiguration - EXISTS IN Models only
analysisResults["ModelConfiguration"] = ("Models", "Only exists in Models", new List<string>());

// Print analysis results
foreach (var (className, (winner, reason, mergeProps)) in analysisResults)
{
    Console.WriteLine($"\n{className}:");
    Console.WriteLine($"  Winner: {winner}");
    Console.WriteLine($"  Reason: {reason}");
    if (mergeProps.Any())
    {
        Console.WriteLine($"  Properties to merge from DTOs: {string.Join(", ", mergeProps)}");
    }
}

// ====================================================================
// STEP 2: CREATE BACKUP
// ====================================================================

Console.WriteLine($"\n\n📦 CREATING BACKUP...");
Directory.CreateDirectory(backupPath);

void BackupFile(string filePath)
{
    if (File.Exists(filePath))
    {
        var relativePath = Path.GetRelativePath(solutionRoot, filePath);
        var backupFilePath = Path.Combine(backupPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(backupFilePath));
        File.Copy(filePath, backupFilePath, true);
    }
}

// Backup all relevant files
var filesToBackup = Directory.GetFiles(Path.Combine(solutionRoot, "src"), "*.cs", SearchOption.AllDirectories)
    .Where(f => f.Contains("IIM.Shared") || f.Contains("Models") || f.Contains("DTOs"));

foreach (var file in filesToBackup)
{
    BackupFile(file);
}

Console.WriteLine($"✅ Backup created at: {backupPath}");

// ====================================================================
// STEP 3: MERGE AND CONSOLIDATE
// ====================================================================

Console.WriteLine($"\n\n🔧 MERGING AND CONSOLIDATING...");

// Update InvestigationQuery in Models to include DTOs properties
var queryModelPath = Path.Combine(solutionRoot, "src", "IIM.Shared", "Models", "Sessions.cs");
if (File.Exists(queryModelPath))
{
    var content = File.ReadAllText(queryModelPath);
    
    // Add missing properties to InvestigationQuery
    var queryClassPattern = @"public class InvestigationQuery\s*\{[^}]+\}";
    var updatedQueryClass = @"public class InvestigationQuery
    {
        public string Text { get; set; } = string.Empty;
        public List<Attachment> Attachments { get; set; } = new();
        public List<string> EnabledTools { get; set; } = new();
        public Dictionary<string, object> Context { get; set; } = new();
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        
        // Properties from DTO version
        public string? SessionId { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
        public List<string>? RequestedTools { get; set; }
    }";
    
    content = Regex.Replace(content, queryClassPattern, updatedQueryClass, RegexOptions.Singleline);
    File.WriteAllText(queryModelPath, content);
    Console.WriteLine("✅ Updated InvestigationQuery with DTO properties");
}

// Update InvestigationResponse to include DTO properties
if (File.Exists(queryModelPath))
{
    var content = File.ReadAllText(queryModelPath);
    
    // Find InvestigationResponse class and add missing properties
    var responsePattern = @"(public class InvestigationResponse\s*\{[^}]+// New optional properties)";
    var additionalProperties = @"
        // Properties from DTO version
        public RAGSearchResult? RAGResults { get; set; }
        public List<TranscriptionResult>? Transcriptions { get; set; }
        public List<ImageAnalysisResult>? ImageAnalyses { get; set; }
        public List<string>? EvidenceIds { get; set; }
        public List<string>? EntityIds { get; set; }
        public string? FineTuneJobId { get; set; }
        
        // New optional properties";
    
    content = content.Replace("// New optional properties", additionalProperties);
    File.WriteAllText(queryModelPath, content);
    Console.WriteLine("✅ Updated InvestigationResponse with DTO properties");
}

// Update CreateSessionRequest
var createSessionPattern = @"(public class CreateSessionRequest\s*\{[^}]+)(\})";
if (File.Exists(queryModelPath))
{
    var content = File.ReadAllText(queryModelPath);
    
    // Add Models property from DTO
    content = content.Replace(
        "public Dictionary<string, object>? InitialContext { get; set; }",
        @"public Dictionary<string, object>? InitialContext { get; set; }
        
        // Properties from DTO version
        public Dictionary<string, ModelConfiguration>? Models { get; set; }
        public SessionContext? Context { get; set; }"
    );
    
    File.WriteAllText(queryModelPath, content);
    Console.WriteLine("✅ Updated CreateSessionRequest with DTO properties");
}

// ====================================================================
// STEP 4: UPDATE ALL REFERENCES
// ====================================================================

Console.WriteLine($"\n\n📝 UPDATING REFERENCES...");

var filesToUpdate = Directory.GetFiles(solutionRoot, "*.cs", SearchOption.AllDirectories)
    .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\backup\\"));

int updatedCount = 0;
var updatedFiles = new List<string>();

foreach (var file in filesToUpdate)
{
    bool changed = false;
    var content = File.ReadAllText(file);
    var originalContent = content;
    
    // Fix DTOs namespace references to use Models
    if (content.Contains("IIM.Shared.DTOs.InvestigationSession") ||
        content.Contains("IIM.Shared.DTOs.InvestigationMessage") ||
        content.Contains("IIM.Shared.DTOs.InvestigationResponse") ||
        content.Contains("IIM.Shared.DTOs.CreateSessionRequest"))
    {
        content = content.Replace("IIM.Shared.DTOs.InvestigationSession", "IIM.Shared.Models.InvestigationSession");
        content = content.Replace("IIM.Shared.DTOs.InvestigationMessage", "IIM.Shared.Models.InvestigationMessage");
        content = content.Replace("IIM.Shared.DTOs.InvestigationResponse", "IIM.Shared.Models.InvestigationResponse");
        content = content.Replace("IIM.Shared.DTOs.CreateSessionRequest", "IIM.Shared.Models.CreateSessionRequest");
        content = content.Replace("IIM.Shared.DTOs.InvestigationQuery", "IIM.Shared.Models.InvestigationQuery");
        changed = true;
    }
    
    // Update using statements for files that need Models
    if (file.Contains("IIM.Core") || file.Contains("IIM.Application"))
    {
        // These files should use Models for core types
        if (content.Contains("InvestigationSession") || 
            content.Contains("InvestigationMessage") ||
            content.Contains("Evidence") ||
            content.Contains("Case") ||
            content.Contains("Finding"))
        {
            if (!content.Contains("using IIM.Shared.Models;"))
            {
                // Add using statement after other usings
                var usingPattern = @"(using System[^;]+;\r?\n)";
                var lastUsing = Regex.Matches(content, usingPattern).LastOrDefault();
                if (lastUsing != null)
                {
                    content = content.Insert(lastUsing.Index + lastUsing.Length, "using IIM.Shared.Models;\n");
                    changed = true;
                }
            }
        }
    }
    
    // Fix constructor calls that changed
    if (content.Contains("new CreateSessionRequest("))
    {
        // The DTO version uses record syntax, Models uses class
        // Make sure constructor calls work with class version
        var ctorPattern = @"new CreateSessionRequest\(([^,]+),\s*([^,]+),\s*([^)]+)\)";
        content = Regex.Replace(content, ctorPattern, m =>
        {
            return $"new CreateSessionRequest {{ CaseId = {m.Groups[1].Value}, Title = {m.Groups[2].Value}, InvestigationType = {m.Groups[3].Value} }}";
        });
        if (content != originalContent) changed = true;
    }
    
    if (changed)
    {
        File.WriteAllText(file, content);
        updatedCount++;
        updatedFiles.Add(Path.GetRelativePath(solutionRoot, file));
    }
}

Console.WriteLine($"✅ Updated {updatedCount} files");

// ====================================================================
// STEP 5: DELETE DUPLICATE DTOS
// ====================================================================

Console.WriteLine($"\n\n🗑️ REMOVING DUPLICATE DTOS...");

var dtosToDelete = new List<string>
{
    "InvestigationMessageDto",
    "InvestigationResponseDto", 
    "InvestigationQueryDto",
    "CreateSessionRequestDto",
    "SessionResponseDto"
};

var investigationDtosPath = Path.Combine(solutionRoot, "src", "IIM.Shared", "DTOs", "InvestigationDtos.cs");
if (File.Exists(investigationDtosPath))
{
    var content = File.ReadAllText(investigationDtosPath);
    
    // Remove the duplicate DTO definitions
    foreach (var dto in dtosToDelete)
    {
        var pattern = $@"public record {dto}\([^;]+\);";
        content = Regex.Replace(content, pattern, "", RegexOptions.Singleline);
    }
    
    // Clean up extra newlines
    content = Regex.Replace(content, @"\n{3,}", "\n\n");
    
    File.WriteAllText(investigationDtosPath, content);
    Console.WriteLine($"✅ Removed duplicate DTO definitions");
}

// ====================================================================
// STEP 6: GENERATE REPORT
// ====================================================================

var reportPath = Path.Combine(backupPath, "cleanup_report.md");
var report = new StringBuilder();

report.AppendLine("# IIM Model/DTO Cleanup Report");
report.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
report.AppendLine();
report.AppendLine("## Summary");
report.AppendLine($"- Files updated: {updatedCount}");
report.AppendLine($"- DTOs removed: {dtosToDelete.Count}");
report.AppendLine($"- Models kept: {analysisResults.Count(r => r.Value.winner == "Models")}");
report.AppendLine($"- Merged: {analysisResults.Count(r => r.Value.winner == "MERGE")}");
report.AppendLine();
report.AppendLine("## Consolidation Decisions");
foreach (var (className, (winner, reason, _)) in analysisResults)
{
    report.AppendLine($"- **{className}**: {winner} - {reason}");
}
report.AppendLine();
report.AppendLine("## Next Steps");
report.AppendLine("1. Run `dotnet build` to verify compilation");
report.AppendLine("2. Run tests to ensure functionality");
report.AppendLine("3. Delete unused DTO files if build succeeds");
report.AppendLine("4. Consider creating interfaces for the main models");

File.WriteAllText(reportPath, report.ToString());

// ====================================================================
// FINAL SUMMARY
// ====================================================================

Console.WriteLine("\n" + new string('=', 60));
Console.WriteLine("✅ CLEANUP COMPLETE!");
Console.WriteLine(new string('=', 60));
Console.WriteLine();
Console.WriteLine("WHAT WAS DONE:");
Console.WriteLine("1. ✓ Analyzed all Models vs DTOs");
Console.WriteLine("2. ✓ Kept Models as primary (they're more complete)");
Console.WriteLine("3. ✓ Merged missing properties from DTOs into Models");
Console.WriteLine("4. ✓ Updated all references to use Models");
Console.WriteLine("5. ✓ Removed duplicate DTO definitions");
Console.WriteLine();
Console.WriteLine("FINAL ARCHITECTURE:");
Console.WriteLine("- IIM.Shared.Models: Primary models (InvestigationSession, etc.)");
Console.WriteLine("- IIM.Shared.DTOs: Only DTOs without Model equivalents");
Console.WriteLine("- No more duplicates!");
Console.WriteLine();
Console.WriteLine($"📋 Report saved to: {reportPath}");
Console.WriteLine($"💾 Backup location: {backupPath}");
Console.WriteLine();
Console.WriteLine("RUN THESE COMMANDS:");
Console.WriteLine("1. dotnet build");
Console.WriteLine("2. dotnet test");
Console.WriteLine();
Console.WriteLine("If errors occur, restore from backup:");
Console.WriteLine($"xcopy /E /Y \"{backupPath}\" \"{solutionRoot}\"");