# ============================================================================
# Fix-PluginSDK.ps1
# Fixes all missing dependencies in IIM.Plugin.SDK
# ============================================================================

Write-Host "Fixing IIM.Plugin.SDK Dependencies" -ForegroundColor Green
Write-Host "===================================" -ForegroundColor Green

# ============================================================================
# Step 1: Add missing NuGet packages to Plugin.SDK
# ============================================================================
Write-Host "`n[1] Adding missing packages to IIM.Plugin.SDK..." -ForegroundColor Yellow

Set-Location src\IIM.Plugin.SDK

# Add Configuration package for IConfiguration
Write-Host "  - Adding Microsoft.Extensions.Configuration.Abstractions..." -ForegroundColor Cyan
dotnet add package Microsoft.Extensions.Configuration.Abstractions

# Add HTTP client abstractions
Write-Host "  - Adding Microsoft.Extensions.Http..." -ForegroundColor Cyan
dotnet add package Microsoft.Extensions.Http

# Return to root
Set-Location ..\..

# ============================================================================
# Step 2: Check if these types are defined in IIM.Shared
# ============================================================================
Write-Host "`n[2] Checking IIM.Shared for missing types..." -ForegroundColor Yellow

$sharedTypes = @(
    "ISecureHttpClient",
    "ISecureProcessRunner", 
    "IEvidenceStore",
    "EvidenceContext",
    "ProcessResult",
    "FileMetadata",
    "PluginInfo"
)

$missingTypes = @()

foreach ($type in $sharedTypes) {
    $found = Get-ChildItem -Path "src\IIM.Shared" -Filter "*.cs" -Recurse | 
             Select-String -Pattern "class $type|interface $type|record $type|struct $type" -Quiet
    
    if ($found) {
        Write-Host "  ✓ Found $type in IIM.Shared" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Missing $type" -ForegroundColor Red
        $missingTypes += $type
    }
}

# ============================================================================
# Step 3: Create missing types
# ============================================================================
if ($missingTypes.Count -gt 0) {
    Write-Host "`n[3] Creating missing types..." -ForegroundColor Yellow
    
    # Create Models directory in IIM.Shared if it doesn't exist
    New-Item -ItemType Directory -Force -Path "src\IIM.Shared\Models" | Out-Null
    New-Item -ItemType Directory -Force -Path "src\IIM.Shared\Interfaces" | Out-Null
    
    # Create missing interfaces
    if ($missingTypes -contains "ISecureHttpClient") {
        @'
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// Secure HTTP client for plugin system
    /// </summary>
    public interface ISecureHttpClient
    {
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request);
        Task<string> GetStringAsync(string requestUri);
        Task<byte[]> GetByteArrayAsync(string requestUri);
    }
}
'@ | Out-File -FilePath "src\IIM.Shared\Interfaces\ISecureHttpClient.cs" -Encoding UTF8
        Write-Host "  Created ISecureHttpClient" -ForegroundColor Green
    }
    
    if ($missingTypes -contains "ISecureProcessRunner") {
        @'
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// Secure process runner for plugin system
    /// </summary>
    public interface ISecureProcessRunner
    {
        Task<ProcessResult> RunAsync(string fileName, string arguments);
    }
}
'@ | Out-File -FilePath "src\IIM.Shared\Interfaces\ISecureProcessRunner.cs" -Encoding UTF8
        Write-Host "  Created ISecureProcessRunner" -ForegroundColor Green
    }
    
    if ($missingTypes -contains "IEvidenceStore") {
        @'
using System;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// Evidence storage interface
    /// </summary>
    public interface IEvidenceStore
    {
        Task<string> StoreEvidenceAsync(byte[] data, string metadata);
        Task<byte[]> RetrieveEvidenceAsync(string evidenceId);
        Task<bool> VerifyIntegrityAsync(string evidenceId);
    }
}
'@ | Out-File -FilePath "src\IIM.Shared\Interfaces\IEvidenceStore.cs" -Encoding UTF8
        Write-Host "  Created IEvidenceStore" -ForegroundColor Green
    }
    
    # Create missing models
    if ($missingTypes -contains "EvidenceContext") {
        @'
using System;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Context for evidence operations
    /// </summary>
    public class EvidenceContext
    {
        public string CaseId { get; set; } = string.Empty;
        public string EvidenceId { get; set; } = string.Empty;
        public string CollectedBy { get; set; } = string.Empty;
        public DateTime CollectedAt { get; set; }
        public string ChainOfCustody { get; set; } = string.Empty;
    }
}
'@ | Out-File -FilePath "src\IIM.Shared\Models\EvidenceContext.cs" -Encoding UTF8
        Write-Host "  Created EvidenceContext" -ForegroundColor Green
    }
    
    if ($missingTypes -contains "ProcessResult") {
        @'
namespace IIM.Shared.Models
{
    /// <summary>
    /// Result from process execution
    /// </summary>
    public class ProcessResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
        public bool Success => ExitCode == 0;
    }
}
'@ | Out-File -FilePath "src\IIM.Shared\Models\ProcessResult.cs" -Encoding UTF8
        Write-Host "  Created ProcessResult" -ForegroundColor Green
    }
    
    if ($missingTypes -contains "FileMetadata") {
        @'
using System;

namespace IIM.Shared.Models
{
    /// <summary>
    /// File metadata for evidence files
    /// </summary>
    public class FileMetadata
    {
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Hash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public string MimeType { get; set; } = string.Empty;
    }
}
'@ | Out-File -FilePath "src\IIM.Shared\Models\FileMetadata.cs" -Encoding UTF8
        Write-Host "  Created FileMetadata" -ForegroundColor Green
    }
    
    if ($missingTypes -contains "PluginInfo") {
        @'
using System;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Plugin information
    /// </summary>
    public class PluginInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
    }
}
'@ | Out-File -FilePath "src\IIM.Shared\Models\PluginInfo.cs" -Encoding UTF8
        Write-Host "  Created PluginInfo" -ForegroundColor Green
    }
}

# ============================================================================
# Step 4: Add using statements to Plugin.SDK files
# ============================================================================
Write-Host "`n[4] Adding using statements to Plugin.SDK files..." -ForegroundColor Yellow

# Fix InvestigationPlugin.cs
$pluginFile = "src\IIM.Plugin.SDK\InvestigationPlugin.cs"
if (Test-Path $pluginFile) {
    $content = Get-Content $pluginFile -Raw
    
    # Add using statements if not present
    if ($content -notmatch "using IIM.Shared.Interfaces;") {
        $content = "using IIM.Shared.Interfaces;`nusing IIM.Shared.Models;`nusing Microsoft.Extensions.Configuration;`n" + $content
        $content | Out-File $pluginFile -Encoding UTF8
        Write-Host "  Updated InvestigationPlugin.cs" -ForegroundColor Green
    }
}

# Fix PluginContext.cs
$contextFile = "src\IIM.Plugin.SDK\PluginContext.cs"
if (Test-Path $contextFile) {
    $content = Get-Content $contextFile -Raw
    
    if ($content -notmatch "using IIM.Shared.Interfaces;") {
        $content = "using IIM.Shared.Interfaces;`nusing IIM.Shared.Models;`n" + $content
        $content | Out-File $contextFile -Encoding UTF8
        Write-Host "  Updated PluginContext.cs" -ForegroundColor Green
    }
}

# Fix PluginRequest.cs
$requestFile = "src\IIM.Plugin.SDK\PluginRequest.cs"
if (Test-Path $requestFile) {
    $content = Get-Content $requestFile -Raw
    
    if ($content -notmatch "using IIM.Shared.Models;") {
        $content = "using IIM.Shared.Models;`n" + $content
        $content | Out-File $requestFile -Encoding UTF8
        Write-Host "  Updated PluginRequest.cs" -ForegroundColor Green
    }
}

# Fix ISecureFileSystem.cs
$fsFile = "src\IIM.Plugin.SDK\Security\ISecureFileSystem.cs"
if (Test-Path $fsFile) {
    $content = Get-Content $fsFile -Raw
    
    if ($content -notmatch "using IIM.Shared.Models;") {
        $content = "using IIM.Shared.Models;`n" + $content
        $content | Out-File $fsFile -Encoding UTF8
        Write-Host "  Updated ISecureFileSystem.cs" -ForegroundColor Green
    }
}

# ============================================================================
# Step 5: Ensure Plugin.SDK references IIM.Shared
# ============================================================================
Write-Host "`n[5] Ensuring Plugin.SDK references IIM.Shared..." -ForegroundColor Yellow

# Check if reference exists
$projFile = "src\IIM.Plugin.SDK\IIM.Plugin.SDK.csproj"
$projContent = Get-Content $projFile -Raw

if ($projContent -notmatch "IIM.Shared.csproj") {
    Write-Host "  Adding reference to IIM.Shared..." -ForegroundColor Cyan
    dotnet add src\IIM.Plugin.SDK reference src\IIM.Shared
} else {
    Write-Host "  Reference to IIM.Shared already exists" -ForegroundColor Green
}

# ============================================================================
# Step 6: Build everything
# ============================================================================
Write-Host "`n[6] Building the solution..." -ForegroundColor Yellow

# Build IIM.Shared first
Write-Host "  Building IIM.Shared..." -ForegroundColor Cyan
dotnet build src\IIM.Shared

if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✓ IIM.Shared built successfully" -ForegroundColor Green
    
    # Build Plugin.SDK
    Write-Host "  Building IIM.Plugin.SDK..." -ForegroundColor Cyan
    dotnet build src\IIM.Plugin.SDK
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ IIM.Plugin.SDK built successfully" -ForegroundColor Green
        
        # Build Core
        Write-Host "  Building IIM.Core..." -ForegroundColor Cyan
        dotnet build src\IIM.Core
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ IIM.Core built successfully" -ForegroundColor Green
            
            # Finally build the app
            Write-Host "  Building IIM.App.Hybrid..." -ForegroundColor Cyan
            dotnet build src\IIM.App.Hybrid /p:TreatWarningsAsErrors=false
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "`n✅ All projects built successfully!" -ForegroundColor Green
                Write-Host "`nRunning the application..." -ForegroundColor Cyan
                dotnet run --project src\IIM.App.Hybrid
            }
        }
    }
} else {
    Write-Host "`n❌ Build failed. Checking errors..." -ForegroundColor Red
    dotnet build 2>&1 | Select-String "error CS"
}
