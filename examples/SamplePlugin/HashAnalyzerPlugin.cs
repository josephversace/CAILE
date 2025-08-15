using IIM.Plugin.SDK;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text.Json;

namespace SamplePlugin;

/// <summary>
/// Example plugin that analyzes file hashes
/// </summary>
[PluginMetadata(
    Category = "forensics",
    Tags = new[] { "hash", "file", "analysis" },
    Author = "IIM Team",
    Version = "1.0.0"
)]
public class HashAnalyzerPlugin : InvestigationPlugin
{
    private PluginContext? _context;
    
    /// <summary>
    /// Unique identifier for this plugin
    /// </summary>
    public override string Id => "com.iim.example.hash-analyzer";
    
    /// <summary>
    /// Display name
    /// </summary>
    public override string Name => "Hash Analyzer";
    
    /// <summary>
    /// Description of functionality
    /// </summary>
    public override string Description => 
        "Analyzes file hashes and checks against known databases";
    
    /// <summary>
    /// Plugin capabilities
    /// </summary>
    public override PluginCapabilities Capabilities => new()
    {
        RequiresInternet = true,
        RequiresElevation = false,
        RequiredPermissions = new[] { "filesystem.read", "network.api" },
        SupportedIntents = new[] { "analyze_hash", "check_file_hash" },
        SupportedFileTypes = new[] { "*" }
    };
    
    /// <summary>
    /// Initialize the plugin
    /// </summary>
    public override async Task InitializeAsync(PluginContext context)
    {
        _context = context;
        await base.InitializeAsync(context);
    }
    
    /// <summary>
    /// Main execution method
    /// </summary>
    public override async Task<PluginResult> ExecuteAsync(
        PluginRequest request, 
        CancellationToken ct = default)
    {
        Logger.LogInformation("Executing hash analysis for intent: {Intent}", request.Intent);
        
        return request.Intent switch
        {
            "analyze_hash" => await AnalyzeHashAsync(request, ct),
            "check_file_hash" => await CheckFileHashAsync(request, ct),
            _ => PluginResult.CreateError($"Unknown intent: {request.Intent}")
        };
    }
    
    /// <summary>
    /// Analyze a provided hash value
    /// </summary>
    [IntentHandler("analyze_hash", 
        Description = "Analyzes a hash value against known databases",
        Example = "Check if hash abc123... is known malware")]
    private async Task<PluginResult> AnalyzeHashAsync(
        PluginRequest request, 
        CancellationToken ct)
    {
        if (!request.Parameters.TryGetValue("hash", out var hashObj) || 
            hashObj is not string hash)
        {
            return PluginResult.CreateError("Missing required parameter: hash");
        }
        
        Logger.LogInformation("Analyzing hash: {Hash}", hash);
        
        var results = new HashAnalysisResult
        {
            Hash = hash,
            Algorithm = DetectHashAlgorithm(hash),
            CheckedDatabases = new List<string>()
        };
        
        // Check against NSRL (known good)
        if (await CheckNSRLAsync(hash, ct))
        {
            results.IsKnownGood = true;
            results.CheckedDatabases.Add("NSRL");
        }
        
        // Check against threat intelligence (mock)
        var threatInfo = await CheckThreatIntelAsync(hash, ct);
        if (threatInfo != null)
        {
            results.IsKnownBad = true;
            results.ThreatInfo = threatInfo;
            results.CheckedDatabases.Add("ThreatIntel");
        }
        
        // Store in evidence
        await Evidence.StoreAsync($"hash_analysis_{hash}", results);
        
        return PluginResult.CreateSuccess(results, 
            "NSRL Database",
            "Internal Threat Intelligence");
    }
    
    /// <summary>
    /// Calculate and analyze hash of a file
    /// </summary>
    [IntentHandler("check_file_hash",
        Description = "Calculates file hash and checks against databases",
        Example = "Analyze the hash of evidence file IMG001.jpg")]
    private async Task<PluginResult> CheckFileHashAsync(
        PluginRequest request,
        CancellationToken ct)
    {
        if (!request.Parameters.TryGetValue("file", out var fileObj) || 
            fileObj is not string filePath)
        {
            return PluginResult.CreateError("Missing required parameter: file");
        }
        
        // Check if file exists and is accessible - FIX: Use _context.FileSystem
        if (!await _context!.FileSystem.FileExistsAsync(filePath))
        {
            return PluginResult.CreateError($"File not found: {filePath}");
        }
        
        // Calculate hash - FIX: Use _context.FileSystem
        var fileBytes = await _context.FileSystem.ReadFileAsync(filePath, ct);
        var hash = CalculateHash(fileBytes, "SHA256");
        
        Logger.LogInformation("Calculated hash for {File}: {Hash}", filePath, hash);
        
        // Now analyze the hash
        request.Parameters["hash"] = hash;
        var analysisResult = await AnalyzeHashAsync(request, ct);
        
        // Add file metadata - FIX: Use _context.FileSystem
        if (analysisResult.Success && analysisResult.Data is HashAnalysisResult result)
        {
            var metadata = await _context.FileSystem.GetFileMetadataAsync(filePath);
            result.FileInfo = new FileHashInfo
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                FileSize = metadata?.Size ?? 0,
                ModifiedDate = metadata?.ModifiedAt ?? DateTime.MinValue
            };
        }
        
        return analysisResult;
    }
    
    /// <summary>
    /// Detect hash algorithm based on length
    /// </summary>
    private string DetectHashAlgorithm(string hash)
    {
        return hash.Length switch
        {
            32 => "MD5",
            40 => "SHA1",
            64 => "SHA256",
            128 => "SHA512",
            _ => "Unknown"  // FIX: Added default case
        };
    }
    
    /// <summary>
    /// Calculate hash of byte array
    /// </summary>
    private string CalculateHash(byte[] data, string algorithm)
    {
        using HashAlgorithm hasher = algorithm switch
        {
            "MD5" => MD5.Create(),
            "SHA1" => SHA1.Create(),
            "SHA256" => SHA256.Create(),
            "SHA512" => SHA512.Create(),
            _ => SHA256.Create()
        };
        
        var hashBytes = hasher.ComputeHash(data);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
    
    /// <summary>
    /// Check hash against NSRL database (mock)
    /// </summary>
    private async Task<bool> CheckNSRLAsync(string hash, CancellationToken ct)
    {
        // In real implementation, would call NSRL API
        await Task.Delay(100, ct); // Simulate API call
        
        // Mock: randomly mark some hashes as known good
        return hash.GetHashCode() % 3 == 0;
    }
    
    /// <summary>
    /// Check hash against threat intelligence (mock)
    /// </summary>
    private async Task<ThreatInfo?> CheckThreatIntelAsync(string hash, CancellationToken ct)
    {
        // In real implementation, would call threat intel API
        await Task.Delay(100, ct); // Simulate API call
        
        // Mock: randomly mark some hashes as threats
        if (hash.GetHashCode() % 5 == 0)
        {
            return new ThreatInfo
            {
                ThreatName = "Trojan.Generic",
                Severity = "High",
                FirstSeen = DateTime.UtcNow.AddDays(-30),
                Description = "Generic trojan detected by behavioral analysis"
            };
        }
        
        return null;
    }
}

/// <summary>
/// Result of hash analysis
/// </summary>
public class HashAnalysisResult
{
    public required string Hash { get; set; }
    public string Algorithm { get; set; } = "Unknown";
    public bool IsKnownGood { get; set; }
    public bool IsKnownBad { get; set; }
    public ThreatInfo? ThreatInfo { get; set; }
    public FileHashInfo? FileInfo { get; set; }
    public List<string> CheckedDatabases { get; set; } = new();
}

/// <summary>
/// File information for hash
/// </summary>
public class FileHashInfo
{
    public required string FileName { get; set; }
    public required string FilePath { get; set; }
    public long FileSize { get; set; }
    public DateTime ModifiedDate { get; set; }
}

/// <summary>
/// Threat intelligence information
/// </summary>
public class ThreatInfo
{
    public required string ThreatName { get; set; }
    public required string Severity { get; set; }
    public DateTime FirstSeen { get; set; }
    public string? Description { get; set; }
}