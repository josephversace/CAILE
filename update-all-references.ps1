# update-all-references.ps1
# Updates all references to moved models

$replacements = @{
    # Infrastructure references
    "using IIM.Shared.Models;" = @"
using IIM.Core.Models;
using IIM.Infrastructure.Platform.Models;
using IIM.Infrastructure.Hardware;
using IIM.Core.Plugins.Models;
using IIM.Shared.Common;
"@
    
    # Specific type references
    "IIM.Shared.Models.ProcessResult" = "IIM.Infrastructure.Platform.Models.ProcessResult"
    "IIM.Shared.Models.GpuInfo" = "IIM.Infrastructure.Hardware.GpuInfo"
    "IIM.Shared.Models.EvidenceContext" = "IIM.Core.Models.EvidenceContext"
    "IIM.Shared.Models.FileMetadata" = "IIM.Core.Models.FileMetadata"
    "IIM.Shared.Models.PluginInfo" = "IIM.Core.Plugins.Models.PluginInfo"
    "IIM.Shared.Models.GeoLocation" = "IIM.Shared.Common.GeoLocation"
    "IIM.Shared.Models.TimeRange" = "IIM.Shared.Common.TimeRange"
    "IIM.Shared.Models.Common.GeoLocation" = "IIM.Shared.Common.GeoLocation"
    "IIM.Shared.Models.Common.TimeRange" = "IIM.Shared.Common.TimeRange"
}

Write-Host "Updating all C# file references..." -ForegroundColor Green

$files = Get-ChildItem -Path "src" -Filter "*.cs" -Recurse
$updatedCount = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    
    foreach ($oldRef in $replacements.Keys) {
        if ($content -match [regex]::Escape($oldRef)) {
            $content = $content -replace [regex]::Escape($oldRef), $replacements[$oldRef]
        }
    }
    
    if ($content -ne $originalContent) {
        $content | Out-File -FilePath $file.FullName -Encoding UTF8 -NoNewline
        Write-Host "✓ Updated: $($file.Name)" -ForegroundColor Green
        $updatedCount++
    }
}

Write-Host "Updated $updatedCount files" -ForegroundColor Cyan
