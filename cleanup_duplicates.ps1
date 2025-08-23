# IIM Duplicate Cleanup Script - ROBUST VERSION
# Handles file locking and processes all changes at once

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "    IIM Duplicate Cleanup Script v2" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Check if Visual Studio is locking the file
Write-Host "`nChecking for file locks..." -ForegroundColor Yellow
$processes = Get-Process devenv -ErrorAction SilentlyContinue
if ($processes) {
    Write-Host "WARNING: Visual Studio is running. Please:" -ForegroundColor Red
    Write-Host "  1. Close all tabs with dtos_all.cs open" -ForegroundColor Yellow
    Write-Host "  2. Or close Visual Studio completely" -ForegroundColor Yellow
    $continue = Read-Host "`nContinue anyway? (yes/no)"
    if ($continue -ne "yes") {
        exit
    }
}

# Set paths
$currentDir = Get-Location
$dtosFile = "src\IIM.Shared\Dtos\dtos_all.cs"
$modelsFile = "src\IIM.Shared\Models\models_all.cs"

# Verify files exist
if (-not (Test-Path $dtosFile)) {
    Write-Host "ERROR: Cannot find DTOs file at: $dtosFile" -ForegroundColor Red
    exit
}

Write-Host "Files found successfully!" -ForegroundColor Green

# Create timestamped backups
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$dtosBackup = "$dtosFile.backup_$timestamp"

Write-Host "`nCreating backup..." -ForegroundColor Yellow
Copy-Item $dtosFile $dtosBackup -Force
Write-Host "Backup created: $dtosBackup" -ForegroundColor Green

# Read the entire file once
Write-Host "`nReading file..." -ForegroundColor Yellow
$allLines = Get-Content $dtosFile -ErrorAction Stop
$originalCount = $allLines.Count
Write-Host "File has $originalCount lines" -ForegroundColor Gray

# Define ALL duplicates to remove (sorted in descending order)
$allDuplicateLines = @(
    # ExportResult - 21 duplicates
    2390, 2378, 2359, 2332, 2293, 2244, 2183, 2113, 2038, 1955, 1867, 1771, 1666, 1553, 1432, 1304, 1169, 1025, 872, 709, 538,
    # BatchExportRequest - 20 duplicates
    2371, 2352, 2325, 2286, 2237, 2176, 2106, 2031, 1948, 1860, 1764, 1659, 1546, 1425, 1297, 1162, 1018, 865, 702, 531,
    # ExportResponseRequest - 19 duplicates
    2344, 2317, 2278, 2229, 2168, 2098, 2023, 1940, 1852, 1756, 1651, 1538, 1417, 1289, 1154, 1010, 857, 694, 523,
    # ExportOptions - 18 duplicates
    2305, 2266, 2217, 2156, 2086, 2011, 1928, 1840, 1744, 1639, 1526, 1405, 1277, 1142, 998, 845, 682, 511,
    # AuditLogResponse - 17 duplicates
    2256, 2207, 2146, 2076, 2001, 1918, 1830, 1734, 1629, 1516, 1395, 1267, 1132, 988, 835, 672, 501,
    # AuditLogEntry - 16 duplicates
    2195, 2134, 2064, 1989, 1906, 1818, 1722, 1617, 1504, 1383, 1255, 1120, 976, 823, 660, 489,
    # ProcessingResponse - 15 duplicates
    2125, 2055, 1980, 1897, 1809, 1713, 1608, 1495, 1374, 1246, 1111, 967, 814, 651, 480,
    # ProcessingRequest - 14 duplicates
    2050, 1975, 1892, 1804, 1708, 1603, 1490, 1369, 1241, 1106, 962, 809, 646, 475,
    # FileSyncResponse - 13 duplicates
    1967, 1884, 1796, 1700, 1595, 1482, 1361, 1233, 1098, 954, 801, 638, 467,
    # FileSyncRequest - 12 duplicates
    1879, 1791, 1695, 1590, 1477, 1356, 1228, 1093, 949, 796, 633, 462,
    # Node - 13 duplicates (high line numbers)
    8587, 8569, 8545, 8513, 8472, 8422, 8359, 8288, 8208, 8121, 8021, 7913, 7797,
    # Edge - 14 duplicates (high line numbers)
    8605, 8596, 8578, 8554, 8522, 8481, 8431, 8368, 8297, 8217, 8130, 8030, 7922, 7806,
    # NetworkGraph - 12 duplicates
    8563, 8539, 8507, 8466, 8416, 8353, 8282, 8202, 8115, 8015, 7907, 7791,
    # CriticalPeriod - 12 duplicates
    8531, 8499, 8458, 8408, 8345, 8274, 8194, 8107, 8007, 7899, 7783,
    # Visualization - 16 duplicates
    5660, 5649, 5628, 5595, 5556, 5510, 5456, 5396, 5326, 5248, 5157, 5054, 4933, 4802, 4664, 4519,
    # Citation - 15 duplicates
    5639, 5618, 5585, 5546, 5500, 5446, 5386, 5316, 5238, 5147, 5044, 4923, 4792, 4654, 4509,
    # ToolResult - 14 duplicates
    5606, 5573, 5534, 5488, 5434, 5374, 5304, 5226, 5135, 5032, 4911, 4780, 4642, 4497,
    # SimilarImage - 13 duplicates
    5567, 5528, 5482, 5428, 5368, 5298, 5220, 5129, 5026, 4905, 4774, 4636, 4491,
    # BoundingBox - 12 duplicates
    5521, 5475, 5421, 5361, 5291, 5213, 5122, 5019, 4898, 4767, 4629, 4484,
    # DetectedFace - 11 duplicates
    5467, 5413, 5353, 5283, 5205, 5114, 5011, 4890, 4759, 4621, 4476
) | Sort-Object -Descending -Unique

Write-Host "`nPreparing to remove $($allDuplicateLines.Count) duplicate lines..." -ForegroundColor Yellow

# Convert to ArrayList for efficient removal
$linesList = [System.Collections.ArrayList]::new($allLines)

# Remove all duplicate lines in one pass (from bottom to top)
$removed = 0
foreach ($lineNum in $allDuplicateLines) {
    $index = $lineNum - 1
    if ($index -ge 0 -and $index -lt $linesList.Count) {
        try {
            $linesList.RemoveAt($index)
            $removed++
            if ($removed % 50 -eq 0) {
                Write-Host "  Removed $removed lines..." -ForegroundColor Gray
            }
        }
        catch {
            Write-Host "  Warning: Could not remove line $lineNum" -ForegroundColor Yellow
        }
    }
}

Write-Host "`nRemoved $removed duplicate lines total" -ForegroundColor Green

# Write the cleaned content back in one operation
Write-Host "`nWriting cleaned file..." -ForegroundColor Yellow
$retryCount = 0
$maxRetries = 3
$success = $false

while (-not $success -and $retryCount -lt $maxRetries) {
    try {
        # Try to write with retry logic
        [System.IO.File]::WriteAllLines($dtosFile, $linesList, [System.Text.Encoding]::UTF8)
        $success = $true
        Write-Host "File saved successfully!" -ForegroundColor Green
    }
    catch {
        $retryCount++
        if ($retryCount -lt $maxRetries) {
            Write-Host "  File is locked, retrying in 2 seconds... (attempt $retryCount/$maxRetries)" -ForegroundColor Yellow
            Start-Sleep -Seconds 2
        }
        else {
            Write-Host "ERROR: Could not save file after $maxRetries attempts" -ForegroundColor Red
            Write-Host "Error: $_" -ForegroundColor Red
            
            # Save to alternative location
            $alternativeFile = "$dtosFile.cleaned"
            [System.IO.File]::WriteAllLines($alternativeFile, $linesList, [System.Text.Encoding]::UTF8)
            Write-Host "`nSaved cleaned version to: $alternativeFile" -ForegroundColor Yellow
            Write-Host "Manually replace the original file with this one." -ForegroundColor Yellow
        }
    }
}

# Summary
$finalCount = $linesList.Count
$reduction = $originalCount - $finalCount

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "         CLEANUP COMPLETE!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "`nResults:" -ForegroundColor Cyan
Write-Host "  Original lines: $originalCount" -ForegroundColor White
Write-Host "  Final lines: $finalCount" -ForegroundColor White
Write-Host "  Lines removed: $reduction" -ForegroundColor White
Write-Host "  Reduction: $([math]::Round(($reduction / $originalCount) * 100, 1))%" -ForegroundColor White

Write-Host "`nBackup saved as:" -ForegroundColor Yellow
Write-Host "  $dtosBackup" -ForegroundColor Gray

Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "  1. Rebuild the solution (Ctrl+Shift+B)" -ForegroundColor White
Write-Host "  2. Fix any compilation errors" -ForegroundColor White
Write-Host "  3. Run tests" -ForegroundColor White

Write-Host "`nTo restore from backup if needed:" -ForegroundColor Yellow
Write-Host "  Copy-Item '$dtosBackup' '$dtosFile' -Force" -ForegroundColor Gray

Write-Host "`nScript completed!" -ForegroundColor Green