# Analyze Remaining Duplicates in DTOs file
# Run this to see what duplicates are still present

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "    Analyzing Remaining Duplicates" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Set path
$dtosFile = "src\IIM.Shared\Dtos\dtos_all.cs"

if (-not (Test-Path $dtosFile)) {
    Write-Host "ERROR: Cannot find file at: $dtosFile" -ForegroundColor Red
    exit
}

Write-Host "`nReading file..." -ForegroundColor Yellow
$content = Get-Content $dtosFile -Raw
$lines = $content -split "`n"
Write-Host "File has $($lines.Count) lines" -ForegroundColor Gray

# Find all type definitions
$typeDefinitions = @{}
$lineNumber = 0

foreach ($line in $lines) {
    $lineNumber++
    
    # Match type definitions (record, class, interface, enum)
    if ($line -match '^\s*public\s+(record|class|interface|enum)\s+(\w+)') {
        $kind = $matches[1]
        $typeName = $matches[2]
        
        if (-not $typeDefinitions.ContainsKey($typeName)) {
            $typeDefinitions[$typeName] = @()
        }
        
        $typeDefinitions[$typeName] += [PSCustomObject]@{
            Line = $lineNumber
            Kind = $kind
            FullLine = $line.Trim()
        }
    }
}

# Find duplicates
$duplicates = @{}
foreach ($type in $typeDefinitions.GetEnumerator()) {
    if ($type.Value.Count -gt 1) {
        $duplicates[$type.Key] = $type.Value
    }
}

# Display results
Write-Host "`n========================================" -ForegroundColor Magenta
Write-Host "           DUPLICATE ANALYSIS" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta

Write-Host "`nSummary:" -ForegroundColor Cyan
Write-Host "  Total unique types: $($typeDefinitions.Count)" -ForegroundColor White
Write-Host "  Types with duplicates: $($duplicates.Count)" -ForegroundColor Yellow
Write-Host "  Total duplicate instances: $(($duplicates.Values | ForEach-Object { $_.Count - 1 } | Measure-Object -Sum).Sum)" -ForegroundColor Yellow

if ($duplicates.Count -gt 0) {
    Write-Host "`nTop Remaining Duplicates:" -ForegroundColor Cyan
    Write-Host "=========================" -ForegroundColor Cyan
    
    # Sort by number of duplicates
    $sorted = $duplicates.GetEnumerator() | Sort-Object { $_.Value.Count } -Descending
    
    $shown = 0
    foreach ($dup in $sorted) {
        if ($shown -ge 20) { break }
        $shown++
        
        Write-Host "`n$($dup.Key): $($dup.Value.Count) copies" -ForegroundColor Yellow
        foreach ($def in $dup.Value) {
            Write-Host "  Line $($def.Line): $($def.Kind)" -ForegroundColor Gray
            if ($dup.Value.Count -le 5) {
                Write-Host "    $($def.FullLine.Substring(0, [Math]::Min(80, $def.FullLine.Length)))..." -ForegroundColor DarkGray
            }
        }
        
        if ($dup.Value.Count -gt 1) {
            # Show which lines to remove (keep first, remove rest)
            $toRemove = $dup.Value | Select-Object -Skip 1 | ForEach-Object { $_.Line }
            Write-Host "  → Keep line $($dup.Value[0].Line), remove: $($toRemove -join ', ')" -ForegroundColor Green
        }
    }
    
    # Generate removal script
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "     GENERATING REMOVAL SCRIPT" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    
    $allLinesToRemove = @()
    foreach ($dup in $duplicates.GetEnumerator()) {
        # Keep first occurrence, remove the rest
        $toRemove = $dup.Value | Select-Object -Skip 1 | ForEach-Object { $_.Line }
        $allLinesToRemove += $toRemove
    }
    
    # Sort in descending order for removal
    $allLinesToRemove = $allLinesToRemove | Sort-Object -Descending -Unique
    
    # Create removal script
    $scriptContent = @"
# Auto-generated script to remove remaining duplicates
# Generated: $(Get-Date)

`$dtosFile = "src\IIM.Shared\Dtos\dtos_all.cs"
`$linesToRemove = @(
    $($allLinesToRemove -join ', ')
)

Write-Host "Removing `$(`$linesToRemove.Count) duplicate lines..."
`$content = Get-Content `$dtosFile
`$linesList = [System.Collections.ArrayList]::new(`$content)

foreach (`$lineNum in `$linesToRemove) {
    `$index = `$lineNum - 1
    if (`$index -ge 0 -and `$index -lt `$linesList.Count) {
        `$linesList.RemoveAt(`$index)
    }
}

Set-Content -Path `$dtosFile -Value `$linesList -Force
Write-Host "Removed `$(`$linesToRemove.Count) lines"
"@
    
    $scriptPath = "remove_remaining_duplicates.ps1"
    Set-Content -Path $scriptPath -Value $scriptContent -Force
    
    Write-Host "`nRemoval script generated: $scriptPath" -ForegroundColor Green
    Write-Host "This script will remove $($allLinesToRemove.Count) duplicate lines" -ForegroundColor Yellow
    Write-Host "`nTo run it: .\$scriptPath" -ForegroundColor Cyan
}
else {
    Write-Host "`nNo duplicates found! The file is clean." -ForegroundColor Green
}

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "         ANALYSIS COMPLETE" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green