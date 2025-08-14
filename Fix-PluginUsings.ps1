# ============================================================================
# Fix-PluginUsings.ps1
# Adds missing using statements to all Plugin.SDK files
# ============================================================================

Write-Host "Fixing Plugin.SDK Using Statements" -ForegroundColor Green
Write-Host "===================================" -ForegroundColor Green

# Function to add using statements to a file
function Add-UsingStatements {
    param(
        [string]$FilePath,
        [string[]]$Usings
    )
    
    if (Test-Path $FilePath) {
        $content = Get-Content $FilePath -Raw
        
        # Check if file already has using statements
        $hasUsings = $content -match "^using\s"
        
        # Create using block
        $usingBlock = ($Usings | ForEach-Object { "using $_;" }) -join "`n"
        
        if (-not $hasUsings) {
            # Add usings at the beginning
            $content = "$usingBlock`n`n$content"
        } else {
            # Check which usings are missing and add them
            foreach ($using in $Usings) {
                if ($content -notmatch "using\s+$using\s*;") {
                    # Add after the last using statement
                    $content = $content -replace "(using\s+[^;]+;)([^u])", "`$1`nusing $using;`$2"
                }
            }
        }
        
        $content | Out-File $FilePath -Encoding UTF8
        Write-Host "  Fixed: $(Split-Path $FilePath -Leaf)" -ForegroundColor Gray
    }
}

# ============================================================================
# Fix each file in Plugin.SDK
# ============================================================================

Write-Host "`nFixing Attribute files..." -ForegroundColor Yellow

# Fix IntentHandlerAttribute.cs
Add-UsingStatements -FilePath "src\IIM.Plugin.SDK\Attributes\IntentHandlerAttribute.cs" -Usings @(
    "System",
    "System.Collections.Generic",
    "System.Threading.Tasks"
)

# Fix PluginMetadataAttribute.cs
Add-UsingStatements -FilePath "src\IIM.Plugin.SDK\Attributes\PluginMetadataAttribute.cs" -Usings @(
    "System",
    "System.Collections.Generic"
)

Write-Host "`nFixing Core files..." -ForegroundColor Yellow

# Fix IInvestigationPlugin.cs
Add-UsingStatements -FilePath "src\IIM.Plugin.SDK\IInvestigationPlugin.cs" -Usings @(
    "System",
    "System.Threading",
    "System.Threading.Tasks"
)

# Fix InvestigationPlugin.cs
Add-UsingStatements -FilePath "src\IIM.Plugin.SDK\InvestigationPlugin.cs" -Usings @(
    "System",
    "System.Net.Http",
    "System.Threading",
    "System.Threading.Tasks",
    "Microsoft.Extensions.Configuration",
    "IIM.Shared.Interfaces",
    "IIM.Shared.Models"
)

# Fix PluginCapabilities.cs
Add-UsingStatements -FilePath "src\IIM.Plugin.SDK\PluginCapabilities.cs" -Usings @(
    "System",
    "System.Collections.Generic"
)

# Fix PluginContext.cs
Add-UsingStatements -FilePath "src\IIM.Plugin.SDK\PluginContext.cs" -Usings @(
    "System",
    "IIM.Shared.Interfaces",
    "IIM.Shared.Models"
)

# Fix PluginRequest.cs
Add-UsingStatements -FilePath "src\IIM.Plugin.SDK\PluginRequest.cs" -Usings @(
    "System",
    "System.Collections.Generic",
    "IIM.Shared.Models"
)

# Fix PluginResult.cs
Add-UsingStatements -FilePath "src\IIM.Plugin.SDK\PluginResult.cs" -Usings @(
    "System",
    "System.Collections.Generic"
)

Write-Host "`nFixing Security files..." -ForegroundColor Yellow

# Fix ISecureFileSystem.cs
Add-UsingStatements -FilePath "src\IIM.Plugin.SDK\Security\ISecureFileSystem.cs" -Usings @(
    "System",
    "System.Threading",
    "System.Threading.Tasks",
    "IIM.Shared.Models"
)

# ============================================================================
# Enable nullable reference types in project
# ============================================================================
Write-Host "`nUpdating Plugin.SDK project file..." -ForegroundColor Yellow

$projFile = "src\IIM.Plugin.SDK\IIM.Plugin.SDK.csproj"
$projContent = Get-Content $projFile -Raw

# Add Nullable enable to PropertyGroup if not present
if ($projContent -notmatch "<Nullable>") {
    $projContent = $projContent -replace "(<LangVersion>.*</LangVersion>)", "`$1`n    <Nullable>enable</Nullable>"
    $projContent | Out-File $projFile -Encoding UTF8
    Write-Host "  Added <Nullable>enable</Nullable>" -ForegroundColor Gray
}

# ============================================================================
# Build the fixed project
# ============================================================================
Write-Host "`n=================================" -ForegroundColor Green
Write-Host "Building Plugin.SDK..." -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green

dotnet build src\IIM.Plugin.SDK

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Plugin.SDK fixed and built successfully!" -ForegroundColor Green
    
    Write-Host "`nBuilding IIM.Core..." -ForegroundColor Yellow
    dotnet build src\IIM.Core
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ IIM.Core built successfully!" -ForegroundColor Green
        
        Write-Host "`nBuilding IIM.App.Hybrid..." -ForegroundColor Yellow
        dotnet build src\IIM.App.Hybrid
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "`n✅ All projects built successfully!" -ForegroundColor Green
            Write-Host "`nRunning the application..." -ForegroundColor Cyan
            dotnet run --project src\IIM.App.Hybrid
        }
    }
} else {
    Write-Host "`n❌ Still have errors. Checking what's missing..." -ForegroundColor Red
    
    # Show remaining errors
    dotnet build src\IIM.Plugin.SDK 2>&1 | Select-String "error CS" | Select-Object -First 5
    
    Write-Host "`nTry running the script again, or manually add these usings to the files with errors:" -ForegroundColor Yellow
    Write-Host "  using System;" -ForegroundColor White
    Write-Host "  using System.Collections.Generic;" -ForegroundColor White
    Write-Host "  using System.Threading;" -ForegroundColor White
    Write-Host "  using System.Threading.Tasks;" -ForegroundColor White
    Write-Host "  using IIM.Shared.Interfaces;" -ForegroundColor White
    Write-Host "  using IIM.Shared.Models;" -ForegroundColor White
}
