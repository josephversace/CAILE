# update-final-references.ps1

$replacements = @{
    # Export model references
    "using IIM.Shared.Models;" = "using IIM.Core.Services.Export.Models;"
    "IIM.Shared.Models.ExportConfiguration" = "IIM.Core.Services.Export.Models.ExportConfiguration"
    "IIM.Shared.Models.ExportTemplate" = "IIM.Core.Services.Export.Models.ExportTemplate"
    "IIM.Shared.Models.ExportOperation" = "IIM.Core.Services.Export.Models.ExportOperation"
    "IIM.Shared.Models.ExportStatus" = "IIM.Core.Services.Export.Models.ExportStatus"
    
    # Qdrant model references
    "IIM.Shared.Models.QdrantInfo" = "IIM.Infrastructure.VectorStore.Models.QdrantInfo"
    "IIM.Shared.Models.VectorConfig" = "IIM.Infrastructure.VectorStore.Models.VectorConfig"
    "IIM.Shared.Models.CollectionInfo" = "IIM.Infrastructure.VectorStore.Models.CollectionInfo"
    "IIM.Shared.Models.VectorPoint" = "IIM.Infrastructure.VectorStore.Models.VectorPoint"
    "IIM.Shared.Models.SearchResult" = "IIM.Infrastructure.VectorStore.Models.SearchResult"
    "IIM.Shared.Models.SearchFilter" = "IIM.Infrastructure.VectorStore.Models.SearchFilter"
    "IIM.Shared.Models.Cluster" = "IIM.Infrastructure.VectorStore.Models.Cluster"
    "IIM.Shared.Models.StorageInfo" = "IIM.Infrastructure.VectorStore.Models.StorageInfo"
    
    # QuickAction model references
    "IIM.Shared.Models.QuickAction" = "IIM.Components.Models.QuickAction"
    "IIM.Shared.Models.ActionTemplate" = "IIM.Components.Models.ActionTemplate"
    "IIM.Shared.Models.QuickActionResult" = "IIM.Components.Models.QuickActionResult"
    "IIM.Shared.Models.MessageActionRequest" = "IIM.Components.Models.MessageActionRequest"
}

Write-Host "Updating references in all C# files..." -ForegroundColor Green

$files = Get-ChildItem -Path "src" -Filter "*.cs" -Recurse
$razorFiles = Get-ChildItem -Path "src" -Filter "*.razor" -Recurse
$allFiles = $files + $razorFiles

$updatedCount = 0
foreach ($file in $allFiles) {
    $content = Get-Content $file.FullName -Raw
    $original = $content
    
    foreach ($old in $replacements.Keys) {
        if ($content -match [regex]::Escape($old)) {
            $content = $content -replace [regex]::Escape($old), $replacements[$old]
        }
    }
    
    if ($content -ne $original) {
        $content | Out-File -FilePath $file.FullName -Encoding UTF8 -NoNewline
        Write-Host "✓ Updated: $($file.Name)" -ForegroundColor Green
        $updatedCount++
    }
}

Write-Host "Updated $updatedCount files" -ForegroundColor Cyan
