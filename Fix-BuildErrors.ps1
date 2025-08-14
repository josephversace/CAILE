# ============================================================================
# Fix-BuildErrors.ps1
# Fixes the IIM.Core build errors (missing references and duplicates)
# ============================================================================

Write-Host "Fixing IIM.Core Build Errors" -ForegroundColor Green
Write-Host "============================" -ForegroundColor Green

# ============================================================================
# Fix 1: Add missing project reference
# ============================================================================
Write-Host "`n[1] Adding missing IIM.Plugin.SDK reference to IIM.Core..." -ForegroundColor Yellow

dotnet add src\IIM.Core reference src\IIM.Plugin.SDK

# ============================================================================
# Fix 2: Remove duplicate model definitions
# ============================================================================
Write-Host "`n[2] Checking for duplicate model definitions..." -ForegroundColor Yellow

# Check if the mock models we added are duplicating existing ones
$inferenceModelsPath = "src\IIM.Core\Models\InferenceModels.cs"
$existingModelsPath = "src\IIM.Core\Models\Transcription.cs"

if (Test-Path $inferenceModelsPath) {
    Write-Host "Found InferenceModels.cs - checking for duplicates..." -ForegroundColor Cyan
    
    # Check if TranscriptionResult already exists elsewhere
    $duplicateFiles = Get-ChildItem "src\IIM.Core\Models" -Filter "*.cs" | 
        Where-Object { $_.Name -ne "InferenceModels.cs" } |
        Where-Object { 
            $content = Get-Content $_.FullName -Raw
            $content -match "class TranscriptionResult" -or $content -match "class BoundingBox"
        }
    
    if ($duplicateFiles) {
        Write-Host "Found duplicate definitions in:" -ForegroundColor Yellow
        $duplicateFiles | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Gray }
        
        Write-Host "`nRenaming our mock models to avoid conflicts..." -ForegroundColor Cyan
        
        # Read the file
        $content = Get-Content $inferenceModelsPath -Raw
        
        # Rename the conflicting classes
        $content = $content -replace "class TranscriptionResult", "class MockTranscriptionResult"
        $content = $content -replace "class BoundingBox", "class MockBoundingBox"
        $content = $content -replace "TranscriptionResult ", "MockTranscriptionResult "
        $content = $content -replace "BoundingBox\?", "MockBoundingBox?"
        
        # Save the updated file
        $content | Out-File $inferenceModelsPath -Encoding UTF8
        
        Write-Host "✅ Renamed conflicting classes to Mock* versions" -ForegroundColor Green
        
        # Also update the interface if it exists
        $interfacePath = "src\IIM.Core\Interfaces\IInferenceService.cs"
        if (Test-Path $interfacePath) {
            $interfaceContent = Get-Content $interfacePath -Raw
            $interfaceContent = $interfaceContent -replace "TranscriptionResult", "MockTranscriptionResult"
            $interfaceContent | Out-File $interfacePath -Encoding UTF8
            Write-Host "✅ Updated interface to use Mock* types" -ForegroundColor Green
        }
        
        # Update MockInferenceService if it exists
        $mockServicePath = "src\IIM.Core\Services\Mocks\MockInferenceService.cs"
        if (Test-Path $mockServicePath) {
            $mockContent = Get-Content $mockServicePath -Raw
            $mockContent = $mockContent -replace "TranscriptionResult", "MockTranscriptionResult"
            $mockContent = $mockContent -replace "BoundingBox", "MockBoundingBox"
            $mockContent | Out-File $mockServicePath -Encoding UTF8
            Write-Host "✅ Updated MockInferenceService to use Mock* types" -ForegroundColor Green
        }
    }
}

# ============================================================================
# Fix 3: Alternative - Remove the mock files if they're causing issues
# ============================================================================
Write-Host "`n[3] Alternative fix options:" -ForegroundColor Yellow
Write-Host "If the models are still conflicting, choose an option:" -ForegroundColor Cyan
Write-Host "1. Remove the mock InferenceModels.cs (use existing models)" -ForegroundColor White
Write-Host "2. Keep mock models with Mock* prefix (already applied above)" -ForegroundColor White
Write-Host "3. Skip and try building anyway" -ForegroundColor White
Write-Host "Enter choice (1/2/3): " -ForegroundColor Cyan -NoNewline
$choice = Read-Host

switch ($choice) {
    "1" {
        # Remove the conflicting file
        if (Test-Path $inferenceModelsPath) {
            Remove-Item $inferenceModelsPath -Force
            Write-Host "✅ Removed InferenceModels.cs" -ForegroundColor Green
        }
    }
    "2" {
        Write-Host "Mock models already renamed with Mock* prefix" -ForegroundColor Green
    }
    "3" {
        Write-Host "Skipping..." -ForegroundColor Gray
    }
}

# ============================================================================
# Fix 4: Add missing using statements
# ============================================================================
Write-Host "`n[4] Adding missing dependencies to IIM.Core..." -ForegroundColor Yellow

Set-Location src\IIM.Core
dotnet add package Microsoft.Extensions.Configuration.Abstractions
dotnet add package Microsoft.Extensions.Logging.Abstractions
Set-Location ..\..

# ============================================================================
# Build again
# ============================================================================
Write-Host "`n[5] Attempting to build IIM.Core..." -ForegroundColor Yellow
dotnet build src\IIM.Core

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ IIM.Core build successful!" -ForegroundColor Green
    
    Write-Host "`n[6] Building IIM.App.Hybrid..." -ForegroundColor Yellow
    dotnet build src\IIM.App.Hybrid /p:TreatWarningsAsErrors=false
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✅ Build successful! Running the app..." -ForegroundColor Green
        dotnet run --project src\IIM.App.Hybrid
    }
} else {
    Write-Host "`n❌ Still have build errors" -ForegroundColor Red
    Write-Host "`nShowing current errors:" -ForegroundColor Yellow
    dotnet build src\IIM.Core 2>&1 | Select-String "error CS"
    
    Write-Host "`n=================================" -ForegroundColor Yellow
    Write-Host "Quick fix - bypass IIM.Core issues:" -ForegroundColor Yellow
    Write-Host "=================================" -ForegroundColor Yellow
    Write-Host @"
    
Since the mock services are causing conflicts, let's just run without them:

1. Remove the IIM.Core project reference temporarily:
   dotnet remove src\IIM.App.Hybrid reference src\IIM.Core
   
2. Build and run just the Hybrid app:
   dotnet build src\IIM.App.Hybrid /p:TreatWarningsAsErrors=false
   dotnet run --project src\IIM.App.Hybrid
   
This will let you test the UI even without the mock services.
"@ -ForegroundColor White
}