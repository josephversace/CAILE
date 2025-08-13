{\rtf1\ansi\ansicpg1252\cocoartf2761
\cocoatextscaling0\cocoaplatform0{\fonttbl\f0\fswiss\fcharset0 Helvetica;}
{\colortbl;\red255\green255\blue255;}
{\*\expandedcolortbl;;}
\margl1440\margr1440\vieww11520\viewh8400\viewkind0
\pard\tx720\tx1440\tx2160\tx2880\tx3600\tx4320\tx5040\tx5760\tx6480\tx7200\tx7920\tx8640\pardirnatural\partightenfactor0

\f0\fs24 \cf0 # IIM Framework Desktop Deployment Script\
# Run as Administrator\
# Version: 1.0.0\
# This script prepares a Framework Desktop for the CAI-LE course\
\
param(\
    [string]$SourcePath = "D:\\IIM-Course-Files",\
    [string]$InstallPath = "C:\\IIM",\
    [switch]$SkipPrerequisites,\
    [switch]$SkipWSL,\
    [switch]$SkipModels,\
    [switch]$ValidateOnly\
)\
\
$ErrorActionPreference = "Stop"\
$ProgressPreference = "Continue"\
\
# Script configuration\
$script:Config = @\{\
    RequiredRAM = 120GB\
    RequiredDisk = 500GB\
    RequiredCores = 8\
    IIMVersion = "1.0.0"\
    CourseVersion = "2024.1"\
\}\
\
# Logging\
$script:LogPath = "$InstallPath\\Logs\\deployment_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"\
New-Item -ItemType Directory -Force -Path (Split-Path $LogPath) | Out-Null\
\
function Write-Log \{\
    param(\
        [string]$Message,\
        [string]$Level = "INFO"\
    )\
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"\
    $logMessage = "[$timestamp] [$Level] $Message"\
    Add-Content -Path $script:LogPath -Value $logMessage\
    \
    switch ($Level) \{\
        "ERROR" \{ Write-Host $Message -ForegroundColor Red \}\
        "WARNING" \{ Write-Host $Message -ForegroundColor Yellow \}\
        "SUCCESS" \{ Write-Host $Message -ForegroundColor Green \}\
        default \{ Write-Host $Message \}\
    \}\
\}\
\
function Test-Administrator \{\
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()\
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)\
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)\
\}\
\
function Test-SystemRequirements \{\
    Write-Log "Checking system requirements..." "INFO"\
    \
    $results = @\{\
        Passed = $true\
        Details = @()\
    \}\
    \
    # Check RAM\
    $ram = (Get-CimInstance Win32_PhysicalMemory | Measure-Object -Property Capacity -Sum).Sum\
    if ($ram -lt $script:Config.RequiredRAM) \{\
        $results.Passed = $false\
        $results.Details += "Insufficient RAM: $([math]::Round($ram/1GB, 2))GB found, $($script:Config.RequiredRAM/1GB)GB required"\
    \} else \{\
        $results.Details += "\uc0\u10003  RAM: $([math]::Round($ram/1GB, 2))GB"\
    \}\
    \
    # Check CPU cores\
    $cores = (Get-CimInstance Win32_Processor).NumberOfCores\
    if ($cores -lt $script:Config.RequiredCores) \{\
        $results.Passed = $false\
        $results.Details += "Insufficient CPU cores: $cores found, $($script:Config.RequiredCores) required"\
    \} else \{\
        $results.Details += "\uc0\u10003  CPU: $cores cores"\
    \}\
    \
    # Check disk space\
    $disk = Get-PSDrive C | Select-Object -ExpandProperty Free\
    if ($disk -lt $script:Config.RequiredDisk) \{\
        $results.Passed = $false\
        $results.Details += "Insufficient disk space: $([math]::Round($disk/1GB, 2))GB free, $($script:Config.RequiredDisk/1GB)GB required"\
    \} else \{\
        $results.Details += "\uc0\u10003  Disk: $([math]::Round($disk/1GB, 2))GB free"\
    \}\
    \
    # Check Windows version\
    $os = Get-CimInstance Win32_OperatingSystem\
    if ($os.BuildNumber -lt 19041) \{\
        $results.Passed = $false\
        $results.Details += "Windows version too old. Windows 10 20H1 or later required"\
    \} else \{\
        $results.Details += "\uc0\u10003  Windows: Build $($os.BuildNumber)"\
    \}\
    \
    return $results\
\}\
\
function Install-Prerequisites \{\
    Write-Log "Installing prerequisites..." "INFO"\
    \
    # Install .NET 8 Runtime and SDK\
    Write-Log "Installing .NET 8 SDK..."\
    $dotnetUrl = "https://download.visualstudio.microsoft.com/download/pr/6902745c-34bd-4d66-a5b0-c3e7a75e4cc5/d98a15a3b2c8e8d8c8a714c9cbf8c5e2/dotnet-sdk-8.0.100-win-x64.exe"\
    $dotnetInstaller = "$env:TEMP\\dotnet-sdk-8.exe"\
    \
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) \{\
        Invoke-WebRequest -Uri $dotnetUrl -OutFile $dotnetInstaller\
        Start-Process -FilePath $dotnetInstaller -ArgumentList "/quiet" -Wait\
        Remove-Item $dotnetInstaller\
        Write-Log "\uc0\u10003  .NET 8 SDK installed" "SUCCESS"\
    \} else \{\
        Write-Log "\uc0\u10003  .NET 8 SDK already installed" "SUCCESS"\
    \}\
    \
    # Install Visual C++ Redistributables\
    Write-Log "Installing Visual C++ Redistributables..."\
    $vcRedistUrl = "https://aka.ms/vs/17/release/vc_redist.x64.exe"\
    $vcRedistInstaller = "$env:TEMP\\vc_redist.x64.exe"\
    \
    Invoke-WebRequest -Uri $vcRedistUrl -OutFile $vcRedistInstaller\
    Start-Process -FilePath $vcRedistInstaller -ArgumentList "/quiet", "/norestart" -Wait\
    Remove-Item $vcRedistInstaller\
    Write-Log "\uc0\u10003  Visual C++ Redistributables installed" "SUCCESS"\
    \
    # Install Git\
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) \{\
        Write-Log "Installing Git..."\
        $gitUrl = "https://github.com/git-for-windows/git/releases/download/v2.43.0.windows.1/Git-2.43.0-64-bit.exe"\
        $gitInstaller = "$env:TEMP\\git-installer.exe"\
        \
        Invoke-WebRequest -Uri $gitUrl -OutFile $gitInstaller\
        Start-Process -FilePath $gitInstaller -ArgumentList "/VERYSILENT", "/NORESTART" -Wait\
        Remove-Item $gitInstaller\
        Write-Log "\uc0\u10003  Git installed" "SUCCESS"\
    \}\
    \
    # Enable Windows features\
    Write-Log "Enabling Windows features..."\
    \
    $features = @(\
        "Microsoft-Windows-Subsystem-Linux",\
        "VirtualMachinePlatform",\
        "Microsoft-Hyper-V-All"\
    )\
    \
    foreach ($feature in $features) \{\
        $state = Get-WindowsOptionalFeature -Online -FeatureName $feature\
        if ($state.State -ne "Enabled") \{\
            Enable-WindowsOptionalFeature -Online -FeatureName $feature -All -NoRestart | Out-Null\
            Write-Log "\uc0\u10003  Enabled $feature" "SUCCESS"\
        \}\
    \}\
\}\
\
function Install-WSL2 \{\
    Write-Log "Setting up WSL2..." "INFO"\
    \
    # Install WSL2\
    wsl --install --no-distribution\
    wsl --set-default-version 2\
    \
    # Install IIM Ubuntu distribution\
    $distroPath = "$SourcePath\\Software\\IIM-Ubuntu-22.04.tar.gz"\
    if (Test-Path $distroPath) \{\
        Write-Log "Installing IIM Ubuntu distribution..."\
        \
        $installDir = "$InstallPath\\WSL\\IIM-Ubuntu"\
        New-Item -ItemType Directory -Force -Path $installDir | Out-Null\
        \
        wsl --import IIM-Ubuntu $installDir $distroPath --version 2\
        wsl --set-default IIM-Ubuntu\
        \
        Write-Log "\uc0\u10003  IIM Ubuntu distribution installed" "SUCCESS"\
    \} else \{\
        Write-Log "Distribution file not found: $distroPath" "ERROR"\
        throw "WSL distribution installation failed"\
    \}\
    \
    # Start services\
    Write-Log "Starting WSL services..."\
    wsl -d IIM-Ubuntu -u root /opt/iim/scripts/startup.sh\
    \
    # Verify services\
    Start-Sleep -Seconds 10\
    $wslIp = wsl -d IIM-Ubuntu hostname -I | ForEach-Object \{ $_.Trim() \}\
    \
    $services = @\{\
        "Qdrant" = "http://$\{wslIp\}:6333"\
        "Embedding" = "http://$\{wslIp\}:8081/health"\
    \}\
    \
    foreach ($service in $services.GetEnumerator()) \{\
        try \{\
            $response = Invoke-WebRequest -Uri $service.Value -TimeoutSec 5\
            Write-Log "\uc0\u10003  $($service.Key) service is running" "SUCCESS"\
        \} catch \{\
            Write-Log "\uc0\u10007  $($service.Key) service failed to start" "WARNING"\
        \}\
    \}\
\}\
\
function Install-IIMApplication \{\
    Write-Log "Installing IIM application..." "INFO"\
    \
    # Create directory structure\
    $directories = @(\
        "$InstallPath\\Application",\
        "$InstallPath\\Models",\
        "$InstallPath\\Datasets",\
        "$InstallPath\\Evidence",\
        "$InstallPath\\Logs",\
        "$InstallPath\\Config"\
    )\
    \
    foreach ($dir in $directories) \{\
        New-Item -ItemType Directory -Force -Path $dir | Out-Null\
    \}\
    \
    # Copy application files\
    Write-Log "Copying application files..."\
    Copy-Item -Path "$SourcePath\\Software\\IIM\\*" -Destination "$InstallPath\\Application" -Recurse -Force\
    \
    # Create configuration\
    $config = @\{\
        InstallPath = $InstallPath\
        WSL = @\{\
            DistroName = "IIM-Ubuntu"\
            IP = $wslIp\
        \}\
        Services = @\{\
            API = "http://localhost:5080"\
            UI = "http://localhost:5000"\
        \}\
        Evidence = @\{\
            StorePath = "$InstallPath\\Evidence"\
            MaxFileSizeMb = 10240\
        \}\
    \}\
    \
    $config | ConvertTo-Json -Depth 10 | Set-Content "$InstallPath\\Config\\iim.config.json"\
    \
    # Create desktop shortcut\
    $WshShell = New-Object -comObject WScript.Shell\
    $Shortcut = $WshShell.CreateShortcut("$env:USERPROFILE\\Desktop\\IIM Investigation Platform.lnk")\
    $Shortcut.TargetPath = "$InstallPath\\Application\\IIM.App.exe"\
    $Shortcut.WorkingDirectory = "$InstallPath\\Application"\
    $Shortcut.IconLocation = "$InstallPath\\Application\\IIM.App.exe"\
    $Shortcut.Save()\
    \
    Write-Log "\uc0\u10003  IIM application installed" "SUCCESS"\
\}\
\
function Install-Models \{\
    Write-Log "Installing AI models..." "INFO"\
    \
    $models = @(\
        @\{\
            Name = "llama-2-13b-chat.Q4_K_M.gguf"\
            Size = "8GB"\
            Source = "$SourcePath\\Models\\llama-2-13b-chat.Q4_K_M.gguf"\
            Destination = "$InstallPath\\Models"\
        \},\
        @\{\
            Name = "whisper-large-v3.bin"\
            Size = "3GB"\
            Source = "$SourcePath\\Models\\whisper-large-v3.bin"\
            Destination = "$InstallPath\\Models"\
        \},\
        @\{\
            Name = "clip-vit-large-patch14.onnx"\
            Size = "2GB"\
            Source = "$SourcePath\\Models\\clip-vit-large-patch14.onnx"\
            Destination = "$InstallPath\\Models"\
        \}\
    )\
    \
    foreach ($model in $models) \{\
        Write-Log "Installing $($model.Name) ($($model.Size))..."\
        \
        if (Test-Path $model.Source) \{\
            Copy-Item -Path $model.Source -Destination $model.Destination -Force\
            Write-Log "\uc0\u10003  $($model.Name) installed" "SUCCESS"\
        \} else \{\
            Write-Log "Model file not found: $($model.Source)" "WARNING"\
        \}\
    \}\
\}\
\
function Install-SampleDatasets \{\
    Write-Log "Installing sample datasets..." "INFO"\
    \
    # Copy sample evidence files\
    if (Test-Path "$SourcePath\\Datasets") \{\
        Copy-Item -Path "$SourcePath\\Datasets\\*" -Destination "$InstallPath\\Datasets" -Recurse -Force\
        Write-Log "\uc0\u10003  Sample datasets installed" "SUCCESS"\
    \}\
    \
    # Create case folders\
    $cases = @("CASE-2024-001", "CASE-2024-002", "CASE-2024-003")\
    foreach ($case in $cases) \{\
        New-Item -ItemType Directory -Force -Path "$InstallPath\\Evidence\\$case" | Out-Null\
    \}\
\}\
\
function Test-Installation \{\
    Write-Log "Running installation tests..." "INFO"\
    \
    $tests = @()\
    \
    # Test 1: .NET Runtime\
    try \{\
        $dotnetVersion = dotnet --version\
        $tests += @\{\
            Name = ".NET SDK"\
            Passed = $true\
            Details = "Version: $dotnetVersion"\
        \}\
    \} catch \{\
        $tests += @\{\
            Name = ".NET SDK"\
            Passed = $false\
            Details = "Not installed or not in PATH"\
        \}\
    \}\
    \
    # Test 2: WSL2\
    try \{\
        $wslVersion = wsl --version\
        $tests += @\{\
            Name = "WSL2"\
            Passed = $true\
            Details = "Installed"\
        \}\
    \} catch \{\
        $tests += @\{\
            Name = "WSL2"\
            Passed = $false\
            Details = "Not installed"\
        \}\
    \}\
    \
    # Test 3: IIM Application\
    if (Test-Path "$InstallPath\\Application\\IIM.App.exe") \{\
        $tests += @\{\
            Name = "IIM Application"\
            Passed = $true\
            Details = "Installed at $InstallPath\\Application"\
        \}\
    \} else \{\
        $tests += @\{\
            Name = "IIM Application"\
            Passed = $false\
            Details = "Not found"\
        \}\
    \}\
    \
    # Test 4: Models\
    $modelCount = (Get-ChildItem "$InstallPath\\Models" -Filter "*.gguf", "*.bin", "*.onnx" -ErrorAction SilentlyContinue).Count\
    $tests += @\{\
        Name = "AI Models"\
        Passed = $modelCount -gt 0\
        Details = "$modelCount models found"\
    \}\
    \
    # Test 5: API connectivity\
    try \{\
        $response = Invoke-WebRequest -Uri "http://localhost:5080/healthz" -TimeoutSec 5\
        $tests += @\{\
            Name = "API Service"\
            Passed = $true\
            Details = "Running on port 5080"\
        \}\
    \} catch \{\
        $tests += @\{\
            Name = "API Service"\
            Passed = $false\
            Details = "Not responding"\
        \}\
    \}\
    \
    return $tests\
\}\
\
function New-ValidationReport \{\
    param($SystemCheck, $InstallTests)\
    \
    $report = @"\
IIM DEPLOYMENT VALIDATION REPORT\
=================================\
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")\
Machine: $env:COMPUTERNAME\
User: $env:USERNAME\
\
SYSTEM REQUIREMENTS\
-------------------\
$($SystemCheck.Details -join "`n")\
Status: $(if ($SystemCheck.Passed) \{ "PASSED" \} else \{ "FAILED" \})\
\
INSTALLATION TESTS\
------------------\
$(foreach ($test in $InstallTests) \{\
    $status = if ($test.Passed) \{ "\uc0\u10003 " \} else \{ "\u10007 " \}\
    "$status $($test.Name): $($test.Details)"\
\} | Out-String)\
\
NEXT STEPS\
----------\
$(if ($InstallTests | Where-Object \{ -not $_.Passed \}) \{\
    "1. Review failed components above\
2. Run deployment script again with -SkipPrerequisites flag\
3. Contact support if issues persist"\
\} else \{\
    "1. Launch IIM from desktop shortcut\
2. Log in with course credentials\
3. Complete Day 0 validation lab\
4. Begin Module 1"\
\})\
\
SUPPORT\
-------\
Email: support@iim-training.com\
Portal: https://portal.iim-training.com\
"@\
    \
    $reportPath = "$InstallPath\\deployment_report_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"\
    $report | Set-Content $reportPath\
    \
    Write-Log "Validation report saved to: $reportPath" "INFO"\
    return $reportPath\
\}\
\
# Main execution\
try \{\
    Clear-Host\
    Write-Host @"\
===================================\
IIM Framework Desktop Deployment\
Version: $($script:Config.IIMVersion)\
===================================\
"@ -ForegroundColor Cyan\
\
    # Check if running as admin\
    if (-not (Test-Administrator)) \{\
        Write-Log "This script must be run as Administrator" "ERROR"\
        Write-Log "Right-click and select 'Run as Administrator'" "INFO"\
        exit 1\
    \}\
    \
    # Check system requirements\
    Write-Host "`nPhase 1: System Requirements Check" -ForegroundColor Yellow\
    $systemCheck = Test-SystemRequirements\
    \
    foreach ($detail in $systemCheck.Details) \{\
        Write-Log $detail\
    \}\
    \
    if (-not $systemCheck.Passed) \{\
        Write-Log "System requirements not met. Deployment cannot continue." "ERROR"\
        exit 1\
    \}\
    \
    if ($ValidateOnly) \{\
        Write-Log "Validation only mode - skipping installation" "INFO"\
        exit 0\
    \}\
    \
    # Install prerequisites\
    if (-not $SkipPrerequisites) \{\
        Write-Host "`nPhase 2: Installing Prerequisites" -ForegroundColor Yellow\
        Install-Prerequisites\
    \}\
    \
    # Install WSL2\
    if (-not $SkipWSL) \{\
        Write-Host "`nPhase 3: Setting up WSL2" -ForegroundColor Yellow\
        Install-WSL2\
    \}\
    \
    # Install IIM application\
    Write-Host "`nPhase 4: Installing IIM Application" -ForegroundColor Yellow\
    Install-IIMApplication\
    \
    # Install models\
    if (-not $SkipModels) \{\
        Write-Host "`nPhase 5: Installing AI Models" -ForegroundColor Yellow\
        Install-Models\
    \}\
    \
    # Install sample datasets\
    Write-Host "`nPhase 6: Installing Sample Datasets" -ForegroundColor Yellow\
    Install-SampleDatasets\
    \
    # Run tests\
    Write-Host "`nPhase 7: Running Installation Tests" -ForegroundColor Yellow\
    $installTests = Test-Installation\
    \
    foreach ($test in $installTests) \{\
        $symbol = if ($test.Passed) \{ "\uc0\u10003 " \} else \{ "\u10007 " \}\
        $color = if ($test.Passed) \{ "Green" \} else \{ "Red" \}\
        Write-Host "$symbol $($test.Name): $($test.Details)" -ForegroundColor $color\
    \}\
    \
    # Generate report\
    Write-Host "`nPhase 8: Generating Report" -ForegroundColor Yellow\
    $reportPath = New-ValidationReport -SystemCheck $systemCheck -InstallTests $installTests\
    \
    # Final message\
    Write-Host "`n========================================" -ForegroundColor Green\
    Write-Host "DEPLOYMENT COMPLETE!" -ForegroundColor Green\
    Write-Host "========================================" -ForegroundColor Green\
    Write-Host ""\
    Write-Host "Installation Path: $InstallPath" -ForegroundColor Cyan\
    Write-Host "Report: $reportPath" -ForegroundColor Cyan\
    Write-Host ""\
    \
    if ($installTests | Where-Object \{ -not $_.Passed \}) \{\
        Write-Host "\uc0\u9888  Some components failed. Review the report for details." -ForegroundColor Yellow\
    \} else \{\
        Write-Host "\uc0\u10003  All components installed successfully!" -ForegroundColor Green\
        Write-Host ""\
        Write-Host "Next steps:" -ForegroundColor Cyan\
        Write-Host "1. Launch IIM from the desktop shortcut"\
        Write-Host "2. Complete the Day 0 validation lab"\
        Write-Host "3. Begin your AI investigation training!"\
    \}\
    \
    # Prompt for restart if needed\
    $pendingReboot = (Get-ItemProperty "HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager" -Name PendingFileRenameOperations -ErrorAction SilentlyContinue) -ne $null\
    if ($pendingReboot) \{\
        Write-Host "`n\uc0\u9888  A system restart is required to complete installation." -ForegroundColor Yellow\
        $restart = Read-Host "Restart now? (Y/N)"\
        if ($restart -eq "Y") \{\
            Restart-Computer -Force\
        \}\
    \}\
    \
\} catch \{\
    Write-Log "Deployment failed: $_" "ERROR"\
    Write-Log $_.ScriptStackTrace "ERROR"\
    exit 1\
\}}